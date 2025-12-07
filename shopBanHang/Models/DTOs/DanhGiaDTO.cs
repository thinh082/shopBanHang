namespace shopBanHang.Models.DTOs;

public class DanhGiaRequestDTO
{
    public int SanPhamId { get; set; }
    public string? NoiDung { get; set; }
    public int Diem { get; set; } // 1-5
}

public class DanhGiaUpdateDTO
{
    public string? NoiDung { get; set; }
    public int Diem { get; set; }
}

public class DanhGiaResponseDTO
{
    public int Id { get; set; }
    public int SanPhamId { get; set; }
    public int TaiKhoanId { get; set; }
    public string? HoTen { get; set; }
    public string? NoiDung { get; set; }
    public int? Diem { get; set; }
    public DateTime? NgayDanhGia { get; set; }
}

