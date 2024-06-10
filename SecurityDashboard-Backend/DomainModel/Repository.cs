namespace SecurityDashboard_Backend.DomainModel;

public class Repository
{
    public int Id { get; set; }

    public string Name { get; set; }
    public string Url { get; set; }

    public List<PipelineRun> PipelineRuns { get; set; } = new List<PipelineRun>();
}
