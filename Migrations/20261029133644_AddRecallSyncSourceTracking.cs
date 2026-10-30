using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Migrations
{
    /// <inheritdoc />
    public partial class AddRecallSyncSourceTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrugRecalls_Companies_CompanyId",
                table: "DrugRecalls");

            migrationBuilder.AlterColumn<string>(
                name: "SourceUrl",
                table: "DrugRecalls",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "DrugRecalls",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ApprovalStatus",
                table: "DrugRecalls",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "DrugRecalls",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceName",
                table: "DrugRecalls",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecallSyncLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ItemsFetched = table.Column<int>(type: "int", nullable: false),
                    ItemsSaved = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecallSyncLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrugRecalls_IsApproved_ApprovalStatus_RecallDate",
                table: "DrugRecalls",
                columns: new[] { "IsApproved", "ApprovalStatus", "RecallDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DrugRecalls_SourceName_ExternalId",
                table: "DrugRecalls",
                columns: new[] { "SourceName", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_DrugRecalls_Companies_CompanyId",
                table: "DrugRecalls",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrugRecalls_Companies_CompanyId",
                table: "DrugRecalls");

            migrationBuilder.DropTable(
                name: "RecallSyncLogs");

            migrationBuilder.DropIndex(
                name: "IX_DrugRecalls_IsApproved_ApprovalStatus_RecallDate",
                table: "DrugRecalls");

            migrationBuilder.DropIndex(
                name: "IX_DrugRecalls_SourceName_ExternalId",
                table: "DrugRecalls");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "DrugRecalls");

            migrationBuilder.DropColumn(
                name: "SourceName",
                table: "DrugRecalls");

            migrationBuilder.AlterColumn<string>(
                name: "SourceUrl",
                table: "DrugRecalls",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "DrugRecalls",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApprovalStatus",
                table: "DrugRecalls",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_DrugRecalls_Companies_CompanyId",
                table: "DrugRecalls",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
