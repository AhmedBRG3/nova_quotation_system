using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Inova.Quotations.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "company_profile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Phone = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Website = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LogoPath = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DefaultVatPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    DefaultTerms = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_profile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quotations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobNo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SubjectEn = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SubjectAr = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    QuoteDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CompanyEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CompanyAr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContactPersonEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactPersonAr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContactDetails = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VatPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Terms = table.Column<string>(type: "text", nullable: false),
                    NoteEn = table.Column<string>(type: "text", nullable: true),
                    NoteAr = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quotation_images",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuotationId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Caption = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quotation_images_quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quotation_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuotationId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DescriptionEn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DescriptionAr = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quotation_items_quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quotation_images_QuotationId",
                table: "quotation_images",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_items_QuotationId",
                table: "quotation_items",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_JobNo",
                table: "quotations",
                column: "JobNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_profile");

            migrationBuilder.DropTable(
                name: "quotation_images");

            migrationBuilder.DropTable(
                name: "quotation_items");

            migrationBuilder.DropTable(
                name: "quotations");
        }
    }
}
