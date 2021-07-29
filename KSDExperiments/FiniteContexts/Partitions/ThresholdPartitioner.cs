using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NLog;

using KSDExperiments.Configuration;
using KSDExperiments.Datasets;
using KSDExperiments.Pipelines;
using KSDExperiments.Util;


namespace KSDExperiments.FiniteContexts.Partitions
{
    public class ThresholdPartitioner : PipelineStage
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        public ThresholdPartitioner(Pipeline pipeline, PipelineStageConfigurationElement configuration)
            : base(pipeline, configuration)
        {
        }

        const int THRESHOLD = 1500;

        public void ProcessSession(Session session)
        {
            List<int> cutoff_points = new List<int>();
            for ( int i = 0; i < session.VKs.Length; i++)
                if ( session.Features[TypingFeature.FT][i] > THRESHOLD )
                    cutoff_points.Add(i);

            session.SetPartitionOffsets(cutoff_points.ToArray());
        }

        protected override void DoRun(Results results)
        {
            log.Info("Partitioning sessions...");
            ExperimentParallelization.ForEachSession(results, (dataset, session) => ProcessSession(session));
        }

        public static void ProcessSessionWithDefaultValues(Session session)
        {
            List<int> cutoff_points = new List<int>();
            for (int i = 0; i < session.VKs.Length; i++)
                if (session.Features[TypingFeature.FT][i] > 1500)
                    cutoff_points.Add(i);

            session.SetPartitionOffsets(cutoff_points.ToArray());
        }
    }
}
