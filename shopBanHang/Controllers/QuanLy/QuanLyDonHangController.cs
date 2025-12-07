using Microsoft.AspNetCore.Mvc;
using shopBanHang.Models.Entities;

namespace shopBanHang.Controllers.QuanLy;

[ApiController]
[Route("api/QuanLy/[controller]")]
public class QuanLyDonHangController : ControllerBase
{
    private readonly ShopContext _context;

    public QuanLyDonHangController(ShopContext context)
    {
        _context = context;
    }

    // GET: api/QuanLy/QuanLyDonHang - Lấy danh sách đơn hàng
    [HttpGet]
    public IActionResult GetDonHangs([FromQuery] int page = 1, [FromQuery] int pageSize = 10, 
        [FromQuery] string? trangThai = null, [FromQuery] int? taiKhoanId = null)
    {
        try
        {
            var query = _context.DonHangs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(dh => dh.TrangThai == trangThai);
            }

            if (taiKhoanId.HasValue)
            {
                query = query.Where(dh => dh.TaiKhoanId == taiKhoanId.Value);
            }

            var total = query.Count();

            var donHangs = query
                .OrderByDescending(dh => dh.NgayDat)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(dh => new
                {
                    dh.Id,
                    dh.TaiKhoanId,
                    TenTaiKhoan = dh.TaiKhoan != null ? dh.TaiKhoan.HoTen : null,
                    EmailTaiKhoan = dh.TaiKhoan != null ? dh.TaiKhoan.Email : null,
                    dh.NgayDat,
                    dh.TongTien,
                    dh.TrangThai,
                    dh.TenNguoiNhan,
                    dh.DiaChiGiaoHang,
                    dh.SdtnguoiNhan,
                    SoLuongSanPham = dh.ChiTietDonHangs.Count,
                    ChiTiet = dh.ChiTietDonHangs.Select(ct => new
                    {
                        ct.Id,
                        ct.SanPhamId,
                        TenSanPham = ct.SanPham != null ? ct.SanPham.TenSanPham : null,
                        ct.SoLuong,
                        ct.DonGia
                    }).ToList()
                })
                .ToList();

            return Ok(new
            {
                code = 200,
                message = "Thành công",
                data = donHangs,
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

    // GET: api/QuanLy/QuanLyDonHang/{id} - Lấy chi tiết đơn hàng
    [HttpGet("{id}")]
    public IActionResult GetDonHang(int id)
    {
        try
        {
            var donHang = _context.DonHangs
                .Where(dh => dh.Id == id)
                .Select(dh => new
                {
                    dh.Id,
                    dh.TaiKhoanId,
                    TenTaiKhoan = dh.TaiKhoan != null ? dh.TaiKhoan.HoTen : null,
                    EmailTaiKhoan = dh.TaiKhoan != null ? dh.TaiKhoan.Email : null,
                    dh.NgayDat,
                    dh.TongTien,
                    dh.TrangThai,
                    dh.TenNguoiNhan,
                    dh.DiaChiGiaoHang,
                    dh.SdtnguoiNhan,
                    ChiTiet = dh.ChiTietDonHangs.Select(ct => new
                    {
                        ct.Id,
                        ct.SanPhamId,
                        TenSanPham = ct.SanPham != null ? ct.SanPham.TenSanPham : null,
                        ct.SoLuong,
                        ct.DonGia
                    }).ToList(),
                    ThanhToan = dh.ThanhToans.Select(tt => new
                    {
                        tt.Id,
                        tt.PhuongThuc,
                        tt.SoTien,
                        tt.TrangThai,
                        tt.NgayThanhToan,
                        tt.MaGiaoDich,
                        tt.CongThanhToan
                    }).ToList()
                })
                .FirstOrDefault();

            if (donHang == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy đơn hàng" });
            }

            return Ok(new { code = 200, message = "Thành công", data = donHang });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // POST: api/QuanLy/QuanLyDonHang - Tạo đơn hàng mới
    [HttpPost]
    public IActionResult CreateDonHang([FromBody] CreateDonHangDTO dto)
    {
        try
        {
            if (dto.TaiKhoanId <= 0)
            {
                return BadRequest(new { code = 400, message = "TaiKhoanId không hợp lệ" });
            }

            // Kiểm tra tài khoản
            var taiKhoan = _context.TaiKhoans.FirstOrDefault(tk => tk.Id == dto.TaiKhoanId);
            if (taiKhoan == null)
            {
                return BadRequest(new { code = 404, message = "Tài khoản không tồn tại" });
            }

            if (dto.ChiTiet == null || !dto.ChiTiet.Any())
            {
                return BadRequest(new { code = 400, message = "Chi tiết đơn hàng không được để trống" });
            }

            // Kiểm tra sản phẩm và số lượng
            decimal tongTien = 0;
            foreach (var item in dto.ChiTiet)
            {
                var sanPham = _context.SanPhams.FirstOrDefault(sp => sp.Id == item.SanPhamId);
                if (sanPham == null)
                {
                    return BadRequest(new { code = 404, message = $"Sản phẩm ID {item.SanPhamId} không tồn tại" });
                }

                if (sanPham.TrangThai != true)
                {
                    return BadRequest(new { code = 400, message = $"Sản phẩm {sanPham.TenSanPham} đã ngừng bán" });
                }

                if (sanPham.SoLuong < item.SoLuong)
                {
                    return BadRequest(new { code = 400, message = $"Sản phẩm {sanPham.TenSanPham} không đủ số lượng" });
                }

                tongTien += sanPham.Gia * item.SoLuong;
            }

            if (dto.PhiVanChuyen.HasValue)
            {
                tongTien += dto.PhiVanChuyen.Value;
            }

            // Tạo đơn hàng
            var donHang = new DonHang
            {
                TaiKhoanId = dto.TaiKhoanId,
                NgayDat = DateTime.Now,
                TongTien = tongTien,
                TrangThai = dto.TrangThai ?? "Chờ xử lý",
                TenNguoiNhan = dto.TenNguoiNhan,
                DiaChiGiaoHang = dto.DiaChiGiaoHang,
                SdtnguoiNhan = dto.SdtnguoiNhan
            };

            _context.DonHangs.Add(donHang);
            _context.SaveChanges();

            // Tạo chi tiết đơn hàng và cập nhật số lượng
            foreach (var item in dto.ChiTiet)
            {
                var sanPham = _context.SanPhams.FirstOrDefault(sp => sp.Id == item.SanPhamId);
                if (sanPham != null)
                {
                    var chiTietDonHang = new ChiTietDonHang
                    {
                        DonHangId = donHang.Id,
                        SanPhamId = item.SanPhamId,
                        SoLuong = item.SoLuong,
                        DonGia = sanPham.Gia
                    };

                    _context.ChiTietDonHangs.Add(chiTietDonHang);

                    // Giảm số lượng sản phẩm
                    sanPham.SoLuong -= item.SoLuong;
                }
            }

            _context.SaveChanges();

            return Ok(new { code = 200, message = "Tạo đơn hàng thành công", data = new { id = donHang.Id } });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // PUT: api/QuanLy/QuanLyDonHang/{id} - Cập nhật đơn hàng
    [HttpPut("{id}")]
    public IActionResult UpdateDonHang(int id, [FromBody] UpdateDonHangDTO dto)
    {
        try
        {
            var donHang = _context.DonHangs.FirstOrDefault(dh => dh.Id == id);
            if (donHang == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy đơn hàng" });
            }

            if (!string.IsNullOrWhiteSpace(dto.TrangThai))
            {
                donHang.TrangThai = dto.TrangThai;
            }

            if (!string.IsNullOrWhiteSpace(dto.TenNguoiNhan))
            {
                donHang.TenNguoiNhan = dto.TenNguoiNhan;
            }

            if (!string.IsNullOrWhiteSpace(dto.DiaChiGiaoHang))
            {
                donHang.DiaChiGiaoHang = dto.DiaChiGiaoHang;
            }

            if (!string.IsNullOrWhiteSpace(dto.SdtnguoiNhan))
            {
                donHang.SdtnguoiNhan = dto.SdtnguoiNhan;
            }

            if (dto.TongTien.HasValue)
            {
                donHang.TongTien = dto.TongTien.Value;
            }

            _context.SaveChanges();

            return Ok(new { code = 200, message = "Cập nhật đơn hàng thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // DELETE: api/QuanLy/QuanLyDonHang/{id} - Xóa đơn hàng
    [HttpDelete("{id}")]
    public IActionResult DeleteDonHang(int id)
    {
        try
        {
            var donHang = _context.DonHangs.FirstOrDefault(dh => dh.Id == id);
            if (donHang == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy đơn hàng" });
            }

            // Kiểm tra trạng thái đơn hàng
            if (donHang.TrangThai == "Đã giao" || donHang.TrangThai == "Đã hoàn thành")
            {
                return BadRequest(new { code = 400, message = "Không thể xóa đơn hàng đã hoàn thành" });
            }

            // Khôi phục số lượng sản phẩm
            var chiTietDonHangs = _context.ChiTietDonHangs.Where(ct => ct.DonHangId == id).ToList();
            foreach (var chiTiet in chiTietDonHangs)
            {
                var sanPham = _context.SanPhams.FirstOrDefault(sp => sp.Id == chiTiet.SanPhamId);
                if (sanPham != null)
                {
                    sanPham.SoLuong += chiTiet.SoLuong;
                }
            }

            // Xóa chi tiết đơn hàng
            _context.ChiTietDonHangs.RemoveRange(chiTietDonHangs);

            // Xóa thanh toán liên quan
            var thanhToans = _context.ThanhToans.Where(tt => tt.DonHangId == id).ToList();
            _context.ThanhToans.RemoveRange(thanhToans);

            // Xóa đơn hàng
            _context.DonHangs.Remove(donHang);
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Xóa đơn hàng thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }
}

// DTOs cho QuanLyDonHang
public class CreateDonHangDTO
{
    public int TaiKhoanId { get; set; }
    public string TenNguoiNhan { get; set; } = null!;
    public string DiaChiGiaoHang { get; set; } = null!;
    public string SdtnguoiNhan { get; set; } = null!;
    public decimal? PhiVanChuyen { get; set; }
    public string? TrangThai { get; set; }
    public List<ChiTietDonHangCreateDTO> ChiTiet { get; set; } = new List<ChiTietDonHangCreateDTO>();
}

public class ChiTietDonHangCreateDTO
{
    public int SanPhamId { get; set; }
    public int SoLuong { get; set; }
}

public class UpdateDonHangDTO
{
    public string? TrangThai { get; set; }
    public string? TenNguoiNhan { get; set; }
    public string? DiaChiGiaoHang { get; set; }
    public string? SdtnguoiNhan { get; set; }
    public decimal? TongTien { get; set; }
}

