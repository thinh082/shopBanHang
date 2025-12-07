namespace shopBanHang.Models.DTOs;

public class DanhMucResponseDTO
{
    public int Id { get; set; }
    public string TenDanhMuc { get; set; } = null!;
    public int SoLuongSanPham { get; set; }
}

