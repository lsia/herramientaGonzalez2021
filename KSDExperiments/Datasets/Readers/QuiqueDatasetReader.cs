using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;
using System.Globalization;
using System.IO;
using System.Windows.Input;

using NLog;


namespace KSDExperiments.Datasets.Readers
{
    class QuiqueDatasetReader : IDatasetReader
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        const string CONFIG_KEY_PATH = "dataset.quique.path";
        public Dataset ReadDataset(string filename, string dataset_name, string dataset_source)
        {
            log.Info("Reading QUIQUE dataset...");

            InitializeReverse();
            log.Info("  {0}", filename);

            string[] lines = File.ReadAllLines(filename);
            log.Info("  {0} lines", lines.Length);

            object giant_lock = new object();
            List<Session> retval = new List<Session>();
            Parallel.For(0, lines.Length, i =>
            // for (int i = 0; i < lines.Length; i++)
            {
                Session s = ProcessLine(lines[i]);
                lock (giant_lock)
                    retval.Add(s);

                if ((i % 1000) == 0)
                    Console.Write(".");
            }
            );

            Console.WriteLine();
            Session[] tmp = retval.ToArray();

            log.Info("CODES");
            foreach (int code in code_counts.Keys.OrderBy(k => k))
                log.Info("  " + code.ToString("X").PadLeft(2, '0') + "  " + code_counts[code]);

            log.Info("LOCATION");
            foreach (int location in location_counts.Keys.OrderBy(k => k))
                log.Info("  " + location.ToString("X").PadLeft(2, '0') + "  " + location_counts[location]);

            log.Info("Ready.");
            return new Dataset(Path.GetFileName(filename), "QUIQUE", filename, tmp);
        }

        const string ENCODING = "0124689qwertyuiopsdfgASDFGHJKLZX35 ahjklzxcvbnmQWERTYUIOPCVBNM7#";

        Dictionary<char, int> reverse = new Dictionary<char, int>();
        void InitializeReverse()
        {
            for (int i = 0; i < ENCODING.Length; i++)
                reverse.Add(ENCODING[i], i);

            reverse.Add('\"', -1);
        }

        int ReadNumber(string str, ref int offset)
        {
            int value = 0;
            int aux = 0;

            for (int i = 0; offset < str.Length && value < 32; i++, offset++)
            {
                char c = str[offset];
                value = reverse[c];
                if (value < 0)
                    return -1;

                aux |= (value & 31) << (5 * i);
            }

            return aux;
        }


        const int DOM_KEY_LOCATION_STANDARD = 0;
        const int DOM_KEY_LOCATION_LEFT = 1;
        const int DOM_KEY_LOCATION_RIGHT = 2;
        const int DOM_KEY_LOCATION_NUMPAD = 3;

        int weird_keys = 0;
        byte AddWeirdKey(int location, int code)
        {
            weird_keys++;
            if (code == 0xFF)
                return 0x00;

            return (byte) code;
        }

        int total_keys = 0;
        byte GetVK(int location, int code)
        {
            total_keys++;
            if (code >= 0x41 && code <= 0x5A)           // Alpha
            {
                if (location != 0)
                    return AddWeirdKey(location, code);
                else
                    return (byte)code;
            }
            else if (code >= 0x30 && code <= 0x39)      // Numeric
            {
                if (location == DOM_KEY_LOCATION_STANDARD)
                    return (byte)code;
                else if (location == DOM_KEY_LOCATION_NUMPAD)
                    return (byte)(code + 0x30);
                else
                    return AddWeirdKey(location, code);
            }
            else if (code >= 0x60 && code <= 0x69)      // Numeric keypad
            {
                if (location == DOM_KEY_LOCATION_NUMPAD)
                    return (byte)code;
                else
                    return AddWeirdKey(location, code);
            }
            else if (code == 0x10)                      // Shift 
            {
                if (location == DOM_KEY_LOCATION_LEFT)
                    return (byte) 0xA0;
                else if (location == DOM_KEY_LOCATION_RIGHT)
                    return (byte)0xA1;
                else
                    return AddWeirdKey(location, code);
            }
            else if (code == 0x11)                      // Control
            {
                if (location == DOM_KEY_LOCATION_LEFT)
                    return (byte)0xA2;
                else if (location == DOM_KEY_LOCATION_RIGHT)
                    return (byte)0xA3;
                else
                    return AddWeirdKey(location, code);
            }
            else if (code == 0x11)                      // Alt
            {
                if (location == DOM_KEY_LOCATION_LEFT)
                    return (byte)0xA2;
                else if (location == DOM_KEY_LOCATION_RIGHT)
                    return (byte)0xA3;
                else
                    return AddWeirdKey(location, code);
            }
            else if (code >= 0x25 && code <= 0x28)
            {
                if (location == DOM_KEY_LOCATION_STANDARD)
                    return (byte)code;
                else
                    return AddWeirdKey(location, code);
            }
            else
            {
                return AddWeirdKey(location, code);
            }
        }

        object giant_lock = new object();
        Dictionary<int, int> code_counts = new Dictionary<int, int>();
        Dictionary<int, int> location_counts = new Dictionary<int, int>();

        const int MAX_SESSION_SIZE = 50000;
        Session ProcessLine(string line)
        {
            // log.Info("-----------------------------------------------------------------------------");
            string[] fields = line.Replace("KHTML,", "KHTML;").Split(',');

            DateTime date = DateTime.ParseExact(fields[0] == "" ? fields[1] : fields[0], "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
            int user_id = int.Parse(fields[2]);
            int session_id = int.Parse(fields[3]);
            // log.Info(session_id);

            string data = fields[4];

            int pos = 0;
            int count = 0;
            while (count < 3)
            {
                if (data[pos] == '"')
                    count++;

                pos++;
            }

            string ks = data.Substring(pos);

            int offset = 0;
            int agent_size = ReadNumber(ks, ref offset);

            string agent = ks.Substring(offset, agent_size);
            offset += agent_size;

            pos = 0;
            byte[] vks = new byte[MAX_SESSION_SIZE];
            int[] ft = new int[MAX_SESSION_SIZE];
            int[] ht = new int[MAX_SESSION_SIZE];

            Dictionary<int, int> last_keydown = new Dictionary<int, int>();
            Dictionary<int, int> last_keydown_pos = new Dictionary<int, int>();

            int last_keydown_time = 0;
            int cumulative_time = 0;
            int time = 0;
            do
            {
                time = ReadNumber(ks, ref offset);
                if (time != -1)
                {
                    cumulative_time += time;
                    int key = ReadNumber(ks, ref offset);

                    bool up = (key & 1) == 0;
                    int code = (key >> 1) & 255;
                    int location = (key >> 9);

                    /*
                    lock (giant_lock)
                    {
                        if (!code_counts.ContainsKey(code))
                            code_counts.Add(code, 0);

                        if (!location_counts.ContainsKey(location))
                            location_counts.Add(location, 0);

                        code_counts[code]++;
                        location_counts[location]++;
                    }
                    */

                    byte vk = GetVK(location, code);
                    if (!up)
                    {
                        if (pos != 0)
                            ft[pos] = cumulative_time - last_keydown_time;

                        vks[pos] = vk;
                        last_keydown_time = cumulative_time;
                        last_keydown[vk] = cumulative_time;
                        last_keydown_pos[vk] = pos;
                        pos++;

                        /*
                        if (pos < 20)
                            log.Info("  D  {0:X3}  FT({1})={2}", vk, pos-1, ft[pos-1]);
                        */
                    }
                    else
                    {
                        if (last_keydown.ContainsKey(vk))
                        {
                            int down_time = last_keydown[vk];
                            int current_ht = cumulative_time - down_time;
                            int current_pos = last_keydown_pos[vk];
                            ht[current_pos] = current_ht;

                            /*
                            if (pos < 20)
                                log.Info("  U  {0:X3}  HT({1})={2}    ", vk, current_pos, current_ht);
                            */
                        }
                        /*
                        else
                            log.Info("  F  {0:X3}  {1,3}", vk, 0);
                        */
                    }
                }
            }
            while (time != -1);

            DateTime birth_date = DateTime.MinValue;
            if (fields[5] != "")
                DateTime.ParseExact(fields[5], "yyyy-MM-dd", CultureInfo.CurrentCulture);

            Gender gender = Gender.Unknown;
            if (fields[6] == "m")
                gender = Gender.Male;
            else if (fields[6] == "f")
                gender = Gender.Female;

            User user = User.GetUser(user_id, user_id.ToString(), birth_date, gender);

            byte[] cut_vks = new byte[pos];
            Buffer.BlockCopy(vks, 0, cut_vks, 0, pos);
            int[] cut_hts = new int[pos];
            Buffer.BlockCopy(ht, 0, cut_hts, 0, pos * sizeof(int));
            int[] cut_fts = new int[pos];
            Buffer.BlockCopy(ft, 0, cut_fts, 0, pos * sizeof(int));

            return new Session(session_id, user, date, agent, cut_vks, cut_hts, cut_fts);
        }
    }
}
