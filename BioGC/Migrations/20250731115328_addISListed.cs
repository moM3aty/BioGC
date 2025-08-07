using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioGC.Migrations
{
    /// <inheritdoc />
    public partial class addISListed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsListed",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsListed",
                table: "Products");
        }
    }
}
