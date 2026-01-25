using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatAppProj.Migrations
{
    /// <inheritdoc />
    public partial class UserConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserConfigurationId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UserConfiguration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AllowRequest = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowBeingAddedToGroup = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowOnlyFriendsChat = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConfiguration", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UserConfigurationId",
                table: "AspNetUsers",
                column: "UserConfigurationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_UserConfiguration_UserConfigurationId",
                table: "AspNetUsers",
                column: "UserConfigurationId",
                principalTable: "UserConfiguration",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_UserConfiguration_UserConfigurationId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "UserConfiguration");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_UserConfigurationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UserConfigurationId",
                table: "AspNetUsers");
        }
    }
}
