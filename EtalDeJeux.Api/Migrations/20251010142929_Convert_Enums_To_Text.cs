using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EtalDeJeux.Api.Migrations
{
    /// <inheritdoc />
    public partial class Convert_Enums_To_Text : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:public.file_kind", "pdf,image,zip,rules,scenario");

            migrationBuilder.AlterColumn<string>(
                name: "kind",
                table: "product_files",
                type: "text",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "file_kind");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:public.file_kind", "pdf,image,zip,rules,scenario");

            migrationBuilder.AlterColumn<int>(
                name: "kind",
                table: "product_files",
                type: "file_kind",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "text",
                oldMaxLength: 40,
                oldNullable: true);
        }
    }
}
