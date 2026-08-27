using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inova.Quotations.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "quotations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotations_ParentId",
                table: "quotations",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_quotations_quotations_ParentId",
                table: "quotations",
                column: "ParentId",
                principalTable: "quotations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_quotations_quotations_ParentId",
                table: "quotations");

            migrationBuilder.DropIndex(
                name: "IX_quotations_ParentId",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "quotations");
        }
    }
}
