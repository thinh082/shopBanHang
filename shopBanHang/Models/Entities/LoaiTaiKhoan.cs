using System;
using System.Collections.Generic;

namespace shopBanHang.Models.Entities;

public partial class LoaiTaiKhoan
{
    public int Id { get; set; }

    public string TenLoai { get; set; } = null!;

    public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
}
