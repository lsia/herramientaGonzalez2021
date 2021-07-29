using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Reflection;

using NLog;

using KSDExperiments.Configuration;
using KSDExperiments.Datasets;
using KSDExperiments.Latex;


namespace KSDExperiments.Pipelines.Stages
{
    class UserSessionsReport : PipelineStage
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        public UserSessionsReport(Pipeline pipeline, PipelineStageConfigurationElement configuration)
            : base(pipeline, configuration)
        {
        }

        public override void Initialize()
        {
            DirectoryInfo folder = new DirectoryInfo(OUTPUT_FOLDER);
            if (!folder.Exists)
                folder.Create();
        }

        void PlotSession(Session s, StringBuilder sb)
        {
            int ymax = 0;

            StringBuilder sb_ht = new StringBuilder();
            StringBuilder sb_ft = new StringBuilder();
            for (int i = 0; i < s.VKs.Length; i++)
            {
                sb_ht.Append("(");
                sb_ht.Append(i);
                sb_ht.Append(",");
                sb_ht.Append(s.Features[TypingFeature.HT][i]);
                sb_ht.Append(")");
                if (s.Features[TypingFeature.HT][i] > ymax)
                    ymax = s.Features[TypingFeature.HT][i];

                int[] FTs = s.Features[TypingFeature.FT];
                sb_ft.Append("(");
                sb_ft.Append(i);
                sb_ft.Append(",");
                sb_ft.Append(FTs[i]);
                sb_ft.Append(")");
                if (FTs[i] > ymax)
                    ymax = FTs[i];
            }

            LatexDocument doc = new Latex.LatexDocument("Templates/SESSION.template", "SESSION");
            doc.Replace("xmax", s.VKs.Length.ToString());
            // doc.Replace("ymax", ymax.ToString());
            doc.Replace("ymax", (500).ToString());
            doc.Replace("hts", sb_ht.ToString());
            doc.Replace("fts", sb_ft.ToString());

            doc.Compile(false);

            string filename = "SESSION-" + s.User.UserID + "-" + s.ID + ".pdf";
            if (File.Exists("Output/" + filename))
                File.Delete("Output/" + filename);

            File.Move("Output/SESSION.pdf", "OUTPUT/" + filename);

            sb.AppendLine(@"\includegraphics[width=\textwidth]{" + filename + "}");
        }

        const string OUTPUT_FOLDER = "Output";

        protected override void DoRun(Results results)
        {
            log.Info("Generating user sessions reports...");
            IndentLayoutRenderer.Add();

            Dataset dataset = results.Datasets[0];

            log.Info("Removing old files...");
            DirectoryInfo folder = new DirectoryInfo(OUTPUT_FOLDER);
            foreach (FileInfo fi in folder.GetFiles())
                if (fi.Extension == ".pdf" &&
                     (fi.Name.StartsWith("SESSION") || fi.Name.StartsWith("USER")))
                {
                    fi.Delete();
                }

            log.Info("Generating reports...");
            var sessions_per_user = dataset.Sessions.GroupBy(s => s.User.UserID);
            foreach (var user_sessions in sessions_per_user)
            {
                log.Info("USER {0}", User.GetUser(user_sessions.Key).Name);

                StringBuilder sb_sessions = new StringBuilder();
                foreach (Session s in user_sessions)
                    PlotSession(s, sb_sessions);

                LatexDocument doc = new LatexDocument("Templates/USER.template", "USER");
                doc.Replace("user", User.GetUser(user_sessions.Key).Name);
                doc.Replace("sessions", sb_sessions.ToString());
                doc.Compile(false);

                string filename = "USER-" + user_sessions.Key + ".pdf";
                if (File.Exists("Output/" + filename))
                    File.Delete("Output/" + filename);

                File.Move("Output/USER.pdf", "Output/" + filename);
            }

            IndentLayoutRenderer.Remove();
        }
    }
}
