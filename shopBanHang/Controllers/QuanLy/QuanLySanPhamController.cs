using Microsoft.AspNetCore.Mvc;
using shopBanHang.Models.Entities;

namespace shopBanHang.Controllers.QuanLy;

[ApiController]
[Route("api/QuanLy/[controller]")]
public class QuanLySanPhamController : ControllerBase
{
    private readonly ShopContext _context;

    public QuanLySanPhamController(ShopContext context)
    {
        _context = context;
    }

    // GET: api/QuanLy/QuanLySanPham - Lấy danh sách sản phẩm
    [HttpGet]
    public IActionResult GetSanPhams([FromQuery] int page = 1, [FromQuery] int pageSize = 10, 
        [FromQuery] string? search = null, [FromQuery] int? danhMucId = null, [FromQuery] bool? trangThai = null)
    {
        try
        {
            var query = _context.SanPhams.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(sp => sp.TenSanPham.Contains(search) || 
                    (sp.MoTa != null && sp.MoTa.Contains(search)));
            }

            if (danhMucId.HasValue)
            {
                query = query.Where(sp => sp.DanhMucId == danhMucId.Value);
            }

            if (trangThai.HasValue)
            {
                query = query.Where(sp => sp.TrangThai == trangThai.Value);
            }

            var total = query.Count();

            var sanPhams = query
                .OrderByDescending(sp => sp.NgayThem)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(sp => new
                {
                    sp.Id,
                    sp.TenSanPham,
                    sp.MoTa,
                    sp.Gia,
                    sp.SoLuong,
                    sp.DanhMucId,
                    TenDanhMuc = sp.DanhMuc != null ? sp.DanhMuc.TenDanhMuc : null,
                    sp.NgayThem,
                    sp.TrangThai,
                    sp.ThuongHieu,
                    sp.KhuyenMai,
                    SoLuongHinhAnh = sp.SanPhamHinhAnhs.Count,
                    SoLuongThongSo = sp.ThongSos.Count,
                    SoLuongDanhGia = sp.DanhGia.Count
                })
                .ToList();

            return Ok(new
            {
                code = 200,
                message = "Thành công",
                data = sanPhams,
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

    // GET: api/QuanLy/QuanLySanPham/{id} - Lấy chi tiết sản phẩm
    [HttpGet("{id}")]
    public IActionResult GetSanPham(int id)
    {
        try
        {
            var sanPham = _context.SanPhams
                .Where(sp => sp.Id == id)
                .Select(sp => new
                {
                    sp.Id,
                    sp.TenSanPham,
                    sp.MoTa,
                    sp.Gia,
                    sp.SoLuong,
                    sp.DanhMucId,
                    TenDanhMuc = sp.DanhMuc != null ? sp.DanhMuc.TenDanhMuc : null,
                    sp.NgayThem,
                    sp.TrangThai,
                    sp.ThuongHieu,
                    sp.KhuyenMai,
                    HinhAnhs = sp.SanPhamHinhAnhs.Select(ha => new
                    {
                        ha.Id,
                        ha.DuongDan
                    }).ToList(),
                    ThongSo = sp.ThongSos.Select(ts => new
                    {
                        ts.Id,
                        ts.Cpu,
                        ts.Vga,
                        ts.Ram,
                        ts.Rom
                    }).ToList(),
                    SoLuongDanhGia = sp.DanhGia.Count,
                    DiemTrungBinh = sp.DanhGia.Any() ? sp.DanhGia.Average(dg => (double?)dg.Diem) : null
                })
                .FirstOrDefault();

            if (sanPham == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy sản phẩm" });
            }

            return Ok(new { code = 200, message = "Thành công", data = sanPham });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // POST: api/QuanLy/QuanLySanPham - Tạo sản phẩm mới
    [HttpPost]
    public IActionResult CreateSanPham([FromBody] CreateSanPhamDTO dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.TenSanPham))
            {
                return BadRequest(new { code = 400, message = "Tên sản phẩm không được để trống" });
            }

            if (dto.Gia <= 0)
            {
                return BadRequest(new { code = 400, message = "Giá sản phẩm phải lớn hơn 0" });
            }

            if (dto.SoLuong < 0)
            {
                return BadRequest(new { code = 400, message = "Số lượng không được âm" });
            }

            // Kiểm tra danh mục nếu có
            if (dto.DanhMucId.HasValue)
            {
                var danhMuc = _context.DanhMucs.FirstOrDefault(dm => dm.Id == dto.DanhMucId.Value);
                if (danhMuc == null)
                {
                    return BadRequest(new { code = 404, message = "Danh mục không tồn tại" });
                }
            }

            var sanPham = new SanPham
            {
                TenSanPham = dto.TenSanPham,
                MoTa = dto.MoTa,
                Gia = dto.Gia,
                SoLuong = dto.SoLuong,
                DanhMucId = dto.DanhMucId,
                NgayThem = DateTime.Now,
                TrangThai = dto.TrangThai ?? true,
                ThuongHieu = dto.ThuongHieu,
                KhuyenMai = dto.KhuyenMai
            };

            _context.SanPhams.Add(sanPham);
            _context.SaveChanges();

            // Thêm hình ảnh nếu có
            if (dto.HinhAnhs != null && dto.HinhAnhs.Any())
            {
                foreach (var duongDan in dto.HinhAnhs)
                {
                    if (!string.IsNullOrWhiteSpace(duongDan))
                    {
                        var hinhAnh = new SanPhamHinhAnh
                        {
                            SanPhamId = sanPham.Id,
                            DuongDan = duongDan
                        };
                        _context.SanPhamHinhAnhs.Add(hinhAnh);
                    }
                }
            }

            // Thêm thông số nếu có
            if (dto.ThongSo != null)
            {
                var thongSo = new ThongSo
                {
                    IdSanPham = sanPham.Id,
                    Cpu = dto.ThongSo.Cpu,
                    Vga = dto.ThongSo.Vga,
                    Ram = dto.ThongSo.Ram,
                    Rom = dto.ThongSo.Rom
                };
                _context.ThongSos.Add(thongSo);
            }

            _context.SaveChanges();

            return Ok(new { code = 200, message = "Tạo sản phẩm thành công", data = new { id = sanPham.Id } });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // PUT: api/QuanLy/QuanLySanPham/{id} - Cập nhật sản phẩm
    [HttpPut("{id}")]
    public IActionResult UpdateSanPham(int id, [FromBody] UpdateSanPhamDTO dto)
    {
        try
        {
            var sanPham = _context.SanPhams.FirstOrDefault(sp => sp.Id == id);
            if (sanPham == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy sản phẩm" });
            }

            if (!string.IsNullOrWhiteSpace(dto.TenSanPham))
            {
                sanPham.TenSanPham = dto.TenSanPham;
            }

            if (dto.MoTa != null)
            {
                sanPham.MoTa = dto.MoTa;
            }

            if (dto.Gia.HasValue && dto.Gia.Value > 0)
            {
                sanPham.Gia = dto.Gia.Value;
            }

            if (dto.SoLuong.HasValue && dto.SoLuong.Value >= 0)
            {
                sanPham.SoLuong = dto.SoLuong.Value;
            }

            if (dto.DanhMucId.HasValue)
            {
                var danhMuc = _context.DanhMucs.FirstOrDefault(dm => dm.Id == dto.DanhMucId.Value);
                if (danhMuc == null)
                {
                    return BadRequest(new { code = 404, message = "Danh mục không tồn tại" });
                }
                sanPham.DanhMucId = dto.DanhMucId;
            }

            if (dto.TrangThai.HasValue)
            {
                sanPham.TrangThai = dto.TrangThai.Value;
            }

            if (dto.ThuongHieu != null)
            {
                sanPham.ThuongHieu = dto.ThuongHieu;
            }

            if (dto.KhuyenMai.HasValue)
            {
                sanPham.KhuyenMai = dto.KhuyenMai;
            }

            _context.SaveChanges();

            return Ok(new { code = 200, message = "Cập nhật sản phẩm thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // DELETE: api/QuanLy/QuanLySanPham/{id} - Xóa sản phẩm
    [HttpDelete("{id}")]
    public IActionResult DeleteSanPham(int id)
    {
        try
        {
            var sanPham = _context.SanPhams.FirstOrDefault(sp => sp.Id == id);
            if (sanPham == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy sản phẩm" });
            }

            // Kiểm tra xem sản phẩm có trong đơn hàng không
            var coTrongDonHang = _context.ChiTietDonHangs.Any(ct => ct.SanPhamId == id);
            if (coTrongDonHang)
            {
                // Nếu có trong đơn hàng, chỉ vô hiệu hóa sản phẩm
                sanPham.TrangThai = false;
                _context.SaveChanges();
                return Ok(new { code = 200, message = "Sản phẩm đã được vô hiệu hóa (có trong đơn hàng)" });
            }

            // Xóa hình ảnh
            var hinhAnhs = _context.SanPhamHinhAnhs.Where(ha => ha.SanPhamId == id).ToList();
            _context.SanPhamHinhAnhs.RemoveRange(hinhAnhs);

            // Xóa thông số
            var thongSos = _context.ThongSos.Where(ts => ts.IdSanPham == id).ToList();
            _context.ThongSos.RemoveRange(thongSos);

            // Xóa chi tiết giỏ hàng
            var chiTietGioHangs = _context.ChiTietGioHangs.Where(ct => ct.SanPhamId == id).ToList();
            _context.ChiTietGioHangs.RemoveRange(chiTietGioHangs);

            // Xóa sản phẩm
            _context.SanPhams.Remove(sanPham);
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Xóa sản phẩm thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }
}

// DTOs cho QuanLySanPham
public class CreateSanPhamDTO
{
    public string TenSanPham { get; set; } = null!;
    public string? MoTa { get; set; }
    public decimal Gia { get; set; }
    public int SoLuong { get; set; }
    public int? DanhMucId { get; set; }
    public bool? TrangThai { get; set; }
    public string? ThuongHieu { get; set; }
    public int? KhuyenMai { get; set; }
    public List<string>? HinhAnhs { get; set; }
    public ThongSoCreateDTO? ThongSo { get; set; }
}

public class ThongSoCreateDTO
{
    public string? Cpu { get; set; }
    public string? Vga { get; set; }
    public string? Ram { get; set; }
    public string? Rom { get; set; }
}

public class UpdateSanPhamDTO
{
    public string? TenSanPham { get; set; }
    public string? MoTa { get; set; }
    public decimal? Gia { get; set; }
    public int? SoLuong { get; set; }
    public int? DanhMucId { get; set; }
    public bool? TrangThai { get; set; }
    public string? ThuongHieu { get; set; }
    public int? KhuyenMai { get; set; }
}

