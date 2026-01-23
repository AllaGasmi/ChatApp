using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatAppProj.Migrations
{
    /// <inheritdoc />
    public partial class modifConversationParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "ConversationParticipants",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "ConversationParticipants");
        }
    }
}
