using shopBanHang.Models.Entities;

namespace shopBanHang.Models.DTOs;

public class SanPhamResponseDTO
{
    public int Id { get; set; }
    public string TenSanPham { get; set; } = null!;
    public string? MoTa { get; set; }
    public decimal Gia { get; set; }
    public int SoLuong { get; set; }
    public int? DanhMucId { get; set; }
    public string? TenDanhMuc { get; set; }
    public DateTime? NgayThem { get; set; }
    public bool? TrangThai { get; set; }
    public List<string>? HinhAnhs { get; set; }
    public double? DiemTrungBinh { get; set; }
    public int? SoLuongDanhGia { get; set; }
    public string ThuongHieu { get; set; } = null!;
    public ThongSoSanPham? ThongSo { get; set; }
    public int? KhuyenMai { get; set; }
}
public class ThongSoSanPham
{
    public int Id { get; set; }

    public int? IdSanPham { get; set; }

    public string? Cpu { get; set; }

    public string? Vga { get; set; }

    public string? Ram { get; set; }

    public string? Rom { get; set; }
}
public class SanPhamChiTietDTO : SanPhamResponseDTO
{
    public List<DanhGiaResponseDTO>? DanhGia { get; set; }
    public List<SanPhamResponseDTO>? SanPhamLienQuan { get; set; }
}

public class SanPhamFilterDTO
{
    public int? DanhMucId { get; set; }
    public string? TenSanPham { get; set; }
    public decimal? GiaMin { get; set; }
    public decimal? GiaMax { get; set; }
    public bool? TrangThai { get; set; }
    public bool? ConHang { get; set; } // SoLuong > 0
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

