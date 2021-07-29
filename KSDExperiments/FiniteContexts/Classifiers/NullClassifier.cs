using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using KSDExperiments.Configuration;
using KSDExperiments.Datasets;
using KSDExperiments.Experiments.Distances;
using KSDExperiments.Experiments.FiniteContexts.Reporting;
using KSDExperiments.FiniteContexts.Attributes;
using KSDExperiments.FiniteContexts.Features;
using KSDExperiments.FiniteContexts.Models;
using KSDExperiments.FiniteContexts.PatternVector;
using KSDExperiments.FiniteContexts.Profiles;
using KSDExperiments.FiniteContexts.Store;
using KSDExperiments.Pipelines;
using KSDExperiments.Reports;
using KSDExperiments.Util;


namespace KSDExperiments.FiniteContexts.Classifiers
{
    public class NullClassifier : Classifier
    {
        public NullClassifier(User user, FiniteContextsConfiguration parameters, DirectoryInfo temp_folder)
            : base(user, parameters, temp_folder)
        {
        }

        public override string Name { get { return "NullClassifier"; } }

        public override void Retrain()
        {
        }

        public override bool IsLegitimate(Dictionary<string, double> numeric_attribute_values)
        {
            return true;
        }
    }
}
