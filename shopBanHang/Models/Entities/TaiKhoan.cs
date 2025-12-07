using System;
using System.Collections.Generic;

namespace shopBanHang.Models.Entities;

public partial class TaiKhoan
{
    public int Id { get; set; }

    public string? HoTen { get; set; }

    public string Email { get; set; } = null!;

    public string MatKhau { get; set; } = null!;

    public string? SoDienThoai { get; set; }

    public string? DiaChi { get; set; }

    public string? AnhDaiDien { get; set; }

    public DateTime? NgayTao { get; set; }

    public bool? TrangThai { get; set; }

    public int? LoaiTaiKhoanId { get; set; }

    public int? IdDanhMuc { get; set; }

    public string? Code { get; set; }

    public virtual ICollection<DanhGium> DanhGia { get; set; } = new List<DanhGium>();

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();

    public virtual ICollection<GioHang> GioHangs { get; set; } = new List<GioHang>();

    public virtual DanhMuc? IdDanhMucNavigation { get; set; }

    public virtual LoaiTaiKhoan? LoaiTaiKhoan { get; set; }

    public virtual ICollection<TinNhan> TinNhanNguoiGuis { get; set; } = new List<TinNhan>();

    public virtual ICollection<TinNhan> TinNhanNguoiNhans { get; set; } = new List<TinNhan>();
}
