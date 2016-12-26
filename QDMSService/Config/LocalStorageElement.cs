using QDMS.Server;
using System.Configuration;

namespace QDMSService.Config
{
    public class LocalStorageElement : ConfigurationElement
    {
        [ConfigurationProperty("Type")]
        public LocalStorageType Type
        {
            get { return (LocalStorageType)this["Type"]; }
            set { this["Type"] = value; }
        }

        [ConfigurationProperty("ConnectionString")]
        public string ConnectionString
        {
            get { return (string)this["ConnectionString"]; }
            set { this["ConnectionString"] = value; }
        }
    }
}