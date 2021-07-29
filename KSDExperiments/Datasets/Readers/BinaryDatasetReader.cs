using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Compression;

using NLog;


namespace KSDExperiments.Datasets.Readers
{
    public class BinaryDatasetReader : IDatasetReader
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        public Dataset ReadDataset(string filename, string dataset_name, string dataset_source)
        {
            log.Info("Reading binary dataset {0}...", dataset_name);

            List<Session> sessions = new List<Session>();
            FileStream fs = File.OpenRead(filename);
            GZipStream zip = new GZipStream(fs, CompressionMode.Decompress, false);

            // Read users
            BinaryReader reader = new BinaryReader(zip);
            int users_count = reader.ReadInt32();
            for ( int i = 0; i < users_count; i++ )
            {
                int id = reader.ReadInt32();
                Gender gender = (Gender) reader.ReadInt32();
                DateTime birth_date = new DateTime(reader.ReadInt64());
                string name = reader.ReadString();
                User user = User.GetUser(id, name, birth_date, gender);
            }

            // Read sessions
            while (fs.Position != fs.Length)
            {
                Session s = Session.Deserialize(zip);
                sessions.Add(s);
            }

            Session[] tmp = sessions.ToArray();
            log.Info("  Ready.");
            reader.Close();
            zip.Close();
            fs.Close();
            return new Dataset(dataset_name, dataset_source, filename, tmp);
        }
    }
}
