using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSDExperiments.Experiments
{
    [AttributeUsage(AttributeTargets.Class)]
    class AlwaysRunInParallelAttribute : Attribute
    {
    }
}
