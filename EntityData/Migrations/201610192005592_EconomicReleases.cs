using System.Configuration;

namespace EntityData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EconomicReleases : DbMigration
    {
        public override void Up()
        {
            string provider = ConfigurationManager.ConnectionStrings["qdmsEntities"].ProviderName;

            if (provider == "MySql.Data.MySqlClient")
            {
                RenameTable(name: "DataUpdateJobDetails", newName: "DataUpdateJobSettings");
            }
            else
            {
                RenameTable(name: "dbo.DataUpdateJobDetails", newName: "DataUpdateJobSettings");
            }
            
            CreateTable(
                "dbo.EconomicReleases",
                c => new
                    {
                        DateTime = c.DateTime(nullable: false, precision: 0),
                        Name = c.String(nullable: false, maxLength: 100, storeType: "nvarchar"),
                        Country = c.String(nullable: false, maxLength: 2, storeType: "nvarchar"),
                        Currency = c.String(maxLength: 3, storeType: "nvarchar"),
                        Expected = c.Double(),
                        Previous = c.Double(),
                        Actual = c.Double(),
                        Importance = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.DateTime, t.Name, t.Country })
                .Index(t => t.Currency);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.EconomicReleases", new[] { "Currency" });
            DropTable("dbo.EconomicReleases");
            RenameTable(name: "dbo.DataUpdateJobSettings", newName: "DataUpdateJobDetails");
        }
    }
}
