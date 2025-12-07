using System;
using System.Collections.Generic;

namespace shopBanHang.Models.Entities;

public partial class GioHang
{
    public int Id { get; set; }

    public int? TaiKhoanId { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();

    public virtual TaiKhoan? TaiKhoan { get; set; }
}
