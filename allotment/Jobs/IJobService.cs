namespace Allotment.Jobs
{
    public interface IJobService
    {
        Task RunAsync(IRunContext ctx);
    }
}
