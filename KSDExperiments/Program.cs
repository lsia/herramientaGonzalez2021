using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Configuration;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;

using NLog;

using KSDExperiments.Configuration;
using KSDExperiments.Datasets;
using KSDExperiments.Datasets.Readers;
using KSDExperiments.Datasets.Summarizers;
using KSDExperiments.Experiments;
using KSDExperiments.FiniteContexts.Models;
using KSDExperiments.Latex;
using KSDExperiments.Pipelines;
using KSDExperiments.Util;


namespace KSDExperiments
{
    class Program
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        private static void ResetConfigMechanism()
        {
            BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
            typeof(ConfigurationManager)
                .GetField("s_initState", Flags)
                .SetValue(null, 0);

            typeof(ConfigurationManager)
                .GetField("s_configSystem", Flags)
                .SetValue(null, null);

            typeof(ConfigurationManager)
                .Assembly.GetTypes()
                .Where(x => x.FullName == "System.Configuration.ClientConfigPaths")
                .First()
                .GetField("s_current", Flags)
                .SetValue(null, null);
            return;
        }

        void FindConfigurationFile(string experiment_name)
        {
            string filename = experiment_name + ".config";

            var attr = ExperimentUtil.CurrentExperimentType.GetCustomAttribute(typeof(ConfigurationFileAttribute)) as ConfigurationFileAttribute;
            if (attr != null)
                filename = attr.Name;
            
            string[] files = Directory.GetFiles(Environment.CurrentDirectory,
                filename, SearchOption.AllDirectories);

            if (files.Length <= 0)
            {
                log.Error("Experiment configuration file {0} not found.", filename);
                Environment.Exit(-1);
            }
            else if (files.Length > 1)
            {
                log.Error("AMBIGUOUS CONFIGURATION FILE:");
                foreach (var file in files)
                    log.Error("  {0}", file);

                Environment.Exit(-1);
            }

            log.Info("CONFIGURATION {0}", files[0]);
            AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", files[0]);
            ResetConfigMechanism();
        }

        void DeleteOldFiles()
        {
            foreach (var file in Directory.GetFiles(".", "*.log"))
                File.Delete(file);

            foreach (var file in Directory.GetFiles(".", "*.err"))
                File.Delete(file);

            foreach (var file in Directory.GetFiles(".", "*.arff"))
                File.Delete(file);

            foreach (var file in Directory.GetFiles(".", "*.csv"))
                File.Delete(file);

            foreach (var file in Directory.GetFiles(".", "*.model"))
                File.Delete(file);

            foreach (var file in Directory.GetFiles(".", "*.sessions"))
                File.Delete(file);
        }

        void SaveLogFiles()
        {
            string folder = ExperimentUtil.OutputFolder + "logs/";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            foreach (var file in Directory.GetFiles(".", "*.log"))
                File.Copy(file, folder + Path.GetFileName(file), true);

            foreach (var file in Directory.GetFiles(".", "*.err"))
                File.Copy(file, folder + Path.GetFileName(file), true);
        }

        void Run(string[] args)
        {
            string[] ARGS = args;
            if (ARGS.Length < 1)
            {
                Console.WriteLine("KSDExperiments.exe {EXPERIMENT} [parameters]");
                Environment.Exit(-1);
            }

            ExperimentUtil.CurrentExperimentName = ARGS[0];
            ExperimentUtil.CommandLineParameters = ARGS;

            DeleteOldFiles();
            FindConfigurationFile(ExperimentUtil.CurrentExperimentName);

            string FOLDER = ExperimentUtil.OutputFolder;
            if (!Directory.Exists(FOLDER))
                Directory.CreateDirectory(FOLDER);
            else
            {
                Directory.Delete(FOLDER, true);
                Directory.CreateDirectory(FOLDER);
            }

            DirectoryInfo di = new DirectoryInfo("Output");
            if (!di.Exists)
                di.Create();

            foreach (FileInfo fi in di.GetFiles())
                fi.Delete();

            LatexDocument.ClosePendingProcesses();
            LatexDocument.Initialize("Output");

            PipelineConfigurationSection section = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
                .Sections.OfType<PipelineConfigurationSection>().First();

            Pipeline pipeline = new Pipeline(section);
            pipeline.Initialize();
            pipeline.Run();

            CsvWriter.DumpAll();
            if (ExperimentUtil.ErrorsFound)
                log.Error("ERRORS FOUND!!!");

            log.Info("READY.");

            SaveLogFiles();
            return;
        }

        static void Main(string[] args)
        {
            try
            {
                Program p = new Program();
                p.Run(args);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }
}
