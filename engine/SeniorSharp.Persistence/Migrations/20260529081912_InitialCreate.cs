using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeniorSharp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "axis_scores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Axis = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Level = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    Rationale = table.Column<string>(type: "text", nullable: false),
                    Citations = table.Column<string>(type: "jsonb", nullable: false),
                    RunIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_axis_scores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "prompt_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Ref = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GraphVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PromptVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModelId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "skill_nodes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Layer = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Axes = table.Column<string>(type: "jsonb", nullable: false),
                    Prerequisites = table.Column<string>(type: "jsonb", nullable: false),
                    MasteryFocus = table.Column<string>(type: "jsonb", nullable: false),
                    SeniorSignal = table.Column<string>(type: "text", nullable: false),
                    ExampleProbe = table.Column<string>(type: "text", nullable: false),
                    GraphVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_nodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "verdicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallLevel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    RunCount = table.Column<int>(type: "integer", nullable: false),
                    Spread = table.Column<double>(type: "double precision", nullable: false),
                    ProfileJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verdicts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rounds_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skill_masteries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Recognition = table.Column<double>(type: "double precision", nullable: false),
                    Application = table.Column<double>(type: "double precision", nullable: false),
                    Depth = table.Column<double>(type: "double precision", nullable: false),
                    EvidenceTurnIds = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_masteries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_skill_masteries_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "turns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_turns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_turns_rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_axis_scores_SessionId_Axis_RunIndex",
                table: "axis_scores",
                columns: new[] { "SessionId", "Axis", "RunIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_prompt_versions_Key_Version",
                table: "prompt_versions",
                columns: new[] { "Key", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rounds_SessionId_Order",
                table: "rounds",
                columns: new[] { "SessionId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_sessions_CandidateRef",
                table: "sessions",
                column: "CandidateRef");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_Status",
                table: "sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_skill_masteries_SessionId_SkillId",
                table: "skill_masteries",
                columns: new[] { "SessionId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_skill_nodes_GraphVersion",
                table: "skill_nodes",
                column: "GraphVersion");

            migrationBuilder.CreateIndex(
                name: "IX_skill_nodes_Layer",
                table: "skill_nodes",
                column: "Layer");

            migrationBuilder.CreateIndex(
                name: "IX_turns_RoundId_CreatedAt",
                table: "turns",
                columns: new[] { "RoundId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_verdicts_SessionId",
                table: "verdicts",
                column: "SessionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "axis_scores");

            migrationBuilder.DropTable(
                name: "prompt_versions");

            migrationBuilder.DropTable(
                name: "skill_masteries");

            migrationBuilder.DropTable(
                name: "skill_nodes");

            migrationBuilder.DropTable(
                name: "turns");

            migrationBuilder.DropTable(
                name: "verdicts");

            migrationBuilder.DropTable(
                name: "rounds");

            migrationBuilder.DropTable(
                name: "sessions");
        }
    }
}
