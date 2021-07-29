using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KSDExperiments.Datasets;
using KSDExperiments.FiniteContexts.ModelStorages;
using KSDExperiments.FiniteContexts.PatternVector;


namespace KSDExperiments.FiniteContexts.Models
{
    public class ModelFeeder
    {
        public int MaxContextOrder { get; private set; }
        public ModelStorage[] Storages { get; private set; }
        public ModelFeeder(int max_context_order, ModelStorage[] storages)
        {
            MaxContextOrder = max_context_order;
            Storages = storages;

            foreach (ModelStorage storage in storages)
                if (storage.MaxContextOrder != max_context_order)
                    throw new ArgumentException("All model storages must have consistent max_context_order.");
        }

        /*
        void ExtractModelFeed(Dictionary<ulong, Model> models, int pos, byte[] vks, int[] values, int context_length, int ngram_length)
        {
            #if DEBUG
            if (pos < (context_length - 1))
                throw new ArgumentException("Not enough space for context");

            if (pos > vks.Length - ngram_length)
                throw new ArgumentException("Not enough space for ngram");
            #endif

            int ngram_timing = 0;
            ulong ngram = 0;
            for (int i = 0; i < ngram_length; i++)
            {
                ngram <<= 8;
                ngram |= vks[pos + i];
                ngram_timing += values[pos + i];
            }

            ulong context = 0;
            for (int i = 0; i < context_length; i++)
            {
                context <<= 8;

                int vks_pos = pos - context_length + i;
                if (vks_pos == -1)
                    context |= 0xFF;
                else
                    context |= vks[vks_pos];
            }

            int[] context_timings = new int[8];
            for (int i = 0; i < 8; i++) context_timings[i] = int.MinValue;
            for (int i = 0; i < 8 && (pos - 1 - i) >= 0; i++)
                context_timings[i] = values[pos - 1 - i];

            ulong context_hash = (ngram << 8 * (sizeof(ulong) - ngram_length)) | context;
            if (!models.ContainsKey(context_hash))
                models.Add(context_hash, Factory.CreateModel(context, (short) context_length, (int) ngram, (short) ngram_length));

            models[context_hash].Feed(ngram_timing, context_timings);
        }

        void FeedModels(TypingFeature feature, Session session, Dictionary<ulong, Model>[,] models_matrix, byte[] vks, int[] values, int context_length, int ngram_length, bool skip_after_partition)
        {
            List<int> offsets = new List<int>();
            offsets.AddRange(session.PartitionOffsets);
            offsets.Add(session.VKs.Length);

            int pos = 0;
            foreach ( int next_offset in offsets )
            {
                int start = pos + context_length - 1;
                if (start < pos)
                    start = pos;

                if (start == pos && skip_after_partition)
                    start = pos + 1;

                int end = next_offset - ngram_length;

                for (int i = start; i <= end; i++)
                    ExtractModelFeed(models_matrix[context_length, ngram_length], i, vks, values, context_length, ngram_length);

                pos = next_offset;
            }   
        }

        public void Feed(Session session)
        {
            for (int i = 0; i <= MaxContextLength; i++)
                for (int j = 1; j <= MaxNGramLength; j++)
                {
                    FeedModels(TypingFeature.HT, session, ModelsHT, session.VKs, session.HTs, i, j, false);
                    FeedModels(TypingFeature.FT, session, ModelsFT, session.VKs, session.FTs, i, j, true);
                }
        }
        */

        void InitializeContext(ref int context_order, ref ulong context, int[] context_values)
        {
            for (int i = 0; i < 8; i++)
                context_values[i] = int.MinValue;

            context_order = 1;
            context = 0xFF;
        }

        public void Feed(Session session, bool initial_training)
        {
            bool must_feed_manually = false;
            foreach (ModelStorage storage in Storages)
                if (!initial_training || !storage.IsPersistent)
                {
                    storage.FeedSession(session);
                    must_feed_manually |= storage.MustFeedManually;
                }

            if (!must_feed_manually)
                return;

            int context_order = 1;
            ulong context = 0xFF;
            int partition_pos = 0;
            int[] context_values = new int[8];

            InitializeContext(ref context_order, ref context, context_values);
            for (int i = 0; i < session.VKs.Length; i++)
            {
                if (partition_pos < session.PartitionOffsets.Length && i == session.PartitionOffsets[partition_pos])
                {
                    InitializeContext(ref context_order, ref context, context_values);
                    partition_pos++;
                }

                ulong current_context_mask = 0;
                for (int cco = 0; cco <= context_order && cco <= MaxContextOrder; cco++)
                {
                    ulong current_context = context & current_context_mask;
                    ulong model_hash = current_context | ((ulong)session.VKs[i] << 56);
                    foreach (ModelStorage storage in Storages)
                        if (!initial_training || !storage.IsPersistent)
                        {
                            int[] parameter_values = session.Features[storage.Feature];
                            if (parameter_values[i] != int.MinValue)
                                storage.FeedModel(cco, model_hash, parameter_values, i);
                        }

                    current_context_mask <<= 8;
                    current_context_mask |= 0xFF;
                }

                context <<= 8;
                context |= session.VKs[i];
                context_order++;
            }

            foreach (ModelStorage storage in Storages)
                storage.Save();
        }

        public static ulong GetModelHash(byte vk, params byte[] context)
        {
            ulong retval = 0;
            for (int i = 0; i < context.Length; i++)
            {
                retval <<= 8;
                retval |= context[i];
            }

            retval |= (ulong) vk << 56;
            return retval;
        }

        public static byte GetVKFromModelHash(ulong model_hash)
        {
            return (byte)(model_hash >> 56);
        }

        public Builder[] GetBuilders()
        {
            List<Builder> tmp_builders = new List<Builder>();
            foreach (var storage in Storages)
                tmp_builders.Add(new Builder(storage));

            return tmp_builders.ToArray();
        }
    }
}
