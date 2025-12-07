namespace shopBanHang.Models.DTOs;

public class TinNhanCreateDTO
{
    public int NguoiNhanId { get; set; }
    public string NoiDung { get; set; } = null!;
}

public class TinNhanResponseDTO
{
    public int Id { get; set; }
    public int NguoiGuiId { get; set; }
    public string? HoTenNguoiGui { get; set; }
    public int NguoiNhanId { get; set; }
    public string? HoTenNguoiNhan { get; set; }
    public string? NoiDung { get; set; }
    public DateTime? ThoiGian { get; set; }
}

