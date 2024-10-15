using System.ComponentModel.DataAnnotations;

namespace FTPProjekt.Model
{
    public class MoveFileModel
    {
        public class MoveFileRequest
        {
            [Required(ErrorMessage = "SourcePath is required")]
            public string? SourcePath { get; set; }

            [Required(ErrorMessage = "DestinationFolder is required")]
            public string? DestinationFolder { get; set; }

            [Required(ErrorMessage = "FileName is required")]
            public string? FileName { get; set; }
        }
        public class MoveFileResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public string? DestinationPath { get; set; }
        }
    }
}
