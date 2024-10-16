using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Net;
using FluentFTP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

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

        // อัปโหลดไฟล์ แบบต่อเซิฟ
        //public void UploadFileToFtp(string filePath, string fileName)
        //{
        //    using (var client = new FtpClient(_ftpServerUrl))
        //    {
        //        client.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);
        //        try
        //        {
        //            client.Connect();

        //            var destinationPath = $"/{fileName}";
        //            if (client.FileExists(destinationPath))
        //            {
        //                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        //                var extension = Path.GetExtension(fileName);
        //                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        //                destinationPath = $"/{fileNameWithoutExtension}_{timestamp}{extension}";
        //            }

        //            client.UploadFile(filePath, destinationPath);
        //            _logger.LogInformation($"อัปโหลดไฟล์ได้สำเร็จ: {_ftpServerUrl}{destinationPath}");
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "เกิดข้อผิดพลาดไม่สามารถอัปโหลดได้");
        //            throw;
        //        }
        //    }
        //}

        // เทสบน Local
        public void UploadFileToLocal(string filePath, string fileName)
        {
            // ฟิค Path
            var localDirectory = "D:/INET/Project/FTPProjekt/FTPProjekt/FTP_Destination";

            // รวม Path
            var destinationPath = Path.Combine(localDirectory, fileName);

            // ถ้ามีไฟล์อยู่แล้วให้สร้างใหม่โดยเพิ่มเวลาให้ไม่ซ้ำ
            if (System.IO.File.Exists(destinationPath))
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                var extension = Path.GetExtension(fileName);
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                destinationPath = Path.Combine(localDirectory, $"{fileNameWithoutExtension}_{timestamp}{extension}");
            }

            // ใส่ไปที่ปลายทาง
            System.IO.File.Copy(filePath, destinationPath);
            _logger.LogInformation($"ไฟล์อัปโหลดได้สำเร็จที่: {destinationPath}");
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
            using (var client = new FtpClient(_ftpServerUrl))
            {
                client.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);

                try
                {
                    client.Connect();

                    using (var memoryStream = new MemoryStream())
                    {
                        // เช็คถ้า DownloadStream ส่งค่ากลับมาเป็น True ก็คือดาวน์โหลดได้
                        if (client.DownloadStream(memoryStream, $"/{fileName}"))
                        {
                            _logger.LogInformation($"ดาวน์โหลดไฟล์บนเซิฟเวอร์สำเร็จ {_ftpServerUrl}/{fileName}");
                            return memoryStream.ToArray();
                        }
                        else
                        {
                            _logger.LogError("ไม่สามารถดาวน์โหลดไฟล์ได้");
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "เกิดข้อผิดพลาดในการดาวน์โหลดแบบ FTP");
                    return null;
                }
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
        private readonly string _ftpServerUrl;
        private readonly string _ftpUsername;
        private readonly string _ftpPassword;

        public FileMoveService(IConfiguration configuration, ILogger<FileMoveService> logger)
        {
            _ftpServerUrl = configuration["FtpSettings:ServerUrl"];
            _ftpUsername = configuration["FtpSettings:Username"];
            _ftpPassword = configuration["FtpSettings:Password"];
            _logger = logger;
        }

        //public string MoveFile(string sourcePath, string destinationFolder, string fileName)
        //{
        //    using (var client = new FtpClient(_ftpServerUrl))
        //    {
        //        client.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);
        //        try
        //        {
        //            client.Connect();

        //            // รวม Path ต้นทาง
        //            var sourceFilePath = $"{sourcePath}/{fileName}";

        //            // เช็คว่าเจอไฟล์ที่ต้องการจะย้ายหรือไม่
        //            if (client.FileExists(sourceFilePath))
        //            {
        //                _logger.LogError($"ไม่พบไฟล์ที่ต้นทาง: {sourceFilePath}");
        //                throw new FileNotFoundException($"ไม่พบไฟล์: {sourceFilePath}");
        //            }

        //            // เก็บ Path ปลายทางโดยการรวมเอาชื่อไฟล์ไปต่อท้าย Root Folder
        //            var destinationPath = $"/{destinationFolder}/{fileName}";

        //            // ถ้ามีไฟล์ชื่อเดียวกันให้เพิ่มเวลาเข้าไป ป้องกันการซ้ำ
        //            if (!client.FileExists(destinationPath))
        //            {
        //                destinationPath = $"/{destinationFolder}/{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(fileName)}";
        //            }

        //            // Check if the destination folder exists
        //            if (client.DirectoryExists($"/{destinationFolder}"))
        //            {
        //                _logger.LogError($"Destination folder does not exist: /{destinationFolder}");
        //                throw new DirectoryNotFoundException($"Destination folder does not exist: /{destinationFolder}");
        //            }

        //            // ย้ายไฟล์จากต้นทาง ไป ปลายทาง
        //            client.Rename(sourceFilePath, destinationPath);


        //            // เอาไว้ดูตอนมันออกมา
        //            _logger.LogInformation($"โอนย้ายไฟล์ไปปลายทางได้สำเร็จ {destinationPath}");

        //            return destinationPath;
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "เกิดข้อผิดพลาดในการย้ายไฟล์");
        //            throw;
        //        }
        //        finally
        //        {
        //            if (client.IsConnected)
        //            {
        //                client.Disconnect();
        //            }
        //        }
        //    }
        //}

        public string MoveFile(string sourcePath, string destinationFolder, string fileName)
        {
            // เทสบน Local
            var fullSourcePath = Path.Combine(sourcePath, fileName);
            var fullDestinationPath = Path.Combine(destinationFolder, fileName);


            if (!File.Exists(fullSourcePath))
            {
                _logger.LogError($"ไม่พบไฟล์ที่ต้นทาง: {fullSourcePath}");
                throw new FileNotFoundException($"ไม่พบไฟล์: {fullSourcePath}");
            }

            // ย้ายไฟล์ไปปลายทาง
            File.Move(fullSourcePath, fullDestinationPath);

            _logger.LogInformation($"โอนย้ายไฟล์สำเร็จ {fullDestinationPath}");
            return fullDestinationPath;
        }


    }

}

