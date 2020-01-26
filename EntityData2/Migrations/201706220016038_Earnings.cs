namespace EntityData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Earnings : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.EarningsAnnouncements",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Forecast = c.Decimal(precision: 16, scale: 8),
                        EarningsPerShare = c.Decimal(precision: 16, scale: 8),
                        Symbol = c.String(maxLength: 25, storeType: "nvarchar"),
                        CompanyName = c.String(maxLength: 100, storeType: "nvarchar"),
                        Date = c.DateTime(nullable: false, precision: 0),
                        EarningsCallTime = c.Int(nullable: false),
                        EarningsTime = c.DateTime(precision: 0),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Symbol)
                .Index(t => t.Date);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.EarningsAnnouncements", new[] { "Date" });
            DropIndex("dbo.EarningsAnnouncements", new[] { "Symbol" });
            DropTable("dbo.EarningsAnnouncements");
        }
    }
}
