using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KSDExperiments.Datasets;
using KSDExperiments.FiniteContexts.Models;


namespace KSDExperiments.FiniteContexts.Profiles
{
    public class Authentication
    {
        public Session Session { get; private set; }
        public bool Legitimate { get; private set; }
        public Dictionary<string,double> MethodValues { get; private set; }

        public Dictionary<string, KeyValuePair<TypingFeature, Model[]>> PatternVectors { get; private set; }

        public double[] MethodValuesArray
        {
            get
            {
                List<double> retval = new List<double>();
                foreach (var kv in MethodValues)
                    retval.Add(kv.Value);

                return retval.ToArray();
            }
        }

        public string Summarize()
        {
            StringBuilder retval = new StringBuilder();
            retval.AppendLine(Session.GetSessionText(true));
            foreach (var kv in MethodValues)
            {
                retval.Append(kv.Key);
                retval.Append("=");
                retval.Append(Math.Round(kv.Value, 2));
                retval.Append(", ");
            }

            return retval.ToString();
        }

        internal Authentication
            (
                Session session, 
                bool legitimate, 
                Dictionary<string, double> method_values,
                Dictionary<string, KeyValuePair<TypingFeature, Model[]>> pattern_vectors
                )
        {
            Session = session;
            Legitimate = legitimate;
            MethodValues = method_values;
            PatternVectors = pattern_vectors;
        }
    }
}
