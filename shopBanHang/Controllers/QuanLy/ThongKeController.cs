using Microsoft.AspNetCore.Mvc;
using shopBanHang.Models.Entities;

namespace shopBanHang.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ThongKeController : ControllerBase
{
    private readonly ShopContext _context;

    public ThongKeController(ShopContext context)
    {
        _context = context;
    }

    // GET: api/ThongKe/san-pham-dat-nhieu - Biểu đồ sản phẩm được đặt nhiều nhất
    [HttpGet("san-pham-dat-nhieu")]
    public IActionResult GetSanPhamDatNhieu([FromQuery] string period = "thang", [FromQuery] int limit = 10)
    {
        try
        {
            var query = _context.ChiTietDonHangs.AsQueryable();

            // Lọc theo thời gian
            DateTime startDate;
            DateTime endDate = DateTime.Now;

            switch (period.ToLower())
            {
                case "ngay":
                    startDate = DateTime.Today;
                    break;
                case "thang":
                    startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    break;
                case "nam":
                    startDate = new DateTime(DateTime.Now.Year, 1, 1);
                    break;
                default:
                    startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    break;
            }

            var donHangIds = _context.DonHangs
                .Where(dh => dh.NgayDat >= startDate && dh.NgayDat <= endDate)
                .Select(dh => dh.Id)
                .ToList();

            var chiTietData = _context.ChiTietDonHangs
                .Where(ct => donHangIds.Contains(ct.DonHangId ?? 0) && ct.SanPhamId.HasValue)
                .Select(ct => new
                {
                    ct.SanPhamId,
                    ct.SoLuong,
                    ct.DonHangId
                })
                .ToList();

            var sanPhamIds = chiTietData.Select(ct => ct.SanPhamId ?? 0).Distinct().ToList();
            var sanPhamNames = _context.SanPhams
                .Where(sp => sanPhamIds.Contains(sp.Id))
                .Select(sp => new { sp.Id, sp.TenSanPham })
                .ToDictionary(sp => sp.Id, sp => sp.TenSanPham);

            var sanPhamStats = chiTietData
                .GroupBy(ct => ct.SanPhamId ?? 0)
                .Select(g => new
                {
                    SanPhamId = g.Key,
                    TenSanPham = sanPhamNames.ContainsKey(g.Key) ? sanPhamNames[g.Key] : "Không xác định",
                    SoLuongDat = g.Sum(ct => ct.SoLuong),
                    SoDonHang = g.Select(ct => ct.DonHangId).Distinct().Count()
                })
                .OrderByDescending(x => x.SoLuongDat)
                .Take(limit)
                .ToList();

            // Format cho biểu đồ
            var labels = sanPhamStats.Select(x => x.TenSanPham).ToList();
            var data = sanPhamStats.Select(x => x.SoLuongDat).ToList();
            var donHangCounts = sanPhamStats.Select(x => x.SoDonHang).ToList();

            return Ok(new
            {
                code = 200,
                message = "Thành công",
                period = period,
                chart = new
                {
                    type = "bar",
                    labels = labels,
                    datasets = new[]
                    {
                        new
                        {
                            label = "Số lượng đặt",
                            data = data,
                            backgroundColor = "rgba(54, 162, 235, 0.6)",
                            borderColor = "rgba(54, 162, 235, 1)",
                            borderWidth = 1
                        },
                        new
                        {
                            label = "Số đơn hàng",
                            data = donHangCounts,
                            backgroundColor = "rgba(255, 99, 132, 0.6)",
                            borderColor = "rgba(255, 99, 132, 1)",
                            borderWidth = 1
                        }
                    }
                },
                data = sanPhamStats.Select(x => new
                {
                    x.SanPhamId,
                    x.TenSanPham,
                    x.SoLuongDat,
                    x.SoDonHang
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // GET: api/ThongKe/tai-khoan-mua-nhieu - Biểu đồ tài khoản mua nhiều sản phẩm nhất
    [HttpGet("tai-khoan-mua-nhieu")]
    public IActionResult GetTaiKhoanMuaNhieu([FromQuery] int limit = 10)
    {
        try
        {
            var taiKhoanStats = _context.DonHangs
                .Where(dh => dh.TaiKhoanId.HasValue)
                .GroupBy(dh => new
                {
                    dh.TaiKhoanId,
                    HoTen = dh.TaiKhoan != null ? dh.TaiKhoan.HoTen : "Không xác định",
                    Email = dh.TaiKhoan != null ? dh.TaiKhoan.Email : "Không xác định"
                })
                .Select(g => new
                {
                    TaiKhoanId = g.Key.TaiKhoanId ?? 0,
                    HoTen = g.Key.HoTen,
                    Email = g.Key.Email,
                    SoDonHang = g.Count(),
                    TongSoLuong = g.SelectMany(dh => dh.ChiTietDonHangs).Sum(ct => ct.SoLuong),
                    TongTien = g.Sum(dh => dh.TongTien ?? 0)
                })
                .OrderByDescending(x => x.TongSoLuong)
                .Take(limit)
                .ToList();

            // Format cho biểu đồ
            var labels = taiKhoanStats.Select(x => string.IsNullOrEmpty(x.HoTen) ? x.Email : x.HoTen).ToList();
            var soLuongData = taiKhoanStats.Select(x => x.TongSoLuong).ToList();
            var donHangData = taiKhoanStats.Select(x => x.SoDonHang).ToList();
            var tienData = taiKhoanStats.Select(x => (double)x.TongTien).ToList();

            return Ok(new
            {
                code = 200,
                message = "Thành công",
                chart = new
                {
                    type = "bar",
                    labels = labels,
                    datasets = new[]
                    {
                        new
                        {
                            label = "Số lượng sản phẩm đã mua",
                            data = soLuongData,
                            backgroundColor = "rgba(75, 192, 192, 0.6)",
                            borderColor = "rgba(75, 192, 192, 1)",
                            borderWidth = 1
                        },
                        new
                        {
                            label = "Số đơn hàng",
                            data = donHangData,
                            backgroundColor = "rgba(153, 102, 255, 0.6)",
                            borderColor = "rgba(153, 102, 255, 1)",
                            borderWidth = 1
                        }
                    }
                },
                chartRevenue = new
                {
                    type = "line",
                    labels = labels,
                    datasets = new[]
                    {
                        new
                        {
                            label = "Tổng tiền (VNĐ)",
                            data = tienData,
                            backgroundColor = "rgba(255, 206, 86, 0.2)",
                            borderColor = "rgba(255, 206, 86, 1)",
                            borderWidth = 2,
                            fill = true
                        }
                    }
                },
                data = taiKhoanStats.Select(x => new
                {
                    x.TaiKhoanId,
                    x.HoTen,
                    x.Email,
                    x.SoDonHang,
                    x.TongSoLuong,
                    TongTien = x.TongTien
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // GET: api/ThongKe/doanh-thu - Biểu đồ doanh thu
    [HttpGet("doanh-thu")]
    public IActionResult GetDoanhThu([FromQuery] string period = "thang", [FromQuery] int? year = null, [FromQuery] int? month = null)
    {
        try
            {
            var query = _context.DonHangs
                .Where(dh => dh.TongTien.HasValue && dh.TongTien > 0);

            DateTime startDate;
            DateTime endDate = DateTime.Now;
            string groupBy = "day";

            switch (period.ToLower())
            {
                case "ngay":
                    startDate = DateTime.Today;
                    groupBy = "hour";
                    break;
                case "thang":
                    if (year.HasValue && month.HasValue)
                    {
                        startDate = new DateTime(year.Value, month.Value, 1);
                        endDate = startDate.AddMonths(1).AddDays(-1);
                    }
                    else
                    {
                        startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    }
                    groupBy = "day";
                    break;
                case "nam":
                    if (year.HasValue)
                    {
                        startDate = new DateTime(year.Value, 1, 1);
                        endDate = new DateTime(year.Value, 12, 31);
                    }
                    else
                    {
                        startDate = new DateTime(DateTime.Now.Year, 1, 1);
                    }
                    groupBy = "month";
                    break;
                default:
                    startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    groupBy = "day";
                    break;
            }

            query = query.Where(dh => dh.NgayDat >= startDate && dh.NgayDat <= endDate);

            List<object> chartData;

            if (groupBy == "hour")
            {
                chartData = query
                    .GroupBy(dh => dh.NgayDat.HasValue ? dh.NgayDat.Value.Hour : 0)
                    .Select(g => new
                    {
                        Label = $"{g.Key}:00",
                        Value = g.Sum(dh => dh.TongTien ?? 0),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Label)
                    .ToList<object>();
            }
            else if (groupBy == "day")
            {
                // Lấy dữ liệu từ database trước, sau đó format date ở client-side
                var rawData = query
                    .GroupBy(dh => dh.NgayDat.HasValue ? dh.NgayDat.Value.Date : DateTime.MinValue)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Value = g.Sum(dh => dh.TongTien ?? 0),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                // Format date ở client-side
                chartData = rawData
                    .Select(g => new
                    {
                        Label = g.Date.ToString("dd/MM/yyyy"),
                        Value = g.Value,
                        Count = g.Count
                    })
                    .ToList<object>();
            }
            else // month
            {
                chartData = query
                    .GroupBy(dh => dh.NgayDat.HasValue ? new { dh.NgayDat.Value.Year, dh.NgayDat.Value.Month } : new { Year = 0, Month = 0 })
                    .Select(g => new
                    {
                        Label = $"{g.Key.Month}/{g.Key.Year}",
                        Value = g.Sum(dh => dh.TongTien ?? 0),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Label)
                    .ToList<object>();
            }

            var labels = chartData.Select(x => ((dynamic)x).Label.ToString()).ToList();
            var values = chartData.Select(x => (double)((dynamic)x).Value).ToList();
            var counts = chartData.Select(x => (int)((dynamic)x).Count).ToList();

            var tongDoanhThu = values.Sum();
            var tongDonHang = counts.Sum();
            var trungBinhDonHang = tongDonHang > 0 ? tongDoanhThu / tongDonHang : 0;

            return Ok(new
            {
                code = 200,
                message = "Thành công",
                period = period,
                summary = new
                {
                    TongDoanhThu = tongDoanhThu,
                    TongDonHang = tongDonHang,
                    TrungBinhDonHang = trungBinhDonHang
                },
                chart = new
                {
                    type = "line",
                    labels = labels,
                    datasets = new[]
                    {
                        new
                        {
                            label = "Doanh thu (VNĐ)",
                            data = values,
                            backgroundColor = "rgba(54, 162, 235, 0.2)",
                            borderColor = "rgba(54, 162, 235, 1)",
                            borderWidth = 2,
                            fill = true,
                            tension = 0.4
                        }
                    }
                },
                chartBar = new
                {
                    type = "bar",
                    labels = labels,
                    datasets = new[]
                    {
                        new
                        {
                            label = "Số đơn hàng",
                            data = counts,
                            backgroundColor = "rgba(255, 99, 132, 0.6)",
                            borderColor = "rgba(255, 99, 132, 1)",
                            borderWidth = 1
                        }
                    }
                },
                data = chartData
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // GET: api/ThongKe/san-pham-danh-gia-cao - Biểu đồ sản phẩm có nhiều đánh giá cao nhất
    [HttpGet("san-pham-danh-gia-cao")]
    public IActionResult GetSanPhamDanhGiaCao([FromQuery] int limit = 10, [FromQuery] int? minDiem = null)
    {
        try
        {
            var query = _context.DanhGia
                .Where(dg => dg.SanPhamId.HasValue && dg.Diem.HasValue);

            if (minDiem.HasValue)
            {
                query = query.Where(dg => dg.Diem >= minDiem.Value);
            }

            var sanPhamStats = query
                .GroupBy(dg => new
                {
                    dg.SanPhamId,
                    TenSanPham = dg.SanPham != null ? dg.SanPham.TenSanPham : "Không xác định",
                    Gia = dg.SanPham != null ? dg.SanPham.Gia : 0
                })
                .Select(g => new
                {
                    SanPhamId = g.Key.SanPhamId ?? 0,
                    TenSanPham = g.Key.TenSanPham,
                    Gia = g.Key.Gia,
                    SoLuongDanhGia = g.Count(),
                    DiemTrungBinh = g.Average(dg => (double?)dg.Diem) ?? 0,
                    DiemCaoNhat = g.Max(dg => dg.Diem) ?? 0,
                    DiemThapNhat = g.Min(dg => dg.Diem) ?? 0
                })
                .Where(x => x.SoLuongDanhGia > 0)
                .OrderByDescending(x => x.DiemTrungBinh)
                .ThenByDescending(x => x.SoLuongDanhGia)
                .Take(limit)
                .ToList();

            // Format cho biểu đồ
            var labels = sanPhamStats.Select(x => x.TenSanPham).ToList();
            var diemTrungBinhData = sanPhamStats.Select(x => Math.Round(x.DiemTrungBinh, 2)).ToList();
            var soLuongDanhGiaData = sanPhamStats.Select(x => x.SoLuongDanhGia).ToList();
            var diemCaoNhatData = sanPhamStats.Select(x => (double)x.DiemCaoNhat).ToList();
            var diemThapNhatData = sanPhamStats.Select(x => (double)x.DiemThapNhat).ToList();

            return Ok(new
            {
                code = 200,
                message = "Thành công",
                chart = new
                {
                    type = "bar",
                    labels = labels,
                    datasets = new[]
                    {
                        new
                        {
                            label = "Điểm trung bình",
                            data = diemTrungBinhData,
                            backgroundColor = "rgba(75, 192, 192, 0.6)",
                            borderColor = "rgba(75, 192, 192, 1)",
                            borderWidth = 1
                        },
                        new
                        {
                            label = "Điểm cao nhất",
                            data = diemCaoNhatData,
                            backgroundColor = "rgba(255, 206, 86, 0.6)",
                            borderColor = "rgba(255, 206, 86, 1)",
                            borderWidth = 1
                        },
                        new
                        {
                            label = "Điểm thấp nhất",
                            data = diemThapNhatData,
                            backgroundColor = "rgba(255, 99, 132, 0.6)",
                            borderColor = "rgba(255, 99, 132, 1)",
                            borderWidth = 1
                        }
                    }
                },
                chartCount = new
                {
                    type = "bar",
                    labels = labels,
                    datasets = new[]
                    {
                        new
                        {
                            label = "Số lượng đánh giá",
                            data = soLuongDanhGiaData,
                            backgroundColor = "rgba(153, 102, 255, 0.6)",
                            borderColor = "rgba(153, 102, 255, 1)",
                            borderWidth = 1
                        }
                    }
                },
                chartRadar = new
                {
                    type = "radar",
                    labels = labels,
                    datasets = new[]
                    {
                        new
                        {
                            label = "Điểm trung bình",
                            data = diemTrungBinhData,
                            backgroundColor = "rgba(75, 192, 192, 0.2)",
                            borderColor = "rgba(75, 192, 192, 1)",
                            borderWidth = 2
                        }
                    }
                },
                data = sanPhamStats.Select(x => new
                {
                    x.SanPhamId,
                    x.TenSanPham,
                    x.Gia,
                    x.SoLuongDanhGia,
                    DiemTrungBinh = Math.Round(x.DiemTrungBinh, 2),
                    x.DiemCaoNhat,
                    x.DiemThapNhat
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // GET: api/ThongKe/tong-quan - Tổng quan thống kê
    [HttpGet("tong-quan")]
    public IActionResult GetTongQuan([FromQuery] string period = "thang")
    {
        try
        {
            // Tính toán khoảng thời gian dựa trên period
            DateTime startDate;
            DateTime endDate = DateTime.Now;

            switch (period.ToLower())
            {
                case "ngay":
                    startDate = DateTime.Today;
                    break;
                case "thang":
                    startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    break;
                case "quy":
                    int currentQuarter = (DateTime.Now.Month - 1) / 3 + 1;
                    startDate = new DateTime(DateTime.Now.Year, (currentQuarter - 1) * 3 + 1, 1);
                    break;
                case "nam":
                    startDate = new DateTime(DateTime.Now.Year, 1, 1);
                    break;
                default:
                    startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    break;
            }

            // Tổng tài khoản và sản phẩm luôn là tổng (không lọc theo thời gian)
            var tongTaiKhoan = _context.TaiKhoans.Count();
            var tongSanPham = _context.SanPhams.Count();
            
            // Tổng đơn hàng và doanh thu lọc theo period
            var tongDonHang = _context.DonHangs
                .Where(dh => dh.NgayDat.HasValue && dh.NgayDat >= startDate && dh.NgayDat <= endDate)
                .Count();
            
            var tongDoanhThu = _context.DonHangs
                .Where(dh => dh.NgayDat.HasValue && dh.NgayDat >= startDate && dh.NgayDat <= endDate && dh.TongTien.HasValue)
                .Sum(dh => dh.TongTien ?? 0);

            var donHangHomNay = _context.DonHangs
                .Count(dh => dh.NgayDat.HasValue && dh.NgayDat.Value.Date == DateTime.Today);

            var doanhThuHomNay = _context.DonHangs
                .Where(dh => dh.NgayDat.HasValue && dh.NgayDat.Value.Date == DateTime.Today && dh.TongTien.HasValue)
                .Sum(dh => dh.TongTien ?? 0);

            var sanPhamBanChay = _context.ChiTietDonHangs
                .GroupBy(ct => ct.SanPhamId)
                .Select(g => new
                {
                    SanPhamId = g.Key,
                    SoLuong = g.Sum(ct => ct.SoLuong)
                })
                .OrderByDescending(x => x.SoLuong)
                .FirstOrDefault();

            var tenSanPhamBanChay = "Không có";
            if (sanPhamBanChay != null && sanPhamBanChay.SanPhamId.HasValue)
            {
                var sp = _context.SanPhams.FirstOrDefault(s => s.Id == sanPhamBanChay.SanPhamId.Value);
                tenSanPhamBanChay = sp?.TenSanPham ?? "Không xác định";
            }

            return Ok(new
            {
                code = 200,
                message = "Thành công",
                data = new
                {
                    TongTaiKhoan = tongTaiKhoan,
                    TongSanPham = tongSanPham,
                    TongDonHang = tongDonHang,
                    TongDoanhThu = tongDoanhThu,
                    DonHangHomNay = donHangHomNay,
                    DoanhThuHomNay = doanhThuHomNay,
                    SanPhamBanChay = tenSanPhamBanChay
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }
}

