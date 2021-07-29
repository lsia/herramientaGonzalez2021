using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KSDExperiments.Datasets;
using KSDExperiments.Pipelines;
using KSDExperiments.Util;


namespace KSDExperiments.Util
{
    static class SentenceUtil
    {
        public static Dictionary<int, Session[]> SplitBySentenceSize
            (
                Results results, 
                int min_sentence_size = -1
            )
        {
            List<Session> sentences = new List<Session>();
            ExperimentParallelization.ForEachSession(results, (d, s) =>
            {
                int start = 0;
                for (int i = 0; i < s.VKs.Length; i++)
                    if (s.VKs[i] == (byte) VirtualKeys.VK_OEM_PERIOD &&
                        (min_sentence_size == -1 || (i - start) >= min_sentence_size))
                    {
                        Session split = s.Split(start, i);
                        sentences.Add(split);
                        start = i + 1;
                    }
            });

            return sentences.GroupBy(s => s.VKs.Length).ToDictionary(g => g.Key, g => g.ToArray());
        }

        public static Session[] Split(Session[] sessions)
        {
            var sentences = SplitBySentenceSize(sessions);

            List<Session> retval = new List<Session>();
            foreach (var kv in sentences)
                foreach (var sentence in kv.Value)
                    retval.Add(sentence);

            return retval.ToArray();
        }

        public static Dictionary<int, Session[]> SplitBySentenceSize(Session[] sessions)
        {
            object lock_sentences = new object();
            List<Session> sentences = new List<Session>();
            Parallel.ForEach(sessions, s =>
            {
                int start = 0;
                for (int i = 0; i < s.VKs.Length; i++)
                    if (s.VKs[i] == (byte) VirtualKeys.VK_OEM_PERIOD)
                    {
                        Session split = s.Split(start, i);
                        lock (lock_sentences)
                            sentences.Add(split);

                        start = i + 1;
                    }

                if (start < (s.VKs.Length - 5))
                    lock (lock_sentences)
                        sentences.Add(s.Split(start));
            });

            Dictionary<int, List<Session>> tmp = new Dictionary<int, List<Session>>();
            foreach (var sentence in sentences)
            {
                if (!tmp.ContainsKey(sentence.Length))
                    tmp.Add(sentence.Length, new List<Session>());

                tmp[sentence.Length].Add(sentence);
            }

            Dictionary<int, Session[]> retval = new Dictionary<int, Session[]>();
            foreach (var kv in tmp)
                retval.Add(kv.Key, kv.Value.ToArray());

            return retval;
            /*
            return sentences.GroupBy(s => s.VKs.Length).ToDictionary(g => {
                if (g == null)
                {
                    int k = 9;
                }


                return g.Key;
            }, 
                
                g => {
                    if (g == null)
                    {
                        int k = 9;
                    }

                    return g.ToArray();
                    });
            */
        }
    }
}
