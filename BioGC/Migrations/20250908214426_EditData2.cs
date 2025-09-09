using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioGC.Migrations
{
    /// <inheritdoc />
    public partial class EditData2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RelaxationPackages_Categories_CategoryId1",
                table: "RelaxationPackages");

            migrationBuilder.DropIndex(
                name: "IX_RelaxationPackages_CategoryId1",
                table: "RelaxationPackages");

            migrationBuilder.DropColumn(
                name: "CategoryId1",
                table: "RelaxationPackages");

            migrationBuilder.AlterColumn<string>(
                name: "TitleEn",
                table: "RelaxationPackages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TitleAr",
                table: "RelaxationPackages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TitleEn",
                table: "RelaxationPackages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "TitleAr",
                table: "RelaxationPackages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId1",
                table: "RelaxationPackages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RelaxationPackages_CategoryId1",
                table: "RelaxationPackages",
                column: "CategoryId1");

            migrationBuilder.AddForeignKey(
                name: "FK_RelaxationPackages_Categories_CategoryId1",
                table: "RelaxationPackages",
                column: "CategoryId1",
                principalTable: "Categories",
                principalColumn: "Id");
        }
    }
}
