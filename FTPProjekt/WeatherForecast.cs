using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;

namespace FTPProjekt
{
    public class WeatherForecast
    {
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string? Summary { get; set; }
    }

    public class FileUploadService
    {
        private readonly string _ftpServerUrl;
        private readonly string _ftpUsername;
        private readonly string _ftpPassword;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(IConfiguration configuration, ILogger<FileUploadService> logger)
        {
            _ftpServerUrl = configuration["FtpSettings:ServerUrl"];
            _ftpUsername = configuration["FtpSettings:Username"];
            _ftpPassword = configuration["FtpSettings:Password"];
            _logger = logger;
        }

        // อัปโหลดไฟล์
        public void UploadFileToFtp(string filePath, string fileName)
        {
            try
            {
                var request = (FtpWebRequest)WebRequest.Create(new Uri($"{_ftpServerUrl}/{fileName}"));
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);

                byte[] fileContents = File.ReadAllBytes(filePath);

                using (var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    _logger.LogInformation($"อัปโหลดสำเร็จ, สำเร็จ: {response.StatusDescription}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการอัปโหลดแบบ FTP");
                throw;
            }
        }

    }

    public class FileDownloadService
    {
        private readonly string _ftpServerUrl;
        private readonly string _ftpUsername;
        private readonly string _ftpPassword;
        private readonly ILogger<FileDownloadService> _logger;

        public FileDownloadService(IConfiguration configuration, ILogger<FileDownloadService> logger)
        {
            _ftpServerUrl = configuration["FtpSettings:ServerUrl"];
            _ftpUsername = configuration["FtpSettings:Username"];
            _ftpPassword = configuration["FtpSettings:Password"];
            _logger = logger;
        }

        // โหลดไฟล์
        public byte[]? DownloadFileFromFtp(string fileName)
        {
            try
            {
                var request = (FtpWebRequest)WebRequest.Create(new Uri($"{_ftpServerUrl}/{fileName}"));
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);

                using (var response = (FtpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var memoryStream = new MemoryStream())
                {
                    responseStream.CopyTo(memoryStream);
                    _logger.LogInformation($"ดาวน์โหลดสำเร็จ, สถานะ: {response.StatusDescription}");
                    return memoryStream.ToArray();
                }
            }
            catch (WebException ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดาวน์โหลดแบบ FTP");
                return null;
            }
        }
    }

    public class FileProcessService
    {
        // จำลอง Path บนเซิฟโดยการประกาศใน Local เอาไว้ทดลองการทำงาน
        private readonly string sourceFolder = @"D:\INET\Project\FTPProjekt\FTPProjekt\FTP_Source";
        private readonly string destinationFolder = @"D:\INET\Project\FTPProjekt\FTPProjekt\FTP_Destination";

        // อ่านไฟล์ แปลงเป็น JSON และบันทึก
        public string ReadAndConvertToJson(string fileName)
        {
            // รวม Path ต้นทางและชื่อไฟล์ที่รับเข้ามา
            string filePath = Path.Combine(sourceFolder, fileName);

            // ถ้าเจอไฟล์ที่ Mock ไว้ใน path ที่เราสร้าง ให้อ่านไฟล์
            if (File.Exists(filePath))
            {
                // อ่านไฟล์ที่ Encoding เป็น UFT-8
                string content = File.ReadAllText(filePath);

                // เก็บไฟล์เป็น Object ถ้าไม่ทำเป็น Object มันจะแปลงไปเป็น Json ไม่ได้ ในที่นี้ตั้งชื่อไว้ว่า fileContent
                var fileContent = new
                {
                    Content = content
                };

                // Note: JsonSerializer.Serialize เป็น namespace ของ System.Text.Json เพื่อแปลงเป็น JSON
                string jsonContent = JsonSerializer.Serialize(fileContent);

                // บันทึกไฟล์ JSON ไปยัง Folder ปลายทาง
                SaveJsonToDestination(fileName, jsonContent);

                return jsonContent;
            }
            return $"ไม่พบชื่อ {fileName} ที่ต้องการจะย้าย.";
        }
        
        // Function บันทึกไปปลายทาง

        private void SaveJsonToDestination(string fileName, string jsonContent)
        {
            // เติม .json ลงท้ายชื่อไฟล์
            string jsonFileName = Path.ChangeExtension(fileName, ".json");

            // รวม Path ปลายทางหรือบนเซิฟจริง กับไฟล์ที่เราแปลงแล้วไว้ในชื่อ destinationFilePath
            string destinationFilePath = Path.Combine(destinationFolder, jsonFileName);

            // ถ้าไม่มีโฟล์เดอร์ปลายทางก็สร้างใหม่
            Directory.CreateDirectory(destinationFolder);

            // บันทึกไฟล์ JSON ใส่ Folder ปลายทาง
            File.WriteAllText(destinationFilePath, jsonContent);
        }
    }

    // Function ย้ายไฟล์

    public class FileMoveService
    {
        // รับ Logger เอาไว้ดู Output ตอนรันเสยๆ
        private readonly ILogger<FileMoveService> _logger;

        public FileMoveService(ILogger<FileMoveService> logger)
        {
            _logger = logger;
        }

        public string MoveFile(string sourcePath, string destinationFolder, string fileName)
        {
            try
            {
                // เก็บ Path โดยการรวมเอาชื่อไฟล์ไปต่อท้าย Root Folder
                var destinationPath = Path.Combine(destinationFolder, fileName);

                if (File.Exists(destinationPath))
                {
                    // ถ้ามีไฟล์ชื่อเดียวกันให้เพิ่มเวลาเข้าไป ป้องกันการซ้ำ
                    destinationPath = Path.Combine(destinationFolder, $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(fileName)}");
                }

                // ย้ายไฟล์ไปยัง Folder ที่ต้องการตาม Path ปลายทาง
                File.Move(sourcePath, destinationPath);

                // เอาไว้ดูตอนมันออกมา
                _logger.LogInformation($"ย้ายไฟล์ไปยัง {destinationPath}");

                return destinationPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการย้ายไฟล์");
                throw;
            }
        }
    }

}

