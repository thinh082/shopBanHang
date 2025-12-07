using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using shopBanHang.Models.DTOs;
using shopBanHang.Models.Entities;
using shopBanHang.Services.VnpayServices.Enums;
using Phuc.Services.VnpayServices;
using VNPAY.NET.Models;
using VNPAY.NET.Utilities;
using System.Text;

namespace shopBanHang.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DonHangController : ControllerBase
{
    private readonly ShopContext _context;
    private readonly IConfiguration _configuration;

    public DonHangController(ShopContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // Tạo đơn hàng từ giỏ hàng
    [HttpPost("tao-don-hang")]
    public IActionResult TaoDonHang([FromBody] DonHangCreateDTO dto, [FromQuery] int taiKhoanId)
    {
        try
        {
            // Kiểm tra tài khoản
            var taiKhoan = _context.TaiKhoans.FirstOrDefault(tk => tk.Id == taiKhoanId);
            if (taiKhoan == null)
            {
                return Unauthorized(new { code = 401, message = "Tài khoản không tồn tại" });
            }

            // Lấy giỏ hàng
            var gioHang = _context.GioHangs
                .Where(gh => gh.TaiKhoanId == taiKhoanId)
                .Select(gh => new
                {
                    gh.Id,
                    ChiTiet = gh.ChiTietGioHangs.Select(ct => new
                    {
                        ct.Id,
                        ct.SanPhamId,
                        ct.SoLuong,
                        SanPham = ct.SanPham
                    }).ToList()
                })
                .FirstOrDefault();

            if (gioHang == null || !gioHang.ChiTiet.Any())
            {
                return BadRequest(new { code = 400, message = "Giỏ hàng trống" });
            }

            // Kiểm tra số lượng sản phẩm
            foreach (var item in gioHang.ChiTiet)
            {
                if (item.SanPham == null)
                {
                    return BadRequest(new { code = 400, message = $"Sản phẩm ID {item.SanPhamId} không tồn tại" });
                }

                if (item.SanPham.SoLuong < item.SoLuong)
                {
                    return BadRequest(new { code = 400, message = $"Sản phẩm {item.SanPham.TenSanPham} không đủ số lượng" });
                }

                if (item.SanPham.TrangThai != true)
                {
                    return BadRequest(new { code = 400, message = $"Sản phẩm {item.SanPham.TenSanPham} đã ngừng bán" });
                }
            }

            // Tính tổng tiền
            decimal tongTien = gioHang.ChiTiet.Sum(item => 
                (item.SanPham?.Gia ?? 0) * item.SoLuong);
            
            if (dto.PhiVanChuyen.HasValue)
            {
                tongTien += dto.PhiVanChuyen.Value;
            }

            // Tạo đơn hàng
            var donHang = new DonHang
            {
                TaiKhoanId = taiKhoanId,
                NgayDat = DateTime.Now,
                TongTien = tongTien,
                TrangThai = "Chờ xử lý",
                TenNguoiNhan = dto.TenNguoiNhan,
                DiaChiGiaoHang = dto.DiaChiGiaoHang,
                SdtnguoiNhan = dto.SdtnguoiNhan
            };

            _context.DonHangs.Add(donHang);
            _context.SaveChanges();

            // Tạo chi tiết đơn hàng và cập nhật số lượng sản phẩm
            foreach (var item in gioHang.ChiTiet)
            {
                var chiTietDonHang = new ChiTietDonHang
                {
                    DonHangId = donHang.Id,
                    SanPhamId = item.SanPhamId,
                    SoLuong = item.SoLuong,
                    DonGia = item.SanPham?.Gia ?? 0
                };

                _context.ChiTietDonHangs.Add(chiTietDonHang);

                // Giảm số lượng sản phẩm
                if (item.SanPham != null)
                {
                    item.SanPham.SoLuong -= item.SoLuong;
                }
            }

            // Xóa giỏ hàng
            var chiTietGioHangs = _context.ChiTietGioHangs
                .Where(ct => ct.GioHangId == gioHang.Id)
                .ToList();
            _context.ChiTietGioHangs.RemoveRange(chiTietGioHangs);

            // Tạo bản ghi thanh toán mặc định theo phương thức người dùng chọn (nếu có)
            ThanhToan? thanhToan = null;
            if (!string.IsNullOrWhiteSpace(dto.PhuongThucThanhToan))
            {
                var phuongThucHopLe = new[] { "COD", "ChuyenKhoan", "ViDienTu", "VnPay" };
                if (!phuongThucHopLe.Contains(dto.PhuongThucThanhToan))
                {
                    return BadRequest(new { code = 400, message = "Phương thức thanh toán không hợp lệ" });
                }

                thanhToan = new ThanhToan
                {
                    DonHangId = donHang.Id,
                    PhuongThuc = dto.PhuongThucThanhToan,
                    SoTien = donHang.TongTien,
                    TrangThai = "Chưa thanh toán",
                    NgayThanhToan = dto.PhuongThucThanhToan == "COD" ? null : (dto.PhuongThucThanhToan == "vnpay" ? null : DateTime.Now),
                    CongThanhToan = dto.CongThanhToan,
                    MaGiaoDich = dto.PhuongThucThanhToan != "COD" && dto.PhuongThucThanhToan != "vnpay"
                        ? $"DH{donHang.Id}-AUTO-{DateTime.Now:yyyyMMddHHmmss}"
                        : null
                };

                _context.ThanhToans.Add(thanhToan);
            }

            _context.SaveChanges();

            // Xử lý thanh toán VNPay nếu phương thức là vnpay
            if (dto.PhuongThucThanhToan == "VnPay" && thanhToan != null)
            {
                var vnpayConfig = _configuration.GetSection("Vnpay");
                var vnpay = new Vnpay();
                vnpay.Initialize(
                    vnpayConfig["TmnCode"] ?? "",
                    vnpayConfig["HashSecret"] ?? "",
                    vnpayConfig["CallbackUrl"] ?? "",
                    vnpayConfig["BaseUrl"] ?? ""
                );

                var ipAddress = NetworkHelper.GetIpAddress(HttpContext); // Lấy địa chỉ IP của thiết bị thực hiện giao dịch
                var request = new PaymentRequest
                {
                    PaymentId = thanhToan.Id,
                    Money = (double)donHang.TongTien,
                    Description = $"Thanh toán đơn hàng #{donHang.Id}",
                    IpAddress = ipAddress,
                    BankCode = BankCode.ANY, // Tùy chọn. Mặc định là tất cả phương thức giao dịch
                    CreatedDate = DateTime.Now, // Tùy chọn. Mặc định là thời điểm hiện tại
                    Currency = Currency.VND, // Tùy chọn. Mặc định là VND (Việt Nam đồng)
                    Language = DisplayLanguage.Vietnamese // Tùy chọn. Mặc định là tiếng Việt
                };
                var paymentUrl = vnpay.GetPaymentUrl(request);
                
                return Ok(new
                {
                    code = 200,
                    message = "Tạo đơn hàng thành công! Vui lòng thanh toán qua VNPay.",
                    donHangId = donHang.Id,
                    url = paymentUrl
                });
            }
            
            return Ok(new { code = 200, message = "Tạo đơn hàng thành công", donHangId = donHang.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }
    [HttpGet("Callback")]
    public async Task<IActionResult> Callback()
    {
        if (!Request.QueryString.HasValue)
        {
            return NotFound("Không tìm thấy thông tin thanh toán!");
        }

        // Lưu ý: Biến 'transaction' cần được khai báo trước try-catch nếu bạn muốn rollback
        // IDbContextTransaction transaction = null; 
        var vnpayConfig = _configuration.GetSection("Vnpay");
        var vnpay = new Vnpay();
        vnpay.Initialize(
            vnpayConfig["TmnCode"] ?? "",
            vnpayConfig["HashSecret"] ?? "",
            vnpayConfig["CallbackUrl"] ?? "",
            vnpayConfig["BaseUrl"] ?? ""
        );
        try
        {
            var paymentResult = vnpay.GetPaymentResult(Request.Query);
            var thanhToanCho = await _context.ThanhToans.FindAsync((int)paymentResult.PaymentId);

            if (thanhToanCho == null)
            {
                return BadRequest(new { statusCode = 404, message = "Không tìm thấy giao dịch thanh toán!" });
            }

            // Cập nhật trạng thái
            thanhToanCho.TrangThai = "Đã thanh toán";
            _context.ThanhToans.Update(thanhToanCho);
            await _context.SaveChangesAsync(); // Dùng Async cho tối ưu

            var htmlContent = $@"
  <html>
      <head>
          <title>Thanh toán VNPAY - Xác nhận đặt bàn</title>
          <meta name='viewport' content='width=device-width, initial-scale=1.0'>
          <style>
              body {{
                  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                  background-color: #f0f2f5;
                  margin: 0;
                  display: flex;
                  justify-content: center;
                  align-items: center;
                  height: 100vh;
                  text-align: center;
              }}
              .card {{
                  background-color: #ffffff;
                  border-radius: 16px;
                  box-shadow: 0 10px 25px rgba(0,0,0,0.1);
                  padding: 2.5rem;
                  max-width: 420px;
                  width: 90%;
                  border-top: 6px solid #28a745;
              }}
              h1 {{
                  color: #28a745;
                  font-size: 1.8rem;
                  margin-bottom: 0.5rem;
                  margin-top: 0;
              }}
              .icon-success {{
                  font-size: 4rem;
                  margin-bottom: 1rem;
                  display: block;
              }}
              p {{
                  color: #6c757d;
                  font-size: 1rem;
                  margin: 0.5rem 0;
                  line-height: 1.5;
              }}
              .info {{
                  background-color: #f8f9fa;
                  padding: 1.2rem;
                  border-radius: 10px;
                  margin: 1.5rem 0;
                  text-align: left;
                  border: 1px solid #e9ecef;
              }}
              .info p {{
                  margin: 0.3rem 0;
                  display: flex;
                  justify-content: space-between;
                  font-size: 0.95rem;
              }}
              .info strong {{
                  color: #333;
              }}
              .total-amount {{
                  color: #d63384;
                  font-weight: bold;
                  font-size: 1.1rem !important;
              }}
              @media (max-width: 480px) {{
                  h1 {{ font-size: 1.5rem; }}
                  .card {{ padding: 1.5rem; }}
              }}
          </style>
      </head>
      <body>
          <div class='card'>
              <h1>Thanh toán thành công!</h1>
              <p>Cảm ơn bạn đã sử dụng dịch vụ. Đơn hàng của bạn đã được xử lý.</p>
              
              <div class='info'>
                  <p><strong>Mã giao dịch:</strong> <span>{thanhToanCho.Id}</span></p>
                  <p><strong>Trạng thái:</strong> <span style='color:#28a745; font-weight:bold;'>Đã thanh toán</span></p>
                  <p><strong>Tổng tiền:</strong> <span class='total-amount'>{thanhToanCho.SoTien:N0} VNĐ</span></p>
              </div>

              <a href=""http://127.0.0.1:5500/shopbanhang_fe/index.html"">
                  <p style='font-size: 0.9rem;'>Bạn có thể quay lại ứng dụng để xem chi tiết.</p>
              </a>

              <p style='font-size: 0.8rem; color: #adb5bd;'>Cửa sổ này sẽ tự đóng sau 5 giây...</p>
          </div>
          <script>
              setTimeout(() => {{
                  window.close();
              }}, 5000);
          </script>
      </body>
  </html>";


            return Content(htmlContent, "text/html;charset=utf-8");
        }
        catch (Exception ex)
        {
            // Lưu ý: Biến 'transaction' chưa được định nghĩa trong scope này ở code cũ. 
            // Nếu bạn có transaction bên ngoài, hãy uncomment dòng dưới:
            // await transaction.RollbackAsync();

            var detailedError = new StringBuilder();
            detailedError.AppendLine($"[Error Message] {ex.Message}");
            detailedError.AppendLine($"[Stack Trace] {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                detailedError.AppendLine($"[Inner Exception] {ex.InnerException.Message}");
            }

            Console.WriteLine("=== ERROR LOG (Đặt bàn) ===");
            Console.WriteLine(detailedError.ToString());

            return Ok(new
            {
                statusCode = 500,
                message = "Đặt bàn không thành công! Vui lòng thử lại.",
                // Chỉ hiện lỗi chi tiết khi Debug
#if DEBUG
                detailedError = detailedError.ToString()
#endif
            });
        }
    }

    // Xem lịch sử đơn hàng (có phân trang)
    [HttpGet("lich-su")]
    public IActionResult GetLichSuDonHang([FromQuery] int taiKhoanId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            // Validate page và pageSize
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Giới hạn tối đa 100 items/trang

            var query = _context.DonHangs
                .Where(dh => dh.TaiKhoanId == taiKhoanId)
                .OrderByDescending(dh => dh.NgayDat);

            // Đếm tổng số đơn hàng
            var totalCount = query.Count();

            // Lấy dữ liệu phân trang
            var donHangs = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

            // Tính tổng số trang
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return Ok(new 
            { 
                code = 200, 
                message = "Thành công", 
                data = donHangs,
                pagination = new
                {
                    page = page,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    totalPages = totalPages
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Xem chi tiết đơn hàng
    [HttpGet("{id}")]
    public IActionResult GetChiTietDonHang(int id, [FromQuery] int taiKhoanId)
    {
        try
        {
            var donHang = _context.DonHangs
                .Where(dh => dh.Id == id && dh.TaiKhoanId == taiKhoanId)
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

    // Xác nhận đơn hàng (chuyển trạng thái)
    [HttpPut("{id}/xac-nhan")]
    public IActionResult XacNhanDonHang(int id, [FromQuery] int taiKhoanId)
    {
        try
        {
            var donHang = _context.DonHangs
                .FirstOrDefault(dh => dh.Id == id && dh.TaiKhoanId == taiKhoanId);

            if (donHang == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy đơn hàng" });
            }

            if (donHang.TrangThai != "Chờ xử lý")
            {
                return BadRequest(new { code = 400, message = "Đơn hàng không thể xác nhận" });
            }

            donHang.TrangThai = "Đã xác nhận";
            _context.SaveChanges();

            return Ok(new { code = 200, message = "Xác nhận đơn hàng thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }
}

