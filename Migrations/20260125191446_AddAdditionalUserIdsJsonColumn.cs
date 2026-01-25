using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatAppProj.Migrations
{
    /// <inheritdoc />
    public partial class AddAdditionalUserIdsJsonColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AdditionalUserIds",
                table: "ConversationRequests",
                newName: "AdditionalUserIdsJson");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AdditionalUserIdsJson",
                table: "ConversationRequests",
                newName: "AdditionalUserIds");
        }
    }
}
