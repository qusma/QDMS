using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QDMSService.Config
{
    public class InstrumentService : ConfigurationElement
    {
        [ConfigurationProperty("Port", DefaultValue = 5558, IsRequired = false)]
        [IntegerValidator(MinValue = 1, MaxValue = 65535)]
        public int Port
        {
            get { return (int)this["Port"]; }
            set { this["Port"] = value; }
        }
    }
}
