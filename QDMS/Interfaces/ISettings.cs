using System.ComponentModel;
using System.Configuration;

namespace QDMS
{
    public interface ISettings
    {
        string ibClientHost { get; set; }
        int ibClientPort { get; set; }
        string mySqlHost { get; set; }
        string instrumentsGridLayout { get; set; }
        int rtDBPubPort { get; set; }
        int rtDBReqPort { get; set; }
        int instrumentServerPort { get; set; }
        int hDBPort { get; set; }
        string logDirectory { get; set; }
        string mySqlUsername { get; set; }
        string mySqlPassword { get; set; }
        string quandlAuthCode { get; set; }
        string databaseType { get; set; }
        string sqlServerHost { get; set; }
        bool sqlServerUseWindowsAuthentication { get; set; }
        string sqlServerUsername { get; set; }
        string sqlServerPassword { get; set; }
        int histClientIBID { get; set; }
        int rtdClientIBID { get; set; }
        bool updateJobReportErrors { get; set; }
        bool updateJobReportOutliers { get; set; }
        bool updateJobReportNoData { get; set; }
        int updateJobTimeout { get; set; }
        string updateJobEmail { get; set; }
        string updateJobEmailHost { get; set; }
        string updateJobEmailUsername { get; set; }
        string updateJobEmailPassword { get; set; }
        int updateJobEmailPort { get; set; }
        string updateJobEmailSender { get; set; }
        bool updateJobTimeouts { get; set; }
        string forexFeedAccessKey { get; set; }
        string barChartApiKey { get; set; }
        string EconomicReleaseDefaultDatasource { get; set; }
        string apiKey { get; set; }
        int httpPort { get; set; }
        bool useSsl { get; set; }
        SettingsContext Context { get; }
        SettingsPropertyCollection Properties { get; }
        SettingsPropertyValueCollection PropertyValues { get; }
        SettingsProviderCollection Providers { get; }
        string SettingsKey { get; set; }
        bool IsSynchronized { get; }
        object GetPreviousVersion(string propertyName);
        void Reload();
        void Reset();
        void Save();
        void Upgrade();
        object this[string propertyName] { get; set; }
        event PropertyChangedEventHandler PropertyChanged;
        event SettingChangingEventHandler SettingChanging;
        event SettingsLoadedEventHandler SettingsLoaded;
        event SettingsSavingEventHandler SettingsSaving;
        void Initialize(SettingsContext context, SettingsPropertyCollection properties, SettingsProviderCollection providers);
    }
}