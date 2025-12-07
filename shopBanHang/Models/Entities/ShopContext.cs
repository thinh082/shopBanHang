using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace shopBanHang.Models.Entities;

public partial class ShopContext : DbContext
{
    public ShopContext()
    {
    }

    public ShopContext(DbContextOptions<ShopContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }

    public virtual DbSet<ChiTietGioHang> ChiTietGioHangs { get; set; }

    public virtual DbSet<DanhGium> DanhGia { get; set; }

    public virtual DbSet<DanhMuc> DanhMucs { get; set; }

    public virtual DbSet<DonHang> DonHangs { get; set; }

    public virtual DbSet<GioHang> GioHangs { get; set; }

    public virtual DbSet<LoaiTaiKhoan> LoaiTaiKhoans { get; set; }

    public virtual DbSet<SanPham> SanPhams { get; set; }

    public virtual DbSet<SanPhamHinhAnh> SanPhamHinhAnhs { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<ThanhToan> ThanhToans { get; set; }

    public virtual DbSet<ThongSo> ThongSos { get; set; }

    public virtual DbSet<TinNhan> TinNhans { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:Connection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChiTietDonHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChiTietD__3214EC07E05C32C1");

            entity.ToTable("ChiTietDonHang");

            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.DonHang).WithMany(p => p.ChiTietDonHangs)
                .HasForeignKey(d => d.DonHangId)
                .HasConstraintName("FK__ChiTietDo__DonHa__4316F928");

            entity.HasOne(d => d.SanPham).WithMany(p => p.ChiTietDonHangs)
                .HasForeignKey(d => d.SanPhamId)
                .HasConstraintName("FK__ChiTietDo__SanPh__440B1D61");
        });

        modelBuilder.Entity<ChiTietGioHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChiTietG__3214EC07F497592B");

            entity.ToTable("ChiTietGioHang");

            entity.HasOne(d => d.GioHang).WithMany(p => p.ChiTietGioHangs)
                .HasForeignKey(d => d.GioHangId)
                .HasConstraintName("FK__ChiTietGi__GioHa__44FF419A");

            entity.HasOne(d => d.SanPham).WithMany(p => p.ChiTietGioHangs)
                .HasForeignKey(d => d.SanPhamId)
                .HasConstraintName("FK__ChiTietGi__SanPh__45F365D3");
        });

        modelBuilder.Entity<DanhGium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DanhGia__3214EC075550E83A");

            entity.Property(e => e.NgayDanhGia)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NoiDung).HasMaxLength(500);

            entity.HasOne(d => d.SanPham).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.SanPhamId)
                .HasConstraintName("FK__DanhGia__SanPham__46E78A0C");

            entity.HasOne(d => d.TaiKhoan).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.TaiKhoanId)
                .HasConstraintName("FK__DanhGia__TaiKhoa__47DBAE45");
        });

        modelBuilder.Entity<DanhMuc>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DanhMuc__3214EC075C27A1CA");

            entity.ToTable("DanhMuc");

            entity.Property(e => e.TenDanhMuc).HasMaxLength(100);
        });

        modelBuilder.Entity<DonHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DonHang__3214EC079B5308A2");

            entity.ToTable("DonHang");

            entity.Property(e => e.DiaChiGiaoHang).HasMaxLength(255);
            entity.Property(e => e.NgayDat)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SdtnguoiNhan)
                .HasMaxLength(20)
                .HasColumnName("SDTNguoiNhan");
            entity.Property(e => e.TenNguoiNhan).HasMaxLength(100);
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Chờ xử lý");

            entity.HasOne(d => d.TaiKhoan).WithMany(p => p.DonHangs)
                .HasForeignKey(d => d.TaiKhoanId)
                .HasConstraintName("FK__DonHang__TaiKhoa__48CFD27E");
        });

        modelBuilder.Entity<GioHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GioHang__3214EC07A889F86F");

            entity.ToTable("GioHang");

            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.TaiKhoan).WithMany(p => p.GioHangs)
                .HasForeignKey(d => d.TaiKhoanId)
                .HasConstraintName("FK__GioHang__TaiKhoa__49C3F6B7");
        });

        modelBuilder.Entity<LoaiTaiKhoan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LoaiTaiK__3214EC0712F477F7");

            entity.ToTable("LoaiTaiKhoan");

            entity.Property(e => e.TenLoai).HasMaxLength(50);
        });

        modelBuilder.Entity<SanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SanPham__3214EC0750AD598E");

            entity.ToTable("SanPham");

            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NgayThem)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TenSanPham).HasMaxLength(255);
            entity.Property(e => e.ThuongHieu).HasMaxLength(30);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.DanhMuc).WithMany(p => p.SanPhams)
                .HasForeignKey(d => d.DanhMucId)
                .HasConstraintName("FK__SanPham__DanhMuc__4AB81AF0");
        });

        modelBuilder.Entity<SanPhamHinhAnh>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SanPhamH__3214EC071D442A49");

            entity.ToTable("SanPhamHinhAnh");

            entity.Property(e => e.DuongDan).HasMaxLength(255);

            entity.HasOne(d => d.SanPham).WithMany(p => p.SanPhamHinhAnhs)
                .HasForeignKey(d => d.SanPhamId)
                .HasConstraintName("FK__SanPhamHi__SanPh__5DCAEF64");
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TaiKhoan__3214EC071B6F67B4");

            entity.ToTable("TaiKhoan");

            entity.HasIndex(e => e.Email, "UQ__TaiKhoan__A9D105343CC06F79").IsUnique();

            entity.Property(e => e.AnhDaiDien).HasMaxLength(255);
            entity.Property(e => e.Code).HasMaxLength(30);
            entity.Property(e => e.DiaChi).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.MatKhau).HasMaxLength(255);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoDienThoai).HasMaxLength(20);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.IdDanhMucNavigation).WithMany(p => p.TaiKhoans)
                .HasForeignKey(d => d.IdDanhMuc)
                .HasConstraintName("fk_TaiKhoan_DanhMuc");

            entity.HasOne(d => d.LoaiTaiKhoan).WithMany(p => p.TaiKhoans)
                .HasForeignKey(d => d.LoaiTaiKhoanId)
                .HasConstraintName("FK__TaiKhoan__LoaiTa__4BAC3F29");
        });

        modelBuilder.Entity<ThanhToan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ThanhToa__3214EC0763ACF2CE");

            entity.ToTable("ThanhToan");

            entity.Property(e => e.CongThanhToan).HasMaxLength(50);
            entity.Property(e => e.MaGiaoDich).HasMaxLength(100);
            entity.Property(e => e.NgayThanhToan)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PhuongThuc).HasMaxLength(100);
            entity.Property(e => e.SoTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Chưa thanh toán");

            entity.HasOne(d => d.DonHang).WithMany(p => p.ThanhToans)
                .HasForeignKey(d => d.DonHangId)
                .HasConstraintName("FK__ThanhToan__DonHa__4D94879B");
        });

        modelBuilder.Entity<ThongSo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ThongSo__3214EC072C253E43");

            entity.ToTable("ThongSo");

            entity.Property(e => e.Cpu)
                .HasMaxLength(100)
                .HasColumnName("CPU");
            entity.Property(e => e.Ram).HasMaxLength(100);
            entity.Property(e => e.Rom)
                .HasMaxLength(100)
                .HasColumnName("ROM");
            entity.Property(e => e.Vga)
                .HasMaxLength(100)
                .HasColumnName("VGA");

            entity.HasOne(d => d.IdSanPhamNavigation).WithMany(p => p.ThongSos)
                .HasForeignKey(d => d.IdSanPham)
                .HasConstraintName("FK_ThongSo_SanPham");
        });

        modelBuilder.Entity<TinNhan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TinNhan__3214EC073BB165A7");

            entity.ToTable("TinNhan");

            entity.Property(e => e.NoiDung).HasMaxLength(1000);
            entity.Property(e => e.ThoiGian)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.NguoiGui).WithMany(p => p.TinNhanNguoiGuis)
                .HasForeignKey(d => d.NguoiGuiId)
                .HasConstraintName("FK__TinNhan__NguoiGu__4E88ABD4");

            entity.HasOne(d => d.NguoiNhan).WithMany(p => p.TinNhanNguoiNhans)
                .HasForeignKey(d => d.NguoiNhanId)
                .HasConstraintName("FK__TinNhan__NguoiNh__4F7CD00D");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
