namespace EntityData.Migrations.DataDBContextNS
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DataInitial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.data",
                c => new
                    {
                        DT = c.DateTime(nullable: false, precision: 3),
                        InstrumentID = c.Int(nullable: false),
                        Frequency = c.Int(nullable: false),
                        Open = c.Decimal(nullable: false, precision: 16, scale: 8),
                        High = c.Decimal(nullable: false, precision: 16, scale: 8),
                        Low = c.Decimal(nullable: false, precision: 16, scale: 8),
                        Close = c.Decimal(nullable: false, precision: 16, scale: 8),
                        AdjOpen = c.Decimal(precision: 16, scale: 8),
                        AdjHigh = c.Decimal(precision: 16, scale: 8),
                        AdjLow = c.Decimal(precision: 16, scale: 8),
                        AdjClose = c.Decimal(precision: 16, scale: 8),
                        Volume = c.Long(),
                        OpenInterest = c.Int(),
                        Dividend = c.Decimal(precision: 16, scale: 8),
                        Split = c.Decimal(precision: 16, scale: 8),
                    })
                .PrimaryKey(t => new { t.DT, t.InstrumentID, t.Frequency });
            
            CreateTable(
                "dbo.instrumentinfo",
                c => new
                    {
                        InstrumentID = c.Int(nullable: false),
                        Frequency = c.Int(nullable: false),
                        EarliestDate = c.DateTime(nullable: false, precision: 3),
                        LatestDate = c.DateTime(nullable: false, precision: 3),
                    })
                .PrimaryKey(t => new { t.InstrumentID, t.Frequency });
            
        }
        
        public override void Down()
        {
            DropTable("dbo.instrumentinfo");
            DropTable("dbo.data");
        }
    }
}
