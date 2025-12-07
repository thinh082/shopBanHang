using System;
using System.Collections.Generic;

namespace shopBanHang.Models.Entities;

public partial class DonHang
{
    public int Id { get; set; }

    public int? TaiKhoanId { get; set; }

    public DateTime? NgayDat { get; set; }

    public decimal? TongTien { get; set; }

    public string? TrangThai { get; set; }

    public string? TenNguoiNhan { get; set; }

    public string? DiaChiGiaoHang { get; set; }

    public string? SdtnguoiNhan { get; set; }

    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    public virtual TaiKhoan? TaiKhoan { get; set; }

    public virtual ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();
}
