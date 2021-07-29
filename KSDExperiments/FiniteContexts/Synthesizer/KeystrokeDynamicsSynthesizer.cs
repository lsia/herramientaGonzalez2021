using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KSDExperiments.Datasets;
using KSDExperiments.FiniteContexts.Partitions;
using KSDExperiments.FiniteContexts.Profiles;
using KSDExperiments.FiniteContexts.Models;
using KSDExperiments.FiniteContexts.PatternVector;
using KSDExperiments.Util;


namespace KSDExperiments.FiniteContexts.Synthesizer
{
    abstract class KeystrokeDynamicsSynthesizer
    {
        public Profile Profile { get; private set; }
        public KeystrokeDynamicsSynthesizer(Profile profile)
        {
            Profile = profile;
        }

        public virtual void Initialize()
        {
        }

        public abstract int[] SynthesizeFeature(TypingFeature feature, Session dummy);

        public Session Synthesize(byte[] vks, int session_id = int.MinValue)
        {
            Session dummy = Session.CreateDummy(vks, session_id);

            int[] hts = SynthesizeFeature(TypingFeature.HT, dummy);
            int[] fts = SynthesizeFeature(TypingFeature.FT, dummy);

            Session retval = new Session(session_id, null, DateTime.Now, GetType().Name, vks, hts, fts);

            ThresholdPartitioner.ProcessSessionWithDefaultValues(retval);
            CleanFTs.ProcessSession(retval);
            
            return retval;
        }

        public Session Synthesize(Session session, User user = null)
        {
            Session retval = Synthesize(session.VKs, session.ID);
            retval.SetUser(user);
            return retval;
        }
    }
}
