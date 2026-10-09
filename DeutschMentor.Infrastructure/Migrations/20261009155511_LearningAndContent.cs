using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeutschMentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LearningAndContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Levels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    PassMarkPercent = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Levels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LevelPasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScorePercent = table.Column<int>(type: "int", nullable: false),
                    PassedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LevelPasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LevelPasses_Levels_LevelId",
                        column: x => x.LevelId,
                        principalTable: "Levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LevelPasses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Modules_Levels_LevelId",
                        column: x => x.LevelId,
                        principalTable: "Levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Units_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lessons_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GrammarTopics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrammarTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrammarTopics_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLessonProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepsSeenMask = table.Column<int>(type: "int", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLessonProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLessonProgress_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserLessonProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VocabularyItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    German = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Article = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PartOfSpeech = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    English = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Arabic = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ExampleGerman = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ExampleEnglish = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VocabularyItems_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VocabularyItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IntervalDays = table.Column<int>(type: "int", nullable: false),
                    Repetitions = table.Column<int>(type: "int", nullable: false),
                    EaseFactor = table.Column<double>(type: "float", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewItems_VocabularyItems_VocabularyItemId",
                        column: x => x.VocabularyItemId,
                        principalTable: "VocabularyItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GrammarTopics_LessonId_Order",
                table: "GrammarTopics",
                columns: new[] { "LessonId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_Code",
                table: "Lessons",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_UnitId_Order",
                table: "Lessons",
                columns: new[] { "UnitId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LevelPasses_LevelId",
                table: "LevelPasses",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_LevelPasses_UserId_LevelId",
                table: "LevelPasses",
                columns: new[] { "UserId", "LevelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Levels_Code",
                table: "Levels",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Levels_Order",
                table: "Levels",
                column: "Order",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modules_LevelId_Order",
                table: "Modules",
                columns: new[] { "LevelId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewItems_UserId_DueAtUtc",
                table: "ReviewItems",
                columns: new[] { "UserId", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewItems_UserId_VocabularyItemId",
                table: "ReviewItems",
                columns: new[] { "UserId", "VocabularyItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewItems_VocabularyItemId",
                table: "ReviewItems",
                column: "VocabularyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_ModuleId_Order",
                table: "Units",
                columns: new[] { "ModuleId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLessonProgress_LessonId",
                table: "UserLessonProgress",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLessonProgress_UserId_LessonId",
                table: "UserLessonProgress",
                columns: new[] { "UserId", "LessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyItems_LessonId_Order",
                table: "VocabularyItems",
                columns: new[] { "LessonId", "Order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GrammarTopics");

            migrationBuilder.DropTable(
                name: "LevelPasses");

            migrationBuilder.DropTable(
                name: "ReviewItems");

            migrationBuilder.DropTable(
                name: "UserLessonProgress");

            migrationBuilder.DropTable(
                name: "VocabularyItems");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "Levels");
        }
    }
}
