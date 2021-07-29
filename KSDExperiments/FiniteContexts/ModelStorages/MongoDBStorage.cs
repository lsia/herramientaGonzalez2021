using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using System.Data.SqlClient;

using NLog;

/*
using MongoDB;
using MongoDB.Bson;
using MongoDB.Libmongocrypt;
*/

using KSDExperiments.Datasets;
using KSDExperiments.FiniteContexts.Models;
using KSDExperiments.FiniteContexts.Models.Directionality;
using KSDExperiments.FiniteContexts.Models.Histogram;


namespace KSDExperiments.FiniteContexts.ModelStorages
{
    class MongoDBStorage : ModelStorage
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        public MongoDBStorage(User user, string name, TypingFeature feature, int max_context_order, int max_ngram_order, IModelFactory<Model> factory)
            : base(user, name, feature, max_context_order, max_ngram_order, factory)
        {
        }


        public override void Initialize()
        {
        }

        string GetKey(ulong context, int context_order)
        {
            StringBuilder key = new StringBuilder();
            key.Append(User.UserID);
            key.Append("-");
            key.Append(Feature);
            key.Append("-");
            key.Append(Name);
            key.Append("-");
            key.Append(context_order);
            key.Append("-");
            key.Append(context.ToString("X").PadLeft(8,'0'));

            return key.ToString();
        }

        Dictionary<int, Dictionary<ulong, Model>> pending_models = new Dictionary<int, Dictionary<ulong, Model>>();

        static int count_queries = 0;
        static int last_check_count_queries = 0;
        static DateTime last_count_queries = DateTime.MinValue;

        public override Model GetModel(bool create, int context_order, int ngram_order, ulong model_hash)
        {
            throw new NotImplementedException();
        }

        public override Model[] GetBulk(int context_order, ulong[] model_hash)
        {
            /*
            IBatch batch = db.CreateBatch();

            Dictionary<string, Task<RedisValue>> tasks = new Dictionary<string, Task<RedisValue>>();
            for (int i = 0; i < model_hash.Length; i += (MaxContextOrder + 1))
                for (int j = 0; j <= MaxContextOrder; j++)
                {
                    ulong current_hash = model_hash[i + j];

                    string key = GetKey(current_hash, j);
                    if (!tasks.ContainsKey(key))
                        tasks.Add(key, batch.StringGetAsync(key));
                }

            batch.Execute();
            Model[] retval = new Model[model_hash.Length];
            for (int i = 0; i < model_hash.Length; i += (MaxContextOrder + 1))
                for (short j = 0; j <= MaxContextOrder; j++)
                {
                    ulong current_hash = model_hash[i + j];

                    string key = GetKey(current_hash, j);
                    tasks[key].Wait();

                    byte[] blob = tasks[key].Result;
                    retval[i + j] = CreateModel(current_hash, j, blob);
                }

            Console.Write("B");
            return retval;
            */

            throw new NotImplementedException();    
        }

        class PendingFeed
        {
            public PendingFeed(string key, ulong model_hash, int context_order, int[] parameter_values, int pos)
            {
                Key = key;
                ModelHash = model_hash;
                ContextOrder = (short) context_order;
                ParameterValues = parameter_values;
                Pos = pos;
            }

            public string Key { get; private set; }
            public ulong ModelHash { get; private set; }
            public short ContextOrder { get; private set; }
            public int[] ParameterValues { get; private set; }
            public int Pos { get; private set; }
        }

        List<PendingFeed> pending_feeds = new List<PendingFeed>();

        public override void FeedModel(int context_order, ulong model_hash, int[] parameter_values, int pos)
        {
            string key = GetKey(model_hash, context_order);
            PendingFeed pending = new PendingFeed(key, model_hash, context_order, parameter_values, pos);
            pending_feeds.Add(pending);
        }

        Model CreateModel(ulong model_hash, short context_order, byte[] blob)
        {
            int ngram = (int)(model_hash >> 8 * 7);

            if (blob == null)
                return Factory.CreateModel(model_hash, context_order, ngram, 1);
            else
            {
                Model tmp = null;
                if (Name == "TD")
                    tmp = AvgStdevModelLinear.Deserialize(model_hash, context_order, ngram, 1, blob);
                else if (Name == "DIE")
                    tmp = SimpleExponentialDirectionalityModel.Deserialize(model_hash, context_order, ngram, 1, blob);
                else if (Name == "DIL")
                    tmp = SimpleLinearDirectionalityModel.Deserialize(model_hash, context_order, ngram, 1, blob);
                else if (Name == "HI")
                    tmp = HistogramModel.Deserialize(model_hash, context_order, ngram, 1, blob);
                else
                    throw new ArgumentException("Unrecognized model.");

                return tmp;
            }
        }

        public override void Save()
        {
            /*
            IBatch batch = db.CreateBatch();

            Dictionary<string, Task<RedisValue>> tasks = new Dictionary<string, Task<RedisValue>>();
            foreach (var pending in pending_feeds.Select(s => s.Key).Distinct())
                tasks.Add(pending, batch.StringGetAsync(pending));

            batch.Execute();

            Dictionary<string, Model> models = new Dictionary<string, Model>();
            foreach (var pending in pending_feeds)
            {
                if (!models.ContainsKey(pending.Key))
                {
                    tasks[pending.Key].Wait();

                    byte[] blob = (byte[]) tasks[pending.Key].Result;
                    models[pending.Key] = CreateModel(pending.ModelHash, pending.ContextOrder, blob);
                }

                models[pending.Key].Feed(pending.ParameterValues, pending.Pos);
            }

            batch = db.CreateBatch();
            foreach (var kv in models)
                batch.StringSetAsync(kv.Key, kv.Value.Serialize());

            batch.Execute();
            pending_feeds.Clear();
            Console.Write("S");
            */    
        }
    }
}
