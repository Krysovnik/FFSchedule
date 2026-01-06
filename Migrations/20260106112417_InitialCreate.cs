using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FFSchedule.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepartmentType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rank",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Number = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rank", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TypesOfLocality",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypesOfLocality", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VillageCouncils",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VillageCouncils", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Department",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Number = table.Column<string>(type: "TEXT", nullable: false),
                    DepartmentTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    address = table.Column<string>(type: "TEXT", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Department", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Department_DepartmentType_DepartmentTypeId",
                        column: x => x.DepartmentTypeId,
                        principalTable: "DepartmentType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LicensePlate = table.Column<string>(type: "TEXT", nullable: false),
                    EquipmentTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipment_EquipmentType_EquipmentTypeId",
                        column: x => x.EquipmentTypeId,
                        principalTable: "EquipmentType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RankBudgetAllLocation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RankId = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipmentQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankBudgetAllLocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RankBudgetAllLocation_Rank_RankId",
                        column: x => x.RankId,
                        principalTable: "Rank",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpecialStatusObject",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: false),
                    RankId = table.Column<int>(type: "INTEGER", nullable: false),
                    PeoplePresenceTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    MassScale = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialStatusObject", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecialStatusObject_Rank_RankId",
                        column: x => x.RankId,
                        principalTable: "Rank",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Login = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employee_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Settlement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    VillageCouncilId = table.Column<int>(type: "INTEGER", nullable: false),
                    TypesOfLocalityId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settlement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settlement_TypesOfLocality_TypesOfLocalityId",
                        column: x => x.TypesOfLocalityId,
                        principalTable: "TypesOfLocality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Settlement_VillageCouncils_VillageCouncilId",
                        column: x => x.VillageCouncilId,
                        principalTable: "VillageCouncils",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentEquipmentSummary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipmentTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipmentQuantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentEquipmentSummary", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentEquipmentSummary_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DepartmentEquipmentSummary_EquipmentType_EquipmentTypeId",
                        column: x => x.EquipmentTypeId,
                        principalTable: "EquipmentType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentEquipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentEquipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentEquipment_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DepartmentEquipment_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Chief",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fio = table.Column<string>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chief", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chief_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CorrectionHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrectionHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorrectionHistory_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dispatcher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fio = table.Column<string>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dispatcher", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dispatcher_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SettlementDepartmentDistance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SettlementId = table.Column<int>(type: "INTEGER", nullable: false),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Distance = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementDepartmentDistance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettlementDepartmentDistance_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SettlementDepartmentDistance_Settlement_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "Settlement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RankResponseTime",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RankId = table.Column<int>(type: "INTEGER", nullable: false),
                    DepartmentEquipmentSummaryId = table.Column<int>(type: "INTEGER", nullable: false),
                    ResponseTime = table.Column<TimeSpan>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankResponseTime", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RankResponseTime_DepartmentEquipmentSummary_DepartmentEquipmentSummaryId",
                        column: x => x.DepartmentEquipmentSummaryId,
                        principalTable: "DepartmentEquipmentSummary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RankResponseTime_Rank_RankId",
                        column: x => x.RankId,
                        principalTable: "Rank",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chief_EmployeeId",
                table: "Chief",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionHistory_EmployeeId",
                table: "CorrectionHistory",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Department_DepartmentTypeId",
                table: "Department",
                column: "DepartmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentEquipment_DepartmentId",
                table: "DepartmentEquipment",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentEquipment_EquipmentId",
                table: "DepartmentEquipment",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentEquipmentSummary_DepartmentId",
                table: "DepartmentEquipmentSummary",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentEquipmentSummary_EquipmentTypeId",
                table: "DepartmentEquipmentSummary",
                column: "EquipmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Dispatcher_EmployeeId",
                table: "Dispatcher",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_RoleId",
                table: "Employee",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_EquipmentTypeId",
                table: "Equipment",
                column: "EquipmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RankBudgetAllLocation_RankId",
                table: "RankBudgetAllLocation",
                column: "RankId");

            migrationBuilder.CreateIndex(
                name: "IX_RankResponseTime_DepartmentEquipmentSummaryId",
                table: "RankResponseTime",
                column: "DepartmentEquipmentSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_RankResponseTime_RankId",
                table: "RankResponseTime",
                column: "RankId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlement_TypesOfLocalityId",
                table: "Settlement",
                column: "TypesOfLocalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlement_VillageCouncilId",
                table: "Settlement",
                column: "VillageCouncilId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementDepartmentDistance_DepartmentId",
                table: "SettlementDepartmentDistance",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementDepartmentDistance_SettlementId",
                table: "SettlementDepartmentDistance",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialStatusObject_RankId",
                table: "SpecialStatusObject",
                column: "RankId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Chief");

            migrationBuilder.DropTable(
                name: "CorrectionHistory");

            migrationBuilder.DropTable(
                name: "DepartmentEquipment");

            migrationBuilder.DropTable(
                name: "Dispatcher");

            migrationBuilder.DropTable(
                name: "RankBudgetAllLocation");

            migrationBuilder.DropTable(
                name: "RankResponseTime");

            migrationBuilder.DropTable(
                name: "SettlementDepartmentDistance");

            migrationBuilder.DropTable(
                name: "SpecialStatusObject");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropTable(
                name: "DepartmentEquipmentSummary");

            migrationBuilder.DropTable(
                name: "Settlement");

            migrationBuilder.DropTable(
                name: "Rank");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "Department");

            migrationBuilder.DropTable(
                name: "EquipmentType");

            migrationBuilder.DropTable(
                name: "TypesOfLocality");

            migrationBuilder.DropTable(
                name: "VillageCouncils");

            migrationBuilder.DropTable(
                name: "DepartmentType");
        }
    }
}
