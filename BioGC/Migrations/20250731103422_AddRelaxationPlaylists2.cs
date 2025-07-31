using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioGC.Migrations
{
    /// <inheritdoc />
    public partial class AddRelaxationPlaylists2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VideoUrl",
                table: "RelaxationVideos",
                newName: "VideoGuid");

            migrationBuilder.RenameColumn(
                name: "AudioUrl",
                table: "RelaxationAudios",
                newName: "AudioGuid");

            migrationBuilder.AddColumn<int>(
                name: "LibraryId",
                table: "RelaxationVideos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LibraryId",
                table: "RelaxationAudios",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LibraryId",
                table: "RelaxationVideos");

            migrationBuilder.DropColumn(
                name: "LibraryId",
                table: "RelaxationAudios");

            migrationBuilder.RenameColumn(
                name: "VideoGuid",
                table: "RelaxationVideos",
                newName: "VideoUrl");

            migrationBuilder.RenameColumn(
                name: "AudioGuid",
                table: "RelaxationAudios",
                newName: "AudioUrl");
        }
    }
}
