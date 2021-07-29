using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;
using System.IO;
using System.Threading;

using NLog;

using KSDExperiments.Configuration;
using KSDExperiments.Datasets;
using KSDExperiments.FiniteContexts.Classifiers;
using KSDExperiments.FiniteContexts.Partitions;
using KSDExperiments.FiniteContexts.Profiles;
using KSDExperiments.Pipelines;


namespace KSDExperiments.FiniteContexts.Util
{
    class FiniteContextsHelper
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        public FiniteContextsConfiguration Parameters { get; private set; }

        public void InitializeParameters(string finite_contexts_section_name)
        {
            var section = (FiniteContextsExperimentConfigurationSection)ConfigurationManager.GetSection(finite_contexts_section_name);

            Parameters = new FiniteContextsConfiguration
                (
                    section.BiometricParameters,
                    section.Models,
                    section.Features,
                    section.Attributes
                );

            Parameters.Initialize();
        }

        public enum Stage
        {
            InitializeClassifier,
            BuildInitialProfile,
            EvaluateLegitimate,
            EvaluateImpostors,
            RetrainClassifier
        }

        public delegate void OnCreateProfileProgressDelegate(Stage stage);

        public Profile CreateProfile(Session[] initial_training, bool clear_classifier_cache = true, OnCreateProfileProgressDelegate progress = null)
        {
            User user = initial_training[0].User;
            return CreateProfile(user, initial_training, clear_classifier_cache, progress);
        }

        public Profile CreateProfile(User user, Session[] initial_training, bool clear_classifier_cache = true, OnCreateProfileProgressDelegate progress = null)
        {
            Session[] impostors = Pipeline.CurrentDataset.Sessions.Where(s => s.User.UserID != user.UserID).ToArray();
            return CreateProfile(user, initial_training, impostors, clear_classifier_cache, progress);
        }

        public Profile CreateProfile(User user, Session[] initial_training, Session[] impostors, bool clear_classifier_cache = true, OnCreateProfileProgressDelegate progress = null)
        {
            if (progress != null) progress(Stage.InitializeClassifier);

            Classifier classifier = new ClassifierWEKA(user, Parameters, new DirectoryInfo(Environment.CurrentDirectory));
            classifier.Initialize();
            if (clear_classifier_cache)
                classifier.ClearClassifierCache();

            Profile profile = new Profile(user, Parameters, classifier, impostors);
            if (progress != null) progress(Stage.BuildInitialProfile);
            profile.BuildInitialProfile(initial_training, initial_training.Length);
            if (progress != null) progress(Stage.EvaluateLegitimate);
            profile.EvaluateLegitimate(initial_training, 0, false);
            if (progress != null) progress(Stage.EvaluateImpostors);
            profile.EvaluateImpostors(initial_training.Length);
            if (progress != null) progress(Stage.RetrainClassifier);
            profile.RetrainClassifier();
            return profile;
        }

        public double TrainTestSplit { get; set; } = 0.75;

        public Profile CreateProfile(Session[] legitimate, Session[] impostors, bool clear_classifier_cache = true, OnCreateProfileProgressDelegate progress = null)
        {
            User user = legitimate[0].User;
            if (progress != null) progress(Stage.InitializeClassifier);

            ClassifierWEKA classifier = new ClassifierWEKA(user, Parameters, new DirectoryInfo(Environment.CurrentDirectory));
            classifier.Initialize();
            classifier.TrainTestSplit = TrainTestSplit;
            if (clear_classifier_cache)
                classifier.ClearClassifierCache();

            Profile profile = new Profile(user, Parameters, classifier, Pipeline.CurrentPipeline.CurrentResults.Datasets[0].Sessions);
            if (progress != null) progress(Stage.BuildInitialProfile);
            profile.BuildInitialProfile(legitimate, legitimate.Length);
            if (progress != null) progress(Stage.EvaluateLegitimate);
            profile.EvaluateLegitimate(legitimate, 0, false);
            if (progress != null) progress(Stage.EvaluateImpostors);
            profile.EvaluateFixedImpostors(impostors);
            if (progress != null) progress(Stage.RetrainClassifier);
            profile.RetrainClassifier();
            return profile;
        }

        public Profile CreateProfileWithNullClassifier(User user, Session[] initial_training)
        {
            Classifier classifier = new NullClassifier(user, Parameters, new DirectoryInfo(Environment.CurrentDirectory));
            Profile profile = new Profile(user, Parameters, classifier, Pipeline.CurrentPipeline.CurrentResults.Datasets[0].Sessions);
            profile.BuildInitialProfile(initial_training, initial_training.Length);

            /*
            profile.EvaluateLegitimate(initial_training, 0, true);
            profile.EvaluateImpostors(initial_training.Length);
            profile.RetrainClassifier();
            */

            return profile;
        }

        object giant_lock = new object();
        Dictionary<int, Profile> profiles_with_null_classifier = new Dictionary<int, Profile>();
        public Profile GetOrCreateProfileWithNullClassifier(Session[] initial_training)
        {
            User user = initial_training[0].User;

            Profile retval = null;
            bool found = false;
            while (retval == null)
            {
                found = false;
                lock (giant_lock)
                {
                    if (!profiles_with_null_classifier.ContainsKey(user.UserID))
                        profiles_with_null_classifier.Add(user.UserID, null);
                    else
                    {
                        retval = profiles_with_null_classifier[user.UserID];
                        log.Info("FOUND!!!");
                        found = true;
                    }
                }

                if (retval == null)
                {
                    if (found)
                        Thread.Sleep(1000);
                    else
                    {
                        Profile profile = CreateProfileWithNullClassifier(user, initial_training);
                        profiles_with_null_classifier[user.UserID] = profile;
                    }
                }
            }

            return retval;
        }

        public void SetupSession(Session session)
        {
            ThresholdPartitioner.ProcessSessionWithDefaultValues(session);
            CleanFTs.ProcessSession(session);
        }
    }
}
