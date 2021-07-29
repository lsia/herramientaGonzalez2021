using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSDExperiments.Datasets
{
    public class Dataset
    {
        public string Name { get; private set; }
        public string Source { get; private set; }
        public string Filename { get; private set; }
        public Session[] Sessions { get; private set; }
        public Dictionary<int,Session[]> SessionsByUser { get; private set; }

        public Dataset(string name, string source, string filename, Session[] sessions)
        {
            Name = name;
            Source = source;
            Filename = filename;
            SetSessions(sessions);
        }

        public void SetSessions(Session[] sessions)
        {
            Sessions = sessions;

            Dictionary<int, List<Session>> tmp = new Dictionary<int, List<Session>>();
            foreach (Session session in Sessions)
            {
                if (!tmp.ContainsKey(session.User.UserID))
                    tmp.Add(session.User.UserID, new List<Session>());

                tmp[session.User.UserID].Add(session);
            }

            SessionsByUser = new Dictionary<int, Session[]>();
            foreach (var kv in tmp)
                SessionsByUser.Add(kv.Key, kv.Value.ToArray());
        }
    }
}
