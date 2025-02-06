using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace onion.Models
{
    public class ForensicsToolsViewModel
    {
        [Required]
        public string? TargetUrl { get; set; }
        public string WebsiteUrl { get; set; }
        public List<ImageData> AnalyzedImages { get; set; }
        public bool IsPost { get; set; }
    }
}
