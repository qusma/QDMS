using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QDMSService.Config
{
    public class RealtimeDataService : ConfigurationElement
    {
        [ConfigurationProperty("RequestPort", DefaultValue = 5556, IsRequired = false)]
        [IntegerValidator(MinValue = 1, MaxValue = 65535)]
        public int RequestPort
        {
            get { return (int)this["RequestPort"]; }
            set { this["RequestPort"] = value; }
        }

        [ConfigurationProperty("PublisherPort", DefaultValue = 5557, IsRequired = false)]
        [IntegerValidator(MinValue = 1, MaxValue = 65535)]
        public int PublisherPort
        {
            get { return (int)this["PublisherPort"]; }
            set { this["PublisherPort"] = value; }
        }
    }
}
