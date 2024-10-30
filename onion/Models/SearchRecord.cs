using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using onion.Areas.Identity.Data; // Adjust namespace as needed

public class SearchRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string SearchTerm { get; set; }

    public string UserId { get; set; } // Keep this as string (nullable by default)

    public string IpAddress { get; set; }

    public DateTime SearchTime { get; set; } = DateTime.UtcNow;

    // Optional: Establish a relationship with AppUser
    [ForeignKey("UserId")]
    public virtual AppUser User { get; set; }
}
