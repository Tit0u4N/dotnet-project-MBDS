using System.ComponentModel.DataAnnotations;

namespace Gauniv.WebServer.Dtos;

public class CategoryFullDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
}

public class CategoryCreateOrEditDto
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
    public string Title { get; set; } = null!;
}
