namespace SecurityDashboard_Backend.DomainModel;

public class GitLeaksFinding : Finding
{
    public string CommitHash { get; set; }
    public string CommitMessage { get; set; }
    public string Author { get; set; }
    public string Email { get; set; }
    public DateTime CommitDate { get; set; }
}
