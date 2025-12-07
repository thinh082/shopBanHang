namespace shopBanHang.Models.DTOs;

public class DangKyDTO
{
    public string HoTen { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string MatKhau { get; set; } = null!;
    public string? SoDienThoai { get; set; }
    public string? DiaChi { get; set; }
}

public class DangNhapDTO
{
    public string Email { get; set; } = null!;
    public string MatKhau { get; set; } = null!;
}

public class DoiMatKhauDTO
{
    public string MatKhauCu { get; set; } = null!;
    public string MatKhauMoi { get; set; } = null!;
}

public class TaiKhoanUpdateDTO
{
    public string? HoTen { get; set; }
    public string? SoDienThoai { get; set; }
    public string? DiaChi { get; set; }
    public string? AnhDaiDien { get; set; }
}

public class AnhDaiDienUpdateDTO
{
    public string AnhDaiDien { get; set; } = null!;
}

public class TaiKhoanResponseDTO
{
    public int Id { get; set; }
    public string? HoTen { get; set; }
    public string Email { get; set; } = null!;
    public string? SoDienThoai { get; set; }
    public string? DiaChi { get; set; }
    public string? AnhDaiDien { get; set; }
    public DateTime? NgayTao { get; set; }
    public bool? TrangThai { get; set; }
    public string? TenLoaiTaiKhoan { get; set; }
}

public class GuiOTPDTO
{
    public string Email { get; set; } = null!;
}

public class XacNhanOTPDTO
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
}

public class DoiMatKhauQuenDTO
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string MatKhauMoi { get; set; } = null!;
}

