using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioGC.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIdToRelaxationContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "RelaxationContents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RelaxationContents_ProductId",
                table: "RelaxationContents",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_RelaxationContents_Products_ProductId",
                table: "RelaxationContents",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RelaxationContents_Products_ProductId",
                table: "RelaxationContents");

            migrationBuilder.DropIndex(
                name: "IX_RelaxationContents_ProductId",
                table: "RelaxationContents");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "RelaxationContents");
        }
    }
}
