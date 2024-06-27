using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityDashboard_Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Repositories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repositories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ToolRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tool = table.Column<int>(type: "INTEGER", nullable: false),
                    RuleId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    ShortDescription = table.Column<string>(type: "TEXT", nullable: true),
                    FullDescription = table.Column<string>(type: "TEXT", nullable: true),
                    HelpText = table.Column<string>(type: "TEXT", nullable: true),
                    HelpUri = table.Column<string>(type: "TEXT", nullable: true),
                    CvssVector = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PipelineRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PipelineId = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Branch = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CommitHash = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RepositoryId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PipelineRuns_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PipelineFindings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsFalsePositive = table.Column<bool>(type: "INTEGER", nullable: false),
                    PipelineRunId = table.Column<long>(type: "INTEGER", nullable: false),
                    ToolRuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    DetectionType = table.Column<int>(type: "INTEGER", nullable: false),
                    CweIds = table.Column<string>(type: "TEXT", nullable: false),
                    SecurityAdvisories = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PipelineFindings_PipelineRuns_PipelineRunId",
                        column: x => x.PipelineRunId,
                        principalTable: "PipelineRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PipelineFindings_ToolRules_ToolRuleId",
                        column: x => x.ToolRuleId,
                        principalTable: "ToolRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToolFindings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tool = table.Column<int>(type: "INTEGER", nullable: false),
                    Snippet = table.Column<string>(type: "TEXT", nullable: false),
                    SnippetStartLine = table.Column<int>(type: "INTEGER", nullable: false),
                    SnippetStartColumn = table.Column<int>(type: "INTEGER", nullable: true),
                    SnippetEndLine = table.Column<int>(type: "INTEGER", nullable: true),
                    SnippetEndColumn = table.Column<int>(type: "INTEGER", nullable: true),
                    PipelineRunId = table.Column<long>(type: "INTEGER", nullable: false),
                    ToolRuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    PipelineFindingId = table.Column<long>(type: "INTEGER", nullable: true),
                    Cvss31Vector = table.Column<string>(type: "TEXT", nullable: true),
                    Cvss40Vector = table.Column<string>(type: "TEXT", nullable: true),
                    Line = table.Column<int>(type: "INTEGER", nullable: true),
                    Kind = table.Column<string>(type: "TEXT", nullable: true),
                    Technique = table.Column<string>(type: "TEXT", nullable: true),
                    SourceMethod = table.Column<string>(type: "TEXT", nullable: true),
                    CommitHash = table.Column<string>(type: "TEXT", nullable: true),
                    CommitMessage = table.Column<string>(type: "TEXT", nullable: true),
                    Author = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    CommitDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Attack = table.Column<string>(type: "TEXT", nullable: true),
                    WebRequest = table.Column<string>(type: "TEXT", nullable: true),
                    WebResponse = table.Column<string>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    DetectionType = table.Column<int>(type: "INTEGER", nullable: false),
                    CweIds = table.Column<string>(type: "TEXT", nullable: false),
                    SecurityAdvisories = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolFindings_PipelineFindings_PipelineFindingId",
                        column: x => x.PipelineFindingId,
                        principalTable: "PipelineFindings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ToolFindings_PipelineRuns_PipelineRunId",
                        column: x => x.PipelineRunId,
                        principalTable: "PipelineRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ToolFindings_ToolRules_ToolRuleId",
                        column: x => x.ToolRuleId,
                        principalTable: "ToolRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PipelineFindings_PipelineRunId",
                table: "PipelineFindings",
                column: "PipelineRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineFindings_ToolRuleId",
                table: "PipelineFindings",
                column: "ToolRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineRuns_RepositoryId",
                table: "PipelineRuns",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolFindings_PipelineFindingId",
                table: "ToolFindings",
                column: "PipelineFindingId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolFindings_PipelineRunId",
                table: "ToolFindings",
                column: "PipelineRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolFindings_ToolRuleId",
                table: "ToolFindings",
                column: "ToolRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolRules_RuleId_Tool",
                table: "ToolRules",
                columns: new[] { "RuleId", "Tool" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ToolFindings");

            migrationBuilder.DropTable(
                name: "PipelineFindings");

            migrationBuilder.DropTable(
                name: "PipelineRuns");

            migrationBuilder.DropTable(
                name: "ToolRules");

            migrationBuilder.DropTable(
                name: "Repositories");
        }
    }
}
