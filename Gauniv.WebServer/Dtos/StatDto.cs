namespace Gauniv.WebServer.Dtos;

public class PlatformStatsDto
{
    public int TotalGamesAvailable { get; set; }
    public List<CategoryStatsDto> GamesPerCategory { get; set; } = new();
    public double AverageGamesPerAccount { get; set; }
    public double AverageTimePlayedPerGameInMinutes { get; set; }
    public int CurrentPlayersOnline { get; set; }
    public int MaxPlayersOnPlatform { get; set; }
    public List<GameMaxPlayersDto> MaxPlayersPerGame { get; set; } = new();
}

public class CategoryStatsDto
{
    public int CategoryId { get; set; }
    public string CategoryTitle { get; set; } = string.Empty;
    public int GameCount { get; set; }
}

public class GameMaxPlayersDto
{
    public int GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public int MaxPlayersConnectedSimultaneously { get; set; }
}
