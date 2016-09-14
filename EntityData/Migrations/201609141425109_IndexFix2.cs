namespace EntityData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class IndexFix2 : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Instruments", "IX_Symbol_DatasourceID_ExchangeID_Expiration");
            DropIndex("dbo.Instruments", new[] { "ExchangeID" });
            DropIndex("dbo.Instruments", new[] { "DatasourceID" });
            CreateIndex("dbo.Instruments", new[] { "Symbol", "Type", "DatasourceID", "ExchangeID", "Expiration", "Strike" }, unique: true, name: "IX_Unique");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Instruments", "IX_Unique");
            CreateIndex(
                "dbo.Instruments",
                new string[5] { "Symbol", "DatasourceID", "ExchangeID", "Expiration", "Strike" },
                unique: true);
            CreateIndex("dbo.Instruments", "DatasourceID");
            CreateIndex("dbo.Instruments", "ExchangeID");
        }
    }
}
