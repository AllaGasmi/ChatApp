using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatAppProj.Migrations
{
    /// <inheritdoc />
    public partial class FixUserConfigurationRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropForeignKey(
            //     name: "FK_AspNetUsers_UserConfiguration_UserConfigurationId",
            //     table: "AspNetUsers");

            // migrationBuilder.AddColumn<string>(
            //     name: "GroupPicture",
            //     table: "Conversations",
            //     type: "longtext",
            //     nullable: true)
            //     .Annotation("MySql:CharSet", "utf8mb4");

            // migrationBuilder.AlterColumn<int>(
            //     name: "UserConfigurationId",
            //     table: "AspNetUsers",
            //     type: "int",
            //     nullable: true,
            //     oldClrType: typeof(int),
            //     oldType: "int");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_AspNetUsers_UserConfiguration_UserConfigurationId",
            //     table: "AspNetUsers",
            //     column: "UserConfigurationId",
            //     principalTable: "UserConfiguration",
            //     principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_UserConfiguration_UserConfigurationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GroupPicture",
                table: "Conversations");

            migrationBuilder.AlterColumn<int>(
                name: "UserConfigurationId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_UserConfiguration_UserConfigurationId",
                table: "AspNetUsers",
                column: "UserConfigurationId",
                principalTable: "UserConfiguration",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
