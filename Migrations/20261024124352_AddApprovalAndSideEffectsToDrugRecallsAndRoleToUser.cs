using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalAndSideEffectsToDrugRecallsAndRoleToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "DrugRecalls",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "DrugRecalls",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                table: "DrugRecalls",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "DrugRecalls",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "DrugRecalls",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SideEffects",
                table: "DrugRecalls",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "DrugRecalls",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedByUserId",
                table: "DrugRecalls",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrugRecalls_ApprovedByUserId",
                table: "DrugRecalls",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugRecalls_SubmittedByUserId",
                table: "DrugRecalls",
                column: "SubmittedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DrugRecalls_Users_ApprovedByUserId",
                table: "DrugRecalls",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DrugRecalls_Users_SubmittedByUserId",
                table: "DrugRecalls",
                column: "SubmittedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrugRecalls_Users_ApprovedByUserId",
                table: "DrugRecalls");

            migrationBuilder.DropForeignKey(
                name: "FK_DrugRecalls_Users_SubmittedByUserId",
                table: "DrugRecalls");

            migrationBuilder.DropIndex(
                name: "IX_DrugRecalls_ApprovedByUserId",
                table: "DrugRecalls");

            migrationBuilder.DropIndex(
                name: "IX_DrugRecalls_SubmittedByUserId",
                table: "DrugRecalls");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "DrugRecalls");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "DrugRecalls");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "DrugRecalls");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "DrugRecalls");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "DrugRecalls");

            migrationBuilder.DropColumn(
                name: "SideEffects",
                table: "DrugRecalls");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "DrugRecalls");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "DrugRecalls");
        }
    }
}
