namespace EntityData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class IndexFix3 : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Dividends", new[] { "ExDate" });
            DropIndex("dbo.Dividends", new[] { "Symbol" });
            DropIndex("dbo.EconomicReleases", new[] { "Currency" });
            CreateIndex("dbo.UnderlyingSymbols", "Symbol", unique: true);
            CreateIndex("dbo.Currencies", "Code", unique: true);
            CreateIndex("dbo.Dividends", "ExDate");
            CreateIndex("dbo.Dividends", "Symbol");
            CreateIndex("dbo.EconomicReleases", "Currency");
        }
        
        public override void Down()
        {
            DropIndex("dbo.EconomicReleases", new[] { "Currency" });
            DropIndex("dbo.Dividends", new[] { "Symbol" });
            DropIndex("dbo.Dividends", new[] { "ExDate" });
            DropIndex("dbo.Currencies", new[] { "Code" });
            DropIndex("dbo.UnderlyingSymbols", new[] { "Symbol" });
            CreateIndex("dbo.EconomicReleases", "Currency");
            CreateIndex("dbo.Dividends", "Symbol");
            CreateIndex("dbo.Dividends", "ExDate");
        }
    }
}
