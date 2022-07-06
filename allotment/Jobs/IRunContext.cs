namespace Allotment.Jobs
{
    public interface IRunContext
    {
        CancellationToken CancellationToken { get; }
        void RunAgainAt(DateTime nextRunUtc, CancellationToken cancellationToken = default); 
        void RunAgainIn(TimeSpan duration, CancellationToken cancellationToken = default);
    }
}
