using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NameParser.Migrations
{
    /// <inheritdoc />
    public partial class AddRecentRaceVmaLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Classifications_RaceId",
                table: "Classifications");

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsMember = table.Column<bool>(type: "bit", nullable: false),
                    IsChallenger = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecentRaceVmaLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberFirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MemberLastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AverageVma = table.Column<double>(type: "float", nullable: true),
                    RaceCount = table.Column<int>(type: "int", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecentRaceVmaLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VmaExtrapolationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberFirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MemberLastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CalibrationPointCount = table.Column<int>(type: "int", nullable: false),
                    TargetSpeed = table.Column<double>(type: "float", nullable: true),
                    Slope = table.Column<double>(type: "float", nullable: true),
                    Intercept = table.Column<double>(type: "float", nullable: true),
                    PredictedVma = table.Column<double>(type: "float", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VmaExtrapolationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemberVmaHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    Vma = table.Column<double>(type: "float", nullable: false),
                    RecordedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsExtrapolated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberVmaHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberVmaHistories_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecentRaceVmaLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecentRaceVmaLogId = table.Column<int>(type: "int", nullable: false),
                    RaceName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DistanceKm = table.Column<int>(type: "int", nullable: true),
                    RaceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Speed = table.Column<double>(type: "float", nullable: false),
                    EstimatedVma = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecentRaceVmaLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecentRaceVmaLines_RecentRaceVmaLogs_RecentRaceVmaLogId",
                        column: x => x.RecentRaceVmaLogId,
                        principalTable: "RecentRaceVmaLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Classifications_RaceId_MemberFirstName_MemberLastName_Position",
                table: "Classifications",
                columns: new[] { "RaceId", "MemberFirstName", "MemberLastName", "Position" },
                unique: true,
                filter: "[Position] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Members_LastName_FirstName",
                table: "Members",
                columns: new[] { "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberVmaHistories_MemberId",
                table: "MemberVmaHistories",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_RecentRaceVmaLines_RecentRaceVmaLogId",
                table: "RecentRaceVmaLines",
                column: "RecentRaceVmaLogId");

            migrationBuilder.CreateIndex(
                name: "IX_RecentRaceVmaLogs_MemberLastName_MemberFirstName",
                table: "RecentRaceVmaLogs",
                columns: new[] { "MemberLastName", "MemberFirstName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberVmaHistories");

            migrationBuilder.DropTable(
                name: "RecentRaceVmaLines");

            migrationBuilder.DropTable(
                name: "VmaExtrapolationLogs");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "RecentRaceVmaLogs");

            migrationBuilder.DropIndex(
                name: "IX_Classifications_RaceId_MemberFirstName_MemberLastName_Position",
                table: "Classifications");

            migrationBuilder.CreateIndex(
                name: "IX_Classifications_RaceId",
                table: "Classifications",
                column: "RaceId");
        }
    }
}
