using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using System.IO;

using NLog;

using KSDExperiments.Util;



namespace KSDExperiments.FiniteContexts.Classifiers
{
    enum AttributeRankingMethods
    {
        CorrelationAttributeEval,
        GainRatioAttributeEval,
        InfoGainAttributeEval,
        OneRAttributeEval,
        SymmetricalUncertAttributeEval
    }

    class WEKA
    {
        static Logger log = LogManager.GetCurrentClassLogger();
        public WEKA()
        {
        }

        const string JAVA_PATH = @"c:\Program Files\Java\jre1.8.0_271\bin\java.exe";
        const string WEKA_JAR = "\"c:\\Program Files\\weka-3-8-4\\weka.jar\"";

        bool VerboseWEKACommands
        {
            get
            {
                return ConfigUtil.GetBoolSetting("verbose.WEKAcommands", false);
            }
        }

        public string[] RunWEKA(string command_line, string logname)
        {
            Process p = new Process();
            p.StartInfo.FileName = JAVA_PATH;
            p.StartInfo.Arguments = "-classpath " + WEKA_JAR + " " + command_line;

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            if (VerboseWEKACommands)
                log.Info(p.StartInfo.Arguments);

            p.Start();

            string stdout = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            File.WriteAllText(logname, stdout);

            return File.ReadAllLines(logname);
        }

        public string[] SelectAttributesCfsSubset(string arff_file)
        {
            string COMMAND_LINE = "weka.attributeSelection.CfsSubsetEval -P 1 -E 1 " +
                "-s \"weka.attributeSelection.GreedyStepwise -T -1.7976931348623157E308 -N -1 -num-slots 1\" -i " +
                arff_file;

            string logname = "Output/WEKA.STDOUT.SelectAttributes." + Path.GetFileName(arff_file) + ".stdout";
            string[] lines = RunWEKA(COMMAND_LINE, logname);

            int i = 0;
            for (i = 0; i < lines.Length; i++)
                if (lines[i].Contains("Selected attributes"))
                {
                    i++;
                    break;
                }

            List<string> retval = new List<string>();
            for (; i < lines.Length; i++)
            {
                string attribute = lines[i].Trim();
                if (attribute != "")
                    retval.Add(attribute);
            }

            return retval.ToArray();
        }



        public KeyValuePair<string, double>[] RankAttributes(AttributeRankingMethods method, string arff_file)
        {
            string COMMAND_LINE = "weka.attributeSelection." + method.ToString() +
                " -s \"weka.attributeSelection.Ranker -T -1.7976931348623157E308 -N -1\" -i " +
                arff_file;

            string logname = "Output/WEKA.STDOUT.SelectAttributes." + Path.GetFileName(arff_file) + ".stdout";
            string[] lines = RunWEKA(COMMAND_LINE, logname);

            int i = 0;
            for (i = 0; i < lines.Length; i++)
                if (lines[i].Contains("Ranked attributes"))
                {
                    i++;
                    break;
                }

            List<KeyValuePair<string, double>> retval = new List<KeyValuePair<string, double>>();
            for (; i < lines.Length; i++)
            {
                if (lines[i].Contains("Selected"))
                    break;

                string attribute = lines[i].Trim();
                if (attribute != "")
                {
                    while (attribute.Contains("  "))
                        attribute = attribute.Replace("  ", " ");

                    string[] fields = attribute.Split(' ');
                    retval.Add(new KeyValuePair<string, double>(fields[2], double.Parse(fields[0])));
                }
            }

            return retval.ToArray();
        }
    }
}
