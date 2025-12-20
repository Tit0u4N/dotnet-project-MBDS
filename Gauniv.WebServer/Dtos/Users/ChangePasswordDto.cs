using System.ComponentModel.DataAnnotations;

namespace Gauniv.WebServer.Dtos.Users
{
    
    public  class ChangePasswordDto
    {
        [Required(ErrorMessage =  "Current password is required.")]
        public string CurrentPassword { get; set; } = null!;
        
        [Required(ErrorMessage =  "New password is required.")]
        public string NewPassword { get; set; } = null!;
    }
}