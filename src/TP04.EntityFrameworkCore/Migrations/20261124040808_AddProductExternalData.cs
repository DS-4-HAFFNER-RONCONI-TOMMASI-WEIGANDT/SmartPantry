using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TP04.Migrations
{
    /// <inheritdoc />
    public partial class AddProductExternalData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllergenTags",
                table: "AppProducts",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Categories",
                table: "AppProducts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExternalDataUpdatedAt",
                table: "AppProducts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "AppProducts",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IngredientsText",
                table: "AppProducts",
                type: "nvarchar(max)",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NovaGroup",
                table: "AppProducts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NutriScore",
                table: "AppProducts",
                type: "nvarchar(1)",
                maxLength: 1,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllergenTags",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "Categories",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "ExternalDataUpdatedAt",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "IngredientsText",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "NovaGroup",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "NutriScore",
                table: "AppProducts");
        }
    }
}
