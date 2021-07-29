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
    public class CleanFTs : PipelineStage
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        public CleanFTs(Pipeline pipeline, PipelineStageConfigurationElement configuration)
            : base(pipeline, configuration)
        {
        }

        public static void ProcessSession(Session session)
        {
            for (int i = 0; i < session.VKs.Length; i++)
            {
                if (session.Features[TypingFeature.FT][i] < 0)
                    session.Features[TypingFeature.FT][i] = int.MaxValue;

                if (session.Features[TypingFeature.FT][i] > 1500)
                    session.Features[TypingFeature.FT][i] = 1500;

                if (session.Features[TypingFeature.HT][i] < 0)
                    session.Features[TypingFeature.HT][i] = int.MaxValue;

                if (session.Features[TypingFeature.HT][i] > 1500)
                    session.Features[TypingFeature.HT][i] = 1500;
            }

            session.Features[TypingFeature.FT][0] = int.MinValue;
            foreach (int offset in session.PartitionOffsets)
                session.Features[TypingFeature.FT][offset] = int.MinValue;
        }

        protected override void DoRun(Results results)
        {
            log.Info("Cleaning FTs after partitions...");
            ExperimentParallelization.ForEachSession(results, (dataset, session) => ProcessSession(session));
        }
    }
}
