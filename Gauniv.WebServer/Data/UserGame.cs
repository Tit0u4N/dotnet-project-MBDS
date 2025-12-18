using Gauniv.WebServer.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gauniv.WebServer.Data
{
    [PrimaryKey(nameof(UserId), nameof(GameId))]
    public class UserGame
    {
        [Required]
        [MaxLength(100)]
        public String UserId { get; set; }
        public User User { get; set; } = null!;

        public int GameId { get; set; }
        public Game Game { get; set; } = null!;

        public DateTime PurchaseDate { get; set; }

        public bool IsFavorite { get; set; }
    }
}