using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KSDExperiments.Datasets;
using KSDExperiments.FiniteContexts.Models;


namespace KSDExperiments.FiniteContexts.Attributes
{
    public interface INumericAttribute
    {
        double GetValue(Dictionary<string, object> features, Dictionary<string, double> available_parameters);
    }
}
