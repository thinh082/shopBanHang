using Microsoft.AspNetCore.Mvc;
using shopBanHang.Models.Entities;

namespace shopBanHang.Controllers.QuanLy;

[ApiController]
[Route("api/QuanLy/[controller]")]
public class QuanLyTaiKhoanController : ControllerBase
{
    private readonly ShopContext _context;

    public QuanLyTaiKhoanController(ShopContext context)
    {
        _context = context;
    }

    // GET: api/QuanLy/QuanLyTaiKhoan - Lấy danh sách tài khoản
    [HttpGet]
    public IActionResult GetTaiKhoans([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        try
        {
            var query = _context.TaiKhoans.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(tk => 
                    (tk.HoTen != null && tk.HoTen.Contains(search)) ||
                    tk.Email.Contains(search) ||
                    (tk.SoDienThoai != null && tk.SoDienThoai.Contains(search))
                );
            }

            var total = query.Count();

            var taiKhoans = query
                .OrderByDescending(tk => tk.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(tk => new
                {
                    tk.Id,
                    tk.HoTen,
                    tk.Email,
                    tk.SoDienThoai,
                    tk.DiaChi,
                    tk.AnhDaiDien,
                    tk.NgayTao,
                    tk.TrangThai,
                    LoaiTaiKhoanId = tk.LoaiTaiKhoanId,
                    TenLoaiTaiKhoan = tk.LoaiTaiKhoan != null ? tk.LoaiTaiKhoan.TenLoai : null,
                    IdDanhMuc = tk.IdDanhMuc
                })
                .ToList();

            return Ok(new
            {
                code = 200,
                message = "Thành công",
                data = taiKhoans,
                total = total,
                page = page,
                pageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // GET: api/QuanLy/QuanLyTaiKhoan/{id} - Lấy chi tiết tài khoản
    [HttpGet("{id}")]
    public IActionResult GetTaiKhoan(int id)
    {
        try
        {
            var taiKhoan = _context.TaiKhoans
                .Where(tk => tk.Id == id)
                .Select(tk => new
                {
                    tk.Id,
                    tk.HoTen,
                    tk.Email,
                    tk.SoDienThoai,
                    tk.DiaChi,
                    tk.AnhDaiDien,
                    tk.NgayTao,
                    tk.TrangThai,
                    LoaiTaiKhoanId = tk.LoaiTaiKhoanId,
                    TenLoaiTaiKhoan = tk.LoaiTaiKhoan != null ? tk.LoaiTaiKhoan.TenLoai : null,
                    IdDanhMuc = tk.IdDanhMuc
                })
                .FirstOrDefault();

            if (taiKhoan == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy tài khoản" });
            }

            return Ok(new { code = 200, message = "Thành công", data = taiKhoan });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // POST: api/QuanLy/QuanLyTaiKhoan - Tạo tài khoản mới
    [HttpPost]
    public IActionResult CreateTaiKhoan([FromBody] CreateTaiKhoanDTO dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return BadRequest(new { code = 400, message = "Email không được để trống" });
            }

            if (string.IsNullOrWhiteSpace(dto.MatKhau))
            {
                return BadRequest(new { code = 400, message = "Mật khẩu không được để trống" });
            }

            if (dto.MatKhau.Length < 6)
            {
                return BadRequest(new { code = 400, message = "Mật khẩu phải có ít nhất 6 ký tự" });
            }

            // Kiểm tra email đã tồn tại
            var emailTonTai = _context.TaiKhoans.Any(tk => tk.Email == dto.Email);
            if (emailTonTai)
            {
                return BadRequest(new { code = 400, message = "Email đã được sử dụng" });
            }

            // Kiểm tra LoaiTaiKhoanId nếu có
            if (dto.LoaiTaiKhoanId.HasValue)
            {
                var loaiTaiKhoan = _context.LoaiTaiKhoans.FirstOrDefault(ltk => ltk.Id == dto.LoaiTaiKhoanId.Value);
                if (loaiTaiKhoan == null)
                {
                    return BadRequest(new { code = 400, message = "Loại tài khoản không tồn tại" });
                }
            }

            // Kiểm tra DanhMucId nếu có
            if (dto.IdDanhMuc.HasValue)
            {
                var danhMuc = _context.DanhMucs.FirstOrDefault(dm => dm.Id == dto.IdDanhMuc.Value);
                if (danhMuc == null)
                {
                    return BadRequest(new { code = 400, message = "Danh mục không tồn tại" });
                }
            }

            // Mã hóa mật khẩu
            var matKhauHash = HashPassword(dto.MatKhau);

            var taiKhoan = new TaiKhoan
            {
                HoTen = dto.HoTen,
                Email = dto.Email,
                MatKhau = matKhauHash,
                SoDienThoai = dto.SoDienThoai,
                DiaChi = dto.DiaChi,
                AnhDaiDien = dto.AnhDaiDien,
                NgayTao = DateTime.Now,
                TrangThai = dto.TrangThai ?? true,
                LoaiTaiKhoanId = dto.LoaiTaiKhoanId,
                IdDanhMuc = dto.IdDanhMuc
            };

            _context.TaiKhoans.Add(taiKhoan);
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Tạo tài khoản thành công", data = new { id = taiKhoan.Id } });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // PUT: api/QuanLy/QuanLyTaiKhoan/{id} - Cập nhật tài khoản
    [HttpPut("{id}")]
    public IActionResult UpdateTaiKhoan(int id, [FromBody] UpdateTaiKhoanDTO dto)
    {
        try
        {
            var taiKhoan = _context.TaiKhoans.FirstOrDefault(tk => tk.Id == id);
            if (taiKhoan == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy tài khoản" });
            }

            // Kiểm tra email nếu thay đổi
            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != taiKhoan.Email)
            {
                var emailTonTai = _context.TaiKhoans.Any(tk => tk.Email == dto.Email && tk.Id != id);
                if (emailTonTai)
                {
                    return BadRequest(new { code = 400, message = "Email đã được sử dụng" });
                }
                taiKhoan.Email = dto.Email;
            }

            // Kiểm tra LoaiTaiKhoanId nếu có
            if (dto.LoaiTaiKhoanId.HasValue)
            {
                var loaiTaiKhoan = _context.LoaiTaiKhoans.FirstOrDefault(ltk => ltk.Id == dto.LoaiTaiKhoanId.Value);
                if (loaiTaiKhoan == null)
                {
                    return BadRequest(new { code = 400, message = "Loại tài khoản không tồn tại" });
                }
                taiKhoan.LoaiTaiKhoanId = dto.LoaiTaiKhoanId;
            }

            // Kiểm tra DanhMucId nếu có
            if (dto.IdDanhMuc.HasValue)
            {
                var danhMuc = _context.DanhMucs.FirstOrDefault(dm => dm.Id == dto.IdDanhMuc.Value);
                if (danhMuc == null)
                {
                    return BadRequest(new { code = 400, message = "Danh mục không tồn tại" });
                }
                taiKhoan.IdDanhMuc = dto.IdDanhMuc;
            }

            if (!string.IsNullOrWhiteSpace(dto.HoTen))
            {
                taiKhoan.HoTen = dto.HoTen;
            }

            if (dto.SoDienThoai != null)
            {
                taiKhoan.SoDienThoai = dto.SoDienThoai;
            }

            if (dto.DiaChi != null)
            {
                taiKhoan.DiaChi = dto.DiaChi;
            }

            if (dto.AnhDaiDien != null)
            {
                taiKhoan.AnhDaiDien = dto.AnhDaiDien;
            }

            if (dto.TrangThai.HasValue)
            {
                taiKhoan.TrangThai = dto.TrangThai.Value;
            }

            // Cập nhật mật khẩu nếu có
            if (!string.IsNullOrWhiteSpace(dto.MatKhau))
            {
                if (dto.MatKhau.Length < 6)
                {
                    return BadRequest(new { code = 400, message = "Mật khẩu phải có ít nhất 6 ký tự" });
                }
                taiKhoan.MatKhau = HashPassword(dto.MatKhau);
            }

            _context.SaveChanges();

            return Ok(new { code = 200, message = "Cập nhật tài khoản thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // DELETE: api/QuanLy/QuanLyTaiKhoan/{id} - Xóa tài khoản
    [HttpDelete("{id}")]
    public IActionResult DeleteTaiKhoan(int id)
    {
        try
        {
            var taiKhoan = _context.TaiKhoans.FirstOrDefault(tk => tk.Id == id);
            if (taiKhoan == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy tài khoản" });
            }

            // Kiểm tra xem tài khoản có đơn hàng không
            var coDonHang = _context.DonHangs.Any(dh => dh.TaiKhoanId == id);
            if (coDonHang)
            {
                // Nếu có đơn hàng, chỉ vô hiệu hóa tài khoản
                taiKhoan.TrangThai = false;
                _context.SaveChanges();
                return Ok(new { code = 200, message = "Tài khoản đã được vô hiệu hóa (có đơn hàng liên quan)" });
            }

            _context.TaiKhoans.Remove(taiKhoan);
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Xóa tài khoản thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Helper method để hash mật khẩu
    private string HashPassword(string password)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}

// DTOs cho QuanLyTaiKhoan
public class CreateTaiKhoanDTO
{
    public string? HoTen { get; set; }
    public string Email { get; set; } = null!;
    public string MatKhau { get; set; } = null!;
    public string? SoDienThoai { get; set; }
    public string? DiaChi { get; set; }
    public string? AnhDaiDien { get; set; }
    public bool? TrangThai { get; set; }
    public int? LoaiTaiKhoanId { get; set; }
    public int? IdDanhMuc { get; set; }
}

public class UpdateTaiKhoanDTO
{
    public string? HoTen { get; set; }
    public string? Email { get; set; }
    public string? MatKhau { get; set; }
    public string? SoDienThoai { get; set; }
    public string? DiaChi { get; set; }
    public string? AnhDaiDien { get; set; }
    public bool? TrangThai { get; set; }
    public int? LoaiTaiKhoanId { get; set; }
    public int? IdDanhMuc { get; set; }
}

