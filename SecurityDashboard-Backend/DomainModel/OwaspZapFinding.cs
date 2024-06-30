namespace SecurityDashboard_Backend.DomainModel;

public class OwaspZapFinding : ToolFinding
{
    public string Attack { get; set; }
    public string WebRequest { get; set; }
    public string WebResponse { get; set; }
}
