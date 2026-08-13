namespace QuickSnip;

internal sealed class CaptureGate : IDisposable
{
    private const string SemaphoreName = "Local\\QuickSnip.CaptureGate";
    private readonly Semaphore _semaphore;
    private bool _ownsGate;

    private CaptureGate(Semaphore semaphore)
    {
        _semaphore = semaphore;
        _ownsGate = true;
    }

    public static CaptureGate? TryEnter()
    {
        var semaphore = new Semaphore(1, 1, SemaphoreName);

        if (!semaphore.WaitOne(0))
        {
            semaphore.Dispose();
            return null;
        }

        return new CaptureGate(semaphore);
    }

    public void Dispose()
    {
        if (_ownsGate)
        {
            _semaphore.Release();
            _ownsGate = false;
        }

        _semaphore.Dispose();
    }
}
