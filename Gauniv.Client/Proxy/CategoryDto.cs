
namespace Gauniv.Client.Proxy
{
    // DTO client minimal pour les catégories (aligné sur la DTO serveur)
    public class CategoryDto : CategoryFullDto
    {
        public bool IsSelected { get; set; } = false;
    }
}
