using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardanoTxBuilding.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "LockedAda",
                schema: "public",
                columns: table => new
                {
                    Slot = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false),
                    Index = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Datum = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LockedAda", x => new { x.Slot, x.Hash, x.Index });
                });

            migrationBuilder.CreateTable(
                name: "ReducerStates",
                schema: "public",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slot = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReducerStates", x => x.Name);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LockedAda",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ReducerStates",
                schema: "public");
        }
    }
}
