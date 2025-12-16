using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopBanHang.Models.DTOs;
using shopBanHang.Models.Entities;

namespace shopBanHang.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TinNhanController : ControllerBase
{
    private readonly ShopContext _context;

    public TinNhanController(ShopContext context)
    {
        _context = context;
    }

    // Nhắn tin cho Admin/Hỗ trợ
    [HttpPost]
    public IActionResult GuiTinNhan([FromBody] TinNhanCreateDTO dto, [FromQuery] int taiKhoanId)
    {
        try
        {
            // Kiểm tra tài khoản
            var taiKhoan = _context.TaiKhoans.FirstOrDefault(tk => tk.Id == taiKhoanId);
            if (taiKhoan == null)
            {
                return Unauthorized(new { code = 401, message = "Tài khoản không tồn tại" });
            }

            // Kiểm tra người nhận
            var nguoiNhan = _context.TaiKhoans.FirstOrDefault(tk => tk.Id == dto.NguoiNhanId);
            if (nguoiNhan == null)
            {
                return BadRequest(new { code = 404, message = "Người nhận không tồn tại" });
            }

            if (string.IsNullOrWhiteSpace(dto.NoiDung))
            {
                return BadRequest(new { code = 400, message = "Nội dung tin nhắn không được để trống" });
            }

            // Tạo tin nhắn
            var tinNhan = new TinNhan
            {
                NguoiGuiId = taiKhoanId,
                NguoiNhanId = dto.NguoiNhanId,
                NoiDung = dto.NoiDung,
                ThoiGian = DateTime.Now
            };

            _context.TinNhans.Add(tinNhan);
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Gửi tin nhắn thành công", tinNhanId = tinNhan.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Xem lịch sử tin nhắn (tin nhắn đã gửi và nhận)
    [HttpGet("lich-su")]
    public IActionResult GetLichSuTinNhan([FromQuery] int taiKhoanId, [FromQuery] int nguoiNhanId = 5)
    {
        try
        {
            var query = _context.TinNhans
                .Where(tn => tn.NguoiGuiId == taiKhoanId || tn.NguoiNhanId == taiKhoanId);

            // Nếu có nguoiNhanId, chỉ lấy tin nhắn giữa 2 người
            if (nguoiNhanId != 0)
            {
                query = query.Where(tn => tn.NguoiGuiId == taiKhoanId && tn.NguoiNhanId == nguoiNhanId );       
            }

            var tinNhans = query
                .OrderBy(tn => tn.ThoiGian)
                .Select(tn => new TinNhanResponseDTO
                {
                    Id = tn.Id,
                    NguoiGuiId = tn.NguoiGuiId ?? 0,
                    HoTenNguoiGui = tn.NguoiGui != null ? tn.NguoiGui.HoTen : null,
                    NguoiNhanId = tn.NguoiNhanId ?? 0,
                    HoTenNguoiNhan = tn.NguoiNhan != null ? tn.NguoiNhan.HoTen : null,
                    NoiDung = tn.NoiDung,
                    ThoiGian = tn.ThoiGian
                })
                .ToList();

            return Ok(new { code = 200, message = "Thành công", data = tinNhans });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Xem danh sách cuộc trò chuyện (người đã nhắn tin với)
    [HttpGet("cuoc-tro-chuyen")]
    public IActionResult GetCuocTroChuyen([FromQuery] int taiKhoanId)
    {
        try
        {
            // Lấy danh sách người đã nhắn tin với (cả người gửi và người nhận)
            var nguoiNhanTin = _context.TinNhans
                .Where(tn => tn.NguoiGuiId == taiKhoanId || tn.NguoiNhanId == taiKhoanId)
                .Select(tn => new
                {
                    NguoiId = tn.NguoiGuiId == taiKhoanId ? tn.NguoiNhanId : tn.NguoiGuiId,
                    HoTen = tn.NguoiGuiId == taiKhoanId 
                        ? (tn.NguoiNhan != null ? tn.NguoiNhan.HoTen : null)
                        : (tn.NguoiGui != null ? tn.NguoiGui.HoTen : null),
                    TinNhanCuoi = tn.NoiDung,
                    ThoiGian = tn.ThoiGian
                })
                .GroupBy(x => x.NguoiId)
                .Select(g => new
                {
                    NguoiId = g.Key,
                    HoTen = g.First().HoTen,
                    TinNhanCuoi = g.OrderByDescending(x => x.ThoiGian).First().TinNhanCuoi,
                    ThoiGian = g.Max(x => x.ThoiGian)
                })
                .OrderByDescending(x => x.ThoiGian)
                .ToList();

            return Ok(new { code = 200, message = "Thành công", data = nguoiNhanTin });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Nhận phản hồi từ admin (xem tin nhắn mới)
    [HttpGet("tin-nhan-moi")]
    public IActionResult GetTinNhanMoi([FromQuery] int taiKhoanId, [FromQuery] DateTime? thoiGianCuoi = null)
    {
        try
        {
            var query = _context.TinNhans
                .Where(tn => tn.NguoiNhanId == taiKhoanId);

            // Nếu có thời gian cuối, chỉ lấy tin nhắn sau thời gian đó
            if (thoiGianCuoi.HasValue)
            {
                query = query.Where(tn => tn.ThoiGian > thoiGianCuoi.Value);
            }

            var tinNhans = query
                .OrderBy(tn => tn.ThoiGian)
                .Select(tn => new TinNhanResponseDTO
                {
                    Id = tn.Id,
                    NguoiGuiId = tn.NguoiGuiId ?? 0,
                    HoTenNguoiGui = tn.NguoiGui != null ? tn.NguoiGui.HoTen : null,
                    NguoiNhanId = tn.NguoiNhanId ?? 0,
                    HoTenNguoiNhan = tn.NguoiNhan != null ? tn.NguoiNhan.HoTen : null,
                    NoiDung = tn.NoiDung,
                    ThoiGian = tn.ThoiGian
                })
                .ToList();

            return Ok(new { code = 200, message = "Thành công", data = tinNhans });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }
}

