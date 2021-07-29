using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using System.Data.SqlClient;

using NLog;
using StackExchange.Redis;

using KSDExperiments.Datasets;
using KSDExperiments.FiniteContexts.Models;
using KSDExperiments.FiniteContexts.Models.Directionality;
using KSDExperiments.FiniteContexts.Models.Histogram;


namespace KSDExperiments.FiniteContexts.ModelStorages
{
    class RedisStorage : ModelStorage
    {
        static Logger log = LogManager.GetCurrentClassLogger();

        ConnectionMultiplexer redis;
        IDatabase db;

        object lock_storage_names = new object();
        static Dictionary<string, int> storage_names = new Dictionary<string, int>();

        public RedisStorage(User user, string name, TypingFeature feature, int max_context_order, int max_ngram_order, IModelFactory<Model> factory)
            : base(user, name, feature, max_context_order, max_ngram_order, factory)
        {
            lock (lock_storage_names)
                if (!storage_names.ContainsKey(name))
                    storage_names.Add(name, storage_names.Count);
        }

        public override void Initialize()
        {
            redis = ConnectionMultiplexer.Connect("localhost");
            db = redis.GetDatabase();
        }

        public override bool IsPersistent { get { return true; } }

        class Key : IEquatable<Key>
        {
            public Key(byte[] binary)
            {
                Binary = binary;
            }

            public byte[] Binary { get; private set; }

            public override int GetHashCode()
            {
                int hash = 0;
                for (int i = 0; i < Binary.Length; i++)
                {
                    hash *= 31;
                    hash += Binary[i];
                }

                return hash;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Key);
            }

            public bool Equals(Key obj)
            {
                if (obj == null || obj.Binary.Length != Binary.Length)
                    return false;

                for (int i = 0; i < Binary.Length; i++)
                    if (Binary[i] != obj.Binary[i])
                        return false;

                return true;
            }
        }

        Key GetKey(ulong context, int context_order)
        {
            /*
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
            */

            int storage_id = storage_names[Name];

            byte[] retval = new byte[16];
            retval[0] = 0xF0;
            retval[1] = (byte) storage_id;
            retval[2] = (byte) (Feature == TypingFeature.HT ? 0 : 1);
            retval[3] = (byte) context_order;
            Array.Copy(BitConverter.GetBytes(User.UserID), 0, retval, 4, sizeof(int));
            Array.Copy(BitConverter.GetBytes(context), 0, retval, 8, sizeof(ulong));
            return new Key(retval);
        }

        /*
        byte[] GetKey(ulong context, int context_order)
        {
            int storage_id = storage_names[Name];

            byte[] retval = new byte[20];
            retval[0] = (byte) (0xF0 | storage_id);
            retval[1] = (byte)(
                (Feature == TypingFeature.HT ? (byte)0 : (byte)0xF0) |
                (byte)context_order);

            Array.Copy(BitConverter.GetBytes(User.UserID), 0, retval, 2, sizeof(int));
            Array.Copy(BitConverter.GetBytes(context), 0, retval, 2 + sizeof(int), sizeof(ulong));
            return retval;
        }
        */

        static int count_queries = 0;
        static int last_check_count_queries = 0;
        static DateTime last_count_queries = DateTime.MinValue;

        public override Model GetModel(bool create, int context_order, int ngram_order, ulong model_hash)
        {
            throw new NotImplementedException();
        }

        public override Model[] GetBulk(int context_order, ulong[] model_hash)
        {
            IBatch batch = db.CreateBatch();

            Dictionary<Key, Task<RedisValue>> tasks = new Dictionary<Key, Task<RedisValue>>();
            for (int i = 0; i < model_hash.Length; i += (MaxContextOrder + 1))
                for (int j = 0; j <= MaxContextOrder; j++)
                {
                    ulong current_hash = model_hash[i + j];

                    if (!models[j, 1].ContainsKey(current_hash))
                    {
                        Key key = GetKey(current_hash, j);
                        if (!tasks.ContainsKey(key))
                            tasks.Add(key, batch.StringGetAsync(key.Binary));
                    }
                }

            batch.Execute();
            Model[] retval = new Model[model_hash.Length];
            for (int i = 0; i < model_hash.Length; i += (MaxContextOrder + 1))
                for (short j = 0; j <= MaxContextOrder; j++)
                {
                    ulong current_hash = model_hash[i + j];

                    if (models[j, 1].ContainsKey(current_hash))
                        retval[i + j] = models[j, 1][current_hash];
                    else
                    {
                        Key key = GetKey(current_hash, j);
                        tasks[key].Wait();

                        byte[] blob = tasks[key].Result;
                        Model model = CreateModel(current_hash, j, blob);
                        models[j, 1].Add(current_hash, model);
                        retval[i + j] = model;
                    }
                }

            // Console.Write("B");
            return retval;
        }

        class PendingFeed
        {
            public PendingFeed(Key key, ulong model_hash, int context_order, int[] parameter_values, int pos)
            {
                Key = key;
                ModelHash = model_hash;
                ContextOrder = (short) context_order;
                ParameterValues = parameter_values;
                Pos = pos;
            }

            public Key Key { get; private set; }
            public ulong ModelHash { get; private set; }
            public short ContextOrder { get; private set; }
            public int[] ParameterValues { get; private set; }
            public int Pos { get; private set; }
        }

        List<PendingFeed> pending_feeds = new List<PendingFeed>();
        HashSet<Model> pending_models = new HashSet<Model>();

        public override void FeedModel(int context_order, ulong model_hash, int[] parameter_values, int pos)
        {
            if (models[context_order, 1].ContainsKey(model_hash))
            {
                Model model = models[context_order, 1][model_hash];
                model.Feed(parameter_values, pos);
                pending_models.Add(model);
            }
            else
            {
                Key key = GetKey(model_hash, context_order);
                PendingFeed pending = new PendingFeed(key, model_hash, context_order, parameter_values, pos);
                pending_feeds.Add(pending);
            }
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
            IBatch batch = db.CreateBatch();

            Dictionary<Key, Task<RedisValue>> tasks = new Dictionary<Key, Task<RedisValue>>();
            foreach (var pending in pending_feeds.Select(s => s.Key).Distinct())
                tasks.Add(pending, batch.StringGetAsync(pending.Binary));

            batch.Execute();

            Dictionary<Key, Model> models_save = new Dictionary<Key, Model>();
            foreach (var pending in pending_feeds)
            {
                if (!models_save.ContainsKey(pending.Key))
                {
                    tasks[pending.Key].Wait();

                    byte[] blob = (byte[]) tasks[pending.Key].Result;
                    Model model = CreateModel(pending.ModelHash, pending.ContextOrder, blob);
                    models_save.Add(pending.Key, model);

                    if (!models[pending.ContextOrder, 1].ContainsKey(pending.ModelHash))
                        models[pending.ContextOrder, 1].Add(pending.ModelHash, model);
                }

                models_save[pending.Key].Feed(pending.ParameterValues, pending.Pos);
            }

            foreach (var model in pending_models)
                models_save.Add(GetKey(model.Hash, model.ContextOrder), model);

            batch = db.CreateBatch();
            foreach (var kv in models_save)
                batch.StringSetAsync(kv.Key.Binary, kv.Value.Serialize());

            batch.Execute();
            pending_feeds.Clear();
            pending_models.Clear();

            int total_cached_models = 0;
            for (int i = 0; i <= MaxContextOrder; i++)
                total_cached_models += models[i, 1].Count;

            if (total_cached_models > MAX_CACHED_MODELS)
                for (int i = 3; i <= MaxContextOrder; i++)
                    models[i, 1].Clear();
        }

        const int MAX_CACHED_MODELS = 50000;
    }
}
