using System.Configuration;

namespace EntityData.Migrations.DataDBContextNS
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class IndexFix : DbMigration
    {
        public override void Up()
        {
            string provider = ConfigurationManager.ConnectionStrings["qdmsEntities"].ProviderName;

            if (provider == "MySql.Data.MySqlClient")
            {
                DropPrimaryKey("data");
                AddPrimaryKey("data", new[] { "InstrumentID", "Frequency", "DT" });
            }
            else
            {
                DropPrimaryKey("dbo.data");
                AddPrimaryKey("dbo.data", new[] { "InstrumentID", "Frequency", "DT" });
            }
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.data");
            AddPrimaryKey("dbo.data", new[] { "DT", "InstrumentID", "Frequency" });
        }
    }
}
