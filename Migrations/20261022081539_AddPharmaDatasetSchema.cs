using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Migrations
{
    /// <inheritdoc />
    public partial class AddPharmaDatasetSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DocumentedRecallEvents",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExportCapability",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternationalRecallRecords",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DrugCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Definition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrescriptionRequired = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WhoMaySell = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LabelWarning = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegalReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugCategories", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrugCategories");

            migrationBuilder.DropColumn(
                name: "DocumentedRecallEvents",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ExportCapability",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "InternationalRecallRecords",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "Companies");
        }
    }
}
