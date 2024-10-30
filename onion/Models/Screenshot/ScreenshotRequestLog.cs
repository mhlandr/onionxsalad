using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace onion.Models
{
    public class ScreenshotRequestLog
    {
        [Key]
        public int Id { get; set; }

        public Guid RequestId { get; set; }

        [Required]
        public string Url { get; set; }

        public DateTime RequestedAt { get; set; }

        public string? UserId { get; set; }

        public string IPAddress { get; set; }

        public string Status { get; set; } // "Pending", "Processing", "Completed", "Failed"
        public string? ScreenshotPath { get; set; }
        public string? ErrorMessage { get; set; }

    }

}
