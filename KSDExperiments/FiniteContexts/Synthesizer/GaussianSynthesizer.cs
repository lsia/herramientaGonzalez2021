using KSDExperiments.Datasets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KSDExperiments.FiniteContexts.Models;
using KSDExperiments.FiniteContexts.PatternVector;
using KSDExperiments.FiniteContexts.Profiles;
using KSDExperiments.Util;


namespace KSDExperiments.FiniteContexts.Synthesizer
{
    class GaussianSynthesizer : LocalForwardSynthesizer<AvgStdevModel>
    {
        public GaussianSynthesizer(Profile profile)
            : base(profile)
        {
        }

        public override int OnNullModel(int pos)
        {
            return (int)(1000 * RNG.Instance.NextDouble());
        }

        public override int OnModelFound(int pos, AvgStdevModel model)
        {
            return (int) RNG.Instance.SampleFromNormalDistribution(model.Average, model.StandardDeviation);
        }
    }
}
