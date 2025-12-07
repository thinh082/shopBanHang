using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;

namespace shopBanHang.Services;

public class CloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly string _cloudName;
    private readonly string _apiKey;
    private readonly string _apiSecret;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudinarySettings = configuration.GetSection("CloudinarySettings");
        _cloudName = cloudinarySettings["CloudName"] ?? throw new Exception("CloudName không được để trống trong appsettings.json");
        _apiKey = cloudinarySettings["ApiKey"] ?? throw new Exception("ApiKey không được để trống trong appsettings.json");
        _apiSecret = cloudinarySettings["ApiSecret"] ?? throw new Exception("ApiSecret không được để trống trong appsettings.json");

        // Validate không được null hoặc empty
        if (string.IsNullOrWhiteSpace(_cloudName))
            throw new Exception("CloudName không được để trống");
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new Exception("ApiKey không được để trống");
        if (string.IsNullOrWhiteSpace(_apiSecret))
            throw new Exception("ApiSecret không được để trống");

        var account = new Account(_cloudName, _apiKey, _apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string folder = "avatars")
    {
        try
        {
            // Kiểm tra stream
            if (imageStream == null || imageStream.Length == 0)
            {
                throw new Exception("Stream ảnh không hợp lệ hoặc rỗng");
            }

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(fileName, imageStream),
                Folder = folder,
                Transformation = new Transformation()
                    .Width(500)
                    .Height(500)
                    .Crop("fill")
                    .Gravity("face")
                    .Quality("auto")
                    .FetchFormat("auto"),
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK && uploadResult.SecureUrl != null)
            {
                return uploadResult.SecureUrl.ToString();
            }
            else
            {
                var errorMsg = uploadResult.Error?.Message ?? "Unknown error";
                throw new Exception($"Upload failed: {errorMsg}. CloudName: {_cloudName}, StatusCode: {uploadResult.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            // Log thông tin config để debug (không log secret)
            var debugInfo = $"CloudName: {_cloudName}, ApiKey: {_apiKey.Substring(0, Math.Min(5, _apiKey.Length))}...";
            throw new Exception($"Lỗi khi upload ảnh lên Cloudinary: {ex.Message}. {debugInfo}", ex);
        }
    }

    public async Task<string> UploadImageFromBase64Async(string base64String, string fileName, string folder = "avatars")
    {
        try
        {
            // Xử lý base64 string (loại bỏ data:image/...;base64, nếu có)
            var base64Data = base64String.Contains(",") 
                ? base64String.Split(',')[1] 
                : base64String;

            var bytes = Convert.FromBase64String(base64Data);
            using var stream = new MemoryStream(bytes);

            return await UploadImageAsync(stream, fileName, folder);
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi upload ảnh từ base64: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteImageAsync(string imageUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return false;

            // Lấy public_id từ URL
            var uri = new Uri(imageUrl);
            var pathParts = uri.AbsolutePath.Split('/');
            if (pathParts.Length < 2)
                return false;

            var publicId = string.Join("/", pathParts.Skip(1)).Replace(".jpg", "").Replace(".png", "").Replace(".jpeg", "");

            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            return result.Result == "ok";
        }
        catch
        {
            return false;
        }
    }
}

