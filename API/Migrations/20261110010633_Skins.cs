using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class Skins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Skins",
                keyColumn: "Id",
                keyValue: "default",
                column: "EnabledInSelector",
                value: false);

            migrationBuilder.UpdateData(
                table: "Skins",
                keyColumn: "Id",
                keyValue: "skin_01",
                column: "EnabledInSelector",
                value: false);

            migrationBuilder.UpdateData(
                table: "Skins",
                keyColumn: "Id",
                keyValue: "skin_02",
                column: "EnabledInSelector",
                value: false);

            migrationBuilder.UpdateData(
                table: "Skins",
                keyColumn: "Id",
                keyValue: "skin_03",
                column: "EnabledInSelector",
                value: false);

            migrationBuilder.InsertData(
                table: "Skins",
                columns: new[] { "Id", "DisplayName", "EnabledInSelector", "SortOrder" },
                values: new object[,]
                {
                    { "maria", "Maria", true, 4 },
                    { "zombie", "Zombie", true, 5 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Skins",
                keyColumn: "Id",
                keyValue: "maria");

            migrationBuilder.DeleteData(
                table: "Skins",
                keyColumn: "Id",
                keyValue: "zombie");

            migrationBuilder.UpdateData(
                table: "Skins",
                keyColumn: "Id",
                keyValue: "default",
                column: "EnabledInSelector",
                value: true);

            migrationBuilder.UpdateData(
                table: "Skins",
                keyColumn: "Id",
                keyValue: "skin_01",
                column: "EnabledInSelector",
                value: true);

            migrationBuilder.UpdateData(
                table: "Skins",
                keyColumn: "Id",
                keyValue: "skin_02",
                column: "EnabledInSelector",
                value: true);

            migrationBuilder.UpdateData(
                table: "Skins",
                keyColumn: "Id",
                keyValue: "skin_03",
                column: "EnabledInSelector",
                value: true);
        }
    }
}
