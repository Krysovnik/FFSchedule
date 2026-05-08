using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FFSchedule.Models;

public partial class FfsContext : DbContext
{
    public FfsContext()
    {
    }

    public FfsContext(DbContextOptions<FfsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DepartamentEquipmentSummary> DepartamentEquipmentSummaries { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<DepartmentType> DepartmentTypes { get; set; }

    public virtual DbSet<DepartmentVillageCouncilTime> DepartmentVillageCouncilTimes { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EquipmentType> EquipmentTypes { get; set; }

    public virtual DbSet<EquipmentTypeQuantity> EquipmentTypeQuantities { get; set; }

    public virtual DbSet<FireBrigade> FireBrigades { get; set; }

    public virtual DbSet<Rank> Ranks { get; set; }

    public virtual DbSet<RankBudgetAllocation> RankBudgetAllocations { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Settlement> Settlements { get; set; }

    public virtual DbSet<SettlementDepartamentDistance> SettlementDepartamentDistances { get; set; }

    public virtual DbSet<SettlementMainDepartament> SettlementMainDepartaments { get; set; }

    public virtual DbSet<SpecialStatusObject> SpecialStatusObjects { get; set; }

    public virtual DbSet<StatusesOfSpeObj> StatusesOfSpeObjs { get; set; }

    public virtual DbSet<TypesOfLocality> TypesOfLocalities { get; set; }

    public virtual DbSet<VillageCouncil> VillageCouncils { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlite("Data Source=FFS\\FFS.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DepartamentEquipmentSummary>(entity =>
        {
            entity.HasKey(e => e.DesId);

            entity.ToTable("DepartamentEquipmentSummary");

            entity.Property(e => e.DesId)
                .ValueGeneratedNever()
                .HasColumnName("DES_ID");
            entity.Property(e => e.DesQuantity).HasColumnName("DES_Quantity");
            entity.Property(e => e.DptId).HasColumnName("DPT_ID");
            entity.Property(e => e.EtId).HasColumnName("ET_ID");

            entity.HasOne(d => d.Dpt).WithMany(p => p.DepartamentEquipmentSummaries).HasForeignKey(d => d.DptId);

            entity.HasOne(d => d.Et).WithMany(p => p.DepartamentEquipmentSummaries).HasForeignKey(d => d.EtId);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DptId);

            entity.ToTable("Department");

            entity.Property(e => e.DptId)
                .ValueGeneratedNever()
                .HasColumnName("DPT_ID");
            entity.Property(e => e.DptAddress).HasColumnName("DPT_Address");
            entity.Property(e => e.DptFiretrucks).HasColumnName("DPT_Firetrucks");
            entity.Property(e => e.DptHasLadder)
                .HasDefaultValue(0)
                .HasColumnName("DPT_HasLadder");
            entity.Property(e => e.DptName).HasColumnName("DPT_Name");
            entity.Property(e => e.DptPhoneNum).HasColumnName("DPT_PhoneNum");
            entity.Property(e => e.DptShort).HasColumnName("DPT_Short");
            entity.Property(e => e.DtId).HasColumnName("DT_ID");
            entity.Property(e => e.FbId).HasColumnName("FB_ID");

            entity.HasOne(d => d.Dt).WithMany(p => p.Departments).HasForeignKey(d => d.DtId);

            entity.HasOne(d => d.Fb).WithMany(p => p.Departments).HasForeignKey(d => d.FbId);
        });

        modelBuilder.Entity<DepartmentType>(entity =>
        {
            entity.HasKey(e => e.DtId);

            entity.ToTable("DepartmentType");

            entity.Property(e => e.DtId)
                .ValueGeneratedNever()
                .HasColumnName("DT_ID");
            entity.Property(e => e.DtName).HasColumnName("DT_Name");
        });

        modelBuilder.Entity<DepartmentVillageCouncilTime>(entity =>
        {
            entity.HasKey(e => new { e.DptId, e.VcId });

            entity.ToTable("DepartmentVillageCouncilTime");

            entity.Property(e => e.DptId).HasColumnName("DPT_ID");
            entity.Property(e => e.VcId).HasColumnName("VC_ID");

            entity.HasOne(d => d.Dpt).WithMany(p => p.DepartmentVillageCouncilTimes)
                .HasForeignKey(d => d.DptId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Vc).WithMany(p => p.DepartmentVillageCouncilTimes)
                .HasForeignKey(d => d.VcId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmId);

            entity.Property(e => e.EmId)
                .ValueGeneratedNever()
                .HasColumnName("EM_ID");
            entity.Property(e => e.EmFio).HasColumnName("EM_FIO");
            entity.Property(e => e.EmLogin).HasColumnName("EM_Login");
            entity.Property(e => e.EmPassword).HasColumnName("EM_Password");
            entity.Property(e => e.RoId).HasColumnName("RO_ID");

            entity.HasOne(d => d.Ro).WithMany(p => p.Employees).HasForeignKey(d => d.RoId);
        });

        modelBuilder.Entity<EquipmentType>(entity =>
        {
            entity.HasKey(e => e.EtId);

            entity.ToTable("EquipmentType");

            entity.Property(e => e.EtId)
                .ValueGeneratedNever()
                .HasColumnName("ET_ID");
            entity.Property(e => e.EtName).HasColumnName("ET_Name");
        });

        modelBuilder.Entity<EquipmentTypeQuantity>(entity =>
        {
            entity.HasKey(e => e.EtqId);

            entity.ToTable("EquipmentTypeQuantity");

            entity.Property(e => e.EtqId)
                .ValueGeneratedNever()
                .HasColumnName("ETQ_ID");
            entity.Property(e => e.DptId).HasColumnName("DPT_ID");
            entity.Property(e => e.EtId).HasColumnName("ET_ID");
            entity.Property(e => e.EtqQuantity).HasColumnName("ETQ_Quantity");
            entity.Property(e => e.EtqTime).HasColumnName("ETQ_Time");
            entity.Property(e => e.SeId).HasColumnName("SE_ID");

            entity.HasOne(d => d.Dpt).WithMany(p => p.EquipmentTypeQuantities).HasForeignKey(d => d.DptId);

            entity.HasOne(d => d.Et).WithMany(p => p.EquipmentTypeQuantities).HasForeignKey(d => d.EtId);

            entity.HasOne(d => d.Se).WithMany(p => p.EquipmentTypeQuantities).HasForeignKey(d => d.SeId);
        });

        modelBuilder.Entity<FireBrigade>(entity =>
        {
            entity.HasKey(e => e.FbId);

            entity.ToTable("FireBrigade");

            entity.Property(e => e.FbId)
                .ValueGeneratedNever()
                .HasColumnName("FB_ID");
            entity.Property(e => e.FbName).HasColumnName("FB_Name");
        });

        modelBuilder.Entity<Rank>(entity =>
        {
            entity.HasKey(e => e.RId);

            entity.ToTable("Rank");

            entity.Property(e => e.RId)
                .ValueGeneratedNever()
                .HasColumnName("R_ID");
            entity.Property(e => e.RNumber).HasColumnName("R_Number");
            entity.Property(e => e.RTotalEquipmentQuantity)
                .HasColumnType("INTEGER")
                .HasColumnName("R_TotalEquipmentQuantity");
        });

        modelBuilder.Entity<RankBudgetAllocation>(entity =>
        {
            entity.HasKey(e => e.RbaId);

            entity.ToTable("RankBudgetAllocation");

            entity.Property(e => e.RbaId)
                .ValueGeneratedNever()
                .HasColumnName("RBA_ID");
            entity.Property(e => e.EtId).HasColumnName("ET_ID");
            entity.Property(e => e.RId).HasColumnName("R_ID");
            entity.Property(e => e.RbaTypeTotal).HasColumnName("RBA_TypeTotal");

            entity.HasOne(d => d.Et).WithMany(p => p.RankBudgetAllocations).HasForeignKey(d => d.EtId);

            entity.HasOne(d => d.RIdNavigation).WithMany(p => p.RankBudgetAllocations).HasForeignKey(d => d.RId);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoId);

            entity.ToTable("Role");

            entity.Property(e => e.RoId)
                .ValueGeneratedNever()
                .HasColumnName("RO_ID");
            entity.Property(e => e.RoName).HasColumnName("RO_Name");
        });

        modelBuilder.Entity<Settlement>(entity =>
        {
            entity.HasKey(e => e.SeId);

            entity.ToTable("Settlement");

            entity.Property(e => e.SeId)
                .ValueGeneratedNever()
                .HasColumnName("SE_ID");
            entity.Property(e => e.Optkp).HasColumnName("OPTKP");
            entity.Property(e => e.SeName).HasColumnName("SE_Name");
            entity.Property(e => e.TolId).HasColumnName("TOL_ID");
            entity.Property(e => e.VcId).HasColumnName("VC_ID");

            entity.HasOne(d => d.Tol).WithMany(p => p.Settlements).HasForeignKey(d => d.TolId);

            entity.HasOne(d => d.Vc).WithMany(p => p.Settlements).HasForeignKey(d => d.VcId);
        });

        modelBuilder.Entity<SettlementDepartamentDistance>(entity =>
        {
            entity.HasKey(e => e.SddId);

            entity.ToTable("SettlementDepartamentDistance");

            entity.Property(e => e.SddId)
                .ValueGeneratedNever()
                .HasColumnName("SDD_ID");
            entity.Property(e => e.DptId).HasColumnName("DPT_ID");
            entity.Property(e => e.SddDistanceKm).HasColumnName("SDD_DistanceKm");
            entity.Property(e => e.SeId).HasColumnName("SE_ID");

            entity.HasOne(d => d.Dpt).WithMany(p => p.SettlementDepartamentDistances).HasForeignKey(d => d.DptId);

            entity.HasOne(d => d.Se).WithMany(p => p.SettlementDepartamentDistances).HasForeignKey(d => d.SeId);
        });

        modelBuilder.Entity<SettlementMainDepartament>(entity =>
        {
            entity.HasKey(e => e.SmdId);

            entity.ToTable("SettlementMainDepartament");

            entity.Property(e => e.SmdId)
                .ValueGeneratedNever()
                .HasColumnName("SMD_ID");
            entity.Property(e => e.DptId).HasColumnName("DPT_ID");
            entity.Property(e => e.SeId).HasColumnName("SE_ID");

            entity.HasOne(d => d.Dpt).WithMany(p => p.SettlementMainDepartaments).HasForeignKey(d => d.DptId);

            entity.HasOne(d => d.Se).WithMany(p => p.SettlementMainDepartaments).HasForeignKey(d => d.SeId);
        });

        modelBuilder.Entity<SpecialStatusObject>(entity =>
        {
            entity.HasKey(e => e.SsoId);

            entity.ToTable("SpecialStatusObject");

            entity.Property(e => e.SsoId)
                .ValueGeneratedNever()
                .HasColumnName("SSO_ID");
            entity.Property(e => e.RId).HasColumnName("R_ID");
            entity.Property(e => e.SsoAddress).HasColumnName("SSO_Address");
            entity.Property(e => e.SsoName).HasColumnName("SSO_Name");
            entity.Property(e => e.StobId).HasColumnName("STOB_ID");

            entity.HasOne(d => d.RIdNavigation).WithMany(p => p.SpecialStatusObjects).HasForeignKey(d => d.RId);

            entity.HasOne(d => d.Stob).WithMany(p => p.SpecialStatusObjects).HasForeignKey(d => d.StobId);
        });

        modelBuilder.Entity<StatusesOfSpeObj>(entity =>
        {
            entity.HasKey(e => e.StobId);

            entity.ToTable("StatusesOfSpeObj");

            entity.Property(e => e.StobId)
                .ValueGeneratedNever()
                .HasColumnName("STOB_ID");
            entity.Property(e => e.StobName).HasColumnName("STOB_Name");
        });

        modelBuilder.Entity<TypesOfLocality>(entity =>
        {
            entity.HasKey(e => e.TolId);

            entity.ToTable("TypesOfLocality");

            entity.Property(e => e.TolId)
                .ValueGeneratedNever()
                .HasColumnName("TOL_ID");
            entity.Property(e => e.TolName).HasColumnName("TOL_Name");
            entity.Property(e => e.TolShortName).HasColumnName("TOL_ShortName");
        });

        modelBuilder.Entity<VillageCouncil>(entity =>
        {
            entity.HasKey(e => e.VcId);

            entity.ToTable("VillageCouncil");

            entity.Property(e => e.VcId)
                .ValueGeneratedNever()
                .HasColumnName("VC_ID");
            entity.Property(e => e.VcName).HasColumnName("VC_Name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
