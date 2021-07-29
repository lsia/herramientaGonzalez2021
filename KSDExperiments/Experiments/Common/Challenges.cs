using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KSDExperiments.Datasets;
using KSDExperiments.Util;


namespace KSDExperiments.Experiments.Common
{
    public static class Challenges
    {
        public class TooManyAttemptsException : Exception { }

        public static Session GenerateTextChallenge(Session original_text, Session[] candidates)
        {
            for (int i = 0; i < 10; i++)
            {
                Session candidate = candidates.ChooseOne();
                int starting_pos = 0;
                starting_pos = RNG.Instance.Next(candidate.Length - original_text.Length);

                Session fragment = candidate.Split(starting_pos, starting_pos + original_text.Length - 1);
                if (fragment.Text != original_text.Text)
                {
                    fragment.Properties.Add("original_ID", fragment.ID);
                    fragment.Properties.Add("original_pos", starting_pos);
                    fragment.Properties.Add("original_text", fragment.Text);
                    return fragment.ReplaceVKs(original_text.VKs);
                }
            }

            throw new TooManyAttemptsException();
        }

        public static Session[] GenerateTextChallenges(int count, Session original_text, Session[] candidates)
        {
            List<Session> retval = new List<Session>();
            for (int i = 0; i < count; i++)
                retval.Add(GenerateTextChallenge(original_text, candidates));

            return retval.ToArray();
        }

        public static Session[] FilterAndGenerateTextChallenges(int count, Session sentence, Session[] sessions, bool include_sentence_first = false)
        {
            var candidates = sessions.Where(s => s.ID != sentence.ID && s.Length >= sentence.Length).ToArray();
            if (candidates.Length == 0)
                return null;

            List<Session> retval = new List<Session>();
            retval.Add(sentence);

            for (int i = 0; i < count; i++)
                retval.Add(GenerateTextChallenge(sentence, candidates));

            return retval.ToArray();
        }
    }
}
