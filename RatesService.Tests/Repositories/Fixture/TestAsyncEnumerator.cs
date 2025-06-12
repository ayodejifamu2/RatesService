namespace RatesService.Tests.Repositories.Fixture;

public class TestAsyncEnumerable<T> : IAsyncEnumerable<T>
{
    private readonly IEnumerable<T> _enumerable;
    public TestAsyncEnumerable(IEnumerable<T> enumerable) => _enumerable = enumerable;
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new TestAsyncEnumerator<T>(_enumerable.GetEnumerator());
}

public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _enumerator;
    public TestAsyncEnumerator(IEnumerator<T> enumerator) => _enumerator = enumerator;
    public T Current => _enumerator.Current;
    public ValueTask DisposeAsync()
    {
        _enumerator.Dispose();
        return new ValueTask();
    }
    public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_enumerator.MoveNext());
}