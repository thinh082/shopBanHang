using Microsoft.AspNetCore.Mvc;
using shopBanHang.Models.Entities;

namespace shopBanHang.Controllers.QuanLy;

[ApiController]
[Route("api/QuanLy/[controller]")]
public class QuanLyDanhMucController : ControllerBase
{
    private readonly ShopContext _context;

    public QuanLyDanhMucController(ShopContext context)
    {
        _context = context;
    }

    // GET: api/QuanLy/QuanLyDanhMuc - Lấy danh sách danh mục
    [HttpGet]
    public IActionResult GetDanhMucs([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        try
        {
            var query = _context.DanhMucs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(dm => dm.TenDanhMuc.Contains(search));
            }

            var total = query.Count();

            var danhMucs = query
                .OrderBy(dm => dm.TenDanhMuc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(dm => new
                {
                    dm.Id,
                    dm.TenDanhMuc,
                    SoLuongSanPham = dm.SanPhams.Count(sp => sp.TrangThai == true),
                    SoLuongTaiKhoan = dm.TaiKhoans.Count
                })
                .ToList();

            return Ok(new
            {
                code = 200,
                message = "Thành công",
                data = danhMucs,
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

    // GET: api/QuanLy/QuanLyDanhMuc/{id} - Lấy chi tiết danh mục
    [HttpGet("{id}")]
    public IActionResult GetDanhMuc(int id)
    {
        try
        {
            var danhMuc = _context.DanhMucs
                .Where(dm => dm.Id == id)
                .Select(dm => new
                {
                    dm.Id,
                    dm.TenDanhMuc,
                    SoLuongSanPham = dm.SanPhams.Count(sp => sp.TrangThai == true),
                    SoLuongTaiKhoan = dm.TaiKhoans.Count,
                    SanPhams = dm.SanPhams.Select(sp => new
                    {
                        sp.Id,
                        sp.TenSanPham,
                        sp.Gia,
                        sp.SoLuong,
                        sp.TrangThai
                    }).ToList()
                })
                .FirstOrDefault();

            if (danhMuc == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy danh mục" });
            }

            return Ok(new { code = 200, message = "Thành công", data = danhMuc });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // POST: api/QuanLy/QuanLyDanhMuc - Tạo danh mục mới
    [HttpPost]
    public IActionResult CreateDanhMuc([FromBody] CreateDanhMucDTO dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.TenDanhMuc))
            {
                return BadRequest(new { code = 400, message = "Tên danh mục không được để trống" });
            }

            // Kiểm tra tên danh mục đã tồn tại
            var tenTonTai = _context.DanhMucs.Any(dm => dm.TenDanhMuc == dto.TenDanhMuc);
            if (tenTonTai)
            {
                return BadRequest(new { code = 400, message = "Tên danh mục đã tồn tại" });
            }

            var danhMuc = new DanhMuc
            {
                TenDanhMuc = dto.TenDanhMuc
            };

            _context.DanhMucs.Add(danhMuc);
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Tạo danh mục thành công", data = new { id = danhMuc.Id } });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // PUT: api/QuanLy/QuanLyDanhMuc/{id} - Cập nhật danh mục
    [HttpPut("{id}")]
    public IActionResult UpdateDanhMuc(int id, [FromBody] UpdateDanhMucDTO dto)
    {
        try
        {
            var danhMuc = _context.DanhMucs.FirstOrDefault(dm => dm.Id == id);
            if (danhMuc == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy danh mục" });
            }

            if (string.IsNullOrWhiteSpace(dto.TenDanhMuc))
            {
                return BadRequest(new { code = 400, message = "Tên danh mục không được để trống" });
            }

            // Kiểm tra tên danh mục đã tồn tại (trừ chính nó)
            var tenTonTai = _context.DanhMucs.Any(dm => dm.TenDanhMuc == dto.TenDanhMuc && dm.Id != id);
            if (tenTonTai)
            {
                return BadRequest(new { code = 400, message = "Tên danh mục đã tồn tại" });
            }

            danhMuc.TenDanhMuc = dto.TenDanhMuc;
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Cập nhật danh mục thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // DELETE: api/QuanLy/QuanLyDanhMuc/{id} - Xóa danh mục
    [HttpDelete("{id}")]
    public IActionResult DeleteDanhMuc(int id)
    {
        try
        {
            var danhMuc = _context.DanhMucs.FirstOrDefault(dm => dm.Id == id);
            if (danhMuc == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy danh mục" });
            }

            // Kiểm tra xem danh mục có sản phẩm không
            var coSanPham = _context.SanPhams.Any(sp => sp.DanhMucId == id);
            if (coSanPham)
            {
                return BadRequest(new { code = 400, message = "Không thể xóa danh mục có sản phẩm. Vui lòng xóa hoặc chuyển sản phẩm trước" });
            }

            // Kiểm tra xem danh mục có tài khoản không
            var coTaiKhoan = _context.TaiKhoans.Any(tk => tk.IdDanhMuc == id);
            if (coTaiKhoan)
            {
                return BadRequest(new { code = 400, message = "Không thể xóa danh mục có tài khoản liên quan" });
            }

            _context.DanhMucs.Remove(danhMuc);
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Xóa danh mục thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }
}

// DTOs cho QuanLyDanhMuc
public class CreateDanhMucDTO
{
    public string TenDanhMuc { get; set; } = null!;
}

public class UpdateDanhMucDTO
{
    public string TenDanhMuc { get; set; } = null!;
}

