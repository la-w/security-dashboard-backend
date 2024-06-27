namespace SecurityDashboard_Backend.Services;

public interface IFindingsDeduplicationService
{
    Task DeduplicateFindingsAsync(long pipelineId);
}
