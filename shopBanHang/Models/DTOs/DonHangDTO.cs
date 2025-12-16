namespace shopBanHang.Models.DTOs;

public class DonHangCreateDTO
{
    public string TenNguoiNhan { get; set; } = null!;
    public string DiaChiGiaoHang { get; set; } = null!;
    public string SdtnguoiNhan { get; set; } = null!;
    public decimal? PhiVanChuyen { get; set; }
    public string? PhuongThucThanhToan { get; set; }
    public string? CongThanhToan { get; set; }
}

public class DonHangItemDTO
{
    public int SanPhamId { get; set; }
    public string TenSanPham { get; set; } = null!;
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
}

public class DonHangResponseDTO
{
    public int Id { get; set; }
    public DateTime? NgayDat { get; set; }
    public decimal? TongTien { get; set; }
    public string? TrangThai { get; set; }
    public string? TenNguoiNhan { get; set; }
    public string? DiaChiGiaoHang { get; set; }
    public string? SdtnguoiNhan { get; set; }
    public List<DonHangItemDTO> ChiTiet { get; set; } = new List<DonHangItemDTO>();
    public List<ThanhToanDonHangDTO> ThanhToans { get; set; } = new List<ThanhToanDonHangDTO>();
}

public class ThanhToanDonHangDTO
{
    public int Id { get; set; }
    public string? PhuongThuc { get; set; }
    public decimal? SoTien { get; set; }
    public string? TrangThai { get; set; }
    public DateTime? NgayThanhToan { get; set; }
    public string? MaGiaoDich { get; set; }
    public string? CongThanhToan { get; set; }
}

