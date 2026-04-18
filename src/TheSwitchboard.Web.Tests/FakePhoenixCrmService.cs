using System.Collections.Concurrent;
using TheSwitchboard.Web.Services;

namespace TheSwitchboard.Web.Tests;

public record FakePhoenixCall(string FormType, Dictionary<string, string> Data);

public class FakePhoenixCrmService : IPhoenixCrmService
{
    private readonly ConcurrentQueue<FakePhoenixCall> _calls = new();
    private int _failuresRemaining = 0;

    public int CallCount => _calls.Count;
    public FakePhoenixCall? LastPayload => _calls.LastOrDefault();

    public void Reset()
    {
        _calls.Clear();
        _failuresRemaining = 0;
    }

    public void NextResponsesReturn500(int count) => _failuresRemaining = count;

    public Task<bool> SendFormSubmissionAsync(string formType, Dictionary<string, string> data)
    {
        _calls.Enqueue(new FakePhoenixCall(formType, new Dictionary<string, string>(data)));
        if (_failuresRemaining > 0)
        {
            Interlocked.Decrement(ref _failuresRemaining);
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }
}
