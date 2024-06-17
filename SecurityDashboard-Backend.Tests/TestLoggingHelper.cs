using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SecurityDashboard_Backend.Tests;

public static class TestLoggingHelper
{
    public static ILogger<T> CreateLogger<T>(this ITestOutputHelper testOutputHelper)
    {
        return new TestOutputLogger<T>(testOutputHelper);
    }
}

[ExcludeFromCodeCoverage(Justification = "Test helper")]
internal class TestOutputLogger<T> : ILogger<T>
{
    private readonly ITestOutputHelper _testOutputHelper;

    internal TestOutputLogger(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        // Scopes not supported
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        // Ignore Levels Debug and Trace
        return logLevel >= LogLevel.Information;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        _testOutputHelper.WriteLine("[{0}] {1}", logLevel, formatter(state, exception));
    }
}
