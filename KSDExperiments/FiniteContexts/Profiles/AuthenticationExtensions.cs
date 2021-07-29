using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KSDExperiments.Datasets;


namespace KSDExperiments.FiniteContexts.Profiles
{
    public static class AuthenticationExtensions
    {
        public static void GetEERs(this IEnumerable<Authentication> authentications)
        {
            
            foreach (Authentication authentication in authentications)
                foreach (var kv in authentication.MethodValues)
                {

                }
        }
    }
}
