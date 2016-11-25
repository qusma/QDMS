using System.Configuration;

namespace QDMSService.Config
{
    public class WebService : ConfigurationElement
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

        [ConfigurationProperty("UseSsl", DefaultValue = false, IsRequired = false)]
        public bool UseSsl
        {
            get { return (bool)this["UseSsl"]; }
            set { this["UseSsl"] = value; }
        }
    }
}