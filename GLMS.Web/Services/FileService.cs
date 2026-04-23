namespace GLMS.Web.Services
{
    public interface IFileService
    {
        Task<string> SaveSignedAgreementAsync(IFormFile file, int contractId);
        bool ValidateFile(IFormFile file, out string errorMessage);
        string GetFilePath(string relativePath);
    }

    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileService> _logger;

        private static readonly string[] AllowedExtensions = { ".pdf" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public FileService(IWebHostEnvironment environment, ILogger<FileService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public bool ValidateFile(IFormFile file, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (file == null || file.Length == 0)
            {
                errorMessage = "No file was uploaded.";
                return false;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                errorMessage = $"Only PDF files are allowed. You uploaded a '{extension}' file.";
                return false;
            }

            if (file.Length > MaxFileSizeBytes)
            {
                errorMessage = $"File size exceeds the 10 MB limit.";
                return false;
            }

            return true;
        }

        public async Task<string> SaveSignedAgreementAsync(IFormFile file, int contractId)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"Contract_{contractId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("Saved agreement for Contract {ContractId} at {Path}.", contractId, fullPath);
            return $"/uploads/{fileName}";
        }

        public string GetFilePath(string relativePath)
        {
            return Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/'));
        }
    }
}
