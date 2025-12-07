using System;
using System.Collections.Generic;

namespace shopBanHang.Models.Entities;

public partial class ThanhToan
{
    public int Id { get; set; }

    public int? DonHangId { get; set; }

    public string? PhuongThuc { get; set; }

    public decimal? SoTien { get; set; }

    public string? TrangThai { get; set; }

    public DateTime? NgayThanhToan { get; set; }

    public string? MaGiaoDich { get; set; }

    public string? CongThanhToan { get; set; }

    public virtual DonHang? DonHang { get; set; }
}
