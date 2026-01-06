using FFSchedule.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Text;

namespace FFSchedule
{
    public class AppDbContext: DbContext 
    {
        public AppDbContext() { }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


        public DbSet<Chief> Chief { get; set; }
        public DbSet<CorrectionHistory> CorrectionHistory { get; set; }
        public DbSet<Department> Department { get; set; }
        public DbSet<DepartmentEquipment> DepartmentEquipment { get; set; }
        public DbSet<DepartmentEquipmentSummary> DepartmentEquipmentSummary { get; set; }
        public DbSet<DepartmentType> DepartmentType { get; set; }
        public DbSet<Dispatcher> Dispatcher { get; set; }
        public DbSet<Employee> Employee { get; set; }
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<EquipmentType> EquipmentType { get; set; } 
        public DbSet<Rank> Rank { get; set; }
        public DbSet<RankBudgetAllLocation> RankBudgetAllLocation { get; set; }
        public DbSet<RankResponseTime> RankResponseTime { get; set; }
        public DbSet<Role> Role { get; set; }   
        public DbSet<Settlement> Settlement { get; set; }
        public DbSet<SettlementDepartmentDistance> SettlementDepartmentDistance { get; set; }
        public DbSet<SpecialStatusObject> SpecialStatusObject { get; set; }
        public DbSet<TypesOfLocality> TypesOfLocality { get; set; }
        public DbSet<VillageCouncil> VillageCouncils { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=ffsdatabase.db");
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Chief>()
                .HasOne(c => c.Employee)
                .WithMany()
                .HasForeignKey(c => c.EmployeeId);

            modelBuilder.Entity<CorrectionHistory>()
                .HasOne(ch => ch.Employee)
                .WithMany()
                .HasForeignKey(ch => ch.EmployeeId);

            modelBuilder.Entity<Department>()
                .HasOne(d => d.DepartmentType)
                .WithMany()
                .HasForeignKey(d => d.DepartmentTypeId);

            modelBuilder.Entity<DepartmentEquipment>()
                .HasOne(de => de.Department)
                .WithMany()
                .HasForeignKey(de => de.DepartmentId);

            modelBuilder.Entity<DepartmentEquipment>()
                .HasOne(de => de.Equipment)
                .WithMany()
                .HasForeignKey(de => de.EquipmentId);

            modelBuilder.Entity<DepartmentEquipmentSummary>()
                .HasOne(des => des.Department)
                .WithMany()
                .HasForeignKey(des => des.DepartmentId);

            modelBuilder.Entity<DepartmentEquipmentSummary>()
                .HasOne(des => des.EquipmentType)
                .WithMany()
                .HasForeignKey(des => des.EquipmentTypeId);

            modelBuilder.Entity<Dispatcher>()
                .HasOne(d => d.Employee)
                .WithMany()
                .HasForeignKey(d => d.EmployeeId);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId);

            modelBuilder.Entity<Equipment>()
                .HasOne(e => e.EquipmentType)
                .WithMany()
                .HasForeignKey(e => e.EquipmentTypeId);

            modelBuilder.Entity<RankBudgetAllLocation>()
                .HasOne(rba => rba.Rank)
                .WithMany()
                .HasForeignKey(rba => rba.RankId);

            modelBuilder.Entity<RankResponseTime>()
                .HasOne(rrt => rrt.Rank)
                .WithMany()
                .HasForeignKey(rrt => rrt.RankId);

            modelBuilder.Entity<RankResponseTime>()
                .HasOne(rrt => rrt.DepartmentEquipmentSummary)
                .WithMany()
                .HasForeignKey(rrt => rrt.DepartmentEquipmentSummaryId);

            modelBuilder.Entity<Settlement>()
                .HasOne(s => s.VillageCouncil)
                .WithMany()
                .HasForeignKey(s => s.VillageCouncilId);

            modelBuilder.Entity<Settlement>()
                .HasOne(s => s.TypesOfLocality)
                .WithMany()
                .HasForeignKey(s => s.TypesOfLocalityId);

            modelBuilder.Entity<SettlementDepartmentDistance>()
                .HasOne(sdd => sdd.Settlement)
                .WithMany()
                .HasForeignKey(sdd => sdd.SettlementId);

            modelBuilder.Entity<SettlementDepartmentDistance>()
                .HasOne(sdd => sdd.Department)
                .WithMany()
                .HasForeignKey(sdd => sdd.DepartmentId);

            modelBuilder.Entity<SpecialStatusObject>()
                .HasOne(sso => sso.Rank)
                .WithMany()
                .HasForeignKey(sso => sso.RankId);
        }
    }
}
