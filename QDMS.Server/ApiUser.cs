using System.Collections.Generic;
using Nancy.Security;

namespace QDMS.Server
{
    public class ApiUser : IUserIdentity
    {
        public ApiUser(string userName, IEnumerable<string> claims = null)
        {
            UserName = userName;
            
            if (claims != null)
            {
                Claims = new List<string>(claims);
            }
            else
            {
                Claims = new List<string>();
            }
        }

        public IEnumerable<string> Claims { get; }

        public string UserName { get; }
    }
}
