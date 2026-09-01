using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inova.Quotations.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyTaxNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "company_profile",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "company_profile");
        }
    }
}
