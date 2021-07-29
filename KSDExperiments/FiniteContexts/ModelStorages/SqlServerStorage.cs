using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using System.Data.SqlClient;

using KSDExperiments.Datasets;
using KSDExperiments.FiniteContexts.Models;
using KSDExperiments.FiniteContexts.Models.Directionality;
using KSDExperiments.FiniteContexts.Models.Histogram;


namespace KSDExperiments.FiniteContexts.ModelStorages
{
    class SqlServerStorage : ModelStorage
    {
        public SqlServerStorage(User user, string name, TypingFeature feature, int max_context_order, int max_ngram_order, IModelFactory<Model> factory)
            : base(user, name, feature, max_context_order, max_ngram_order, factory)
        {
        }

        public override Model GetModel(bool create, int context_order, int ngram_order, ulong model_hash)
        {
            throw new NotImplementedException();
        }

        public override Model[] GetBulk(int context_order, ulong[] model_hash)
        {
            ulong[] distinct = model_hash.Distinct().ToArray();
            StringBuilder sb = new StringBuilder();
            foreach (var hash in distinct)
            {
                if (sb.Length != 0) sb.Append(",");
                sb.Append(hash);
            }

            SqlConnection c = new SqlConnection();
            c.ConnectionString = "Server=.;Database=KSD;Integrated Security=SSPI;";
            c.Open();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = c;
            cmd.CommandText = @"
SELECT  ContextOrder, Context, Blob
FROM    AT_Models WITH (NOLOCK)
WHERE   [User] = " + User.UserID + @"
  AND   Feature = " + (Feature == TypingFeature.HT ? 1 : 2) + @"
  AND   ModelType = '" + Name + @"'
  AND   Context IN (" + sb.ToString() + ");";

            cmd.CommandType = CommandType.Text;

            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(ds);

            Dictionary<ulong, Model>[] models = new Dictionary<ulong, Model>[MaxContextOrder + 1];
            for (int i = 0; i <= MaxContextOrder; i++)
                models[i] = new Dictionary<ulong, Model>();

            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                ulong context = (ulong) (long) dr["Context"];
                short order_context = (short) (byte) dr["ContextOrder"];
                int ngram = (int) (context >> 8 * 7);
                byte[] blob = (byte[]) dr["Blob"];

                Model tmp = null;
                if (Name == "TD")
                    tmp = AvgStdevModelLinear.Deserialize(context, order_context, ngram, 1, blob);
                else if (Name == "DIE")
                    tmp = SimpleExponentialDirectionalityModel.Deserialize(context, order_context, ngram, 1, blob);
                else if (Name == "DIL")
                    tmp = SimpleLinearDirectionalityModel.Deserialize(context, order_context, ngram, 1, blob);
                else if (Name == "HI")
                    tmp = HistogramModel.Deserialize(context, order_context, ngram, 1, blob);
                else
                    throw new ArgumentException("Unrecognized model.");

                models[order_context].Add(context, tmp);
            }

            Model[] retval = new Model[model_hash.Length];
            for (int i = 0; i < model_hash.Length; i+= (MaxContextOrder + 1))
                for (int j = 0; j <= MaxContextOrder; j++)
                {
                    ulong current_hash = model_hash[i + j];

                    if (models[j].ContainsKey(current_hash))
                        retval[i + j] = models[j][current_hash];
                }

            return retval;
        }

        public override void Save()
        {
            int k = 9;
        }
    }
}
