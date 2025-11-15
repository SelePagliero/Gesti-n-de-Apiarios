using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionApiario.Migrations
{
    /// <inheritdoc />
    public partial class eliminacionProductoPorEnfermedad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ALIMENTOS",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    UsuarioAlta = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FechaAlta = table.Column<DateTime>(type: "datetime", nullable: true),
                    UsuarioBaja = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FechaBaja = table.Column<DateTime>(type: "datetime", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime", nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ALIMENTO__06370DAD793DF6FA", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "APIARIO",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Empresa = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    UsuarioAlta = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FechaAlta = table.Column<DateTime>(type: "datetime", nullable: true),
                    UsuarioBaja = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FechaBaja = table.Column<DateTime>(type: "datetime", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime", nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Latitud = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Longitud = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__APIARIO__06370DADE50DDD3D", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "CAMPAÑA",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Año = table.Column<int>(type: "int", nullable: true),
                    Responsable = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    UsuarioAlta = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FechaAlta = table.Column<DateTime>(type: "datetime", nullable: true),
                    UsuarioBaja = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FechaBaja = table.Column<DateTime>(type: "datetime", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime", nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CAMPAÑA__06370DAD7B4D3DD6", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "ENFERMEDAD",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    UsuarioAlta = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FechaAlta = table.Column<DateTime>(type: "datetime", nullable: true),
                    UsuarioBaja = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FechaBaja = table.Column<DateTime>(type: "datetime", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime", nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ENFERMED__06370DAD59FB3587", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "PRODUCTOS",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    UsuarioAlta = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FechaAlta = table.Column<DateTime>(type: "datetime", nullable: true),
                    UsuarioBaja = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FechaBaja = table.Column<DateTime>(type: "datetime", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime", nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PRODUCTO__06370DAD5655F7E5", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "CONTROLES",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodCampaña = table.Column<int>(type: "int", nullable: true),
                    CodApiario = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: true),
                    CantDeColmenas = table.Column<int>(type: "int", nullable: true),
                    CodAlimento = table.Column<int>(type: "int", nullable: true),
                    CantidadAlimento = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CodEnfermedad = table.Column<int>(type: "int", nullable: true),
                    CodProductos = table.Column<int>(type: "int", nullable: true),
                    CantProducto = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Obsevaciones = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    UsuarioAlta = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FechaAlta = table.Column<DateTime>(type: "datetime", nullable: true),
                    UsuarioBaja = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FechaBaja = table.Column<DateTime>(type: "datetime", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime", nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CONTROLE__06370DADD3C8B4F3", x => x.Codigo);
                    table.ForeignKey(
                        name: "FK__CONTROLES__CodAl__59063A47",
                        column: x => x.CodAlimento,
                        principalTable: "ALIMENTOS",
                        principalColumn: "Codigo");
                    table.ForeignKey(
                        name: "FK__CONTROLES__CodAp__5812160E",
                        column: x => x.CodApiario,
                        principalTable: "APIARIO",
                        principalColumn: "Codigo");
                    table.ForeignKey(
                        name: "FK__CONTROLES__CodCa__571DF1D5",
                        column: x => x.CodCampaña,
                        principalTable: "CAMPAÑA",
                        principalColumn: "Codigo");
                    table.ForeignKey(
                        name: "FK__CONTROLES__CodEn__59FA5E80",
                        column: x => x.CodEnfermedad,
                        principalTable: "ENFERMEDAD",
                        principalColumn: "Codigo");
                    table.ForeignKey(
                        name: "FK__CONTROLES__CodPr__5AEE82B9",
                        column: x => x.CodProductos,
                        principalTable: "PRODUCTOS",
                        principalColumn: "Codigo");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CONTROLES_CodAlimento",
                table: "CONTROLES",
                column: "CodAlimento");

            migrationBuilder.CreateIndex(
                name: "IX_CONTROLES_CodApiario",
                table: "CONTROLES",
                column: "CodApiario");

            migrationBuilder.CreateIndex(
                name: "IX_CONTROLES_CodCampaña",
                table: "CONTROLES",
                column: "CodCampaña");

            migrationBuilder.CreateIndex(
                name: "IX_CONTROLES_CodEnfermedad",
                table: "CONTROLES",
                column: "CodEnfermedad");

            migrationBuilder.CreateIndex(
                name: "IX_CONTROLES_CodProductos",
                table: "CONTROLES",
                column: "CodProductos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CONTROLES");

            migrationBuilder.DropTable(
                name: "ALIMENTOS");

            migrationBuilder.DropTable(
                name: "APIARIO");

            migrationBuilder.DropTable(
                name: "CAMPAÑA");

            migrationBuilder.DropTable(
                name: "ENFERMEDAD");

            migrationBuilder.DropTable(
                name: "PRODUCTOS");
        }
    }
}
