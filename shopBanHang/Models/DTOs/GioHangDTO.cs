namespace shopBanHang.Models.DTOs;

public class GioHangItemDTO
{
    public int Id { get; set; }
    public int SanPhamId { get; set; }
    public string TenSanPham { get; set; } = null!;
    public decimal Gia { get; set; }
    public int SoLuong { get; set; }
    public string? HinhAnh { get; set; }
    public int SoLuongTon { get; set; }
}

public class GioHangResponseDTO
{
    public int GioHangId { get; set; }
    public List<GioHangItemDTO> Items { get; set; } = new List<GioHangItemDTO>();
    public decimal TongTien { get; set; }
}

public class GioHangAddDTO
{
    public int SanPhamId { get; set; }
    public int SoLuong { get; set; }
}

public class GioHangUpdateDTO
{
    public int SoLuong { get; set; }
}

