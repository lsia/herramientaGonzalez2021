using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KSDExperiments.Datasets;


namespace KSDExperiments.FiniteContexts.Aggregators
{
    public abstract class SessionAgreggator
    {
        public Session AggregatedSession { get; protected set; }
        public int SessionCount { get; protected set; }

        protected abstract void DoAddSession(Session session);

        public void AddSession(Session session)
        {
            SessionCount++;

            if (AggregatedSession == null)
                AggregatedSession = session;
            else
                DoAddSession(session);
        }

        public void Clear()
        {
            AggregatedSession = null;
            SessionCount = 0;
        }
    }
}
