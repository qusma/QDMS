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
                "dbo.Countries",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 100, storeType: "nvarchar"),
                        CountryCode = c.String(maxLength: 2, storeType: "nvarchar"),
                        CurrencyCode = c.String(maxLength: 3, storeType: "nvarchar"),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "IX_Country");
            
            CreateTable(
                "dbo.EconomicReleases",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100, storeType: "nvarchar"),
                        Country = c.String(nullable: false, maxLength: 2, storeType: "nvarchar"),
                        Currency = c.String(maxLength: 3, storeType: "nvarchar"),
                        DateTime = c.DateTime(nullable: false, precision: 0),
                        Expected = c.Double(),
                        Previous = c.Double(),
                        Actual = c.Double(),
                        Importance = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => new { t.Name, t.Country, t.DateTime }, unique: true, name: "IX_Unique")
                .Index(t => t.Currency);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.EconomicReleases", new[] { "Currency" });
            DropIndex("dbo.EconomicReleases", "IX_Unique");
            DropIndex("dbo.Countries", "IX_Country");
            DropTable("dbo.EconomicReleases");
            DropTable("dbo.Countries");
            RenameTable(name: "dbo.DataUpdateJobSettings", newName: "DataUpdateJobDetails");
        }
    }
}
