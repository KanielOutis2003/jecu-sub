using System.ComponentModel.DataAnnotations;

namespace SubdivisionWebsite.ViewModels
{
    public class AnnouncementViewModel
    {
        [Required]
        public required string Title { get; set; }

        [Required]
        public required string Content { get; set; }
    }
} 