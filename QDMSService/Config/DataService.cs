using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace QDMSService.Config
{
    public class DataService : ConfigurationSection
    {
        [ConfigurationProperty("DataStorage")]
        public LocalStorageElement DataStorage
        {
            get { return (LocalStorageElement)this["DataStorage"]; }
            set { this["DataStorage"] = value; }
        }

        [ConfigurationProperty("LocalStorage")]
        public LocalStorageElement LocalStorage
        {
            get { return (LocalStorageElement)this["LocalStorage"]; }
            set { this["LocalStorage"] = value; }
        }

        [ConfigurationProperty("InstrumentService")]
        public InstrumentService InstrumentService
        {
            get { return (InstrumentService)this["InstrumentService"]; }
            set { this["InstrumentService"] = value; }
        }

        [ConfigurationProperty("HistoricalDataService")]
        public HistoricalDataService HistoricalDataService
        {
            get { return (HistoricalDataService)this["HistoricalDataService"]; }
            set { this["HistoricalDataService"] = value; }
        }

        [ConfigurationProperty("RealtimeDataService")]
        public RealtimeDataService RealtimeDataService
        {
            get { return (RealtimeDataService)this["RealtimeDataService"]; }
            set { this["RealtimeDataService"] = value; }
        }
    }
}
