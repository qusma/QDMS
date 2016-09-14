namespace EntityData.Migrations.DataDBContextNS
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class IndexFix : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.data");
            AddPrimaryKey("dbo.data", new[] { "InstrumentID", "Frequency", "DT" });
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.data");
            AddPrimaryKey("dbo.data", new[] { "DT", "InstrumentID", "Frequency" });
        }
    }
}
