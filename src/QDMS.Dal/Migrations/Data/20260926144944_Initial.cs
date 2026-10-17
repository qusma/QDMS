using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntityData.Migrations.Data
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bars",
                columns: table => new
                {
                    dt = table.Column<DateTime>(type: "timestamp(3) without time zone", precision: 3, nullable: false),
                    instrument_id = table.Column<int>(type: "integer", nullable: false),
                    frequency = table.Column<int>(type: "integer", nullable: false),
                    open = table.Column<decimal>(type: "numeric(16,8)", precision: 16, scale: 8, nullable: false),
                    high = table.Column<decimal>(type: "numeric(16,8)", precision: 16, scale: 8, nullable: false),
                    low = table.Column<decimal>(type: "numeric(16,8)", precision: 16, scale: 8, nullable: false),
                    close = table.Column<decimal>(type: "numeric(16,8)", precision: 16, scale: 8, nullable: false),
                    adj_open = table.Column<decimal>(type: "numeric(16,8)", precision: 16, scale: 8, nullable: true),
                    adj_high = table.Column<decimal>(type: "numeric(16,8)", precision: 16, scale: 8, nullable: true),
                    adj_low = table.Column<decimal>(type: "numeric(16,8)", precision: 16, scale: 8, nullable: true),
                    adj_close = table.Column<decimal>(type: "numeric(16,8)", precision: 16, scale: 8, nullable: true),
                    dt_open = table.Column<DateTime>(type: "timestamp(3) without time zone", precision: 3, nullable: true),
                    volume = table.Column<long>(type: "bigint", nullable: true),
                    open_interest = table.Column<int>(type: "integer", nullable: true),
                    dividend = table.Column<decimal>(type: "numeric(16,8)", precision: 16, scale: 8, nullable: true),
                    split = table.Column<decimal>(type: "numeric(16,8)", precision: 16, scale: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bars", x => new { x.instrument_id, x.frequency, x.dt });
                });

            migrationBuilder.CreateTable(
                name: "instrument_info",
                columns: table => new
                {
                    instrument_id = table.Column<int>(type: "integer", nullable: false),
                    frequency = table.Column<int>(type: "integer", nullable: false),
                    earliest_date = table.Column<DateTime>(type: "timestamp(3) without time zone", precision: 3, nullable: false),
                    latest_date = table.Column<DateTime>(type: "timestamp(3) without time zone", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instrument_info", x => new { x.instrument_id, x.frequency });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bars");

            migrationBuilder.DropTable(
                name: "instrument_info");
        }
    }
}
