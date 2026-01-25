using System.ComponentModel.DataAnnotations;

namespace ChatAppProj.DTO
{
    public class UpdateProfileDto
    {
        public string DisplayName { get; set; }
        public string? ProfilePicture { get; set; }

        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        public string? Bio { get; set; }


    }
}
