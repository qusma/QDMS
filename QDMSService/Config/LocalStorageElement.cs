using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QDMSService.Config
{
    public enum LocalStorageType
    {
        MySql,
        SqlServer
    }

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
