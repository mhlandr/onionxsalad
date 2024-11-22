using System.Collections.Generic;

namespace onion.Models
{
    public class ImageData
    {
        public string ImageUrl { get; set; }
        public string LocalFileName { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public byte[] ImageBytes { get; set; }
    }
}
