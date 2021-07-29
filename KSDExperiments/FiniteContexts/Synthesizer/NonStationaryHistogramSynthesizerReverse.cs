using KSDExperiments.Datasets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KSDExperiments.FiniteContexts.Models;
using KSDExperiments.FiniteContexts.Models.Histogram;
using KSDExperiments.FiniteContexts.PatternVector;
using KSDExperiments.FiniteContexts.Profiles;
using KSDExperiments.Util;


namespace KSDExperiments.FiniteContexts.Synthesizer
{
    class NonStationaryHistogramSynthesizerReverse : LocalForwardSynthesizer<HistogramModel>
    {
        public NonStationaryHistogramSynthesizerReverse(Profile profile)
            : base(profile)
        {
        }

        Dictionary<TypingFeature, NormalVariable> differences = new Dictionary<TypingFeature, NormalVariable>();
        public override void Initialize()
        {
            NormalVariable nvht = new NormalVariable(Profile.GetAverageDifferenceDistribution(TypingFeature.HT));
            differences.Add(TypingFeature.HT, nvht);
            NormalVariable nvft = new NormalVariable(Profile.GetAverageDifferenceDistribution(TypingFeature.FT));
            differences.Add(TypingFeature.FT, nvft);
        }

        double offset = 0.0;
        public override void OnSynthesizeFeatureStart(TypingFeature feature, Session dummy)
        {
            offset = differences[feature].GetSample();
        }

        public override int OnNullModel(int pos)
        {
            return (int)(1000 * RNG.Instance.NextDouble());
        }

        public override int OnModelFound(int pos, HistogramModel model)
        {
            int[] observations = model.Observations.ToArray();
            Array.Sort(observations);

            double random_offset = RNG.Instance.NextDouble();
            int random_pos = RNG.Instance.Next(observations.Length);
            double l = 0.0;
            if (random_pos != 0)
                l = observations[random_pos - 1];

            int retval = (int)(l + (observations[random_pos] - l) * random_offset - offset) ;
            return retval;
        }
    }
}
