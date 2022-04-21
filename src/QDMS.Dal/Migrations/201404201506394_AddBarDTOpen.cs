namespace EntityData.Migrations.DataDBContextNS
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddBarDTOpen : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.data", "DTOpen", c => c.DateTime(precision: 3));
        }
        
        public override void Down()
        {
            DropColumn("dbo.data", "DTOpen");
        }
    }
}
