using GLMS.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GLMS.Tests
{
    /// <summary>
    /// Unit tests for FileService validation logic.
    /// Covers allowed extensions, rejected extensions, file size limits, and null inputs.
    /// </summary>
    public class FileServiceTests
    {
        private readonly FileService _service;

        public FileServiceTests()
        {
            var envMock = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            envMock.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

            var logger = new Mock<ILogger<FileService>>().Object;
            _service = new FileService(envMock.Object, logger);
        }

        // ── Helper: build a mock IFormFile ────────────────────────────────

        private static IFormFile CreateMockFile(string fileName, long sizeBytes)
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(sizeBytes);
            return fileMock.Object;
        }

        // ── PDF allowed ────────────────────────────────────────────────────

        [Fact]
        public void ValidateFile_ValidPdf_ReturnsTrue()
        {
            // Arrange
            var file = CreateMockFile("agreement.pdf", 1024 * 500); // 500 KB

            // Act
            bool result = _service.ValidateFile(file, out string error);

            // Assert
            Assert.True(result);
            Assert.Empty(error);
        }

        [Fact]
        public void ValidateFile_PdfUppercaseExtension_ReturnsTrue()
        {
            // Extensions must be case-insensitive
            var file = CreateMockFile("AGREEMENT.PDF", 1024);
            bool result = _service.ValidateFile(file, out _);
            Assert.True(result);
        }

        // ── Rejected file types ────────────────────────────────────────────

        [Fact]
        public void ValidateFile_ExeFile_ReturnsFalseWithError()
        {
            // Arrange
            var file = CreateMockFile("malware.exe", 1024);

            // Act
            bool result = _service.ValidateFile(file, out string error);

            // Assert
            Assert.False(result);
            Assert.Contains(".exe", error, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("document.docx")]
        [InlineData("spreadsheet.xlsx")]
        [InlineData("image.png")]
        [InlineData("script.js")]
        [InlineData("archive.zip")]
        [InlineData("virus.bat")]
        public void ValidateFile_NonPdfExtensions_ReturnsFalse(string fileName)
        {
            var file = CreateMockFile(fileName, 1024);
            bool result = _service.ValidateFile(file, out string error);

            Assert.False(result);
            Assert.NotEmpty(error);
        }

        // ── File size ──────────────────────────────────────────────────────

        [Fact]
        public void ValidateFile_FileTooLarge_ReturnsFalseWithError()
        {
            // Arrange: 11 MB exceeds the 10 MB limit
            var file = CreateMockFile("large.pdf", 11L * 1024 * 1024);

            // Act
            bool result = _service.ValidateFile(file, out string error);

            // Assert
            Assert.False(result);
            Assert.Contains("10 MB", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidateFile_FileExactlyAtLimit_ReturnsTrue()
        {
            // Exactly 10 MB should be accepted
            var file = CreateMockFile("exact.pdf", 10L * 1024 * 1024);
            bool result = _service.ValidateFile(file, out _);
            Assert.True(result);
        }

        // ── Null / empty ───────────────────────────────────────────────────

        [Fact]
        public void ValidateFile_NullFile_ReturnsFalseWithError()
        {
            // Act
            bool result = _service.ValidateFile(null!, out string error);

            // Assert
            Assert.False(result);
            Assert.NotEmpty(error);
        }

        [Fact]
        public void ValidateFile_EmptyFile_ReturnsFalseWithError()
        {
            var file = CreateMockFile("empty.pdf", 0);
            bool result = _service.ValidateFile(file, out string error);

            Assert.False(result);
            Assert.NotEmpty(error);
        }
    }
}
