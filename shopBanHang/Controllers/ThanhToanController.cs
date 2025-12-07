using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopBanHang.Models.DTOs;
using shopBanHang.Models.Entities;

namespace shopBanHang.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ThanhToanController : ControllerBase
{
    private readonly ShopContext _context;

    public ThanhToanController(ShopContext context)
    {
        _context = context;
    }

    // Chọn phương thức thanh toán và tạo thanh toán
    [HttpPost("tao")]
    public IActionResult TaoThanhToan([FromBody] ThanhToanCreateDTO dto, [FromQuery] int taiKhoanId)
    {
        try
        {
            // Kiểm tra tài khoản
            var taiKhoan = _context.TaiKhoans.FirstOrDefault(tk => tk.Id == taiKhoanId);
            if (taiKhoan == null)
            {
                return Unauthorized(new { code = 401, message = "Tài khoản không tồn tại" });
            }

            // Kiểm tra đơn hàng
            var donHang = _context.DonHangs
                .FirstOrDefault(dh => dh.Id == dto.DonHangId && dh.TaiKhoanId == taiKhoanId);

            if (donHang == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy đơn hàng" });
            }

            // Kiểm tra phương thức thanh toán
            var phuongThucHopLe = new[] { "COD", "ChuyenKhoan", "ViDienTu" };
            if (!phuongThucHopLe.Contains(dto.PhuongThuc))
            {
                return BadRequest(new { code = 400, message = "Phương thức thanh toán không hợp lệ" });
            }

            // Kiểm tra đơn hàng đã có thanh toán chưa
            var thanhToanCu = _context.ThanhToans
                .FirstOrDefault(tt => tt.DonHangId == dto.DonHangId && tt.TrangThai == "Đã thanh toán");

            if (thanhToanCu != null)
            {
                return BadRequest(new { code = 400, message = "Đơn hàng đã được thanh toán" });
            }

            // Tạo thanh toán
            var thanhToan = new ThanhToan
            {
                DonHangId = dto.DonHangId,
                PhuongThuc = dto.PhuongThuc,
                SoTien = donHang.TongTien,
                TrangThai = dto.PhuongThuc == "COD" ? "Chờ thanh toán" : "Chưa thanh toán",
                NgayThanhToan = dto.PhuongThuc == "COD" ? null : DateTime.Now,
                CongThanhToan = dto.CongThanhToan,
                MaGiaoDich = dto.PhuongThuc != "COD" ? $"GD{DateTime.Now:yyyyMMddHHmmss}{dto.DonHangId}" : null
            };

            _context.ThanhToans.Add(thanhToan);
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Tạo thanh toán thành công", thanhToanId = thanhToan.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Theo dõi trạng thái thanh toán
    [HttpGet("don-hang/{donHangId}")]
    public IActionResult GetTrangThaiThanhToan(int donHangId, [FromQuery] int taiKhoanId)
    {
        try
        {
            // Kiểm tra đơn hàng thuộc về tài khoản
            var donHang = _context.DonHangs
                .FirstOrDefault(dh => dh.Id == donHangId && dh.TaiKhoanId == taiKhoanId);

            if (donHang == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy đơn hàng" });
            }

            var thanhToan = _context.ThanhToans
                .Where(tt => tt.DonHangId == donHangId)
                .Select(tt => new ThanhToanResponseDTO
                {
                    Id = tt.Id,
                    DonHangId = tt.DonHangId ?? 0,
                    PhuongThuc = tt.PhuongThuc,
                    SoTien = tt.SoTien,
                    TrangThai = tt.TrangThai,
                    NgayThanhToan = tt.NgayThanhToan,
                    MaGiaoDich = tt.MaGiaoDich,
                    CongThanhToan = tt.CongThanhToan
                })
                .FirstOrDefault();

            if (thanhToan == null)
            {
                return Ok(new { code = 200, message = "Chưa có thông tin thanh toán", data = (ThanhToanResponseDTO?)null });
            }

            return Ok(new { code = 200, message = "Thành công", data = thanhToan });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Xác nhận thanh toán (dành cho admin hoặc hệ thống)
    [HttpPut("{id}/xac-nhan")]
    public IActionResult XacNhanThanhToan(int id, [FromQuery] int taiKhoanId)
    {
        try
        {
            var thanhToan = _context.ThanhToans
                .FirstOrDefault(tt => tt.Id == id);

            if (thanhToan == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy thanh toán" });
            }

            // Kiểm tra đơn hàng thuộc về tài khoản
            var donHang = _context.DonHangs
                .FirstOrDefault(dh => dh.Id == thanhToan.DonHangId && dh.TaiKhoanId == taiKhoanId);

            if (donHang == null)
            {
                return Unauthorized(new { code = 401, message = "Không có quyền xác nhận thanh toán này" });
            }

            if (thanhToan.TrangThai == "Đã thanh toán")
            {
                return BadRequest(new { code = 400, message = "Thanh toán đã được xác nhận" });
            }

            thanhToan.TrangThai = "Đã thanh toán";
            thanhToan.NgayThanhToan = DateTime.Now;

            // Cập nhật trạng thái đơn hàng
            if (donHang.TrangThai == "Chờ xử lý" || donHang.TrangThai == "Đã xác nhận")
            {
                donHang.TrangThai = "Đã thanh toán";
            }

            _context.SaveChanges();

            return Ok(new { code = 200, message = "Xác nhận thanh toán thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Lấy lịch sử thanh toán
    [HttpGet("lich-su")]
    public IActionResult GetLichSuThanhToan([FromQuery] int taiKhoanId)
    {
        try
        {
            var thanhToans = _context.ThanhToans
                .Where(tt => tt.DonHang != null && tt.DonHang.TaiKhoanId == taiKhoanId)
                .OrderByDescending(tt => tt.NgayThanhToan)
                .Select(tt => new ThanhToanResponseDTO
                {
                    Id = tt.Id,
                    DonHangId = tt.DonHangId ?? 0,
                    PhuongThuc = tt.PhuongThuc,
                    SoTien = tt.SoTien,
                    TrangThai = tt.TrangThai,
                    NgayThanhToan = tt.NgayThanhToan,
                    MaGiaoDich = tt.MaGiaoDich,
                    CongThanhToan = tt.CongThanhToan
                })
                .ToList();

            return Ok(new { code = 200, message = "Thành công", data = thanhToans });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }
}

