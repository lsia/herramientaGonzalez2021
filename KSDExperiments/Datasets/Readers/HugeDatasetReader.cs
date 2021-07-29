using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using KSDExperiments.Datasets;


namespace KSDExperiments.Datasets.Readers
{
    class HugeDatasetReader : IDatasetReader
    {
        void ProcessUser(string[] lines, List<Session> sessions)
        {
            int last_user_id = int.MinValue;
            string last_sentence = null;
            long last_dt = long.MinValue;
            List<int> hts = new List<int>();
            List<int> fts = new List<int>();
            List<byte> vks = new List<byte>();

            int session_id = 1;
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split('\t');

                int user_id = int.Parse(fields[0]);
                if (last_user_id == int.MinValue)
                {
                    User user = User.GetUser(user_id, "USER" + user_id, DateTime.MinValue, Gender.Unknown);
                }
                else if (user_id != last_user_id)
                {
                    int k = 9;
                }                
                
                string sentence = fields[2];
                if (last_sentence != null && last_sentence != sentence)
                {
                    int id = 100 * user_id + session_id;
                    Session s = new Session(id, User.GetUser(user_id), DateTime.MinValue, "", vks.ToArray(), hts.ToArray(), fts.ToArray());
                    sessions.Add(s);    

                    session_id++;
                    last_dt = long.MinValue;
                    hts.Clear();
                    fts.Clear();
                    vks.Clear();
                }

                if (fields[2] != fields[3])
                {
                    int k = 9;
                }

                long dt = 0;
                if (!long.TryParse(fields[4], System.Globalization.NumberStyles.Float, null, out dt))
                {
                    int k = 9;
                }

                long ut = 0;
                if (!long.TryParse(fields[5], System.Globalization.NumberStyles.Float, null, out ut))
                {
                    int k = 9;
                }
                
                
                long ht = ut - dt;
                long ft = last_dt == long.MinValue ? long.MinValue : dt - last_dt;

                last_dt = dt;
                last_sentence = sentence;
                last_user_id = user_id;

                int vk = 0;
                if (fields.Length < 9)
                {
                    i++;
                    vk = int.Parse(lines[i].Trim());
                }
                else if (!int.TryParse(fields[8], out vk))
                {
                    if (fields[8] == "")
                        vk = (int) VirtualKeys.VK_TAB;
                    else
                    {
                        int k = 9;
                    }
                }

                vks.Add((byte)vk);
                hts.Add((int)ht);
                if (ft == long.MinValue)
                    fts.Add(int.MinValue);
                else
                    fts.Add((int) ft);
            }

            int tid = 100 * last_user_id + session_id;
            Session ss = new Session(tid, User.GetUser(last_user_id), DateTime.MinValue, "", vks.ToArray(), hts.ToArray(), fts.ToArray());
            sessions.Add(ss);
        }

        public Dataset ReadDataset(string filename, string dataset_name, string dataset_source)
        {
            List<Session> sessions = new List<Session>();
            DirectoryInfo di = new DirectoryInfo(filename);

            int count = 0;
            foreach (var file in di.GetFiles())
                if (!file.FullName.Contains("metadata") && !file.FullName.Contains("readme"))
                {
                    count++;
                    string[] lines = File.ReadAllLines(file.FullName);

                    try
                    {
                        ProcessUser(lines, sessions);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine();
                        Console.WriteLine(file.FullName);
                        Console.WriteLine(ex.Message + ex.StackTrace);
                        Console.WriteLine();
                    }

                    if ((count % 1000) == 0)
                        Console.Write(".");
                }

            Console.WriteLine();
            return new Dataset("HUGE", "HUGE", "HUGE", sessions.ToArray());
        }
    }
}
