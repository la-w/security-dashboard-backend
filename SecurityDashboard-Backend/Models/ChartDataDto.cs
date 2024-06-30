namespace SecurityDashboard_Backend.Models;

public class ChartDataDto
{
    public ChartData AbsoluteFindings { get; set; }
    public ChartData LastRunByLevels { get; set; }
    public ChartData LastRunByRules { get; set; }
}

public class ChartData
{
    public List<string> Labels { get; set; } = new();
    public List<int> Data { get; set; } = new();
}
