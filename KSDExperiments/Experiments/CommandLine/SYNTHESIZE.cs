using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using KSDExperiments.Configuration;
using KSDExperiments.Datasets;
using KSDExperiments.Experiments.Attributes;
using KSDExperiments.FiniteContexts.Classifiers;
using KSDExperiments.FiniteContexts.PatternVector;
using KSDExperiments.FiniteContexts.Synthesizer;
using KSDExperiments.FiniteContexts.Profiles;
using KSDExperiments.Pipelines;
using KSDExperiments.Util;


namespace KSDExperiments.Experiments.Articles.CACIC2021
{
    class SYNTHESIZE : Experiment
    {
        public SYNTHESIZE(Pipeline pipeline, PipelineStageConfigurationElement configuration)
            : base(pipeline, configuration)
        {
        }

        public override void OnStageStart()
        {
            if (ExperimentUtil.CommandLineParameters.Length != 5)
            {
                Log.Error("ERROR!!! Four parameters expected, " + ExperimentUtil.CommandLineParameters.Length + " provided.");
                Log.Error("KSDExperiments.exe SYNTHESIZE {dataset} {user} {method} {text}");
                Environment.Exit(-1002);
            }
        }

        bool done = false;
        public override void ForEachUser(Dataset dataset, User user, Session[] sessions)
        {
            if (dataset.Name.ToLower() != ExperimentUtil.CommandLineParameters[1].ToLower() ||
                user.Name.ToLower() != ExperimentUtil.CommandLineParameters[2].ToLower()
            )
                return;

            Log.Info("DATASET " + dataset.Name);
            Log.Info("USER " + user.Name);
            Log.Info("Training user profile...");
            Profile profile = FCH.CreateProfileWithNullClassifier(user, sessions);

            string method = ExperimentUtil.CommandLineParameters[3];

            Log.Info("Loading synthesizer " + method + "...");
            KeystrokeDynamicsSynthesizer synthesizer = null;
            if (method == "AverageSynthesizer")
                synthesizer = new AverageSynthesizer(profile);
            else if (method == "UniformSynthesizer")
                synthesizer = new UniformSynthesizer(profile);
            else if (method == "LCBMSynthesizer")
                synthesizer = new LCBMSynthesizer(profile);
            else if (method == "HistogramSynthesizer")
                synthesizer = new HistogramSynthesizer(profile);
            else if (method == "NonStationaryHistogramSynthesizer")
                synthesizer = new NonStationaryHistogramSynthesizer(profile);
            else if (method == "NonStationaryHistogramSynthesizerReverse")
                synthesizer = new NonStationaryHistogramSynthesizerReverse(profile);
            else
            {
                Log.Error("ERROR!!! Unrecognized synthesizer '" + method + "'.");
                Environment.Exit(-1003);
            }
            
            synthesizer.Initialize();

            string text = ExperimentUtil.CommandLineParameters[4].ToUpper();

            Log.Info("Generating virtual keys sequence...");
            List<byte> vks = new List<byte>();
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ')
                    vks.Add((byte)VirtualKeys.VK_SPACE);
                else
                {
                    int chr = Convert.ToInt32(text[i]);
                    if (chr >= 65 && chr <= 90)
                    {
                        chr -= 65;
                        chr += (int)VirtualKeys.VK_A;
                        vks.Add((byte) chr);
                    }
                    else if (chr >= 48 && chr <= 57)
                    {
                        chr -= 48;
                        chr += (int)VirtualKeys.VK_0;
                        vks.Add((byte) chr);
                    }
                    else
                        vks.Add((byte) VirtualKeys.VK_SPACE);
                }
            }

            Session session = synthesizer.Synthesize(vks.ToArray());

            Log.Info("Saving results...");
            StreamWriter sw = new StreamWriter(ExperimentUtil.OutputFolder + "/SYNTHETIC.csv");
            sw.WriteLine("VK,HT,FT");
            for (int i = 0; i < session.Length; i++)
            {
                sw.Write(session.VKs[i]);
                sw.Write(",");
                sw.Write(session.HTs[i]);
                sw.Write(",");
                sw.Write(session.FTs[i]);
                sw.WriteLine();
            }

            sw.Close();
            done = true;
        }

        public override void OnStageEnd()
        {
            if (!done)
            {
                Log.Error("ERROR!!! DATASET or USER not found.");
                Environment.Exit(-1001);
            }
        }
    }
}
