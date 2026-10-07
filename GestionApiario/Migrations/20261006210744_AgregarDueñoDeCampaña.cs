using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionApiario.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDueñoDeCampaña : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsuarioId",
                table: "CAMPAÑA",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CAMPAÑA_UsuarioId",
                table: "CAMPAÑA",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_CAMPAÑA_AspNetUsers_UsuarioId",
                table: "CAMPAÑA",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CAMPAÑA_AspNetUsers_UsuarioId",
                table: "CAMPAÑA");

            migrationBuilder.DropIndex(
                name: "IX_CAMPAÑA_UsuarioId",
                table: "CAMPAÑA");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "CAMPAÑA");
        }
    }
}
