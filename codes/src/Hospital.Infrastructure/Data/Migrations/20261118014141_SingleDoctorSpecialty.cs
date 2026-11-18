using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hospital.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SingleDoctorSpecialty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reject ambiguous legacy data before changing the schema. Never choose a
            // primary/first specialty silently: that could change the meaning of care history.
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT d.Id FROM Doctors d
                    LEFT JOIN DoctorSpecialties ds ON ds.DoctorId = d.Id
                    GROUP BY d.Id HAVING COUNT(ds.SpecialtyId) <> 1
                )
                    THROW 51001, 'OOAD_A2: Each doctor must have exactly one legacy specialty. Review DoctorSpecialties before retrying migration.', 1;
                """);

            migrationBuilder.AddColumn<int>(
                name: "SpecialtyId",
                table: "Doctors",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE d SET SpecialtyId = ds.SpecialtyId
                FROM Doctors d JOIN DoctorSpecialties ds ON ds.DoctorId = d.Id;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "SpecialtyId", table: "Doctors", type: "int", nullable: false,
                oldClrType: typeof(int), oldType: "int", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_SpecialtyId",
                table: "Doctors",
                column: "SpecialtyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Specialties_SpecialtyId",
                table: "Doctors",
                column: "SpecialtyId",
                principalTable: "Specialties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropTable(name: "DoctorSpecialties");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_Specialties_SpecialtyId",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_SpecialtyId",
                table: "Doctors");

            migrationBuilder.CreateTable(
                name: "DoctorSpecialties",
                columns: table => new
                {
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpecialtyId = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorSpecialties", x => new { x.DoctorId, x.SpecialtyId });
                    table.ForeignKey(
                        name: "FK_DoctorSpecialties_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoctorSpecialties_Specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "Specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSpecialties_SpecialtyId",
                table: "DoctorSpecialties",
                column: "SpecialtyId");

            migrationBuilder.Sql("""
                INSERT INTO DoctorSpecialties (DoctorId, SpecialtyId, IsPrimary)
                SELECT Id, SpecialtyId, CAST(1 AS bit) FROM Doctors;
                """);

            migrationBuilder.DropColumn(name: "SpecialtyId", table: "Doctors");
        }
    }
}
