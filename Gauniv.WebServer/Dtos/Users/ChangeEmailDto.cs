using System.ComponentModel.DataAnnotations;

namespace Gauniv.WebServer.Dtos.Users
{
    public class ChangeEmailDto
    {
        [Required(ErrorMessage =  "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string NewEmail { get; set; } = null!;

        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = null!;
    }
}

