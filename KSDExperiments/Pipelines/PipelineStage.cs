using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;
using System.Threading.Tasks;

using NLog;

using KSDExperiments.Configuration;
using KSDExperiments.Datasets;
using KSDExperiments.Experiments;
using KSDExperiments.Experiments.Attributes;
using KSDExperiments.Reports;
using KSDExperiments.Util;


namespace KSDExperiments.Pipelines
{
    public abstract class PipelineStage
    {
        public Pipeline Pipeline { get; private set; }
        public PipelineStageConfigurationElement Configuration { get; private set; }

        public PipelineStage(Pipeline pipeline, PipelineStageConfigurationElement configuration)
        {
            Pipeline = pipeline;
            Configuration = configuration;

            if (Pipeline != null && Pipeline.Configuration != null)
            {
                RunInParallel = Pipeline.Configuration.Parallel;
                if (AlwaysRunInParallel)
                    RunInParallel = true;
            }

            var attr = this.GetType().GetCustomAttribute<SkipDatasetsAttribute>();
            if (attr != null)
                SkipDatasets = attr.Datasets;

            OnlyDatasets = this.GetType().GetCustomAttributes<OnlyDataset>().ToArray();

            var reorder = this.GetType().GetCustomAttribute<ReorderDatasetsAttribute>();
            if (reorder != null)
                ReorderDatasets = reorder.Datasets;

            SkipUsers = this.GetType().GetMethod("ForEachUser").GetCustomAttributes<SkipUserAttribute>().ToArray();
            OnlyUsers = this.GetType().GetMethod("ForEachUser").GetCustomAttributes<OnlyUserAttribute>().ToArray();
        }

        public string[] ReorderDatasets { get; private set; }

        public bool AlwaysRunInParallel
        {
            get
            {
                return this.GetType().GetCustomAttribute<AlwaysRunInParallelAttribute>() != null;
            }
        }

        public Logger Log
        {
            get
            {
                return LogManager.GetCurrentClassLogger();
            }
        }

        public virtual void Initialize() { }

        public virtual void OnStageStart()
        {
        }

        public virtual void OnStageEnd()
        {
        }

        public string[] SkipDatasets { get; private set; }
        public OnlyDataset[] OnlyDatasets { get; private set; }

        public SkipUserAttribute[] SkipUsers { get; private set; }
        public OnlyUserAttribute[] OnlyUsers { get; private set; }


        public virtual void OnDatasetStart(Dataset dataset) { }
        public virtual void OnDatasetEnd(Dataset dataset) { }

        public virtual void OnUserStart(Dataset dataset, User user) { }
        public virtual void OnUserEnd(Dataset dataset, User user) { }

        public virtual void ForEachDataset(Dataset dataset)
        {
        }

        public virtual void ForEachUser(Dataset dataset, User user, Session[] sessions)
        {
        }
        public virtual void ForEachSession(Dataset dataset, Session session)
        {
        }

        protected virtual void DoRun(Results results)
        {
        }

        public delegate void ForEachFeatureDelegate(TypingFeature feature);
        public void ForEachFeature(ForEachFeatureDelegate f)
        {
            f(TypingFeature.HT);
            f(TypingFeature.FT);
        }

        void DoForEachUser(Dataset dataset, User user, Session[] sessions)
        {
            CurrentUser = user;
            OnUserStart(dataset, user);
            ForEachUser(dataset, user, sessions);

            foreach (var session in sessions)
            {
                CurrentSession = session;
                ForEachSession(dataset, session);
            }

            OnUserEnd(dataset, user);
        }

        public Dataset CurrentDataset { get; private set; }
        public User CurrentUser { get; private set; }
        public Session CurrentSession { get; private set; }

        public ReportCube ReportCube { get; private set; }
        public ReportCube ReportCubeDataset { get; private set; }

        [ThreadStatic]
        ReportCube __reportCubeUser;

        public ReportCube ReportCubeUser { get { return __reportCubeUser; } }


        public CsvWriter DatasetCSV { get; private set; }
        public CsvWriter GlobalCSV { get; private set; }

        string TaskTreeWithSuffix(string suffix)
        {
            if (TaskTree == null)
                return "";
            else
                return TaskTree + suffix;
        }

        public bool RunInParallel { get; private set; }
        void RunTask(Results results)
        {
            if (typeof(Experiment).IsAssignableFrom(this.GetType()))
                GlobalCSV = CsvWriter.Create(ExperimentUtil.OutputFolder + "/" + TaskTreeWithSuffix("-") + "GLOBAL.csv");

            List<string> fields = new List<string>();
            ReportCubeColumnsAttribute attr = (ReportCubeColumnsAttribute) Attribute.GetCustomAttribute(this.GetType(), typeof(ReportCubeColumnsAttribute));
            if (attr != null)
            {
                fields.Add("DATASET");
                fields.Add("USER");
                fields.AddRange(attr.Columns);
                ReportCube = new ReportCube(fields.ToArray());

                fields.Clear();
                fields.Add("USER");
                fields.AddRange(attr.Columns);
                ReportCubeDataset = new ReportCube(fields.ToArray());

                fields.Clear();
                fields.AddRange(attr.Columns);
            }

            if (ReorderDatasets != null)
                results.ReorderDatasets(ReorderDatasets);

            OnStageStart();
            if (results.Datasets != null)
                foreach (var dataset in results.Datasets)
                    if ((SkipDatasets == null || !SkipDatasets.Contains(dataset.Name))
                            && (OnlyDatasets.Length == 0 || OnlyDatasets.Any(d => d.Dataset == dataset.Name))
                        )
                    {
                        CurrentDataset = dataset;

                        if (ReportCubeDataset != null)
                        {
                            ReportCubeDataset.Clear();
                            ReportCubeDataset.Basename = ExperimentUtil.CurrentExperimentName + "-" + dataset.Name;
                        }

                        if (typeof(Experiment).IsAssignableFrom(this.GetType()))
                            Log.Info("---------------------------------------- DATASET {0}", dataset.Name);

                        Pipeline.CurrentDataset = dataset;
                        if (typeof(Experiment).IsAssignableFrom(this.GetType()))
                            DatasetCSV = CsvWriter.Create(ExperimentUtil.OutputFolder + "/" + TaskTreeWithSuffix("-") + dataset.Name + ".csv");

                        OnDatasetStart(dataset);
                        ForEachDataset(dataset);

                        if (RunInParallel)
                            Parallel.ForEach(dataset.SessionsByUser, kv => RunUser(dataset, attr, fields, kv.Key, kv.Value));
                        else
                        {
                            foreach (var kv in dataset.SessionsByUser)
                                RunUser(dataset, attr, fields, kv.Key, kv.Value);
                        }

                        OnDatasetEnd(dataset);
                        if (typeof(Experiment).IsAssignableFrom(this.GetType()))
                            DatasetCSV.Dump();

                        if (ReportCubeDataset != null && ReportCubeDataset.Rows.Count != 0)
                            ReportCubeDataset.Save();
                    }

            DoRun(results);
            OnStageEnd();

            if (ReportCube != null && ReportCube.Rows.Count != 0)
                ReportCube.Save(ExperimentUtil.CurrentExperimentName + "-" + "FULL.csv");

            if (typeof(Experiment).IsAssignableFrom(this.GetType()))
                GlobalCSV.Dump();
        }

        void RunUser(Dataset dataset, ReportCubeColumnsAttribute attr, List<string> fields, int user_id, Session[] sessions)
        {
            User user = User.GetUser(user_id);
            
            if (SkipUsers.Length != 0 && SkipUsers.Any(u => u.User == user.Name))
                return;

            if (OnlyUsers.Length != 0 && !OnlyUsers.Any(u => u.User == user.Name))
                return;

            if (attr != null)
            {
                __reportCubeUser = new ReportCube(fields.ToArray());
                __reportCubeUser.Basename = ExperimentUtil.CurrentExperimentName + "-" + dataset.Name + "-" + user.Name;
                ReportCube.SetFixedInitialValues(dataset.Name, user.Name);
                ReportCubeDataset.SetFixedInitialValues(user.Name);
            }

            DoForEachUser(dataset, user, sessions);

            if (__reportCubeUser != null && __reportCubeUser.Rows.Count != 0)
                __reportCubeUser.Save();
        }

        public Stack<string> CurrentTaskStack { get; private set; } = new Stack<string>();
        public string TaskTree
        {
            get
            {
                if (CurrentTaskStack.Count == 0)
                    return null;

                StringBuilder sb = new StringBuilder();
                foreach (var task in CurrentTaskStack)
                {
                    if (sb.Length != 0)
                        sb.Append("-");

                    sb.Append(task);
                }

                return sb.ToString();
            }
        }

        void RunTaskLevels(Results results, IterateTask[] task_levels)
        {
            if (task_levels.Length > 1)
            {
                IterateTask[] next_level = new IterateTask[task_levels.Length - 1];
                Array.Copy(task_levels, 1, next_level, 0, task_levels.Length - 1);

                for (int i = 0; i < task_levels[0].Values.Length; i++)
                {
                    CurrentTaskStack.Push(task_levels[0].Values[i]);
                    RunTaskLevels(results, next_level);
                    CurrentTaskStack.Pop();
                }
            }
            else
            {
                for (int i = 0; i < task_levels[0].Values.Length; i++)
                {
                    CurrentTaskStack.Push(task_levels[0].Values[i]);
                    RunTask(results);
                    CurrentTaskStack.Pop();
                }
            }
        }

        public virtual void Run(Results results)
        {
            var type = this.GetType();
            var task_levels = this.GetType().GetCustomAttributes<IterateTask>().ToArray();
            Array.Reverse(task_levels);
            
            if (task_levels.Length == 0)
                RunTask(results);
            else
                RunTaskLevels(results, task_levels);
        }
    }
}
