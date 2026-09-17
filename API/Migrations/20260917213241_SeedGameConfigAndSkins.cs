using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class SeedGameConfigAndSkins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "GameConfig",
                columns: new[] { "Id", "BalanceBaseMaxBombs", "BalanceBaseRange", "BalanceBaseSpeed", "BalanceBombRequestTimeoutSeconds", "BalanceFuseSeconds", "BalanceMatchDurationSeconds", "BalanceMaxBombs", "BalanceMaxRange", "BalanceMaxSpeedLevel", "BalanceRemoteMaxSeconds", "BalanceThrowDistanceCells", "BalanceThrowDurationSeconds", "BalanceTimedFuseSeconds", "BalanceWallCooldownSeconds", "BalanceWallDurationSeconds", "ConfigVersion", "EnabledMapIds", "MinClientVersion" },
                values: new object[] { 1, 1, 1, 4f, 2f, 2.5f, 180, 6, 8, 4, 15f, 3, 0.5f, 5f, 8f, 4f, 1, null, "0.1.0" });

            migrationBuilder.InsertData(
                table: "Skins",
                columns: new[] { "Id", "DisplayName", "EnabledInSelector", "SortOrder" },
                values: new object[,]
                {
                    { "default", "Default", true, 0 },
                    { "skin_01", "Skin 01", true, 1 },
                    { "skin_02", "Skin 02", true, 2 },
                    { "skin_03", "Skin 03", true, 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "GameConfig",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Skins",
                keyColumn: "Id",
                keyValue: "default");

            migrationBuilder.DeleteData(
                table: "Skins",
                keyColumn: "Id",
                keyValue: "skin_01");

            migrationBuilder.DeleteData(
                table: "Skins",
                keyColumn: "Id",
                keyValue: "skin_02");

            migrationBuilder.DeleteData(
                table: "Skins",
                keyColumn: "Id",
                keyValue: "skin_03");
        }
    }
}
