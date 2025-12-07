using System;
using System.Collections.Generic;

namespace shopBanHang.Models.Entities;

public partial class TinNhan
{
    public int Id { get; set; }

    public int? NguoiGuiId { get; set; }

    public int? NguoiNhanId { get; set; }

    public string? NoiDung { get; set; }

    public DateTime? ThoiGian { get; set; }

    public virtual TaiKhoan? NguoiGui { get; set; }

    public virtual TaiKhoan? NguoiNhan { get; set; }
}
