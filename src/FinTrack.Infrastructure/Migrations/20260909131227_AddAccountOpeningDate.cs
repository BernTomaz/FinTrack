using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountOpeningDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "OpeningDate",
                table: "Accounts",
                type: "date",
                nullable: false,
                defaultValueSql: "CONVERT(date, SYSUTCDATETIME())");

            migrationBuilder.Sql("UPDATE Accounts SET OpeningDate = CONVERT(date, CreatedAt)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpeningDate",
                table: "Accounts");
        }
    }
}
