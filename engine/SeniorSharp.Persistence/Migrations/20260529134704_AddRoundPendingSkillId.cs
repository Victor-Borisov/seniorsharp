using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeniorSharp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoundPendingSkillId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingSkillId",
                table: "rounds",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingSkillId",
                table: "rounds");
        }
    }
}
