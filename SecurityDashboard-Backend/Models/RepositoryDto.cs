namespace SecurityDashboard_Backend.Models;

/// <summary>
/// Repository Data
/// </summary>
public class RepositoryDto
{
    /// <summary>
    /// Name of the Repository
    /// </summary>
    /// <example>Juice-Shop</example>
    public string Name { get; set; }

    /// <summary>
    /// URL of the Repository
    /// </summary>
    /// <example>https://git.example.com/Juice-Shop</example>
    public string Url { get; set; }
}
