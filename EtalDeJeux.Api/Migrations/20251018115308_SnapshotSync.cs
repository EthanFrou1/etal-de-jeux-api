using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EtalDeJeux.Api.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "idx_email_logs_to",
                table: "email_logs",
                newName: "ix_email_logs_to_email");

            migrationBuilder.RenameIndex(
                name: "idx_email_logs_order",
                table: "email_logs",
                newName: "ix_email_logs_order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ix_email_logs_to_email",
                table: "email_logs",
                newName: "idx_email_logs_to");

            migrationBuilder.RenameIndex(
                name: "ix_email_logs_order_id",
                table: "email_logs",
                newName: "idx_email_logs_order");
        }
    }
}
