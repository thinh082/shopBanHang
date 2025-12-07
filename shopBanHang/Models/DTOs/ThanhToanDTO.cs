namespace shopBanHang.Models.DTOs;

public class ThanhToanCreateDTO
{
    public int DonHangId { get; set; }
    public string PhuongThuc { get; set; } = null!; // "COD", "ChuyenKhoan", "ViDienTu"
    public string? CongThanhToan { get; set; }
}

public class ThanhToanResponseDTO
{
    public int Id { get; set; }
    public int DonHangId { get; set; }
    public string? PhuongThuc { get; set; }
    public decimal? SoTien { get; set; }
    public string? TrangThai { get; set; }
    public DateTime? NgayThanhToan { get; set; }
    public string? MaGiaoDich { get; set; }
    public string? CongThanhToan { get; set; }
}

