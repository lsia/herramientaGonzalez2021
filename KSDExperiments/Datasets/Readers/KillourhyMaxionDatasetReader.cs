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
    class KillourhyMaxionDatasetReader : IDatasetReader
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        string[] NormalizeLines(string[] lines)
        {
            List<string> retval = new List<string>();
            foreach (string line in lines)
            {
                string tmp = line.Trim();
                while (tmp.Contains("  "))
                    tmp = tmp.Replace("  ", " ");

                retval.Add(tmp);
            }

            return retval.ToArray();
        }

        byte StrToVK(string str)
        {
            string tmp = str;
            if (tmp.Contains("Shift."))
                tmp = tmp.Replace("Shift.", "");
            if (tmp.Contains("Caps_Lock."))
                tmp = tmp.Replace("Caps_Lock.", "");

            if (tmp.Length == 1)
                return (byte) tmp.ToUpper()[0];
            else if (tmp == "Shift")
                return 0x10;
            else if (tmp == "Remove")
                return 0x08;
            else if (tmp == "Tab")
                return 0x09;
            else if (tmp == "Caps_Lock")
                return 0x14;
            else if (tmp == "Return")
                return 0x0D;
            else if (tmp == "space")
                return 0x20;
            else if (tmp == "Delete")
                return 0x2E;
            else if (tmp == "apostrophe")
                return 0xDE;
            else if (tmp == "minus")
                return 0x6D;
            else if (tmp == "F12")
                return 0x7B;
            else if (tmp == "F13")
                return 0x7C;
            else if (tmp == "period")
                return 0xBE;
            else if (tmp == "comma")
                return 0xBC;
            else if (tmp == "zero")
                return 0x30;
            else if (tmp == "one")
                return 0x31;
            else if (tmp == "two")
                return 0x32;
            else if (tmp == "three")
                return 0x33;
            else if (tmp == "four")
                return 0x34;
            else if (tmp == "five")
                return 0x35;
            else if (tmp == "six")
                return 0x36;
            else if (tmp == "seven")
                return 0x37;
            else if (tmp == "eight")
                return 0x38;
            else if (tmp == "nine")
                return 0x39;
            else if (tmp == "semicolon")
                return 0xBA;
            else if (tmp == "bracketleft")
                return 0xDB;
            else if (tmp == "grave")
                return 0xDC;
            else if (tmp == "bracketright")
                return 0xDD;
            else if (tmp == "slash")
                return 0xBF;
            else if (tmp == "Left")
                return 0x25;
            else if (tmp == "Up")
                return 0x26;
            else if (tmp == "Right")
                return 0x27;
            else if (tmp == "Down")
                return 0x28;
            else if (tmp == "Select")
                return 0x29;
            else if (tmp == "equal")
                return 0xBB;
            else if (tmp == "Control")
                return 0x11;
            else if (tmp == "backslash")
                return 0xE2;
            else
                throw new ArgumentException("INVALID CHARACTER " + str);
        }

        public Dataset ReadDataset(string filename, string dataset_name, string dataset_source)
        {
            string[] lss = NormalizeLines(File.ReadAllLines(filename + "\\SessionMap.txt"));
            string[] lht = NormalizeLines(File.ReadAllLines(filename + "\\TimingFeatures-HOLD.txt"));
            string[] lft = NormalizeLines(File.ReadAllLines(filename + "\\TimingFeatures-DD.txt"));

            List<Session> sessions = new List<Session>();
            for (int i = 1; i < lss.Length; i++)
            {
                string[] fields = lss[i].Split(' ');

                string user = fields[0];
                string sid = fields[1];

                string type = null;
                if (fields[fields.Length - 1].Contains("Free"))
                    type = "free";
                else if (fields[fields.Length - 1].Contains("Trans"))
                    type = "transcribed";
                else
                    throw new ArgumentException("BAD");

                string filter = user + " " + sid;
                string[] lhts = lht.Where(l => l.StartsWith(filter)).ToArray();
                string[] lfts = lft.Where(l => l.StartsWith(filter)).ToArray();

                List<byte> vks = new List<byte>();
                List<int> hts = new List<int>();
                List<int> fts = new List<int>();
                fts.Add(0);

                for (int j = 0; j < lhts.Length; j++)
                {
                    string[] fh = lhts[j].Split(' ');
                    vks.Add(StrToVK(fh[4]));
                    hts.Add((int)(1000.0 * double.Parse(fh[5])));

                    if (j != lhts.Length - 1)
                    {
                        string[] ff = lfts[j].Split(' ');
                        if (fh[4] != ff[4])
                            throw new ArgumentException("BLGBLG");
                        fts.Add((int)(1000.0 * double.Parse(ff[6])));
                    }
                }

                User u = User.GetUser(1000 + int.Parse(user.Substring(1)), user, DateTime.Now, Gender.Unknown);
                Session s = new Session(i, u,
                    DateTime.Now, "", vks.ToArray(), hts.ToArray(), fts.ToArray());

                s.Properties.Add("task", type);
                sessions.Add(s);
                // Console.WriteLine(user + "/" + sid);
            }

            return new Dataset("KM", "KM", "KM", sessions.ToArray());
        }
    }
}
