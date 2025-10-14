using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EtalDeJeux.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_email = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    message_id = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "sent"),
                    error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_logs_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_email_logs_order",
                table: "email_logs",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "idx_email_logs_to",
                table: "email_logs",
                column: "to_email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_logs");
        }
    }
}
