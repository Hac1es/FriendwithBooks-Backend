using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FriendwithBooksBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddBookImageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "imgurl",
                table: "books",
                newName: "imgurl3");

            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "reviewid",
                table: "reviews",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "agegroup",
                table: "books",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "discount",
                table: "books",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "imgurl1",
                table: "books",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imgurl2",
                table: "books",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "address",
                table: "users");

            migrationBuilder.DropColumn(
                name: "agegroup",
                table: "books");

            migrationBuilder.DropColumn(
                name: "discount",
                table: "books");

            migrationBuilder.DropColumn(
                name: "imgurl1",
                table: "books");

            migrationBuilder.DropColumn(
                name: "imgurl2",
                table: "books");

            migrationBuilder.RenameColumn(
                name: "imgurl3",
                table: "books",
                newName: "imgurl");

            migrationBuilder.AlterColumn<int>(
                name: "reviewid",
                table: "reviews",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
