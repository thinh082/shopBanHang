using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopBanHang.Models.DTOs;
using shopBanHang.Models.Entities;

namespace shopBanHang.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SanPhamController : ControllerBase
{
    private readonly ShopContext _context;

    public SanPhamController(ShopContext context)
    {
        _context = context;
    }
    [HttpGet("Top10")]
    public IActionResult GetTop10SanPham()
    {
        try
        {
            var sanPhams = _context.SanPhams
                .Where(sp => sp.TrangThai == true)
                .OrderByDescending(sp => sp.NgayThem)
                .Take(10)
                .Select(sp => new SanPhamResponseDTO
                {
                    Id = sp.Id,
                    TenSanPham = sp.TenSanPham,
                    MoTa = sp.MoTa,
                    Gia = sp.Gia,
                    SoLuong = sp.SoLuong,
                    DanhMucId = sp.DanhMucId,
                    TenDanhMuc = sp.DanhMuc != null ? sp.DanhMuc.TenDanhMuc : null,
                    NgayThem = sp.NgayThem,
                    TrangThai = sp.TrangThai,
                    HinhAnhs = sp.SanPhamHinhAnhs.Select(ha => ha.DuongDan ?? "").ToList(),
                    DiemTrungBinh = sp.DanhGia.Any() ? sp.DanhGia.Average(dg => (double?)dg.Diem) : null,
                    SoLuongDanhGia = sp.DanhGia.Count,
                    KhuyenMai = sp.KhuyenMai,
                    ThuongHieu = sp.ThuongHieu ?? ""
                })
                .ToList();
            return Ok(new { code = 200, message = "Thành công", data = sanPhams });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }
    // Xem danh sách sản phẩm theo danh mục
    [HttpGet("danh-muc/{danhMucId}")]
    public IActionResult GetSanPhamByDanhMuc(int danhMucId)
    {
        try
        {
            var sanPhams = _context.SanPhams
                .Where(sp => sp.DanhMucId == danhMucId && sp.TrangThai == true)
                .Select(sp => new SanPhamResponseDTO
                {
                    Id = sp.Id,
                    TenSanPham = sp.TenSanPham,
                    MoTa = sp.MoTa,
                    Gia = sp.Gia,
                    SoLuong = sp.SoLuong,
                    DanhMucId = sp.DanhMucId,
                    TenDanhMuc = sp.DanhMuc != null ? sp.DanhMuc.TenDanhMuc : null,
                    NgayThem = sp.NgayThem,
                    TrangThai = sp.TrangThai,
                    HinhAnhs = sp.SanPhamHinhAnhs.Select(ha => ha.DuongDan ?? "").ToList(),
                    ThongSo = sp.ThongSos
                        .OrderBy(ts => ts.Id)
                        .Select(ts => new ThongSoSanPham
                        {
                            Id = ts.Id,
                            IdSanPham = ts.IdSanPham,
                            Cpu = ts.Cpu,
                            Vga = ts.Vga,
                            Ram = ts.Ram,
                            Rom = ts.Rom
                        })
                        .FirstOrDefault(),
                    DiemTrungBinh = sp.DanhGia.Any() ? sp.DanhGia.Average(dg => (double?)dg.Diem) : null,
                    SoLuongDanhGia = sp.DanhGia.Count,
                    KhuyenMai = sp.KhuyenMai,
                    ThuongHieu = sp.ThuongHieu ?? ""
                })
                .ToList();

            return Ok(new { code = 200, message = "Thành công", data = sanPhams });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Tìm kiếm sản phẩm theo tên
    [HttpGet("tim-kiem")]
    public IActionResult TimKiemSanPham([FromQuery] string tenSanPham)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tenSanPham))
            {
                return BadRequest(new { code = 400, message = "Tên sản phẩm không được để trống" });
            }

            var sanPhams = _context.SanPhams
                .Where(sp => sp.TenSanPham.Contains(tenSanPham) && sp.TrangThai == true)
                .Select(sp => new SanPhamResponseDTO
                {
                    Id = sp.Id,
                    TenSanPham = sp.TenSanPham,
                    MoTa = sp.MoTa,
                    Gia = sp.Gia,
                    SoLuong = sp.SoLuong,
                    DanhMucId = sp.DanhMucId,
                    TenDanhMuc = sp.DanhMuc != null ? sp.DanhMuc.TenDanhMuc : null,
                    NgayThem = sp.NgayThem,
                    TrangThai = sp.TrangThai,
                    HinhAnhs = sp.SanPhamHinhAnhs.Select(ha => ha.DuongDan ?? "").ToList(),
                    ThongSo = sp.ThongSos
                        .OrderBy(ts => ts.Id)
                        .Select(ts => new ThongSoSanPham
                        {
                            Id = ts.Id,
                            IdSanPham = ts.IdSanPham,
                            Cpu = ts.Cpu,
                            Vga = ts.Vga,
                            Ram = ts.Ram,
                            Rom = ts.Rom
                        })
                        .FirstOrDefault(),
                    DiemTrungBinh = sp.DanhGia.Any() ? sp.DanhGia.Average(dg => (double?)dg.Diem) : null,
                    SoLuongDanhGia = sp.DanhGia.Count,
                    KhuyenMai = sp.KhuyenMai,
                    ThuongHieu = sp.ThuongHieu ?? ""
                })
                .ToList();

            return Ok(new { code = 200, message = "Thành công", data = sanPhams });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Lọc sản phẩm: giá, trạng thái, số lượng
    [HttpPost("loc")]
    public IActionResult LocSanPham([FromBody] SanPhamFilterDTO filter)
    {
        try
        {
            var query = _context.SanPhams
                .Include(sp => sp.ThongSos)
                .AsQueryable();

            if (filter.DanhMucId.HasValue)
            {
                query = query.Where(sp => sp.DanhMucId == filter.DanhMucId);
            }

            if (!string.IsNullOrWhiteSpace(filter.TenSanPham))
            {
                query = query.Where(sp => sp.TenSanPham.Contains(filter.TenSanPham));
            }

            if (filter.GiaMin.HasValue)
            {
                query = query.Where(sp => sp.Gia >= filter.GiaMin.Value);
            }

            if (filter.GiaMax.HasValue)
            {
                query = query.Where(sp => sp.Gia <= filter.GiaMax.Value);
            }

            if (filter.TrangThai.HasValue)
            {
                query = query.Where(sp => sp.TrangThai == filter.TrangThai.Value);
            }

            if (filter.ConHang == true)
            {
                query = query.Where(sp => sp.SoLuong > 0);
            }


            var total = query.Count();

            var sanPhams = query
                .OrderByDescending(sp => sp.NgayThem)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(sp => new SanPhamResponseDTO
                {
                    Id = sp.Id,
                    TenSanPham = sp.TenSanPham,
                    MoTa = sp.MoTa,
                    Gia = sp.Gia,
                    SoLuong = sp.SoLuong,
                    DanhMucId = sp.DanhMucId,
                    TenDanhMuc = sp.DanhMuc != null ? sp.DanhMuc.TenDanhMuc : null,
                    NgayThem = sp.NgayThem,
                    TrangThai = sp.TrangThai,
                    HinhAnhs = sp.SanPhamHinhAnhs.Select(ha => ha.DuongDan ?? "").ToList(),
                    ThuongHieu = sp.ThuongHieu ?? "",
                    ThongSo = sp.ThongSos
                        .OrderBy(ts => ts.Id)
                        .Select(ts => new ThongSoSanPham
                        {
                            Id = ts.Id,
                            IdSanPham = ts.IdSanPham,
                            Cpu = ts.Cpu,
                            Vga = ts.Vga,
                            Ram = ts.Ram,
                            Rom = ts.Rom
                        })
                        .FirstOrDefault(),
                    DiemTrungBinh = sp.DanhGia.Any() ? sp.DanhGia.Average(dg => (double?)dg.Diem) : null,
                    SoLuongDanhGia = sp.DanhGia.Count,
                    KhuyenMai = sp.KhuyenMai,
    
                })
                .ToList();

            return Ok(new { 
                code = 200, 
                message = "Thành công", 
                data = sanPhams,
                total = total,
                page = filter.Page,
                pageSize = filter.PageSize
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Xem chi tiết sản phẩm
    [HttpGet("{id}")]
    public IActionResult GetChiTietSanPham(int id)
    {
        try
        {
            var sanPham = _context.SanPhams
                .Where(sp => sp.Id == id)
                .Select(sp => new SanPhamChiTietDTO
                {
                    Id = sp.Id,
                    TenSanPham = sp.TenSanPham,
                    MoTa = sp.MoTa,
                    Gia = sp.Gia,
                    SoLuong = sp.SoLuong,
                    DanhMucId = sp.DanhMucId,
                    TenDanhMuc = sp.DanhMuc != null ? sp.DanhMuc.TenDanhMuc : null,
                    NgayThem = sp.NgayThem,
                    TrangThai = sp.TrangThai,
                    HinhAnhs = sp.SanPhamHinhAnhs.Select(ha => ha.DuongDan ?? "").ToList(),
                    ThongSo = sp.ThongSos
                        .OrderBy(ts => ts.Id)
                        .Select(ts => new ThongSoSanPham
                        {
                            Id = ts.Id,
                            IdSanPham = ts.IdSanPham,
                            Cpu = ts.Cpu,
                            Vga = ts.Vga,
                            Ram = ts.Ram,
                            Rom = ts.Rom
                        })
                        .FirstOrDefault() ?? new ThongSoSanPham(),
                    DiemTrungBinh = sp.DanhGia.Any() ? sp.DanhGia.Average(dg => (double?)dg.Diem) : null,
                    SoLuongDanhGia = sp.DanhGia.Count,
                    KhuyenMai = sp.KhuyenMai,
                    ThuongHieu = sp.ThuongHieu ?? "",
                    DanhGia = sp.DanhGia.Select(dg => new DanhGiaResponseDTO
                    {
                        Id = dg.Id,
                        SanPhamId = dg.SanPhamId ?? 0,
                        TaiKhoanId = dg.TaiKhoanId ?? 0,
                        HoTen = dg.TaiKhoan != null ? dg.TaiKhoan.HoTen : null,
                        NoiDung = dg.NoiDung,
                        Diem = dg.Diem,
                        NgayDanhGia = dg.NgayDanhGia
                    }).ToList(),
                    SanPhamLienQuan = _context.SanPhams
                        .Where(sp2 => sp2.DanhMucId == sp.DanhMucId && sp2.Id != sp.Id && sp2.TrangThai == true)
                        .Take(5)
                        .Select(sp2 => new SanPhamResponseDTO
                        {
                            Id = sp2.Id,
                            TenSanPham = sp2.TenSanPham,
                            MoTa = sp2.MoTa,
                            Gia = sp2.Gia,
                            SoLuong = sp2.SoLuong,
                            DanhMucId = sp2.DanhMucId,
                            TenDanhMuc = sp2.DanhMuc != null ? sp2.DanhMuc.TenDanhMuc : null,
                            NgayThem = sp2.NgayThem,
                            TrangThai = sp2.TrangThai,
                            HinhAnhs = sp2.SanPhamHinhAnhs.Select(ha => ha.DuongDan ?? "").ToList(),
                            DiemTrungBinh = sp2.DanhGia.Any() ? sp2.DanhGia.Average(dg => (double?)dg.Diem) : null,
                            SoLuongDanhGia = sp2.DanhGia.Count,
                            KhuyenMai = sp2.KhuyenMai,
                            ThuongHieu = sp2.ThuongHieu ?? ""
                        }).ToList()
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

    // Xem đánh giá người dùng khác
    [HttpGet("{id}/danh-gia")]
    public IActionResult GetDanhGiaSanPham(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var danhGia = _context.DanhGia
                .Where(dg => dg.SanPhamId == id)
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

            var total = _context.DanhGia.Count(dg => dg.SanPhamId == id);

            return Ok(new { 
                code = 200, 
                message = "Thành công", 
                data = danhGia,
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

    // Xem sản phẩm liên quan
    [HttpGet("{id}/lien-quan")]
    public IActionResult GetSanPhamLienQuan(int id)
    {
        try
        {
            var sanPham = _context.SanPhams.FirstOrDefault(sp => sp.Id == id);
            if (sanPham == null)
            {
                return BadRequest(new { code = 404, message = "Không tìm thấy sản phẩm" });
            }

            var sanPhamLienQuan = _context.SanPhams
                .Where(sp => sp.DanhMucId == sanPham.DanhMucId && sp.Id != id && sp.TrangThai == true)
                .Take(10)
                .Select(sp => new SanPhamResponseDTO
                {
                    Id = sp.Id,
                    TenSanPham = sp.TenSanPham,
                    MoTa = sp.MoTa,
                    Gia = sp.Gia,
                    SoLuong = sp.SoLuong,
                    DanhMucId = sp.DanhMucId,
                    TenDanhMuc = sp.DanhMuc != null ? sp.DanhMuc.TenDanhMuc : null,
                    NgayThem = sp.NgayThem,
                    TrangThai = sp.TrangThai,
                    HinhAnhs = sp.SanPhamHinhAnhs.Select(ha => ha.DuongDan ?? "").ToList(),
                    DiemTrungBinh = sp.DanhGia.Any() ? sp.DanhGia.Average(dg => (double?)dg.Diem) : null,
                    SoLuongDanhGia = sp.DanhGia.Count,
                    ThuongHieu = sp.ThuongHieu ?? ""
                })
                .ToList();

            return Ok(new { code = 200, message = "Thành công", data = sanPhamLienQuan });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }

    // Lấy danh sách danh mục
    [HttpGet("danh-muc")]
    public IActionResult GetDanhMuc()
    {
        try
        {
            var danhMucs = _context.DanhMucs
                .Select(dm => new DanhMucResponseDTO
                {
                    Id = dm.Id,
                    TenDanhMuc = dm.TenDanhMuc,
                    SoLuongSanPham = dm.SanPhams.Count(sp => sp.TrangThai == true)
                })
                .ToList();

            return Ok(new { code = 200, message = "Thành công", data = danhMucs });
        }
        catch (Exception ex)
        {
            return BadRequest(new { code = 400, message = $"Lỗi: {ex.Message}" });
        }
    }
}

