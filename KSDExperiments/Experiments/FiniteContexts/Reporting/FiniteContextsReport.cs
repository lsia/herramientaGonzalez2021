using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using KSDExperiments.Configuration;
using KSDExperiments.Datasets;
using KSDExperiments.Reports;
using KSDExperiments.Util;
using KSDExperiments.FiniteContexts.Features;

using NLog;


namespace KSDExperiments.Experiments.FiniteContexts.Reporting
{
    public class FiniteContextsReport
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        SessionReportConfigurationSection section;
        public LatexReport LatexReport
        {
            get; private set;
        }

        public FiniteContextsExperiment Experiment { get; private set; }
        public FiniteContextsReport(FiniteContextsExperiment experiment)
        {
            Experiment = experiment;
            section = SessionReportConfigurationSection.GetSection();
        }

        ARFF arff;
        ARFF arff_legitimate;
        ARFF arff_impostor;
        ARFF StartARFF(string header, int user_id)
        {
            ARFF arff = new ARFF("Output/" + header + "." + user_id);
            arff.StartHeaders();
            foreach (var kv in Experiment.Parameters.NumericAttributes)
                arff.AddNumericAttribute(kv.Key);

            arff.StartData();
            return arff;
        }

        int user_id;
        public void Start(int user_id)
        {
            this.user_id = user_id;
            arff = StartARFF("USER", user_id);
            arff_legitimate = StartARFF("USER.LEG", user_id);
            arff_impostor = StartARFF("USER.IMP", user_id);
            err_value.Clear();
        }

        StringBuilder sb_sessions = new StringBuilder();
        StringBuilder sb_sessions_legitimate = new StringBuilder();
        StringBuilder sb_sessions_impostor = new StringBuilder();
        bool has_session_headers = false;

        void AppendSession(Session session, Dictionary<string, double> numeric_attribute_values, double[] values, string cls)
        {
            string line;
            StringBuilder sb_line = new StringBuilder();
            if (!has_session_headers)
            {
                has_session_headers = true;
                sb_line.Append("SessionID");
                foreach (var kv in numeric_attribute_values)
                {
                    sb_line.Append(",");
                    sb_line.Append(kv.Key);
                }
                sb_line.Append(",CLASS");

                line = sb_line.ToString();
                sb_sessions.AppendLine(line);
                sb_sessions_legitimate.AppendLine(line);
                sb_sessions_impostor.AppendLine(line);
            }

            sb_line = new StringBuilder();
            sb_line.Append(session.ID);
            sb_line.Append(",");
            sb_line.Append(values[0]);
            for (int i = 1; i < values.Length; i++)
            {
                sb_line.Append(",");
                sb_line.Append(values[i]);
            }
            sb_line.Append(",");
            sb_line.Append(cls);

            line = sb_line.ToString();
            sb_sessions.AppendLine(line);
            if (cls == "legitimate") sb_sessions_legitimate.AppendLine(line);
            if (cls == "impostor") sb_sessions_impostor.AppendLine(line);
        }

        void PrintSessionAttributes(string header, Session session, Dictionary<string, double> numeric_attribute_values)
        {
            if (!section.ShowSessionAttributes)
                return;

            StringBuilder sb = new StringBuilder();
            foreach (var kv in numeric_attribute_values)
            {
                sb.Append(kv.Key);
                sb.Append("=");
                sb.Append(Math.Round(kv.Value, 2).ToString("0.00"));
                sb.Append(", ");
            }

            log.Info("  {0} SESSION {1}: {2}", header, session.ID, sb);
        }

        public void StartSession(Session session)
        {
            if (section.CreateLatexReport)
                LatexReport = new LatexReport(session);
        }

        public void EndSession()
        {
            if (section.CreateLatexReport)
                LatexReport.Compile();
        }

        public void OnLegitimateSession(Session session, Dictionary<string, double> numeric_attribute_values)
        {
            PrintSessionAttributes("LEG", session, numeric_attribute_values);

            double[] values = numeric_attribute_values.Values.ToArray();
            arff.AppendData(values);
            arff.AppendCategory("legitimate");
            arff_legitimate.AppendData(values);
            arff_legitimate.AppendCategory("legitimate");

            AppendSession(session, numeric_attribute_values, values, "legitimate");
            EndSession();
        }

        public void OnImpostorSession(Session session, Dictionary<string, double> numeric_attribute_values)
        {
            LatexReport = new LatexReport(session);
            PrintSessionAttributes("LEG", session, numeric_attribute_values);

            double[] values = numeric_attribute_values.Values.ToArray();
            arff.AppendData(values);
            arff.AppendCategory("impostor");
            arff_impostor.AppendData(values);
            arff_impostor.AppendCategory("impostor");

            AppendSession(session, numeric_attribute_values, values, "impostor");
            EndSession();
        }

        static bool eer_method_headers_filled = false;
        static List<string> eer_method = new List<string>();
        List<double> err_value = new List<double>();

        static object giant_lock = new object();
        static Dictionary<int, double[]> eers_per_user = new Dictionary<int, double[]>();

        StringBuilder sb = new StringBuilder();
        StringBuilder sb_err = new StringBuilder();
        public void OnEERFound(string method, double eer, double eer_threshold)
        {
            //
            // TODO: Corregir esta negrada. Enviar los métodos primero, acá hay 
            // posible condición de carrera.
            //
            if (!eer_method_headers_filled)
                eer_method.Add(method);

            err_value.Add(eer);

            sb.Append(method);
            sb.Append("=");
            sb.Append(Math.Round(100.0 * eer, 2).ToString().PadLeft(2));
            sb.Append("%/");
            sb.Append(Math.Round(eer_threshold, 2).ToString().PadRight(4));
            sb.Append(", ");

            sb_err.Append(method);
            sb_err.Append(",");
            sb_err.Append(100.0 * eer);
            sb_err.Append("%,");
            sb_err.Append(eer_threshold);
            sb_err.AppendLine();
        }

        public static void SummarizeEERs()
        {
            log.Info("EER summary by method...");

            StringBuilder sb = new StringBuilder();
            string[] err_method_names = eer_method.ToArray();
            for (int i = 0; i < err_method_names.Length; i++)
            {
                var values = eers_per_user.Select(kv => kv.Value[i]);
                log.Info("    {0,10} {1:00.00}% +/- {2:00.00} (min {3:00.00}, max {4:00.00})",
                    err_method_names[i],
                    100.0 * values.Average(),
                    100.0 * values.StandardDeviation(),
                    100.0 * values.Min(),
                    100.0 * values.Max()
                    );

                sb.Append(err_method_names[i]);
                sb.Append(",");
                sb.Append(values.Average());
                sb.Append(",");
                sb.Append(values.StandardDeviation());
                sb.Append(",");
                sb.Append(values.Min());
                sb.Append(",");
                sb.Append(values.Max());
                sb.AppendLine();
            }

            StreamWriter sw = File.CreateText("Output/ERR.csv");
            sw.Write(sb.ToString());
            sw.Close();
        }

        public void Close()
        {
            log.Info(" USER {0,10} - {1}", user_id, sb);
            eer_method_headers_filled = true;
            arff.Close();
            arff_legitimate.Close();
            arff_impostor.Close();

            if (sb_err.Length != 0)
            {
                StreamWriter sw = File.CreateText("Output/EER." + user_id + ".csv");
                sw.Write(sb_err.ToString());
                sw.Close();
            }

            if (sb_sessions.Length != 0)
            {
                StreamWriter sw = File.CreateText("Output/SESSIONS." + user_id + ".csv");
                sw.Write(sb_sessions.ToString());
                sw.Close();
            }

            if (sb_sessions_legitimate.Length != 0)
            {
                StreamWriter sw = File.CreateText("Output/SESSIONS.LEG." + user_id + ".csv");
                sw.Write(sb_sessions_legitimate.ToString());
                sw.Close();
            }

            if (sb_sessions_impostor.Length != 0)
            {
                StreamWriter sw = File.CreateText("Output/SESSIONS.IMP." + user_id + ".csv");
                sw.Write(sb_sessions_impostor.ToString());
                sw.Close();
            }

            double[] user_eers = err_value.ToArray();
            lock (giant_lock)
                eers_per_user.Add(user_id, user_eers);

            if (section.CreateLatexReport)
                LatexReport.Compile();
        }
    }
}
