using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using KSDExperiments.Datasets;
using KSDExperiments.FiniteContexts.Models;
using KSDExperiments.Latex;
using KSDExperiments.Util;


namespace KSDExperiments.Reports
{
    public class LatexReport
    {
        public Session Session { get; private set; }
        public LatexReport(Session session)
        {
            Session = session;
        }

        string GetLatexSessionText(Session session)
        {
            int partition_pos = 0;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < session.VKs.Length; i++)
            {
                bool partition = false;
                if (partition_pos < session.PartitionOffsets.Length && session.PartitionOffsets[partition_pos] == i)
                {
                    partition = true;
                    partition_pos++;
                }

                if (partition)
                    sb.Append(@"{\colorbox{blue!30}{{");

                int vk = session.VKs[i];

                string translated = KSDConvert.GetLatexFromVK(vk);
                sb.Append(translated);

                if (partition)
                    sb.Append(@"}}}");
            }

            return sb.ToString();
        }

        string MakePlotArray(int[] arr, int offset, int size)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < size && (i + offset) < arr.Length; i++)
            {
                sb.Append("(");
                sb.Append((double) i + 0.5);
                sb.Append(",");
                if (arr[i + offset] < 0)
                    sb.Append("0");
                else
                    sb.Append(arr[i + offset]);

                sb.Append(")");
            }

            return sb.ToString();
        }

        string MakePlotArray(double[] arr, int offset, int size)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < size && (i + offset) < arr.Length; i++)
            {
                sb.Append("(");
                sb.Append((double) i + 0.5);
                sb.Append(",");
                if (arr[i + offset] < 0)
                    sb.Append("0");
                else
                    sb.Append(Math.Round(arr[i + offset]));

                sb.Append(")");
            }

            return sb.ToString();
        }

        string MakePlotShortArray(int[] arr, int offset, int size, bool add_last_mock)
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;
            for (i = 0; i < size && (i + offset) < arr.Length; i++)
            {
                sb.Append("(");
                sb.Append(i);
                sb.Append(",");
                if (arr[i + offset] < 0)
                    sb.Append("0");
                else
                    sb.Append(arr[i + offset]);

                sb.Append(")");
            }

            if ( add_last_mock )
            {
                sb.Append("(");
                sb.Append(i);
                sb.Append(",0)");
            }

            return sb.ToString();
        }

        string MakePlotIntArray(int[] arr, int offset, int size)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < size && (i + offset) < arr.Length; i++)
                if ( arr[i + offset] != int.MinValue)
                {
                    sb.Append("(");
                    sb.Append((double) i + 0.5);
                    sb.Append(",");
                    if (arr[i + offset] < 0)
                        sb.Append("0");
                    else
                        sb.Append(arr[i + offset]);

                    sb.Append(")");
                }

            return sb.ToString();
        }

        string BuildObservationsPlotList(double[] arr, int offset, int size)
        {
            const string ADDPLOT_TEMPLATE = @"\addplot[color=blue, line width=2] coordinates 
		{ <%=observations%> }; 
";

            StringBuilder sb_full = new StringBuilder();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < size && (i + offset) < arr.Length; i++)
            {
                if (arr[i + offset] == int.MinValue)
                {
                    if ( sb.Length != 0 )
                    {
                        string tmp = ADDPLOT_TEMPLATE.Replace("<%=observations%>", sb.ToString());
                        sb_full.AppendLine(tmp);
                        sb.Clear();
                    }
                }
                else
                {
                    sb.Append("(");
                    sb.Append((double) i + 0.5);
                    sb.Append(",");
                    if (arr[i + offset] < 0)
                        sb.Append("0");
                    else
                        sb.Append(arr[i + offset]);

                    sb.Append(")");
                }
            }

            if (sb.Length != 0)
            {
                string tmp = ADDPLOT_TEMPLATE.Replace("<%=observations%>", sb.ToString());
                sb_full.AppendLine(tmp);
                sb.Clear();
            }

            return sb_full.ToString();
        }

        double[] AddVectorsScaled(double[] a, double[] b, double scale)
        {
            double[] retval = new double[a.Length];
            for (int i = 0; i < a.Length; i++)
                retval[i] = a[i] + scale * b[i];

            return retval;
        }

        int GetYMax(double[] timing, int start, int end)
        {
            int retval = 100;
            for ( int i = start; i < end && i < timing.Length; i++)
            {
                int tmp = (int) timing[i] / 100;
                if (100 * tmp > retval)
                    retval = 100 * tmp + 100;
            }

            retval += 100;
            if (retval > 500)
                retval = 500;

            return retval;
        }

        public const int PLOT_KEYSTROKES = 100;
        Dictionary<string, string> pattern_vector = new Dictionary<string, string>();
        public void AddVectorPattern(string name, TypingFeature feature, double[] timing, double[] avg, double[] stdev, int[] order)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < timing.Length; i += PLOT_KEYSTROKES)
            {
                string template = File.ReadAllText("Templates/SESSION_REPORT_PATTERN_VECTOR.template");
                template = template.Replace("<%=xmax%>", PLOT_KEYSTROKES.ToString());
                template = template.Replace("<%=ymax%>", GetYMax(timing, i , i + PLOT_KEYSTROKES).ToString());
                template = template.Replace("<%=pattern%>", MakePlotArray(avg, i, PLOT_KEYSTROKES));
                template = template.Replace("<%=sm1%>", MakePlotArray(AddVectorsScaled(avg, stdev, -1.0), i, PLOT_KEYSTROKES));
                template = template.Replace("<%=sm2%>", MakePlotArray(AddVectorsScaled(avg, stdev, -2.0), i, PLOT_KEYSTROKES));
                template = template.Replace("<%=sp1%>", MakePlotArray(AddVectorsScaled(avg, stdev, 1.0), i, PLOT_KEYSTROKES));
                template = template.Replace("<%=sp2%>", MakePlotArray(AddVectorsScaled(avg, stdev, 2.0), i, PLOT_KEYSTROKES));
                template = template.Replace("<%=orders%>", MakePlotShortArray(order, i, PLOT_KEYSTROKES, true));
                template = template.Replace("<%=observations%>", BuildObservationsPlotList(timing, i, PLOT_KEYSTROKES));

                StringBuilder sb_pattern_partitions = new StringBuilder();
                StringBuilder sb_orders_partitions = new StringBuilder();
                int[] partitions = Session.PartitionOffsets.Where(p => p >= i && p < i + PLOT_KEYSTROKES).ToArray();
                foreach ( int partition in partitions )
                {
                    // int x = 10 * (partition - i);
                    int x = partition - i;
                    sb_pattern_partitions.AppendLine(@"		\fill[green!30] (" + x + ",0) rectangle (" + (x + 1) + ",500);");
                    sb_orders_partitions.AppendLine(@"		\fill[green!30] (" + x + ",0) rectangle (" + (x + 1) + ",700);");
                }

                template = template.Replace("<%=orders_partitions%>", sb_orders_partitions.ToString());
                template = template.Replace("<%=pattern_partitions%>", sb_pattern_partitions.ToString());

                sb.AppendLine(template);
            }

            pattern_vector.Add(name, sb.ToString());
        }

        int max_candidates = int.MinValue;
        StringBuilder sb_pvd_ft = new StringBuilder();
        StringBuilder sb_pvd_ht = new StringBuilder();
        public void OnModelChosen(TypingFeature feature, Session session, int pos, ulong context, AvgStdevModel[] candidates, AvgStdevModel chosen)
        {
            StringBuilder sb_pvd = (feature == TypingFeature.HT ? sb_pvd_ht : sb_pvd_ft);
            if (pos == 0 || session.PartitionOffsets.Contains(pos))
                sb_pvd.AppendLine(@"\rowcolor{gray}");

            sb_pvd.Append(pos);
            sb_pvd.Append(" & ");
            sb_pvd.Append(KSDConvert.GetLatexFromVK(session.VKs[pos]));
            sb_pvd.Append(" & ");
            sb_pvd.Append(session.VKs[pos].ToString("X").PadLeft(2,'0'));
            sb_pvd.Append(" & ");
            sb_pvd.Append(context.ToString("X").PadLeft(2,'0'));

            for (int i = 0; i < candidates.Length; i++)
            {
                sb_pvd.Append(" & ");

                if (candidates[i] == null)
                    sb_pvd.Append(" X ");
                else
                {
                    if (candidates[i] == chosen)
                        sb_pvd.Append(@"\textbf{");

                    sb_pvd.Append(Math.Round(candidates[i].Average));
                    sb_pvd.Append("/");
                    sb_pvd.Append(Math.Round(candidates[i].StandardDeviation));
                    sb_pvd.Append("/");
                    sb_pvd.Append(candidates[i].Count);

                    if (candidates[i] == chosen)
                        sb_pvd.Append(@"}");
                }
            }

            max_candidates = Math.Max(max_candidates, candidates.Length);

            sb_pvd.AppendLine(@" \\ ");
        }

        string BuildPatternVectorTable(StringBuilder sb_pvd)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"\begin{longtable}{|c|c|c|c|");
            for (int i = 0; i < max_candidates; i++)
                sb.Append("c");

            sb.AppendLine(@"|}");
            sb.AppendLine(sb_pvd.ToString());
            sb.AppendLine(@"\end{longtable}");
            return sb.ToString();
        }

        public void Compile()
        {
            int ymax = 0;

            int[] HTs = Session.Features[TypingFeature.HT];
            int[] FTs = Session.Features[TypingFeature.FT];

            StringBuilder sb_ht = new StringBuilder();
            StringBuilder sb_ft = new StringBuilder();
            for (int i = 0; i < Session.VKs.Length; i++)
            {
                sb_ht.Append("(");
                sb_ht.Append(i);
                sb_ht.Append(",");
                sb_ht.Append(HTs[i]);
                sb_ht.Append(")");
                if (HTs[i] > ymax)
                    ymax = HTs[i];

                if (FTs[i] != int.MinValue)
                {
                    sb_ft.Append("(");
                    sb_ft.Append(i);
                    sb_ft.Append(",");
                    sb_ft.Append(FTs[i]);
                    sb_ft.Append(")");
                    if (FTs[i] > ymax)
                        ymax = FTs[i];
                }
            }

            LatexDocument doc = new Latex.LatexDocument("Templates/SESSION_REPORT.template", "SESSION_REPORT");
            doc.Replace("user", Session.User.Name);
            doc.Replace("gender", Session.User.Gender.ToString());
            doc.Replace("id", Session.ID.ToString());

            doc.Replace("hts", sb_ht.ToString());
            doc.Replace("fts", sb_ft.ToString());
            doc.Replace("xmax", Session.VKs.Length.ToString());
            doc.Replace("ymax", (1000).ToString());

            doc.Replace("text_raw", Session.GetSessionText());
            doc.Replace("text", GetLatexSessionText(Session));

            StringBuilder sb = new StringBuilder();
            foreach (var kv in pattern_vector)
            {
                sb.Append(@"\section{Pattern vector - ");
                sb.Append(kv.Key);
                sb.Append("}");
                sb.AppendLine();
                sb.AppendLine(kv.Value);
                sb.AppendLine();
            }

            // doc.Replace("pattern_vector_ht", pattern_vector[TypingFeature.HT]);
            // doc.Replace("pattern_vector_ft", pattern_vector[TypingFeature.FT]);
            // doc.Replace("pattern_vector_details_ht", BuildPatternVectorTable(sb_pvd_ht));
            // doc.Replace("pattern_vector_details_ft", BuildPatternVectorTable(sb_pvd_ft));

            doc.Replace("pattern_vectors", sb.ToString());
            doc.Compile("SESSION." + Session.User.UserID + "." + Session.ID);
        }
    }
}
