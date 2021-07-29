using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NLog;


namespace KSDExperiments.Datasets.Summarizers
{
    class BasicSummarizer : IDatasetSummarizer
    {
        static Logger log = LogManager.GetCurrentClassLogger();


        public void Summarize(Dataset dataset)
        {
            log.Info("Basic dataset summary...");
            log.Info("  {0} sessions", dataset.Sessions.Length);

            double avg_len = dataset.Sessions.Average(s => s.Features[TypingFeature.FT].Length);
            double std_len = dataset.Sessions.StandardDeviation(s => s.Features[TypingFeature.FT].Length);
            log.Info("  Avg. length {0} keystrokes +/- {1} (min {2}, max {3})", 
                Math.Round(avg_len, 1), Math.Round(std_len,1),
                dataset.Sessions.Min(s => s.Features[TypingFeature.FT].Length),
                dataset.Sessions.Max(s => s.Features[TypingFeature.FT].Length)
                );

            DateTime min_date = dataset.Sessions.Min(s => s.Timestamp).Date;
            DateTime max_date = dataset.Sessions.Max(s => s.Timestamp).Date;

            log.Info("  Spanning from {0} to {1} ({2}).", 
                min_date.ToShortDateString(), max_date.ToShortDateString(),                

                (max_date - min_date).TotalDays > 730 
                ? 
                    Math.Round((max_date - min_date).TotalDays / 365).ToString() + " years"
                :
                    Math.Round((max_date - min_date).TotalDays).ToString() + " days"
                );


            var users = dataset.Sessions.Select(s => s.User).Distinct();
            int count_male = users.Where(u => u.Gender == Gender.Male).Count();
            int count_fema = users.Where(u => u.Gender == Gender.Female).Count();
            int count_unkn = users.Where(u => u.Gender == Gender.Unknown).Count();

            log.Info("  {0} users, {1} male ({2}%), {3} female ({4}%), {5} unknown ({6}%).",
                users.Count(),
                count_male, Math.Round(100.0 * count_male / users.Count()),
                count_fema, Math.Round(100.0 * count_fema / users.Count()),
                count_unkn, Math.Round(100.0 * count_unkn / users.Count())
                );

            var su = dataset.Sessions.GroupBy(s => s.User.UserID);
            log.Info("  {0} +/- {1} average sessions per user (min {2}, max {3})",
                Math.Round(su.Average(kv => kv.Count()),2),
                Math.Round(su.StandardDeviation(kv => kv.Count()),2),
                su.Min(kv => kv.Count()),
                su.Max(kv => kv.Count())
            );
        }
    }
}
