using System;
using System.Collections.Generic;

namespace shopBanHang.Models.Entities;

public partial class SanPhamHinhAnh
{
    public int Id { get; set; }

    public int? SanPhamId { get; set; }

    public string? DuongDan { get; set; }

    public virtual SanPham? SanPham { get; set; }
}
