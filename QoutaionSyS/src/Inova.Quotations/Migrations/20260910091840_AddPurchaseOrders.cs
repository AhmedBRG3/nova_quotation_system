using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Inova.Quotations.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultPoTerms",
                table: "company_profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PoNo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PoDate = table.Column<DateOnly>(type: "date", nullable: false),
                    QuotationId = table.Column<int>(type: "integer", nullable: true),
                    SubjectEn = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SubjectAr = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    VendorNameEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VendorNameAr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VendorContactPersonEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VendorContactPersonAr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VendorContactDetails = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VendorTaxNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    VatPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Terms = table.Column<string>(type: "text", nullable: false),
                    NoteEn = table.Column<string>(type: "text", nullable: true),
                    NoteAr = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_orders_quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseOrderId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DescriptionEn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DescriptionAr = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_order_items_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_items_PurchaseOrderId",
                table: "purchase_order_items",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_PoNo",
                table: "purchase_orders",
                column: "PoNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_QuotationId",
                table: "purchase_orders",
                column: "QuotationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchase_order_items");

            migrationBuilder.DropTable(
                name: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "DefaultPoTerms",
                table: "company_profile");
        }
    }
}
