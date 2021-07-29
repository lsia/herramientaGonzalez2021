using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;
using System.Globalization;
using System.IO;

using CsvHelper;
using NLog;


namespace KSDExperiments.Datasets.Readers
{
    class ProsodyDatasetReader : IDatasetReader
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        const string CONFIG_KEY_PATH = "dataset.prosody.gay.path";

        const int MAX_SESSION_SIZE = 50000;

        public Dataset ReadGayGunDataset(string filename)
        {
            log.Info("Reading PROSODY dataset {0}...", Path.GetFileName(filename));
            StreamReader sr = new StreamReader(filename);
            CsvHelper.Configuration.CsvConfiguration cfg = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture);
            CsvReader csv = new CsvReader(sr, cfg);
            csv.Configuration.Delimiter = "\t";
            csv.Configuration.HasHeaderRecord = true;
            csv.Configuration.BadDataFound = null;

            int current_user_id = 1;
            Dictionary<string, int> user_ids = new Dictionary<string, int>();

            List<Session> sessions = new List<Session>();
            int session_id = 0;
            csv.Read();
            while (csv.Read())
            {
                session_id++;
                string user = csv.GetField(0);
                user = user.Replace("?", "");
                if (!user_ids.ContainsKey(user))
                {
                    user_ids.Add(user, current_user_id);
                    current_user_id++;
                }

                if (user == "A12WI1MRO70881")
                {
                    int k = 9;
                }

                string access_key = csv.GetField(1);
                string topic = csv.GetField(2);
                string opinion = csv.GetField(3);
                string date = csv.GetField(4);
                string type = csv.GetField(5);
                string task = csv.GetField(6);
                string group = csv.GetField(7);
                string flow = csv.GetField(8);
                string text = csv.GetField(9);
                string meta = csv.GetField(10);

                int last_vk = 0;
                int pos = 0;
                int[] vks = new int[MAX_SESSION_SIZE];
                int[] ft = new int[MAX_SESSION_SIZE];
                int[] ht = new int[MAX_SESSION_SIZE];

                Dictionary<int, long> last_keydown = new Dictionary<int, long>();
                Dictionary<int, int> last_keydown_pos = new Dictionary<int, int>();

                long last_keydown_time = 0;
                long cumulative_time = 0;

                string[] keys = meta.Trim().Split(';');
                for (int i = 0; i < keys.Length; i++)
                    if (keys[i] != "")
                    {
                        string[] fields = keys[i].Split(' ');

                        cumulative_time = long.Parse(fields[0]);
                        if (fields[1] == "KeyDown")
                        {
                            int vk = int.Parse(fields[2]);
                            if (vk != last_vk)
                            {
                                if (pos != 0)
                                    ft[pos] = (int)(cumulative_time - last_keydown_time);

                                vks[pos] = vk;
                                last_keydown_time = cumulative_time;
                                last_keydown[vk] = (int)cumulative_time;
                                last_keydown_pos[vk] = pos;
                                pos++;
                            }

                            last_vk = vk;
                        }
                        else if (fields[1] == "KeyUp")
                        {
                            int vk = int.Parse(fields[2]);
                            if (last_keydown.ContainsKey(vk))
                            {
                                long down_time = last_keydown[vk];
                                long current_ht = cumulative_time - down_time;
                                int current_pos = last_keydown_pos[vk];
                                ht[current_pos] = (int)current_ht;
                            }
                        }
                    }

                byte[] cut_vks = new byte[pos];
                for (int i = 0; i < pos; i++)
                    cut_vks[i] = (byte) vks[i];

                int[] cut_hts = new int[pos];
                Buffer.BlockCopy(ht, 0, cut_hts, 0, pos * sizeof(int));
                int[] cut_fts = new int[pos];
                Buffer.BlockCopy(ft, 0, cut_fts, 0, pos * sizeof(int));

                User u = User.GetUser(user_ids[user], user, DateTime.MinValue, Gender.Unknown);
                Session s = new Session(session_id, u, DateTime.Now, "", cut_vks, cut_hts, cut_fts);
                s.Properties.Add("task", task);
                sessions.Add(s);
            }

            string name = null;
            if (filename.Contains("Gay"))
                name = "GAY";
            else if (filename.Contains("Gun"))
                name = "GUN";
            else throw new ArgumentException("Invalid dataset filename.");

            return new Dataset(name, "PROSODY", filename, sessions.ToArray());
        }

        public Dataset ReadReviewDataset(string filename)
        {
            log.Info("Reading PROSODY dataset {0}...", Path.GetFileName(filename));
            StreamReader sr = new StreamReader(filename);

            CsvHelper.Configuration.CsvConfiguration cfg = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture);
            CsvReader csv = new CsvReader(sr, cfg);
            csv.Configuration.Delimiter = "\t";
            csv.Configuration.HasHeaderRecord = true;
            csv.Configuration.BadDataFound = null;

            int current_user_id = 1;
            Dictionary<string, int> user_ids = new Dictionary<string, int>();

            List<Session> sessions = new List<Session>();
            int session_id = 0;
            csv.Read();
            while (csv.Read())
            {
                session_id++;
                string user = csv.GetField(0);
                user = user.Replace("?", "");
                if (!user_ids.ContainsKey(user))
                {
                    user_ids.Add(user, current_user_id);
                    current_user_id++;
                }

                string access_key = csv.GetField(1);
                string review_date = csv.GetField(2);
                string review_topic = csv.GetField(3);
                string task = csv.GetField(4);
                string group = csv.GetField(5);
                string flow = csv.GetField(6);
                string restaurant = csv.GetField(7);
                string addr = csv.GetField(8);
                string site = csv.GetField(9);
                string text = csv.GetField(10);
                string meta = csv.GetField(11);

                int last_vk = 0;
                int pos = 0;
                int[] vks = new int[MAX_SESSION_SIZE];
                int[] ft = new int[MAX_SESSION_SIZE];
                int[] ht = new int[MAX_SESSION_SIZE];

                Dictionary<int, long> last_keydown = new Dictionary<int, long>();
                Dictionary<int, int> last_keydown_pos = new Dictionary<int, int>();

                long last_keydown_time = 0;
                long cumulative_time = 0;

                string[] keys = meta.Trim().Split(';');
                for (int i = 0; i < keys.Length; i++)
                    if (keys[i] != "")
                    {
                        string[] fields = keys[i].Split(' ');

                        cumulative_time = long.Parse(fields[0]);
                        if (fields[1] == "KeyDown")
                        {
                            int vk = int.Parse(fields[2]);
                            if (vk != last_vk)
                            {
                                if (pos != 0)
                                    ft[pos] = (int)(cumulative_time - last_keydown_time);

                                vks[pos] = vk;
                                last_keydown_time = cumulative_time;
                                last_keydown[vk] = (int)cumulative_time;
                                last_keydown_pos[vk] = pos;
                                pos++;
                            }

                            last_vk = vk;
                        }
                        else if (fields[1] == "KeyUp")
                        {
                            int vk = int.Parse(fields[2]);
                            if (last_keydown.ContainsKey(vk))
                            {
                                long down_time = last_keydown[vk];
                                long current_ht = cumulative_time - down_time;
                                int current_pos = last_keydown_pos[vk];
                                ht[current_pos] = (int)current_ht;
                            }
                        }
                    }

                byte[] cut_vks = new byte[pos];
                for (int i = 0; i < pos; i++)
                    cut_vks[i] = (byte)vks[i];

                int[] cut_hts = new int[pos];
                Buffer.BlockCopy(ht, 0, cut_hts, 0, pos * sizeof(int));
                int[] cut_fts = new int[pos];
                Buffer.BlockCopy(ft, 0, cut_fts, 0, pos * sizeof(int));

                User u = User.GetUser(user_ids[user], user, DateTime.MinValue, Gender.Unknown);
                Session s = new Session(session_id, u, DateTime.Now, "", cut_vks, cut_hts, cut_fts);
                s.Properties.Add("task", task);
                sessions.Add(s);
            }

            return new Dataset("REVIEW", "PROSODY", filename, sessions.ToArray());
        }

        public Dataset ReadDataset(string filename, string dataset_name, string dataset_source)
        {
            if (filename.Contains("Gun") || filename.Contains("Gay"))
                return ReadGayGunDataset(filename);
            else
                return ReadReviewDataset(filename);
        }
    }
}
