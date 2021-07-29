using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KSDExperiments.Configuration;
using KSDExperiments.Datasets;
using KSDExperiments.Experiments.FiniteContexts.Reporting;
using KSDExperiments.FiniteContexts.Models;


namespace KSDExperiments.FiniteContexts.Features
{
    public abstract class Feature
    {
        public FeatureConfigurationElement Configuration { get; private set; }
        public Feature(FeatureConfigurationElement configuration)
        {
            Configuration = configuration;
        }

        public abstract void CalculateFeatures(FeatureParameters parameters);
    }
}
