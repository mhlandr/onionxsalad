namespace onion.Models
{
    public class ScreenshotRequest
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public DateTime RequestedAt { get; set; }
        public string Status { get; set; } // "Pending", "Processing", "Completed", "Failed"
        public string ScreenshotPath { get; set; }
        public string ErrorMessage { get; set; }
    }
}
