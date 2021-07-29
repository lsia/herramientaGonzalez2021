using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KSDExperiments.FiniteContexts;
using KSDExperiments.FiniteContexts.Profiles;
using KSDExperiments.Pipelines;
using KSDExperiments.Util;


namespace KSDExperiments.FiniteContexts.Util
{
    class SessionAgreggator : SessionAgreggatorBase
    {
        public SessionAgreggator()
            : base(Pipeline.CurrentPipeline.Configuration.Name, 
                  new string[2] { "legitimate", "impostor" })
        {
        }

        public void UpdateARFFs(Authentication auth, bool legitimate)
        {
            base.UpdateARFFs(auth, legitimate ? "legitimate" : "impostor");
        }
    }
}
