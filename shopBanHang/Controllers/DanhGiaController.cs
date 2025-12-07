using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopBanHang.Models.DTOs;
using shopBanHang.Models.Entities;

namespace shopBanHang.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DanhGiaController : ControllerBase
{
    private readonly ShopContext _context;

    public DanhGiaController(ShopContext context)
    {
        _context = context;
    }

    // Gửi đánh giá (1-5 sao)
    [HttpPost]
    public IActionResult GuiDanhGia([FromBody] DanhGiaRequestDTO dto, [FromQuery] int taiKhoanId)
    {
        try
        {
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

            // Kiểm tra điểm đánh giá (1-5)
            if (dto.Diem < 1 || dto.Diem > 5)
            {
                return BadRequest(new { code = 400, message = "Điểm đánh giá phải từ 1 đến 5" });
            }

            // Kiểm tra đã đánh giá chưa
            var danhGiaCu = _context.DanhGia
                .FirstOrDefault(dg => dg.SanPhamId == dto.SanPhamId && dg.TaiKhoanId == taiKhoanId);

            if (danhGiaCu != null)
            {
                return BadRequest(new { code = 400, message = "Bạn đã đánh giá sản phẩm này rồi. Vui lòng chỉnh sửa đánh giá hiện tại." });
            }

            // TODO: Kiểm tra đã mua sản phẩm chưa (có thể kiểm tra sau)
            // var daMua = _context.DonHangs
            //     .Any(dh => dh.TaiKhoanId == taiKhoanId && 
            //                dh.ChiTietDonHangs.Any(ct => ct.SanPhamId == dto.SanPhamId) &&
            //                dh.TrangThai == "Đã giao");

            // Tạo đánh giá
            var danhGia = new DanhGium
            {
                SanPhamId = dto.SanPhamId,
                TaiKhoanId = taiKhoanId,
                NoiDung = dto.NoiDung,
                Diem = dto.Diem,
                NgayDanhGia = DateTime.Now
            };

            _context.DanhGia.Add(danhGia);
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Gửi đánh giá thành công", danhGiaId = danhGia.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Chỉnh sửa đánh giá
    [HttpPut("{id}")]
    public IActionResult ChinhSuaDanhGia(int id, [FromBody] DanhGiaUpdateDTO dto, [FromQuery] int taiKhoanId)
    {
        try
        {
            var danhGia = _context.DanhGia
                .FirstOrDefault(dg => dg.Id == id && dg.TaiKhoanId == taiKhoanId);

            if (danhGia == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy đánh giá" });
            }

            // Kiểm tra điểm đánh giá (1-5)
            if (dto.Diem < 1 || dto.Diem > 5)
            {
                return BadRequest(new { code = 400, message = "Điểm đánh giá phải từ 1 đến 5" });
            }

            danhGia.NoiDung = dto.NoiDung;
            danhGia.Diem = dto.Diem;
            danhGia.NgayDanhGia = DateTime.Now; // Cập nhật lại ngày

            _context.SaveChanges();

            return Ok(new { code = 200, message = "Chỉnh sửa đánh giá thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Xóa đánh giá
    [HttpDelete("{id}")]
    public IActionResult XoaDanhGia(int id, [FromQuery] int taiKhoanId)
    {
        try
        {
            var danhGia = _context.DanhGia
                .FirstOrDefault(dg => dg.Id == id && dg.TaiKhoanId == taiKhoanId);

            if (danhGia == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy đánh giá" });
            }

            _context.DanhGia.Remove(danhGia);
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Xóa đánh giá thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Kiểm tra đánh giá của user cho sản phẩm (phải đặt trước route có parameter để tránh conflict)
    [HttpGet("check")]
    public IActionResult KiemTraDanhGia([FromQuery] int sanPhamId, [FromQuery] int taiKhoanId)
    {
        try
        {
            var danhGia = _context.DanhGia
                .Where(dg => dg.SanPhamId == sanPhamId && dg.TaiKhoanId == taiKhoanId)
                .Select(dg => new DanhGiaResponseDTO
                {
                    Id = dg.Id,
                    SanPhamId = dg.SanPhamId ?? 0,
                    TaiKhoanId = dg.TaiKhoanId ?? 0,
                    HoTen = dg.TaiKhoan != null ? dg.TaiKhoan.HoTen : null,
                    NoiDung = dg.NoiDung,
                    Diem = dg.Diem,
                    NgayDanhGia = dg.NgayDanhGia
                })
                .FirstOrDefault();

            if (danhGia == null)
            {
                return Ok(new { code = 200, message = "Chưa đánh giá", data = (DanhGiaResponseDTO?)null });
            }

            return Ok(new { code = 200, message = "Đã đánh giá", data = danhGia });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Xem đánh giá của một sản phẩm (đã có trong SanPhamController, nhưng có thể thêm ở đây nếu cần)
    [HttpGet("san-pham/{sanPhamId}")]
    public IActionResult GetDanhGiaBySanPham(int sanPhamId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var danhGias = _context.DanhGia
                .Where(dg => dg.SanPhamId == sanPhamId)
                .OrderByDescending(dg => dg.NgayDanhGia)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(dg => new DanhGiaResponseDTO
                {
                    Id = dg.Id,
                    SanPhamId = dg.SanPhamId ?? 0,
                    TaiKhoanId = dg.TaiKhoanId ?? 0,
                    HoTen = dg.TaiKhoan != null ? dg.TaiKhoan.HoTen : null,
                    NoiDung = dg.NoiDung,
                    Diem = dg.Diem,
                    NgayDanhGia = dg.NgayDanhGia
                })
                .ToList();

            var total = _context.DanhGia.Count(dg => dg.SanPhamId == sanPhamId);

            return Ok(new { 
                code = 200, 
                message = "Thành công", 
                data = danhGias,
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
}

