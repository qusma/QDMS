namespace EntityData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TradingClass : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Instruments", "TradingClass", c => c.String(maxLength: 20, storeType: "nvarchar"));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Instruments", "TradingClass");
        }
    }
}
