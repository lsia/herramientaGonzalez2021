using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;

using NLog;


namespace KSDExperiments.Datasets.Readers
{
    class SqlServerDatasetReader : IDatasetReader
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        public Dataset ReadDataset(string parameters, string dataset_name, string dataset_source)
        {
            SqlConnection c = new SqlConnection();
            c.ConnectionString = ConfigurationManager.ConnectionStrings["DB"].ConnectionString;
            c.Open();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = c;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = @"
SELECT  IDUser, QualifiedUsername
FROM    AT_Users;
";

            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                User.GetUser((int)dr["IDUser"], (string)dr["QualifiedUsername"], DateTime.Now, Gender.Unknown);

            dr.Close();

            cmd.CommandText = @"
SELECT  *
FROM    LT_Sessions 
ORDER BY IDSession;
";

            List<Session> sessions = new List<Session>();
            dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                byte[] blob = (byte[])dr["Blob"];
                MemoryStream ms = new MemoryStream(blob);
                Session s = Session.Deserialize(ms);
                sessions.Add(s);
            }

            c.Close();
            return new Dataset("DB", "SqlServer", "N/A", sessions.ToArray());
        }
    }
}
