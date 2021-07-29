using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSDExperiments.Experiments.Distances
{
    interface IDistance
    {
        double GetDistance(double[] a, double[] b);
    }
}
