using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeutschMentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class db3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReadingPassages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    BodyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingPassages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadingPassages_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WritingTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    SampleAnswer = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MinWords = table.Column<int>(type: "int", nullable: false),
                    PassPercent = table.Column<int>(type: "int", nullable: false),
                    RulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WritingTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WritingTasks_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReadingPassageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExplanationEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExplanationAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exercises_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Exercises_ReadingPassages_ReadingPassageId",
                        column: x => x.ReadingPassageId,
                        principalTable: "ReadingPassages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WritingSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WritingTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    WordCount = table.Column<int>(type: "int", nullable: false),
                    ScorePercent = table.Column<int>(type: "int", nullable: false),
                    Passed = table.Column<bool>(type: "bit", nullable: false),
                    FeedbackJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TeacherFeedback = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TeacherScorePercent = table.Column<int>(type: "int", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WritingSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WritingSubmissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WritingSubmissions_WritingTasks_WritingTaskId",
                        column: x => x.WritingTaskId,
                        principalTable: "WritingTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnswerJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseAttempts_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseAttempts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseAttempts_ExerciseId",
                table: "ExerciseAttempts",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseAttempts_UserId_ExerciseId",
                table: "ExerciseAttempts",
                columns: new[] { "UserId", "ExerciseId" });

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_LessonId_ReadingPassageId_Order",
                table: "Exercises",
                columns: new[] { "LessonId", "ReadingPassageId", "Order" },
                unique: true,
                filter: "[ReadingPassageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_ReadingPassageId",
                table: "Exercises",
                column: "ReadingPassageId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingPassages_LessonId_Order",
                table: "ReadingPassages",
                columns: new[] { "LessonId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WritingSubmissions_ReviewStatus",
                table: "WritingSubmissions",
                column: "ReviewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_WritingSubmissions_UserId_WritingTaskId",
                table: "WritingSubmissions",
                columns: new[] { "UserId", "WritingTaskId" });

            migrationBuilder.CreateIndex(
                name: "IX_WritingSubmissions_WritingTaskId",
                table: "WritingSubmissions",
                column: "WritingTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_WritingTasks_LessonId_Order",
                table: "WritingTasks",
                columns: new[] { "LessonId", "Order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseAttempts");

            migrationBuilder.DropTable(
                name: "WritingSubmissions");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "WritingTasks");

            migrationBuilder.DropTable(
                name: "ReadingPassages");
        }
    }
}
