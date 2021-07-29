using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KSDExperiments.Datasets;
using KSDExperiments.FiniteContexts.Models;
using KSDExperiments.Experiments.FiniteContexts.Reporting;


namespace KSDExperiments.FiniteContexts.Features
{
    public class FeatureParameters
    {  
        public FeatureParameters
            (
                Dictionary<string, object> dictionary, 
                Session session,
                FiniteContextsReport report,

                string pattern_name,
                Model[] pattern,
                TypingFeature parameter,
                int[] parameter_values
            )
        {
            Dictionary = dictionary;
            Session = session;
            Report = report;

            PatternName = pattern_name;
            Pattern = pattern;
            Parameter = parameter;
            ParameterValues = parameter_values;
        }

        public Dictionary<string, object> Dictionary { get; private set; }
        public Session Session { get; private set; }

        public FiniteContextsReport Report { get; private set; }

        public string PatternName { get; private set; }
        public Model[] Pattern { get; private set; }
        public TypingFeature Parameter { get; private set; }
        public int[] ParameterValues { get; private set; }
    }
}
