using System;
using System.Collections.Generic;

namespace shopBanHang.Models.Entities;

public partial class ChiTietGioHang
{
    public int Id { get; set; }

    public int? GioHangId { get; set; }

    public int? SanPhamId { get; set; }

    public int SoLuong { get; set; }

    public virtual GioHang? GioHang { get; set; }

    public virtual SanPham? SanPham { get; set; }
}
