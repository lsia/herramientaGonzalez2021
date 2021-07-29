using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;

using NLog;

using KSDExperiments.Configuration;
using KSDExperiments.Datasets;
using KSDExperiments.Experiments.Distances;
using KSDExperiments.Experiments.FiniteContexts.Reporting;
using KSDExperiments.FiniteContexts.Attributes;
using KSDExperiments.FiniteContexts.Classifiers;
using KSDExperiments.FiniteContexts.Features;
using KSDExperiments.FiniteContexts.Models;
using KSDExperiments.FiniteContexts.PatternVector;
using KSDExperiments.FiniteContexts.Profiles;
using KSDExperiments.FiniteContexts.Store;
using KSDExperiments.Pipelines;
using KSDExperiments.Reports;
using KSDExperiments.Util;


namespace KSDExperiments.Experiments.FiniteContexts
{
    public partial class FiniteContextsExperiment : PipelineStage
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        public FiniteContextsExperiment(Pipeline pipeline, PipelineStageConfigurationElement configuration)
            : base(pipeline, configuration)
        {
        }

        public FiniteContextsConfiguration Parameters
        {
            get; private set;
        }

        FiniteContextsExperimentConfigurationSection section;
        int TRAINING_SESSIONS = 50;
        public override void Initialize()
        {
            section = (FiniteContextsExperimentConfigurationSection)ConfigurationManager.GetSection(Configuration.Configuration);
            TRAINING_SESSIONS = section.InitialTrainingSessions;

            Parameters = new FiniteContextsConfiguration
                (
                    section.BiometricParameters,
                    section.Models,
                    section.Features,
                    section.Attributes
                );

            Parameters.Initialize();
        }


        int[] SelectAllTimings(Session[] sessions, byte vk)
        {
            List<int> retval = new List<int>();
            for (int i = 0; i < sessions.Length; i++)
                for (int j = 0; j < sessions[i].VKs.Length; j++)
                    if (sessions[i].VKs[j] == vk)
                        retval.Add(sessions[i].Features[TypingFeature.HT][j]);

            return retval.ToArray();
        }

        void ForEachUser(Dataset dataset, int user_id, Session[] sessions)
        {
            int training_sessions = (TRAINING_SESSIONS == -1 ? sessions.Length : TRAINING_SESSIONS);
            if (sessions.Length <= TRAINING_SESSIONS)
            {
                log.Warn("NOT ENOUGH TRAINING SESSIONS FOR USER {0}", user_id);
                return;
            }

            FiniteContextsReport report = new FiniteContextsReport(this);
            report.Start(user_id);

            User user = User.GetUser(user_id);
            // Classifier classifier = new ClassifierLibSVM(user, Parameters, new DirectoryInfo(Environment.CurrentDirectory));
            Classifier classifier = null;
            Profile profile = new Profile(user, Parameters, classifier, impostors, report);
            profile.BuildInitialProfile(sessions, training_sessions);

            /*
            ModelFeeder feeder = Parameters.CreateFeeder(user);
            for (int i = 0; i < training_sessions; i++)
                feeder.Feed(sessions[i]);

            Builder[] builders = feeder.GetBuilders();
            user.Properties.Add("builders", builders);

            Dictionary<string, List<double>> mix_leg_values = new Dictionary<string, List<double>>();
            mix_leg_values.Add("MIX", new List<double>());
            mix_leg_values.Add("WMIX", new List<double>());
            Dictionary<string, List<double>> mix_imp_values = new Dictionary<string, List<double>>();
            mix_imp_values.Add("MIX", new List<double>());
            mix_imp_values.Add("WMIX", new List<double>());
            */

            if (Configuration.Action == "authenticate")
            {
                profile.EvaluateLegitimate(sessions, training_sessions, section.Retrain);
                List<Session> selected_impostor_sessions = profile.EvaluateImpostors(sessions.Length - training_sessions);

                profile.Classifier.OnFalseRejection += Profile_OnFalseRejection;
                profile.Classifier.OnFalseAcceptance += Profile_OnFalseAcceptance;
                profile.RetrainClassifier();
                profile.CalculateEERs();

                /*
                user.Properties.Add("attributes_leg_values", profile.LegitimateValues);
                user.Properties.Add("attributes_imp_values", profile.ImpostorSessions);
                */

                /*
                int count = sessions.Count();
                for (int i = training_sessions; i < count; i++)
                {
                    double leg_mix = 0.0, imp_mix = 0.0;
                    double leg_wmix = 0.0, leg_wmix_sum = 0.0, imp_wmix = 0.0, imp_wmix_sum = 0.0;
                    foreach (var kv in profile.LegitimateValues)
                    {
                        double leg_val = kv.Value[i - training_sessions];

                        int leg_leq = kv.Value.Where(v => (v <= leg_val)).Count();
                        int imp_leq = profile.ImpostorValues[kv.Key].Where(v => (v <= leg_val)).Count();

                        double component = (double)(imp_leq + 1) / (imp_leq + leg_leq + 1);
                        leg_mix += component;

                        double weight = 1.01 / (0.01 + profile.EERs[kv.Key].Value);
                        leg_wmix_sum += weight;
                        leg_wmix += weight * component;

                        double imp_val = profile.ImpostorValues[kv.Key][i - training_sessions];
                        leg_leq = kv.Value.Where(v => (v <= imp_val)).Count();
                        imp_leq = profile.ImpostorValues[kv.Key].Where(v => (v <= imp_val)).Count();
                        component = (double)(imp_leq + 1) / (imp_leq + leg_leq + 1);
                        imp_mix += component;

                        imp_wmix_sum += weight;
                        imp_wmix += weight * component;
                    }

                    leg_mix /= profile.LegitimateValues.Count;
                    mix_leg_values["MIX"].Add(leg_mix);
                    imp_mix /= profile.LegitimateValues.Count;
                    mix_imp_values["MIX"].Add(imp_mix);

                    leg_wmix /= leg_wmix_sum * profile.LegitimateValues.Count;
                    mix_leg_values["WMIX"].Add(leg_wmix);
                    imp_wmix /= imp_wmix_sum * profile.LegitimateValues.Count;
                    mix_imp_values["WMIX"].Add(imp_wmix);

                    log.Info("{0,10} {1,20}   {2,10} {3,20}", sessions[i].ID, leg_mix, selected_impostor_sessions[i - training_sessions].ID, imp_mix);
                }

                CalculateEERs(report, mix_leg_values, mix_imp_values);
                */
            }

            report.Close();

            /*
            KSDExperiments.FiniteContexts.Store.SqlServerProfileStore store = new KSDExperiments.FiniteContexts.Store.SqlServerProfileStore();
            store.StoreProfile(user_id, feeder);

            RedisProfileStore store = new RedisProfileStore();
            store.StoreProfile(user_id, feeder);
            */
        }

        private void Profile_OnFalseAcceptance(Authentication authentication)
        {
            log.Info("[{0}] FALSE ACCEPTANCE: {1}", authentication.Session.User.UserID, authentication.Session.GetSessionText());
            foreach (var method in authentication.MethodValues)
                log.Info("    {0,-10}: {1}", method.Key, method.Value);
        }

        private void Profile_OnFalseRejection(Authentication authentication)
        {
            log.Info("[{0}] FALSE REJECTION: {1}", authentication.Session.User.UserID, authentication.Session.GetSessionText());
            foreach (var method in authentication.MethodValues)
                log.Info("    {0,-10}: {1}", method.Key, method.Value);
        }

        void EvaluateUserSession(bool legitimate, Session session, FiniteContextsReport report, Builder[] builders, Dictionary<string, List<double>> attribute_values)
        {
            if (report != null)
                report.StartSession(session);

            var numeric_attribute_values = CalculateAttributes(session, builders, report);
            foreach (var attr in numeric_attribute_values)
                attribute_values[attr.Key].Add(attr.Value);

            if (report != null)
                if (legitimate)
                    report.OnLegitimateSession(session, numeric_attribute_values);
                else
                    report.OnImpostorSession(session, numeric_attribute_values);
        }

        Dictionary<string, List<double>> EvaluateLegitimate(Session[] sessions, FiniteContextsReport report, ModelFeeder feeder, Builder[] builders, int training_sessions)
        {
            Dictionary<string, List<double>> legitimate_values = new Dictionary<string, List<double>>();
            foreach (var attr in Parameters.NumericAttributes)
                legitimate_values.Add(attr.Key, new List<double>());

            for (int i = training_sessions; i < sessions.Length; i++)
            {
                /*
                sw = File.CreateText("Output/" + user_id + "." + sessions[i].ID + ".txt");
                sw.WriteLine(sessions[i].GetSessionText());
                sw.WriteLine();
                */

                // ReportSession report = new ReportSession(sessions[i]);
                // builder.OnModelChosen += (feature, session, pos, context, candidates, chosen) => report.OnModelChosen(feature, session, pos, context, candidates.Cast<AvgStdevModel>().ToArray(), (AvgStdevModel) chosen);
                Session session = sessions[i];
                EvaluateUserSession(true, session, report, builders, legitimate_values);
                if (section.Retrain)
                    feeder.Feed(session, false);


                /*
                MakeAvgStdevArrays(patterns["TD"].Cast<AvgStdevModel>().ToArray(), out avg, out std);
                double d_ft = distance.GetDistance(KSDConvert.IntArrayToDouble(sessions[i].FTs), avg, KSDConvert.Invert(std));
                // report.AddVectorPattern(Features.FT, sessions[i].FTs, avg, std, builder.ChosenModels.Select(m => m.ContextOrder).ToArray());
                writer.Write(new object[] { user_id, sessions[i].ID, d_ft });

                patterns = builders.Rebuild(TypingFeature.HT, sessions[i]);
                MakeAvgStdevArrays(patterns["TD"].Cast<AvgStdevModel>().ToArray(), out avg, out std);
                double d_ht = distance.GetDistance(KSDConvert.IntArrayToDouble(sessions[i].HTs), avg, KSDConvert.Invert(std));
                // report.AddVectorPattern(Features.HT, sessions[i].HTs, avg, std, builder.ChosenModels.Select(m => m.ContextOrder).ToArray());
                writer.Write(new object[] { user_id, sessions[i].ID, d_ht });
                */

                // report.Compile();

                // sw.Close();
            }

            return legitimate_values;
        }

        Dictionary<string, List<double>> EvaluateImpostors(List<Session> selected_sessions, Session[] all_sessions, int user_id, FiniteContextsReport report, Builder[] builders, int count)
        {
            Dictionary<string, List<double>> impostor_values = new Dictionary<string, List<double>>();
            foreach (var attr in Parameters.NumericAttributes)
                impostor_values.Add(attr.Key, new List<double>());

            RNG rng = new RNG();
            for (int i = 0; i < count; i++)
            {
                int pos = rng.Next(all_sessions.Length);
                while (all_sessions[pos].User.UserID == user_id)
                    pos = rng.Next(all_sessions.Length);

                selected_sessions.Add(all_sessions[pos]);
                EvaluateUserSession(false, all_sessions[pos], report, builders, impostor_values);
            }

            return impostor_values;
        }

        bool MustCalculateEERs
        {
            get
            {
                return  Configuration.Action != "train" &&
                        section.CalculateEERs;
            }
        }

        Dictionary<string,double> CalculateEERs(FiniteContextsReport report, Dictionary<string, List<double>> legitimate_values, Dictionary<string, List<double>> impostor_values)
        {
            if (!MustCalculateEERs)
                return null;

            Dictionary<string, double> retval = new Dictionary<string, double>();
            foreach (var kv in legitimate_values)
            {
                double[] legitimate = legitimate_values[kv.Key].ToArray();
                double[] impostor = impostor_values[kv.Key].ToArray();

                double deer, deer_value;
                FindEER(legitimate, impostor, out deer, out deer_value);
                report.OnEERFound(kv.Key, deer, deer_value);
                retval.Add(kv.Key, deer);
            }

            return retval;
        }

        object arff_lock = new object();

        int count_identify_sessions = 0;
        DateTime last_count_identify_sessions = DateTime.MinValue;
        void IdentifyForEachSession(Dataset dataset, Session session, User[] users, ARFF arff, CsvWriter csv)
        {
            Console.Write(".");
            count_identify_sessions++;
            if ((count_identify_sessions % 100) == 0)
            {
                DateTime now = DateTime.Now;
                if (last_count_identify_sessions != DateTime.MinValue)
                {
                    Console.WriteLine();
                    log.Info("  {0} sessions/sec", Math.Round(100.0 / (now - last_count_identify_sessions).TotalSeconds));
                }

                last_count_identify_sessions = now;
            }

            List<double> all_user_attributes = new List<double>();
            for (int i = 0; i < users.Length; i++)
            {
                Builder[] builders = (Builder[]) users[i].Properties["builders"];
                var values = CalculateAttributes(session, builders, null);
                foreach (var kv in values)
                    all_user_attributes.Add(kv.Value);
            }

            double[] tmp = all_user_attributes.ToArray();
            lock (arff_lock)
            {
                arff.AppendData(tmp);
                arff.AppendCategory("U" + session.User.UserID);
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < tmp.Length; i++)
            {
                sb.Append(tmp[i]);
                sb.Append(",");
            }

            sb.Append("U" + session.User.UserID);

            List<object> tmp2 = new List<object>();
            for (int i = 0; i < tmp.Length; i++)
                tmp2.Add(tmp[i]);

            tmp2.Add("U" + session.User.UserID);
            csv.WriteLine(tmp2.ToArray());
        }

        void Identify(Results results)
        {
            log.Info("Identification task started...");
            int[] user_ids = results.Datasets[0].SessionsByUser.Keys.ToArray();

            User[] users = new User[user_ids.Length];
            for (int i = 0; i < user_ids.Length; i++)
                users[i] = User.GetUser(user_ids[i]);

            CsvWriter csv = new CsvWriter("Output/LSIA_CSV", users.Length * Parameters.NumericAttributes.Count + 1);
            List<string> headers = new List<string>();

            StringBuilder categories = new StringBuilder();
            ARFF arff = new ARFF("Output/LSIA_IDENTIFY");
            arff.StartHeaders();
            for (int i = 0; i < users.Length; i++)
            {
                if (categories.Length != 0)
                    categories.Append(",");

                categories.Append("U");
                categories.Append(users[i].UserID);

                foreach (var kv in Parameters.NumericAttributes)
                {
                    string attr = "U" + users[i].UserID + "_" + kv.Key;
                    arff.AddNumericAttribute(attr);
                    headers.Add(attr);
                }
            }

            headers.Add("user");
            csv.WriteLine(headers.ToArray());

            arff.AddCategory("user", categories.ToString());
            arff.StartData();
            ExperimentParallelization.ForEachSession(results, (d, s) => IdentifyForEachSession(d, s, users, arff, csv));
            arff.Close();
        }

        Session[] impostors;
        protected override void DoRun(Results results)
        {
            if (results.Datasets.Length == 1)
                impostors = results.Datasets[0].Sessions;
            else
                impostors = results.Datasets[1].Sessions;

            results.KeepFirst();
            log.Info("Running finite contexts distance experiment ({0})...", Configuration.Configuration);
            ExperimentParallelization.ForEachUser(results, ForEachUser);            

            if (Configuration.Action == "identify")
                Identify(results);

            if (MustCalculateEERs)
                FiniteContextsReport.SummarizeEERs();
        }

        void FindEER(double[] legitimate, double[] impostor, out double eer, out double eer_value)
        {
            Array.Sort(legitimate);
            Array.Sort(impostor);

            int ipos = 0;
            for (int i = 0; i < legitimate.Length; i++)
            {
                while (ipos < impostor.Length && impostor[ipos] <= legitimate[i])
                    ipos++;

                double frr = (double) (legitimate.Length - i - 1) / (double) legitimate.Length;
                double far = (double) ipos / (double) impostor.Length;

                if ( far >= frr )
                {
                    eer = (far + frr) / 2.0;

                    if (ipos >= impostor.Length) ipos = impostor.Length - 1;
                    eer_value = (legitimate[i] + impostor[ipos]) / 2.0;
                    return;
                }
            }

            throw new ArgumentException("Unable to calculate EER");
        }

        int blg = 0;
        DateTime last = DateTime.Now;
        Dictionary<string, double> CalculateAttributes(Session session, Builder[] builders, FiniteContextsReport report)
        {
            int clusp = System.Threading.Interlocked.Increment(ref blg);
            if ((clusp % 100) == 0)
            {
                DateTime cnow = DateTime.Now;
                // log.Info("  {0} sessions/second", Math.Round(100.0 / (cnow - last).TotalSeconds,2));
                last = cnow;
            }

            Dictionary<string, object> features_dictionary = new Dictionary<string, object>();
            RunForTypingFeature(session, features_dictionary, builders, report);

            Dictionary<string, double> numeric_attribute_values = new Dictionary<string, double>();
            foreach (var attr in Parameters.NumericAttributes)
                // try
                {
                    double tmp = attr.Value.GetValue(features_dictionary, numeric_attribute_values);
                    numeric_attribute_values.Add(attr.Key, tmp);
                }
                /*
                catch (NotImplementedException)
                {

                }
                */

            return numeric_attribute_values;
        }

        void RunForTypingFeature
            (
                Session session, 
                Dictionary<string,object> features_dictionary,
                Builder[] builders,
                FiniteContextsReport report
            )
        {
            var patterns = builders.Rebuild(session);
            foreach (Feature feature in Parameters.Features)
            {
                string[] fields = feature.Configuration.Pattern.Split(';');
                foreach (string field in fields)
                {
                    FeatureParameters parameters = new FeatureParameters
                    (
                        features_dictionary,
                        session,
                        report,

                        field,
                        patterns[field].Value,
                        patterns[field].Key,
                        session.Features[patterns[field].Key]
                    );

                    feature.CalculateFeatures(parameters);
                }
            }
        }

        StreamWriter sw;
        private void Builder_OnModelChosen(TypingFeature feature, Session session, int pos, ulong context, AvgStdevModel[] candidates, AvgStdevModel chosen)
        {
            sw.Write(string.Format("{0,-8}", pos));
            sw.Write(session.VKs[pos].ToString("X").PadLeft(2, '0'));
            sw.Write("    ");
            sw.WriteLine(context.ToString("X"));

            sw.Write("        ");
            for (int i = 0; i < candidates.Length; i++)
                if (candidates[i] == null)
                    sw.Write("X  ");
                else
                {
                    sw.Write(candidates[i].ToString());
                    sw.Write("  ");
                }

            sw.WriteLine();
            sw.Write("        ");
            if (chosen != null)
                sw.WriteLine(chosen.ToString());
            else
                sw.WriteLine("XXX");            
        }
    }
}
