using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EntityData.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "currencies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    numeric_code = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_currencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "datasources",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_datasources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dividends",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    amount = table.Column<decimal>(type: "numeric(16,8)", precision: 16, scale: 8, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    declaration_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ex_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    payment_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    record_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dividends", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "earnings_announcements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    forecast = table.Column<decimal>(type: "numeric(16,8)", precision: 16, scale: 8, nullable: true),
                    earnings_per_share = table.Column<decimal>(type: "numeric(16,8)", precision: 16, scale: 8, nullable: true),
                    symbol = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    company_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    earnings_call_time = table.Column<int>(type: "integer", nullable: false),
                    earnings_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_earnings_announcements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "economic_releases",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    date_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    expected = table.Column<double>(type: "double precision", nullable: true),
                    previous = table.Column<double>(type: "double precision", nullable: true),
                    actual = table.Column<double>(type: "double precision", nullable: true),
                    importance = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_economic_releases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "exchanges",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    timezone = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    long_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exchanges", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "session_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_session_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "underlying_symbols",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    symbol = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    expiration_rule = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_underlying_symbols", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "exchange_sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    opening_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    closing_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    exchange_id = table.Column<int>(type: "integer", nullable: false),
                    is_session_end = table.Column<bool>(type: "boolean", nullable: false),
                    opening_day = table.Column<int>(type: "integer", nullable: false),
                    closing_day = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exchange_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_exchange_sessions_exchanges_exchange_id",
                        column: x => x.exchange_id,
                        principalTable: "exchanges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "template_sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    opening_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    closing_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    template_id = table.Column<int>(type: "integer", nullable: false),
                    is_session_end = table.Column<bool>(type: "boolean", nullable: false),
                    opening_day = table.Column<int>(type: "integer", nullable: false),
                    closing_day = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_template_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_template_sessions_session_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "session_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "continuous_futures",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    instrument_id = table.Column<int>(type: "integer", nullable: false),
                    underlying_symbol_id = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    rollover_type = table.Column<int>(type: "integer", nullable: false),
                    rollover_days = table.Column<int>(type: "integer", nullable: false),
                    adjustment_mode = table.Column<int>(type: "integer", nullable: false),
                    use_jan = table.Column<bool>(type: "boolean", nullable: false),
                    use_feb = table.Column<bool>(type: "boolean", nullable: false),
                    use_mar = table.Column<bool>(type: "boolean", nullable: false),
                    use_apr = table.Column<bool>(type: "boolean", nullable: false),
                    use_may = table.Column<bool>(type: "boolean", nullable: false),
                    use_jun = table.Column<bool>(type: "boolean", nullable: false),
                    use_jul = table.Column<bool>(type: "boolean", nullable: false),
                    use_aug = table.Column<bool>(type: "boolean", nullable: false),
                    use_sep = table.Column<bool>(type: "boolean", nullable: false),
                    use_oct = table.Column<bool>(type: "boolean", nullable: false),
                    use_nov = table.Column<bool>(type: "boolean", nullable: false),
                    use_dec = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_continuous_futures", x => x.id);
                    table.ForeignKey(
                        name: "fk_continuous_futures_underlying_symbols_underlying_symbol_id",
                        column: x => x.underlying_symbol_id,
                        principalTable: "underlying_symbols",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "instruments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    symbol = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    underlying_symbol = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    primary_exchange_id = table.Column<int>(type: "integer", nullable: true),
                    exchange_id = table.Column<int>(type: "integer", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    multiplier = table.Column<int>(type: "integer", nullable: true),
                    expiration = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    option_type = table.Column<int>(type: "integer", nullable: true),
                    strike = table.Column<decimal>(type: "numeric(16,8)", precision: 16, scale: 8, nullable: true),
                    currency = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    min_tick = table.Column<decimal>(type: "numeric(16,8)", precision: 16, scale: 8, nullable: true),
                    industry = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    category = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    subcategory = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_continuous_future = table.Column<bool>(type: "boolean", nullable: false),
                    valid_exchanges = table.Column<string>(type: "text", nullable: true),
                    datasource_id = table.Column<int>(type: "integer", nullable: false),
                    continuous_future_id = table.Column<int>(type: "integer", nullable: true),
                    sessions_source = table.Column<int>(type: "integer", nullable: false),
                    session_template_id = table.Column<int>(type: "integer", nullable: true),
                    datasource_symbol = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    trading_class = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instruments", x => x.id);
                    table.ForeignKey(
                        name: "fk_instruments_continuous_futures_continuous_future_id",
                        column: x => x.continuous_future_id,
                        principalTable: "continuous_futures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_instruments_datasources_datasource_id",
                        column: x => x.datasource_id,
                        principalTable: "datasources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_instruments_exchanges_exchange_id",
                        column: x => x.exchange_id,
                        principalTable: "exchanges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_instruments_exchanges_primary_exchange_id",
                        column: x => x.primary_exchange_id,
                        principalTable: "exchanges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "data_update_jobs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    use_tag = table.Column<bool>(type: "boolean", nullable: false),
                    instrument_id = table.Column<int>(type: "integer", nullable: true),
                    tag_id = table.Column<int>(type: "integer", nullable: true),
                    week_days_only = table.Column<bool>(type: "boolean", nullable: false),
                    time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    frequency = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_update_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_data_update_jobs_instruments_instrument_id",
                        column: x => x.instrument_id,
                        principalTable: "instruments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_data_update_jobs_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "instrument_sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    opening_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    closing_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    instrument_id = table.Column<int>(type: "integer", nullable: false),
                    is_session_end = table.Column<bool>(type: "boolean", nullable: false),
                    opening_day = table.Column<int>(type: "integer", nullable: false),
                    closing_day = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instrument_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_instrument_sessions_instruments_instrument_id",
                        column: x => x.instrument_id,
                        principalTable: "instruments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tag_map",
                columns: table => new
                {
                    instrument_id = table.Column<int>(type: "integer", nullable: false),
                    tag_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tag_map", x => new { x.instrument_id, x.tag_id });
                    table.ForeignKey(
                        name: "fk_tag_map_instruments_instrument_id",
                        column: x => x.instrument_id,
                        principalTable: "instruments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tag_map_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_continuous_futures_instrument_id",
                table: "continuous_futures",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "ix_continuous_futures_underlying_symbol_id",
                table: "continuous_futures",
                column: "underlying_symbol_id");

            migrationBuilder.CreateIndex(
                name: "ix_countries_name",
                table: "countries",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_currencies_code",
                table: "currencies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_data_update_jobs_instrument_id",
                table: "data_update_jobs",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "ix_data_update_jobs_tag_id",
                table: "data_update_jobs",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_datasources_name",
                table: "datasources",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dividends_ex_date",
                table: "dividends",
                column: "ex_date");

            migrationBuilder.CreateIndex(
                name: "ix_dividends_symbol",
                table: "dividends",
                column: "symbol");

            migrationBuilder.CreateIndex(
                name: "ix_earnings_announcements_date",
                table: "earnings_announcements",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_earnings_announcements_symbol",
                table: "earnings_announcements",
                column: "symbol");

            migrationBuilder.CreateIndex(
                name: "ix_economic_releases_currency",
                table: "economic_releases",
                column: "currency");

            migrationBuilder.CreateIndex(
                name: "ix_economic_releases_name_country_date_time",
                table: "economic_releases",
                columns: new[] { "name", "country", "date_time" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exchange_sessions_exchange_id",
                table: "exchange_sessions",
                column: "exchange_id");

            migrationBuilder.CreateIndex(
                name: "ix_instrument_sessions_instrument_id",
                table: "instrument_sessions",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "ix_instruments_continuous_future_id",
                table: "instruments",
                column: "continuous_future_id");

            migrationBuilder.CreateIndex(
                name: "ix_instruments_datasource_id",
                table: "instruments",
                column: "datasource_id");

            migrationBuilder.CreateIndex(
                name: "ix_instruments_exchange_id",
                table: "instruments",
                column: "exchange_id");

            migrationBuilder.CreateIndex(
                name: "ix_instruments_primary_exchange_id",
                table: "instruments",
                column: "primary_exchange_id");

            migrationBuilder.CreateIndex(
                name: "ix_instruments_symbol_type_datasource_id_exchange_id_expiratio",
                table: "instruments",
                columns: new[] { "symbol", "type", "datasource_id", "exchange_id", "expiration", "strike" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tag_map_tag_id",
                table: "tag_map",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_template_sessions_template_id",
                table: "template_sessions",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_underlying_symbols_symbol",
                table: "underlying_symbols",
                column: "symbol",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_continuous_futures_instruments_instrument_id",
                table: "continuous_futures",
                column: "instrument_id",
                principalTable: "instruments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_continuous_futures_instruments_instrument_id",
                table: "continuous_futures");

            migrationBuilder.DropTable(
                name: "countries");

            migrationBuilder.DropTable(
                name: "currencies");

            migrationBuilder.DropTable(
                name: "data_update_jobs");

            migrationBuilder.DropTable(
                name: "dividends");

            migrationBuilder.DropTable(
                name: "earnings_announcements");

            migrationBuilder.DropTable(
                name: "economic_releases");

            migrationBuilder.DropTable(
                name: "exchange_sessions");

            migrationBuilder.DropTable(
                name: "instrument_sessions");

            migrationBuilder.DropTable(
                name: "tag_map");

            migrationBuilder.DropTable(
                name: "template_sessions");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "session_templates");

            migrationBuilder.DropTable(
                name: "instruments");

            migrationBuilder.DropTable(
                name: "continuous_futures");

            migrationBuilder.DropTable(
                name: "datasources");

            migrationBuilder.DropTable(
                name: "exchanges");

            migrationBuilder.DropTable(
                name: "underlying_symbols");
        }
    }
}
