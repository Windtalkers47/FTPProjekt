using Microsoft.AspNetCore.Mvc;
using FTPProjekt.Model;
using static FTPProjekt.Model.MoveFileModel;

namespace FTPProjekt.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileController : ControllerBase
    {
        private readonly FileUploadService _fileUploadService;
        private readonly FileDownloadService _fileDownloadService;
        private readonly FileMoveService _fileMoveService;
        private readonly FileProcessService _fileProcessService;
        private readonly ILogger<FileController> _logger;

        public FileController(FileUploadService fileUploadService,
                              FileDownloadService fileDownloadService,
                              FileMoveService fileMoveService,
                              FileProcessService fileProcessService,
                              ILogger<FileController> logger)
        {
            _fileUploadService = fileUploadService;
            _fileDownloadService = fileDownloadService;
            _fileMoveService = fileMoveService;
            _fileProcessService = fileProcessService;
            _logger = logger;
        }

        [HttpPost("Upload")]
        public IActionResult UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("ไม่พบไฟล์ที่ต้องการจะอัปโหลด");
                return BadRequest("กรุณาเพิ่มไฟล์ที่ต้องการจะอัปโหลด");
            }

            var filePath = Path.GetTempFileName();
            using (var stream = System.IO.File.Create(filePath))
            {
                file.CopyTo(stream);
            }

            _fileUploadService.UploadFileToLocal(filePath, file.FileName);
            //_fileUploadService.UploadFileToFtp(filePath, file.FileName);


            // ลบไฟล์ Temp ออกจะได้ไม่รก
            System.IO.File.Delete(filePath);

            _logger.LogInformation("อัปโหลดไฟล์ได้สำเร็จ");
            return Ok("อัปโหลดไฟล์ได้สำเร็จ");
        }

        [HttpGet("download/{fileName}")]
        public IActionResult DownloadFile(string fileName)
        {
            var fileData = _fileDownloadService.DownloadFileFromFtp(fileName);
            if (fileData == null)
            {
                _logger.LogWarning($"ไม่พบไฟล์ '{fileName}' บนเซิฟ FTP");
                return NotFound($"ไม่พบไฟล์ '{fileName}' ");
            }

            return File(fileData, "application/octet-stream", fileName);
        }

        [HttpGet("process/{fileName}")]
        public IActionResult ProcessFile(string fileName)
        {
            var result = _fileProcessService.ReadAndConvertToJson(fileName);
            return Ok(new 
            {
                message = result
            });
        }

        [HttpPost("move")]
        public IActionResult MoveFile([FromBody] MoveFileRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var destinationPath = _fileMoveService.MoveFile(request.SourcePath, request.DestinationFolder, request.FileName);
                _logger.LogInformation($"ทำการย้ายไฟล์ '{request.FileName}' ไปยัง '{destinationPath}' ได้สำเร็จ");

                return Ok(new MoveFileResponse
                {
                    Success = true,
                    Message = "ย้ายไฟล์สำเร็จ",
                    DestinationPath = destinationPath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการย้ายไฟล์");
                return StatusCode(500, new MoveFileResponse
                {
                    Success = false,
                    Message = "เกิดข้อผิดพลาดในการย้ายไฟล์ " + ex.Message,
                    DestinationPath = null
                });
            }
        }

    }
}
