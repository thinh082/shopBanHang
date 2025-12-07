using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopBanHang.Models.DTOs;
using shopBanHang.Models.Entities;

namespace shopBanHang.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GioHangController : ControllerBase
{
    private readonly ShopContext _context;

    public GioHangController(ShopContext context)
    {
        _context = context;
    }

    // Thêm sản phẩm vào giỏ hàng
    [HttpPost("them")]
    public IActionResult ThemSanPhamVaoGioHang([FromBody] GioHangAddDTO dto, [FromQuery] int taiKhoanId)
    {
        try
        {
            if (dto.SoLuong <= 0)
            {
                return BadRequest(new { code = 400, message = "Số lượng phải lớn hơn 0" });
            }

            // Kiểm tra tài khoản
            var taiKhoan = _context.TaiKhoans.FirstOrDefault(tk => tk.Id == taiKhoanId);
            if (taiKhoan == null)
            {
                return Unauthorized(new { code = 401, message = "Tài khoản không tồn tại" });
            }

            // Kiểm tra sản phẩm
            var sanPham = _context.SanPhams.FirstOrDefault(sp => sp.Id == dto.SanPhamId);
            if (sanPham == null)
            {
                return BadRequest(new { code = 404, message = "Sản phẩm không tồn tại" });
            }

            if (sanPham.SoLuong < dto.SoLuong)
            {
                return BadRequest(new { code = 400, message = "Số lượng sản phẩm không đủ" });
            }

            // Tìm hoặc tạo giỏ hàng
            var gioHang = _context.GioHangs.FirstOrDefault(gh => gh.TaiKhoanId == taiKhoanId);
            if (gioHang == null)
            {
                gioHang = new GioHang
                {
                    TaiKhoanId = taiKhoanId,
                    NgayCapNhat = DateTime.Now
                };
                _context.GioHangs.Add(gioHang);
                _context.SaveChanges();
            }

            // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
            var chiTietGioHang = _context.ChiTietGioHangs
                .FirstOrDefault(ct => ct.GioHangId == gioHang.Id && ct.SanPhamId == dto.SanPhamId);

            if (chiTietGioHang != null)
            {
                // Cập nhật số lượng
                chiTietGioHang.SoLuong += dto.SoLuong;
                if (chiTietGioHang.SoLuong > sanPham.SoLuong)
                {
                    return BadRequest(new { code = 400, message = "Số lượng sản phẩm không đủ" });
                }
            }
            else
            {
                // Thêm mới
                chiTietGioHang = new ChiTietGioHang
                {
                    GioHangId = gioHang.Id,
                    SanPhamId = dto.SanPhamId,
                    SoLuong = dto.SoLuong
                };
                _context.ChiTietGioHangs.Add(chiTietGioHang);
            }

            gioHang.NgayCapNhat = DateTime.Now;
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Thêm sản phẩm vào giỏ hàng thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Xem giỏ hàng
    [HttpGet]
    public IActionResult XemGioHang([FromQuery] int taiKhoanId)
    {
        try
        {
            var gioHang = _context.GioHangs
                .Where(gh => gh.TaiKhoanId == taiKhoanId)
                .Select(gh => new GioHangResponseDTO
                {
                    GioHangId = gh.Id,
                    Items = gh.ChiTietGioHangs.Select(ct => new GioHangItemDTO
                    {
                        Id = ct.Id,
                        SanPhamId = ct.SanPhamId ?? 0,
                        TenSanPham = ct.SanPham != null ? ct.SanPham.TenSanPham : "",
                        Gia = ct.SanPham != null ? ct.SanPham.Gia : 0,
                        SoLuong = ct.SoLuong,
                        HinhAnh = ct.SanPham != null && ct.SanPham.SanPhamHinhAnhs.Any() 
                            ? ct.SanPham.SanPhamHinhAnhs.First().DuongDan 
                            : null,
                        SoLuongTon = ct.SanPham != null ? ct.SanPham.SoLuong : 0
                    }).ToList(),
                    TongTien = gh.ChiTietGioHangs.Sum(ct => 
                        (ct.SanPham != null ? ct.SanPham.Gia : 0) * ct.SoLuong)
                })
                .FirstOrDefault();

            if (gioHang == null)
            {
                return Ok(new { code = 200, message = "Giỏ hàng trống", data = new GioHangResponseDTO 
                { 
                    GioHangId = 0, 
                    Items = new List<GioHangItemDTO>(), 
                    TongTien = 0 
                }});
            }

            return Ok(new { code = 200, message = "Thành công", data = gioHang });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Xóa sản phẩm khỏi giỏ hàng
    [HttpDelete("{chiTietId}")]
    public IActionResult XoaSanPhamKhoiGioHang(int chiTietId, [FromQuery] int taiKhoanId)
    {
        try
        {
            var chiTietGioHang = _context.ChiTietGioHangs
                .FirstOrDefault(ct => ct.Id == chiTietId && ct.GioHang != null && ct.GioHang.TaiKhoanId == taiKhoanId);

            if (chiTietGioHang == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
            }

            _context.ChiTietGioHangs.Remove(chiTietGioHang);
            
            if (chiTietGioHang.GioHang != null)
            {
                chiTietGioHang.GioHang.NgayCapNhat = DateTime.Now;
            }
            
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Xóa sản phẩm khỏi giỏ hàng thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Cập nhật số lượng sản phẩm
    [HttpPut("{chiTietId}")]
    public IActionResult CapNhatSoLuong(int chiTietId, [FromBody] GioHangUpdateDTO dto, [FromQuery] int taiKhoanId)
    {
        try
        {
            if (dto.SoLuong <= 0)
            {
                return BadRequest(new { code = 400, message = "Số lượng phải lớn hơn 0" });
            }

            var chiTietGioHang = _context.ChiTietGioHangs
                .FirstOrDefault(ct => ct.Id == chiTietId && ct.GioHang != null && ct.GioHang.TaiKhoanId == taiKhoanId);

            if (chiTietGioHang == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
            }

            var sanPham = _context.SanPhams.FirstOrDefault(sp => sp.Id == chiTietGioHang.SanPhamId);
            if (sanPham == null)
            {
                return BadRequest(new { code = 404, message = "Sản phẩm không tồn tại" });
            }

            if (sanPham.SoLuong < dto.SoLuong)
            {
                return BadRequest(new { code = 400, message = "Số lượng sản phẩm không đủ" });
            }

            chiTietGioHang.SoLuong = dto.SoLuong;
            
            if (chiTietGioHang.GioHang != null)
            {
                chiTietGioHang.GioHang.NgayCapNhat = DateTime.Now;
            }
            
            _context.SaveChanges();

            // Tính lại tổng tiền
            var tongTien = _context.ChiTietGioHangs
                .Where(ct => ct.GioHangId == chiTietGioHang.GioHangId)
                .Sum(ct => (ct.SanPham != null ? ct.SanPham.Gia : 0) * ct.SoLuong);

            return Ok(new { code = 200, message = "Cập nhật số lượng thành công", tongTien = tongTien });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Lấy tổng tiền giỏ hàng
    [HttpGet("tong-tien")]
    public IActionResult GetTongTien([FromQuery] int taiKhoanId)
    {
        try
        {
            var tongTien = _context.GioHangs
                .Where(gh => gh.TaiKhoanId == taiKhoanId)
                .SelectMany(gh => gh.ChiTietGioHangs)
                .Sum(ct => (ct.SanPham != null ? ct.SanPham.Gia : 0) * ct.SoLuong);

            return Ok(new { code = 200, message = "Thành công", tongTien = tongTien });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }
}

