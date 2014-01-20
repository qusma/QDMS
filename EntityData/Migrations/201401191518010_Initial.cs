namespace EntityData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ContinuousFutures",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        InstrumentID = c.Int(nullable: false),
                        UnderlyingSymbolID = c.Int(nullable: false),
                        Month = c.Int(nullable: false),
                        RolloverType = c.Int(nullable: false),
                        RolloverDays = c.Int(nullable: false),
                        AdjustmentMode = c.Int(nullable: false),
                        UseJan = c.Boolean(nullable: false),
                        UseFeb = c.Boolean(nullable: false),
                        UseMar = c.Boolean(nullable: false),
                        UseApr = c.Boolean(nullable: false),
                        UseMay = c.Boolean(nullable: false),
                        UseJun = c.Boolean(nullable: false),
                        UseJul = c.Boolean(nullable: false),
                        UseAug = c.Boolean(nullable: false),
                        UseSep = c.Boolean(nullable: false),
                        UseOct = c.Boolean(nullable: false),
                        UseNov = c.Boolean(nullable: false),
                        UseDec = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Instruments", t => t.InstrumentID, cascadeDelete: true)
                .ForeignKey("dbo.UnderlyingSymbols", t => t.UnderlyingSymbolID, cascadeDelete: true)
                .Index(t => t.InstrumentID)
                .Index(t => t.UnderlyingSymbolID);
            
            CreateTable(
                "dbo.Instruments",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Symbol = c.String(maxLength: 255, unicode: false, storeType: "nvarchar"),
                        UnderlyingSymbol = c.String(maxLength: 255, unicode: false, storeType: "nvarchar"),
                        Name = c.String(maxLength: 255, unicode: false, storeType: "nvarchar"),
                        PrimaryExchangeID = c.Int(),
                        ExchangeID = c.Int(),
                        Type = c.Int(nullable: false),
                        Multiplier = c.Int(),
                        Expiration = c.DateTime(precision: 0),
                        OptionType = c.Int(),
                        Strike = c.Decimal(precision: 16, scale: 8),
                        Currency = c.String(maxLength: 25, unicode: false, storeType: "nvarchar"),
                        MinTick = c.Decimal(precision: 16, scale: 8),
                        Industry = c.String(maxLength: 255, unicode: false, storeType: "nvarchar"),
                        Category = c.String(maxLength: 255, unicode: false, storeType: "nvarchar"),
                        Subcategory = c.String(maxLength: 255, unicode: false, storeType: "nvarchar"),
                        IsContinuousFuture = c.Boolean(nullable: false),
                        ValidExchanges = c.String(unicode: false),
                        DatasourceID = c.Int(nullable: false),
                        ContinuousFutureID = c.Int(),
                        SessionsSource = c.Int(nullable: false),
                        SessionTemplateID = c.Int(),
                        DatasourceSymbol = c.String(maxLength: 255, unicode: false, storeType: "nvarchar"),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.ContinuousFutures", t => t.ContinuousFutureID)
                .ForeignKey("dbo.Datasources", t => t.DatasourceID, cascadeDelete: true)
                .ForeignKey("dbo.Exchanges", t => t.ExchangeID)
                .ForeignKey("dbo.Exchanges", t => t.PrimaryExchangeID)
                .Index(t => t.ContinuousFutureID)
                .Index(t => t.DatasourceID)
                .Index(t => t.ExchangeID)
                .Index(t => t.PrimaryExchangeID);

            CreateIndex(
                "dbo.Instruments",
                new string[4] { "Symbol", "DatasourceID", "ExchangeID", "Expiration" },
                unique: true);
            

            
            CreateTable(
                "dbo.Datasources",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 255, unicode: false, storeType: "nvarchar"),
                    })
                .PrimaryKey(t => t.ID);

            CreateIndex(
            "dbo.Datasources",
            "Name",
            unique: true);
            
            CreateTable(
                "dbo.Exchanges",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 255, unicode: false, storeType: "nvarchar"),
                        Timezone = c.String(maxLength: 255, unicode: false, storeType: "nvarchar"),
                        LongName = c.String(maxLength: 255, unicode: false, storeType: "nvarchar"),
                    })
                .PrimaryKey(t => t.ID);

            CreateIndex(
                "dbo.Exchanges",
                "Name",
                unique: true);
            
            CreateTable(
                "dbo.exchangesessions",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        OpeningTime = c.Time(nullable: false, precision: 3),
                        ClosingTime = c.Time(nullable: false, precision: 3),
                        ExchangeID = c.Int(nullable: false),
                        IsSessionEnd = c.Boolean(nullable: false),
                        OpeningDay = c.Int(nullable: false),
                        ClosingDay = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Exchanges", t => t.ExchangeID, cascadeDelete: true)
                .Index(t => t.ExchangeID);
            
            CreateTable(
                "dbo.instrumentsessions",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        OpeningTime = c.Time(nullable: false, precision: 3),
                        ClosingTime = c.Time(nullable: false, precision: 3),
                        InstrumentID = c.Int(nullable: false),
                        IsSessionEnd = c.Boolean(nullable: false),
                        OpeningDay = c.Int(nullable: false),
                        ClosingDay = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Instruments", t => t.InstrumentID, cascadeDelete: true)
                .Index(t => t.InstrumentID);
            
            CreateTable(
                "dbo.Tags",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 255, unicode: false, storeType: "nvarchar"),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.UnderlyingSymbols",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Symbol = c.String(maxLength: 255, unicode: false, storeType: "nvarchar"),
                        ExpirationRule = c.Binary(),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.SessionTemplates",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 255, unicode: false, storeType: "nvarchar"),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.templatesessions",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        OpeningTime = c.Time(nullable: false, precision: 3),
                        ClosingTime = c.Time(nullable: false, precision: 3),
                        TemplateID = c.Int(nullable: false),
                        IsSessionEnd = c.Boolean(nullable: false),
                        OpeningDay = c.Int(nullable: false),
                        ClosingDay = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.SessionTemplates", t => t.TemplateID, cascadeDelete: true)
                .Index(t => t.TemplateID);
            
            CreateTable(
                "dbo.tag_map",
                c => new
                    {
                        InstrumentID = c.Int(nullable: false),
                        TagID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.InstrumentID, t.TagID })
                .ForeignKey("dbo.Instruments", t => t.InstrumentID, cascadeDelete: true)
                .ForeignKey("dbo.Tags", t => t.TagID, cascadeDelete: true)
                .Index(t => t.InstrumentID)
                .Index(t => t.TagID);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.templatesessions", "TemplateID", "dbo.SessionTemplates");
            DropForeignKey("dbo.ContinuousFutures", "UnderlyingSymbolID", "dbo.UnderlyingSymbols");
            DropForeignKey("dbo.ContinuousFutures", "InstrumentID", "dbo.Instruments");
            DropForeignKey("dbo.tag_map", "TagID", "dbo.Tags");
            DropForeignKey("dbo.tag_map", "InstrumentID", "dbo.Instruments");
            DropForeignKey("dbo.instrumentsessions", "InstrumentID", "dbo.Instruments");
            DropForeignKey("dbo.Instruments", "PrimaryExchangeID", "dbo.Exchanges");
            DropForeignKey("dbo.Instruments", "ExchangeID", "dbo.Exchanges");
            DropForeignKey("dbo.exchangesessions", "ExchangeID", "dbo.Exchanges");
            DropForeignKey("dbo.Instruments", "DatasourceID", "dbo.Datasources");
            DropForeignKey("dbo.Instruments", "ContinuousFutureID", "dbo.ContinuousFutures");
            DropIndex("dbo.templatesessions", new[] { "TemplateID" });
            DropIndex("dbo.ContinuousFutures", new[] { "UnderlyingSymbolID" });
            DropIndex("dbo.ContinuousFutures", new[] { "InstrumentID" });
            DropIndex("dbo.tag_map", new[] { "TagID" });
            DropIndex("dbo.tag_map", new[] { "InstrumentID" });
            DropIndex("dbo.instrumentsessions", new[] { "InstrumentID" });
            DropIndex("dbo.Instruments", new[] { "PrimaryExchangeID" });
            DropIndex("dbo.Instruments", new[] { "ExchangeID" });
            DropIndex("dbo.exchangesessions", new[] { "ExchangeID" });
            DropIndex("dbo.Instruments", new[] { "DatasourceID" });
            DropIndex("dbo.Instruments", new[] { "ContinuousFutureID" });
            DropTable("dbo.tag_map");
            DropTable("dbo.templatesessions");
            DropTable("dbo.SessionTemplates");
            DropTable("dbo.UnderlyingSymbols");
            DropTable("dbo.Tags");
            DropTable("dbo.instrumentsessions");
            DropTable("dbo.exchangesessions");
            DropTable("dbo.Exchanges");
            DropTable("dbo.Datasources");
            DropTable("dbo.Instruments");
            DropTable("dbo.ContinuousFutures");
        }
    }
}
