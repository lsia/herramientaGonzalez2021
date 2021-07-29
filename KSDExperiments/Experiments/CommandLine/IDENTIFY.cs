using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using KSDExperiments.Configuration;
using KSDExperiments.Datasets;
using KSDExperiments.Experiments.Attributes;
using KSDExperiments.Experiments.Common;
using KSDExperiments.FiniteContexts.Classifiers;
using KSDExperiments.FiniteContexts.PatternVector;
using KSDExperiments.FiniteContexts.Synthesizer;
using KSDExperiments.FiniteContexts.Profiles;
using KSDExperiments.Pipelines;
using KSDExperiments.Util;


namespace KSDExperiments.Experiments.Articles.CACIC2021
{
    class ResultPerSentence
    {
        public Session Sentence;
        public int Challenges;
        public bool FalseNegative;

        public Session[] FalsePositives;

        public double FAR
        {
            get
            {
                return 100.0 * FalsePositives.Length / (double)Challenges;
            }
        }
    }

    class ResultPerUser
    {
        public User User;
        public ResultPerSentence[] ResultsPerSentence;

        public double FRR
        {
            get
            {
                return 100.0 * ResultsPerSentence.Where(r => r.FalseNegative).Count() / (double)ResultsPerSentence.Length;
            }
        }

        public double FAR
        {
            get
            {
                return ResultsPerSentence.Select(r => r.FAR).Average();
            }
        }
    }

    class IDENTIFY : Experiment
    {
        public IDENTIFY(Pipeline pipeline, PipelineStageConfigurationElement configuration)
            : base(pipeline, configuration)
        {
        }

        public override void OnStageStart()
        {
            if (ExperimentUtil.CommandLineParameters.Length != 3)
            {
                Log.Error("ERROR!!! Two parameters expected, " + ExperimentUtil.CommandLineParameters.Length + " provided.");
                Log.Error("KSDExperiments.exe IDENTIFY {dataset} {user}");
                Environment.Exit(-1002);
            }
        }

        const int MIN_SENTENCE_SIZE = 30;
        bool IsValidSentence(Session sentence)
        {
            return sentence.Length >= MIN_SENTENCE_SIZE && sentence.AlphanumericRate > 70.0;
        }

        const int CHALLENGES = 100;

        bool done = false;
        public override void ForEachUser(Dataset dataset, User user, Session[] sessions)
        {
            if (dataset.Name.ToLower() != ExperimentUtil.CommandLineParameters[1].ToLower() ||
                user.Name.ToLower() != ExperimentUtil.CommandLineParameters[2].ToLower()
            )
                return;

            Log.Info("DATASET " + dataset.Name);
            Log.Info("USER " + user.Name);

            Log.Info("Splitting legitimate samples into sentences...");
            var sentences = SentenceUtil.SplitBySentenceSize(sessions);

            int sentence_count = 0;
            List<ResultPerSentence> results = new List<ResultPerSentence>();
            foreach (var kv in sentences.OrderByDescending(kv => kv.Key))
                foreach (var sentence in kv.Value)
                    if (IsValidSentence(sentence))
                    {
                        sentence_count++;
                        Log.Info("SENTENCE {0}", sentence.Text);

                        ResultPerSentence retval = new ResultPerSentence();
                        results.Add(retval);
                        retval.Challenges = CHALLENGES;

                        Log.Info("    Generating text challenges...");
                        Session[] challenges = Challenges.FilterAndGenerateTextChallenges(CHALLENGES, sentence, sessions, true);
                        if (challenges == null)
                        {
                            Log.Info("        ERROR!!! Unable to generate text challenges.");
                            continue;
                        }

                        Log.Info("    Sampling training sentences...");
                        List<Session> training_sentences = new List<Session>();
                        foreach (var kvs in sentences)
                            foreach (var sent in kvs.Value)
                                training_sentences.Add(sent);

                        Session[] for_training = training_sentences.ToArray();
                        if (for_training.Length > 100)
                            for_training = RNG.Instance.SampleWithoutReposition(for_training, 100);

                        Log.Info("    Generating training instances with GOOD BOT and EVIL BOT...");                        
                        List<Session> good_bot = new List<Session>();
                        List<Session> evil_bot = new List<Session>();

                        foreach (var sent in for_training)
                        {
                            var candidates = sessions.Where(s => s.ID != sent.ID && s.Length >= sent.Length).ToArray();
                            if (candidates.Length != 0)
                            {
                                good_bot.Add(sent);
                                evil_bot.Add(Challenges.GenerateTextChallenge(sent, candidates));
                            }
                        }

                        Log.Info("    Training user profile but without this sentence...");
                        FCH.TrainTestSplit = 1.0;
                        Profile profile = FCH.CreateProfile(good_bot.ToArray(), evil_bot.ToArray());

                        Log.Info("    Saving training...");
                        profile.Classifier.SaveTraining(ExperimentUtil.OutputFolder + dataset.Name + "-USER" + user.Name + "-TRAINING-"
                            + sentence_count + ".arff");

                        Log.Info("    Evaluating sentence and challenges...");
                        Authentication[] auths = profile.AuthenticateWithoutRetrain(challenges);
                        if (!auths[0].Legitimate)
                        {
                            Log.Info("        *** FAILED TO AUTHENTICATE LEGITIMATE SENTENCE ***");
                            Log.Info("        One false negative will be added.");
                            retval.FalseNegative = true;
                        }

                        List<Session> false_positives = new List<Session>();
                        for (int i = 1; i < auths.Length; i++)
                            if (auths[i].Legitimate)
                                false_positives.Add(auths[i].Session);

                        Log.Info("    " + false_positives.Count + " false positives between challenges.");
                        foreach (var fp in false_positives)
                            Log.Info("        " + fp.Text);
                    }

            ResultPerUser rpu = new ResultPerUser();
            rpu.ResultsPerSentence = results.ToArray();
            Log.Info("--------------------");
            Log.Info("Final results for the user:");
            Log.Info("  FRR={0,2}%    FAR={1,2}%", rpu.FRR, rpu.FAR);

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
