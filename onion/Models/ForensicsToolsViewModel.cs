using System.Collections.Generic;

namespace onion.Models
{
    public class ForensicsToolsViewModel
    {
        public string WebsiteUrl { get; set; }
        public List<ImageData> AnalyzedImages { get; set; }
        public bool IsPost { get; set; }
    }
}
