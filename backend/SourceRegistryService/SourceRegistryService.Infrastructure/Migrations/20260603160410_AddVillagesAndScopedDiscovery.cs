using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SourceRegistryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVillagesAndScopedDiscovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscoveryRuns_Hromadas_HromadaId",
                table: "DiscoveryRuns");

            migrationBuilder.DropIndex(
                name: "IX_DiscoveryRuns_HromadaId",
                table: "DiscoveryRuns");

            migrationBuilder.RenameColumn(
                name: "HromadaId",
                table: "DiscoveryRuns",
                newName: "ScopeId");

            migrationBuilder.AddColumn<string>(
                name: "KatottgCode",
                table: "Oblasts",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KatottgCode",
                table: "Hromadas",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ScopeType",
                table: "DiscoveryRuns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Villages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HromadaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NameUk = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    KatottgCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Villages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Villages_Hromadas_HromadaId",
                        column: x => x.HromadaId,
                        principalTable: "Hromadas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryRuns_ScopeType_ScopeId",
                table: "DiscoveryRuns",
                columns: new[] { "ScopeType", "ScopeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Villages_HromadaId_Slug",
                table: "Villages",
                columns: new[] { "HromadaId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Villages");

            migrationBuilder.DropIndex(
                name: "IX_DiscoveryRuns_ScopeType_ScopeId",
                table: "DiscoveryRuns");

            migrationBuilder.DropColumn(
                name: "KatottgCode",
                table: "Oblasts");

            migrationBuilder.DropColumn(
                name: "KatottgCode",
                table: "Hromadas");

            migrationBuilder.DropColumn(
                name: "ScopeType",
                table: "DiscoveryRuns");

            migrationBuilder.RenameColumn(
                name: "ScopeId",
                table: "DiscoveryRuns",
                newName: "HromadaId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryRuns_HromadaId",
                table: "DiscoveryRuns",
                column: "HromadaId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscoveryRuns_Hromadas_HromadaId",
                table: "DiscoveryRuns",
                column: "HromadaId",
                principalTable: "Hromadas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
