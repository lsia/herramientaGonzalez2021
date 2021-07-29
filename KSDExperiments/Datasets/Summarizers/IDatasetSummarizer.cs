using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSDExperiments.Datasets.Summarizers
{
    interface IDatasetSummarizer
    {
        void Summarize(Dataset sessions);
    }
}
