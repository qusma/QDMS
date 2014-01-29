namespace EntityData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDataUpdateJobs : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DataUpdateJobDetails",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 255, unicode: false, storeType: "nvarchar"),
                        UseTag = c.Boolean(nullable: false),
                        InstrumentID = c.Int(),
                        TagID = c.Int(),
                        WeekDaysOnly = c.Boolean(nullable: false),
                        Time = c.Time(nullable: false, precision: 0),
                        Frequency = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Instruments", t => t.InstrumentID, cascadeDelete: true)
                .ForeignKey("dbo.Tags", t => t.TagID, cascadeDelete: true)
                .Index(t => t.InstrumentID)
                .Index(t => t.TagID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.DataUpdateJobDetails", "TagID", "dbo.Tags");
            DropForeignKey("dbo.DataUpdateJobDetails", "InstrumentID", "dbo.Instruments");
            DropIndex("dbo.DataUpdateJobDetails", new[] { "TagID" });
            DropIndex("dbo.DataUpdateJobDetails", new[] { "InstrumentID" });
            DropTable("dbo.DataUpdateJobDetails");
        }
    }
}
