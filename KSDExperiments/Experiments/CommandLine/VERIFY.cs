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
    class VERIFY : Experiment
    {
        public VERIFY(Pipeline pipeline, PipelineStageConfigurationElement configuration)
            : base(pipeline, configuration)
        {
        }

        public override void OnStageStart()
        {
            if (ExperimentUtil.CommandLineParameters.Length != 4)
            {
                Log.Error("ERROR!!! Three parameters expected, " + ExperimentUtil.CommandLineParameters.Length + " provided.");
                Log.Error("KSDExperiments.exe VERIFY {dataset} {user} {between-user|within-user}");
                Environment.Exit(-1002);
            }
        }

        const int MIN_SENTENCE_SIZE = 30;

        bool done = false;
        public override void ForEachUser(Dataset dataset, User user, Session[] sessions)
        {
            if (dataset.Name.ToLower() != ExperimentUtil.CommandLineParameters[1].ToLower() ||
                user.Name.ToLower() != ExperimentUtil.CommandLineParameters[2].ToLower()
            )
                return;

            Log.Info("DATASET " + dataset.Name);
            Log.Info("USER " + user.Name);

            Log.Info("Training " + ExperimentUtil.CommandLineParameters[3].ToUpper() + " adversaries profile...");
            var synthesizer_training = sessions;
            if (ExperimentUtil.CommandLineParameters[3].ToUpper() == "BETWEEN-USER")
            {
                synthesizer_training = dataset.Sessions.Where(s => s.User.UserID != user.UserID).ToArray();
                if (synthesizer_training.Length >= 100)
                    synthesizer_training = RNG.Instance.SampleWithoutReposition(synthesizer_training, 100);
            }
            else if (ExperimentUtil.CommandLineParameters[3].ToUpper() != "WITHIN-USER")
            {
                Log.Error("ERROR!!! Unrecognized evaluation type '" + ExperimentUtil.CommandLineParameters[3] + "'.");
                Environment.Exit(-1004);
            }

                Profile training_synthesizer_profile = FCH.CreateProfileWithNullClassifier(user, synthesizer_training);

            Log.Info("Setting up evil bot synthesizers...");
            var TS1 = new AverageSynthesizer(training_synthesizer_profile);
            TS1.Initialize();
            var TS2 = new UniformSynthesizer(training_synthesizer_profile);
            TS2.Initialize();
            var TS3 = new NonStationaryHistogramSynthesizer(training_synthesizer_profile);
            TS3.Initialize();

            Log.Info("Splitting legitimate samples into sentences...");
            var sentences = SentenceUtil.Split(sessions);

            Log.Info("Synthesizing evil bot forgeries...");
            List<Session> training_legitimate_sentences = new List<Session>();
            List<Session> training_synthetic_forgeries = new List<Session>();
            
            foreach (var sentence in sentences)
                if (sentence.Length >= MIN_SENTENCE_SIZE)
                {
                    Session synthetic_forgery = null;
                    if (RNG.Instance.Choice(0.2))
                        synthetic_forgery = TS1.Synthesize(sentence.VKs);
                    else if (RNG.Instance.Choice(0.2))
                        synthetic_forgery = TS2.Synthesize(sentence.VKs);
                    else
                        synthetic_forgery = TS3.Synthesize(sentence.VKs);

                    training_synthetic_forgeries.Add(synthetic_forgery);
                    training_legitimate_sentences.Add(sentence);
                }

            Log.Info("The training set contains " + sentences.Length + " sentences of " + 
                MIN_SENTENCE_SIZE + " characters or more.");

            ClassifierWEKA.MAX_CACHED_SESSIONS = 999999;
            FCH.TrainTestSplit = 1.0;           
            Profile training_profile = FCH.CreateProfile(training_legitimate_sentences.ToArray(), training_synthetic_forgeries.ToArray(), true);

            ClassifierWEKA weka = training_profile.Classifier as ClassifierWEKA;

            string filename = ExperimentUtil.OutputFolder + "/" + dataset.Name + "-" + user.Name + "-MIXED.arff";
            BinaryConfusionMatrix cm = weka.CrossValidate();
            weka.LastCrossValidation.CopyTo(filename);

            Log.Info("  BETWEEN-USER     C{0} {1,40}    FAR: {2,6}%    FRR: {3,6}%",
                7, "MIXED", cm.FAR, cm.FRR);

            training_profile.CalculateEERs();
            foreach (var kv in training_profile.EERs)
                if (!kv.Key.Contains("WEKA"))
                    Log.Info("    {0}  {1}", kv.Key, kv.Value);

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
