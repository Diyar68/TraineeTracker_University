using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraineeTracker.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExternalId = table.Column<long>(type: "INTEGER", nullable: false),
                    Number = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Effort = table.Column<decimal>(type: "TEXT", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    ExternalLink = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TraineeMentorAssignments",
                columns: table => new
                {
                    TraineeId = table.Column<string>(type: "TEXT", nullable: false),
                    MentorId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraineeMentorAssignments", x => new { x.TraineeId, x.MentorId });
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsClosed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TraineeStartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TraineeEndDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LectionProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TraineeId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    LectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RejectionNote = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Rating = table.Column<int>(type: "INTEGER", nullable: true),
                    IndividualOrderIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SkippedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LectionProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LectionProgresses_Lections_LectionId",
                        column: x => x.LectionId,
                        principalTable: "Lections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LectionProgressId = table.Column<int>(type: "INTEGER", nullable: false),
                    DifficultyRating = table.Column<int>(type: "INTEGER", nullable: false),
                    PriorKnowledge = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ActualEffortDays = table.Column<decimal>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feedbacks_LectionProgresses_LectionProgressId",
                        column: x => x.LectionProgressId,
                        principalTable: "LectionProgresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_LectionProgressId",
                table: "Feedbacks",
                column: "LectionProgressId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LectionProgresses_LectionId",
                table: "LectionProgresses",
                column: "LectionId");

            migrationBuilder.CreateIndex(
                name: "IX_LectionProgresses_TraineeId_LectionId",
                table: "LectionProgresses",
                columns: new[] { "TraineeId", "LectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lections_ExternalId",
                table: "Lections",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lections_Number",
                table: "Lections",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "TraineeMentorAssignments");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "LectionProgresses");

            migrationBuilder.DropTable(
                name: "Lections");
        }
    }
}
