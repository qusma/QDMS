using QDMS.Server;
using System.Configuration;

namespace QDMSService.Config
{
    public class SchedulerService : ConfigurationSection
    {
        [ConfigurationProperty("StorageType", DefaultValue = LocalStorageType.MySql)]
        public LocalStorageType StorageType
        {
            get { return (LocalStorageType)this["StorageType"]; }
            set { this["StorageType"] = value; }
        }

        [ConfigurationProperty("ConnectionString", DefaultValue = "")]
        public string ConnectionString
        {
            get { return (string)this["ConnectionString"]; }
            set { this["ConnectionString"] = value; }
        }
    }
}