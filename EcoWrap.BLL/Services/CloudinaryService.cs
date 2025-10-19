using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using EcoWrap.Models.Config;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace EcoWrap.BLL.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySettings> config)
        {
            var account = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(account);
        }

        //Upload ảnh (jpg, png, webp, ...)
        public async Task<string> UploadImageAsync(IFormFile file, string folder = "ecowrap/products")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ.");

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Folder = folder
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("Upload ảnh thất bại.");

            return result.SecureUrl.ToString();
        }

        //Upload file tài liệu (PDF, DOCX, ZIP, ...)
        public async Task<string> UploadFileAsync(IFormFile file, string folder = "ecowrap/files")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ.");

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Folder = folder
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("Upload file thất bại.");

            return result.SecureUrl.ToString();
        }

        //Xóa file hoặc ảnh
        public async Task<bool> DeleteAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId)) return false;

            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);
            return result.Result == "ok";
        }

        //Lấy publicId từ URL để xóa
        public string ExtractPublicId(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            try
            {
                // Lấy phần sau "/upload/" và bỏ phần mở rộng
                var start = url.IndexOf("/upload/") + 8;
                var pathWithExt = url.Substring(start);
                var withoutVersion = pathWithExt.Contains('/')
                    ? string.Join('/', pathWithExt.Split('/').SkipWhile(p => p.StartsWith("v")))
                    : pathWithExt;

                return Path.ChangeExtension(withoutVersion, null); // bỏ .jpg, .png,...
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}