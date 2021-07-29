using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSDExperiments.Reports
{
    [AttributeUsage(AttributeTargets.Class)]
    class ReportCubeColumnsAttribute : Attribute
    {
        public string[] Columns { get; private set; }
        public ReportCubeColumnsAttribute(params string[] columns)
        {
            Columns = columns;
        }
    }
}
