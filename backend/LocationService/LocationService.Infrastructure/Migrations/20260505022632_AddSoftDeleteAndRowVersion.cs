using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocationService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteAndRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Locations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Locations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Locations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // NOTE: "xmin" is a PostgreSQL system column that exists on every table.
            // We map Location.RowVersion to it for optimistic concurrency. No AddColumn
            // needed — Postgres already provides it.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Locations");

            // "xmin" is a system column — never dropped.
        }
    }
}
