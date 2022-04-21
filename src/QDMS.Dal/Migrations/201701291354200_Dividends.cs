namespace EntityData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Dividends : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Currencies", new[] { "Code" });
            CreateTable(
                "dbo.Dividends",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Amount = c.Decimal(nullable: false, precision: 16, scale: 8),
                        Currency = c.String(maxLength: 3, storeType: "nvarchar"),
                        DeclarationDate = c.DateTime(precision: 0),
                        ExDate = c.DateTime(nullable: false, precision: 0),
                        PaymentDate = c.DateTime(precision: 0),
                        RecordDate = c.DateTime(precision: 0),
                        Symbol = c.String(maxLength: 20, storeType: "nvarchar"),
                        Type = c.String(maxLength: 50, storeType: "nvarchar"),
                    })
                .PrimaryKey(t => t.ID)
                .Index(t => t.ExDate)
                .Index(t => t.Symbol);
        }
        
        public override void Down()
        {
            DropIndex("dbo.Dividends", new[] { "Symbol" });
            DropIndex("dbo.Dividends", new[] { "ExDate" });
            DropTable("dbo.Dividends");
        }
    }
}
