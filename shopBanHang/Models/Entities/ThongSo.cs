using System;
using System.Collections.Generic;

namespace shopBanHang.Models.Entities;

public partial class ThongSo
{
    public int Id { get; set; }

    public int? IdSanPham { get; set; }

    public string? Cpu { get; set; }

    public string? Vga { get; set; }

    public string? Ram { get; set; }

    public string? Rom { get; set; }

    public virtual SanPham? IdSanPhamNavigation { get; set; }
}
