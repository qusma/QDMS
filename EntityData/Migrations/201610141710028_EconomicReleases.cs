namespace EntityData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EconomicReleases : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.EconomicReleases",
                c => new
                    {
                        DateTime = c.DateTime(nullable: false),
                        Name = c.String(nullable: false, maxLength: 100),
                        Country = c.String(nullable: false, maxLength: 2),
                        Currency = c.String(maxLength: 3),
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
        }
    }
}
