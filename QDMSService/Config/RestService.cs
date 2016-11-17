using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QDMSService.Config
{
    public class RestService : ConfigurationElement
    {
        [ConfigurationProperty("Port", DefaultValue = 5559, IsRequired = false)]
        [IntegerValidator(MinValue = 1, MaxValue = 65535)]
        public int Port
        {
            get { return (int)this["Port"]; }
            set { this["Port"] = value; }
        }

        [ConfigurationProperty("ApiKey", DefaultValue = "123", IsRequired = false)]
        public string ApiKey
        {
            get { return (string)this["ApiKey"]; }
            set { this["ApiKey"] = value; }
        }
    }
}
