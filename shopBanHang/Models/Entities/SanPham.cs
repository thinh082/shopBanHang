using System;
using System.Collections.Generic;

namespace shopBanHang.Models.Entities;

public partial class SanPham
{
    public int Id { get; set; }

    public string TenSanPham { get; set; } = null!;

    public string? MoTa { get; set; }

    public decimal Gia { get; set; }

    public int SoLuong { get; set; }

    public int? DanhMucId { get; set; }

    public DateTime? NgayThem { get; set; }

    public bool? TrangThai { get; set; }

    public string? ThuongHieu { get; set; }

    public int? KhuyenMai { get; set; }

    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();

    public virtual ICollection<DanhGium> DanhGia { get; set; } = new List<DanhGium>();

    public virtual DanhMuc? DanhMuc { get; set; }

    public virtual ICollection<SanPhamHinhAnh> SanPhamHinhAnhs { get; set; } = new List<SanPhamHinhAnh>();

    public virtual ICollection<ThongSo> ThongSos { get; set; } = new List<ThongSo>();
}
