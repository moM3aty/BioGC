using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioGC.Migrations
{
    /// <inheritdoc />
    public partial class AddRelaxationPlaylists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioUrl",
                table: "RelaxationContents");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "RelaxationContents");

            migrationBuilder.CreateTable(
                name: "RelaxationAudios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AudioUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelaxationContentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelaxationAudios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RelaxationAudios_RelaxationContents_RelaxationContentId",
                        column: x => x.RelaxationContentId,
                        principalTable: "RelaxationContents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RelaxationVideos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelaxationContentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelaxationVideos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RelaxationVideos_RelaxationContents_RelaxationContentId",
                        column: x => x.RelaxationContentId,
                        principalTable: "RelaxationContents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RelaxationAudios_RelaxationContentId",
                table: "RelaxationAudios",
                column: "RelaxationContentId");

            migrationBuilder.CreateIndex(
                name: "IX_RelaxationVideos_RelaxationContentId",
                table: "RelaxationVideos",
                column: "RelaxationContentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RelaxationAudios");

            migrationBuilder.DropTable(
                name: "RelaxationVideos");

            migrationBuilder.AddColumn<string>(
                name: "AudioUrl",
                table: "RelaxationContents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "RelaxationContents",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
