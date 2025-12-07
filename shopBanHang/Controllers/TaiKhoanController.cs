using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using shopBanHang.Models.DTOs;
using shopBanHang.Models.Entities;
using shopBanHang.Services;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace shopBanHang.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaiKhoanController : ControllerBase
{
    private readonly ShopContext _context;
    private readonly IConfiguration _configuration;
    private readonly CloudinaryService _cloudinaryService;

    public TaiKhoanController(ShopContext context, IConfiguration configuration, CloudinaryService cloudinaryService)
    {
        _context = context;
        _configuration = configuration;
        _cloudinaryService = cloudinaryService;
    }

    // Đăng ký
    [HttpPost("dang-ky")]
    public IActionResult DangKy([FromBody] DangKyDTO dto)
    {
        try
        {
            // Kiểm tra email đã tồn tại chưa
            var emailTonTai = _context.TaiKhoans.Any(tk => tk.Email == dto.Email);
            if (emailTonTai)
            {
                return BadRequest(new { code = 400, message = "Email đã được sử dụng" });
            }

            // Kiểm tra mật khẩu
            if (string.IsNullOrWhiteSpace(dto.MatKhau) || dto.MatKhau.Length < 6)
            {
                return BadRequest(new { code = 400, message = "Mật khẩu phải có ít nhất 6 ký tự" });
            }

            // Mã hóa mật khẩu (SHA256 đơn giản, có thể dùng BCrypt sau)
            var matKhauHash = HashPassword(dto.MatKhau);

            // Tìm loại tài khoản mặc định (khách hàng)
            var loaiTaiKhoan = _context.LoaiTaiKhoans
                .FirstOrDefault(ltk => ltk.TenLoai == "Khách hàng" || ltk.TenLoai == "Customer");
            
            if (loaiTaiKhoan == null)
            {
                // Tạo loại tài khoản mặc định nếu chưa có
                loaiTaiKhoan = new LoaiTaiKhoan { TenLoai = "Khách hàng" };
                _context.LoaiTaiKhoans.Add(loaiTaiKhoan);
                _context.SaveChanges();
            }

            // Tạo tài khoản mới
            var taiKhoan = new TaiKhoan
            {
                HoTen = dto.HoTen,
                Email = dto.Email,
                MatKhau = matKhauHash,
                SoDienThoai = dto.SoDienThoai,
                DiaChi = dto.DiaChi,
                NgayTao = DateTime.Now,
                TrangThai = true,
                LoaiTaiKhoanId = loaiTaiKhoan.Id
            };

            _context.TaiKhoans.Add(taiKhoan);
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Đăng ký thành công", idTaiKhoan = taiKhoan.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Đăng nhập
    [HttpPost("dang-nhap")]
    public IActionResult DangNhap([FromBody] DangNhapDTO dto)
    {
        try
        {
            var matKhauHash = HashPassword(dto.MatKhau);

            var taiKhoan = _context.TaiKhoans
                .FirstOrDefault(tk => tk.Email == dto.Email && tk.MatKhau == matKhauHash);

            if (taiKhoan == null)
            {
                return Unauthorized(new { code = 401, message = "Email hoặc mật khẩu không đúng" });
            }

            if (taiKhoan.TrangThai != true)
            {
                return Unauthorized(new { code = 401, message = "Tài khoản đã bị khóa" });
            }

            // TẠM THỜI KHÔNG DÙNG JWT, chỉ trả về idTaiKhoan
            return Ok(new { idTaiKhoan = taiKhoan.Id, tenTaiKhoan = taiKhoan.HoTen, loaiTaiKhoanId = taiKhoan.LoaiTaiKhoanId });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Đổi mật khẩu
    [HttpPut("doi-mat-khau")]
    public IActionResult DoiMatKhau([FromBody] DoiMatKhauDTO dto, [FromQuery] int taiKhoanId)
    {
        try
        {
            var taiKhoan = _context.TaiKhoans.FirstOrDefault(tk => tk.Id == taiKhoanId);
            if (taiKhoan == null)
            {
                return Unauthorized(new { code = 401, message = "Tài khoản không tồn tại" });
            }

            // Kiểm tra mật khẩu cũ
            var matKhauCuHash = HashPassword(dto.MatKhauCu);
            if (taiKhoan.MatKhau != matKhauCuHash)
            {
                return BadRequest(new { code = 400, message = "Mật khẩu cũ không đúng" });
            }

            // Kiểm tra mật khẩu mới
            if (string.IsNullOrWhiteSpace(dto.MatKhauMoi) || dto.MatKhauMoi.Length < 6)
            {
                return BadRequest(new { code = 400, message = "Mật khẩu mới phải có ít nhất 6 ký tự" });
            }

            // Cập nhật mật khẩu
            taiKhoan.MatKhau = HashPassword(dto.MatKhauMoi);
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Đổi mật khẩu thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Cập nhật thông tin cá nhân
    [HttpPut("cap-nhat")]
    public async Task<IActionResult> CapNhatThongTin([FromBody] TaiKhoanUpdateDTO dto, [FromQuery] int taiKhoanId)
    {
        try
        {
            var taiKhoan = _context.TaiKhoans.FirstOrDefault(tk => tk.Id == taiKhoanId);
            if (taiKhoan == null)
            {
                return Unauthorized(new { code = 401, message = "Tài khoản không tồn tại" });
            }

            if (!string.IsNullOrWhiteSpace(dto.HoTen))
            {
                taiKhoan.HoTen = dto.HoTen;
            }

            if (!string.IsNullOrWhiteSpace(dto.SoDienThoai))
            {
                taiKhoan.SoDienThoai = dto.SoDienThoai;
            }

            if (!string.IsNullOrWhiteSpace(dto.DiaChi))
            {
                taiKhoan.DiaChi = dto.DiaChi;
            }

            // Xử lý upload ảnh đại diện lên Cloudinary
            if (!string.IsNullOrWhiteSpace(dto.AnhDaiDien))
            {
                try
                {
                    // Kiểm tra nếu là base64 string
                    if (dto.AnhDaiDien.StartsWith("data:image/") || dto.AnhDaiDien.StartsWith("/9j/") || dto.AnhDaiDien.StartsWith("iVBORw0KGgo"))
                    {
                        // Xóa ảnh cũ nếu có
                        if (!string.IsNullOrWhiteSpace(taiKhoan.AnhDaiDien) && taiKhoan.AnhDaiDien.Contains("cloudinary.com"))
                        {
                            await _cloudinaryService.DeleteImageAsync(taiKhoan.AnhDaiDien);
                        }

                        // Upload ảnh mới lên Cloudinary
                        var fileName = $"avatar_{taiKhoanId}_{DateTime.Now:yyyyMMddHHmmss}";
                        var imageUrl = await _cloudinaryService.UploadImageFromBase64Async(dto.AnhDaiDien, fileName, "avatars");
                        taiKhoan.AnhDaiDien = imageUrl;
                    }
                    else
                    {
                        // Nếu là URL thì giữ nguyên
                        taiKhoan.AnhDaiDien = dto.AnhDaiDien;
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(new { code = 400, message = $"Lỗi khi upload ảnh: {ex.Message}" });
                }
            }

            _context.SaveChanges();

            return Ok(new { code = 200, message = "Cập nhật thông tin thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Test Cloudinary connection (chỉ dùng để debug)
    [HttpGet("test-cloudinary")]
    public IActionResult TestCloudinary()
    {
        try
        {
            var cloudinarySettings = _configuration.GetSection("CloudinarySettings");
            var cloudName = cloudinarySettings["CloudName"];
            var apiKey = cloudinarySettings["ApiKey"];
            var apiSecret = cloudinarySettings["ApiSecret"];

            return Ok(new
            {
                code = 200,
                message = "Cloudinary config loaded",
                cloudName = cloudName,
                apiKey = apiKey != null ? $"{apiKey.Substring(0, Math.Min(5, apiKey.Length))}..." : "null",
                apiSecret = apiSecret != null ? $"{apiSecret.Substring(0, Math.Min(5, apiSecret.Length))}..." : "null",
                hasAllConfig = !string.IsNullOrWhiteSpace(cloudName) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(apiSecret)
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Cập nhật ảnh đại diện
    [HttpPut("anh-dai-dien")]
    public async Task<IActionResult> CapNhatAnhDaiDien([FromBody] AnhDaiDienUpdateDTO dto, [FromQuery] int taiKhoanId)
    {
        try
        {
            var taiKhoan = _context.TaiKhoans.FirstOrDefault(tk => tk.Id == taiKhoanId);
            if (taiKhoan == null)
            {
                return Unauthorized(new { code = 401, message = "Tài khoản không tồn tại" });
            }

            // Xử lý upload ảnh đại diện lên Cloudinary
            if (!string.IsNullOrWhiteSpace(dto.AnhDaiDien))
            {
                try
                {
                    // Kiểm tra nếu là base64 string
                    if (dto.AnhDaiDien.StartsWith("data:image/") || dto.AnhDaiDien.StartsWith("/9j/") || dto.AnhDaiDien.StartsWith("iVBORw0KGgo"))
                    {
                        // Xóa ảnh cũ nếu có
                        if (!string.IsNullOrWhiteSpace(taiKhoan.AnhDaiDien) && taiKhoan.AnhDaiDien.Contains("cloudinary.com"))
                        {
                            await _cloudinaryService.DeleteImageAsync(taiKhoan.AnhDaiDien);
                        }

                        // Upload ảnh mới lên Cloudinary
                        var fileName = $"avatar_{taiKhoanId}_{DateTime.Now:yyyyMMddHHmmss}";
                        var imageUrl = await _cloudinaryService.UploadImageFromBase64Async(dto.AnhDaiDien, fileName, "avatars");
                        taiKhoan.AnhDaiDien = imageUrl;
                    }
                    else
                    {
                        // Nếu là URL thì giữ nguyên
                        taiKhoan.AnhDaiDien = dto.AnhDaiDien;
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(new { code = 400, message = $"Lỗi khi upload ảnh: {ex.Message}" });
                }
            }

            _context.SaveChanges();

            return Ok(new { code = 200, message = "Cập nhật ảnh đại diện thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Xem thông tin tài khoản
    [HttpGet("thong-tin")]
    public IActionResult GetThongTin([FromQuery] int taiKhoanId)
    {
        try
        {
            var taiKhoan = _context.TaiKhoans
                .Where(tk => tk.Id == taiKhoanId)
                .Select(tk => new TaiKhoanResponseDTO
                {
                    Id = tk.Id,
                    HoTen = tk.HoTen,
                    Email = tk.Email,
                    SoDienThoai = tk.SoDienThoai,
                    DiaChi = tk.DiaChi,
                    AnhDaiDien = tk.AnhDaiDien,
                    NgayTao = tk.NgayTao,
                    TrangThai = tk.TrangThai,
                    TenLoaiTaiKhoan = tk.LoaiTaiKhoan != null ? tk.LoaiTaiKhoan.TenLoai : null
                })
                .FirstOrDefault();

            if (taiKhoan == null)
            {
                return Unauthorized(new { code = 401, message = "Tài khoản không tồn tại" });
            }

            return Ok(new { code = 200, message = "Thành công", data = taiKhoan });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Xem lịch sử mua hàng
    [HttpGet("lich-su-mua-hang")]
    public IActionResult GetLichSuMuaHang([FromQuery] int taiKhoanId)
    {
        try
        {
            var donHangs = _context.DonHangs
                .Where(dh => dh.TaiKhoanId == taiKhoanId)
                .OrderByDescending(dh => dh.NgayDat)
                .Select(dh => new DonHangResponseDTO
                {
                    Id = dh.Id,
                    NgayDat = dh.NgayDat,
                    TongTien = dh.TongTien,
                    TrangThai = dh.TrangThai,
                    TenNguoiNhan = dh.TenNguoiNhan,
                    DiaChiGiaoHang = dh.DiaChiGiaoHang,
                    SdtnguoiNhan = dh.SdtnguoiNhan,
                    ChiTiet = dh.ChiTietDonHangs.Select(ct => new DonHangItemDTO
                    {
                        SanPhamId = ct.SanPhamId ?? 0,
                        TenSanPham = ct.SanPham != null ? ct.SanPham.TenSanPham : "",
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia ?? 0
                    }).ToList()
                })
                .ToList();

            return Ok(new { code = 200, message = "Thành công", data = donHangs });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Gửi mã OTP
    [HttpPost("gui-otp")]
    public async Task<IActionResult> GuiOTP([FromBody] GuiOTPDTO dto)
    {
        try
        {
            // Kiểm tra email có tồn tại không
            var taiKhoan = _context.TaiKhoans.FirstOrDefault(tk => tk.Email == dto.Email);
            if (taiKhoan == null)
            {
                return BadRequest(new { code = 400, message = "Email không tồn tại trong hệ thống" });
            }

            // Kiểm tra tài khoản có bị khóa không
            if (taiKhoan.TrangThai != true)
            {
                return BadRequest(new { code = 400, message = "Tài khoản đã bị khóa" });
            }

            // Tạo mã OTP 6 số
            var random = new Random();
            var otpCode = random.Next(100000, 999999).ToString();

            // Lưu mã OTP vào database
            taiKhoan.Code = otpCode;
            _context.SaveChanges();

            // Gửi email chứa mã OTP
            var emailRequest = new GuiEmailRequest
            {
                Email = dto.Email,
                TieuDe = "Mã OTP đổi mật khẩu - Shop FE",
                NoiDung = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px;'>
                        <h2 style='color: #6366f1;'>Mã OTP đổi mật khẩu</h2>
                        <p>Xin chào <strong>{taiKhoan.HoTen ?? "Quý khách"}</strong>,</p>
                        <p>Bạn đã yêu cầu đổi mật khẩu. Mã OTP của bạn là:</p>
                        <div style='background-color: #f3f4f6; padding: 15px; border-radius: 8px; text-align: center; margin: 20px 0;'>
                            <h1 style='color: #6366f1; font-size: 32px; letter-spacing: 5px; margin: 0;'>{otpCode}</h1>
                        </div>
                        <p style='color: #666; font-size: 14px;'>Mã OTP này có hiệu lực trong 10 phút. Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
                        <p style='color: #666; font-size: 14px;'>Nếu bạn không yêu cầu đổi mật khẩu, vui lòng bỏ qua email này.</p>
                        <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;' />
                        <p style='color: #999; font-size: 12px;'>Trân trọng,<br/>Đội ngũ Shop FE</p>
                    </div>
                "
            };

            var emailResult = await GuiEmailInternal(emailRequest);
            
            // Kiểm tra kết quả gửi email
            if (emailResult is ObjectResult objResult)
            {
                var statusCode = objResult.StatusCode ?? 200;
                if (statusCode != 200)
                {
                    // Nếu gửi email thất bại, xóa mã OTP đã lưu
                    taiKhoan.Code = null;
                    _context.SaveChanges();
                    return StatusCode(500, new { code = 500, message = "Không thể gửi email. Vui lòng thử lại sau." });
                }
            }

            return Ok(new { code = 200, message = "Mã OTP đã được gửi đến email của bạn" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Xác nhận mã OTP
    [HttpPost("xac-nhan-otp")]
    public IActionResult XacNhanOTP([FromBody] XacNhanOTPDTO dto)
    {
        try
        {
            // Kiểm tra email có tồn tại không
            var taiKhoan = _context.TaiKhoans.FirstOrDefault(tk => tk.Email == dto.Email);
            if (taiKhoan == null)
            {
                return BadRequest(new { code = 400, message = "Email không tồn tại trong hệ thống" });
            }

            // Kiểm tra mã OTP
            if (string.IsNullOrWhiteSpace(taiKhoan.Code))
            {
                return BadRequest(new { code = 400, message = "Mã OTP không tồn tại hoặc đã hết hạn. Vui lòng yêu cầu gửi lại mã OTP." });
            }

            if (taiKhoan.Code != dto.Code)
            {
                return BadRequest(new { code = 400, message = "Mã OTP không đúng" });
            }

            // Mã OTP đúng, không xóa mã ngay (để dùng cho bước đổi mật khẩu)
            return Ok(new { code = 200, message = "Xác nhận mã OTP thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Đổi mật khẩu khi quên (sử dụng OTP)
    [HttpPut("doi-mat-khau-quen")]
    public IActionResult DoiMatKhauQuen([FromBody] DoiMatKhauQuenDTO dto)
    {
        try
        {
            // Kiểm tra email có tồn tại không
            var taiKhoan = _context.TaiKhoans.FirstOrDefault(tk => tk.Email == dto.Email);
            if (taiKhoan == null)
            {
                return BadRequest(new { code = 400, message = "Email không tồn tại trong hệ thống" });
            }

            // Kiểm tra mã OTP
            if (string.IsNullOrWhiteSpace(taiKhoan.Code))
            {
                return BadRequest(new { code = 400, message = "Mã OTP không tồn tại hoặc đã hết hạn. Vui lòng yêu cầu gửi lại mã OTP." });
            }

            if (taiKhoan.Code != dto.Code)
            {
                return BadRequest(new { code = 400, message = "Mã OTP không đúng" });
            }

            // Kiểm tra mật khẩu mới
            if (string.IsNullOrWhiteSpace(dto.MatKhauMoi) || dto.MatKhauMoi.Length < 6)
            {
                return BadRequest(new { code = 400, message = "Mật khẩu mới phải có ít nhất 6 ký tự" });
            }

            // Cập nhật mật khẩu mới
            taiKhoan.MatKhau = HashPassword(dto.MatKhauMoi);
            
            // Xóa mã OTP sau khi đổi mật khẩu thành công
            taiKhoan.Code = null;
            
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Đổi mật khẩu thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    private async Task<IActionResult> GuiEmailInternal(GuiEmailRequest request)
    {
        var emailSetting = _configuration.GetSection("EmailSetting").Get<EmailSetting>();
        if (emailSetting == null || string.IsNullOrWhiteSpace(emailSetting.SmtpServer) || string.IsNullOrWhiteSpace(emailSetting.SmtpUsername) || string.IsNullOrWhiteSpace(emailSetting.SmtpPassword) || string.IsNullOrWhiteSpace(emailSetting.SenderEmail))
        {
            return StatusCode(500, new { message = "Thiếu cấu hình EmailSetting trong appsettings." });
        }

        try
        {
            using var smtpClient = new SmtpClient(emailSetting.SmtpServer, emailSetting.SmtpPort)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(emailSetting.SmtpUsername, emailSetting.SmtpPassword)
            };

            var fromAddress = string.IsNullOrWhiteSpace(emailSetting.SenderName)
                ? new MailAddress(emailSetting.SenderEmail)
                : new MailAddress(emailSetting.SenderEmail, emailSetting.SenderName);

            var message = new MailMessage(fromAddress, new MailAddress(request.Email))
            {
                Subject = request.TieuDe,
                Body = request.NoiDung,
                IsBodyHtml = true
            };

            await smtpClient.SendMailAsync(message);

            return Ok(new { message = "Gửi email thành công." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Gửi email thất bại: {ex.Message}" });
        }
    }
    // Helper method để hash mật khẩu
    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
    private class EmailSetting
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string? SenderName { get; set; }
    }
    public class GuiEmailRequest
    {
        public string Email { get; set; } = string.Empty;
        public string TieuDe { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
    }
}

