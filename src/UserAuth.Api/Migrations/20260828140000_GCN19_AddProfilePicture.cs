using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UserAuth.Api.Data;

#nullable disable

namespace UserAuth.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260828140000_GCN19_AddProfilePicture")]
public partial class GCN19_AddProfilePicture : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ProfilePicturePath",
            table: "Users",
            type: "varchar(500)",
            maxLength: 500,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ProfilePicturePath",
            table: "Users");
    }
}
