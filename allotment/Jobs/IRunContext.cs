namespace Allotment.Jobs
{
    public interface IRunContext
    {
        void RunAgainAt(DateTime nextRunUtc);
        void RunAgainIn(TimeSpan duration);
    }
}
