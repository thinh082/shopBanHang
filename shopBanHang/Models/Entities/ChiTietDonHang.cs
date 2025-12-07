using System;
using System.Collections.Generic;

namespace shopBanHang.Models.Entities;

public partial class ChiTietDonHang
{
    public int Id { get; set; }

    public int? DonHangId { get; set; }

    public int? SanPhamId { get; set; }

    public int SoLuong { get; set; }

    public decimal? DonGia { get; set; }

    public virtual DonHang? DonHang { get; set; }

    public virtual SanPham? SanPham { get; set; }
}
