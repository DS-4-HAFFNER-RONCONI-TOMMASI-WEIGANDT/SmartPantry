using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace TP04.ExternalProducts;

/// <summary>
/// TP final (HU-05): pruebas unitarias del registro por defecto de llamadas externas.
/// El logger es un mock de NSubstitute; no se levanta el módulo de la aplicación.
/// </summary>
public class LoggingExternalCallRecorder_Tests
{
    private readonly ILogger<LoggingExternalCallRecorder> _logger;
    private readonly LoggingExternalCallRecorder _recorder;

    public LoggingExternalCallRecorder_Tests()
    {
        _logger = Substitute.For<ILogger<LoggingExternalCallRecorder>>();
        _recorder = new LoggingExternalCallRecorder(_logger);
    }

    [Fact]
    public async Task RecordAsync_Should_Write_Information_Log_When_Called()
    {
        await _recorder.RecordAsync(NewRecord());

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<Arg.AnyType>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<Arg.AnyType, Exception?, string>>());
    }

    [Fact]
    public async Task RecordAsync_Should_Not_Throw_When_Logger_Fails()
    {
        _logger
            .When(l => l.Log(
                Arg.Any<LogLevel>(),
                Arg.Any<EventId>(),
                Arg.Any<Arg.AnyType>(),
                Arg.Any<Exception?>(),
                Arg.Any<Func<Arg.AnyType, Exception?, string>>()))
            .Do(_ => throw new InvalidOperationException("Falla simulada del logger"));

        await Should.NotThrowAsync(() => _recorder.RecordAsync(NewRecord()));
    }

    private static ExternalCallRecord NewRecord()
    {
        return new ExternalCallRecord
        {
            Operation = ExternalCallOperation.BarcodeLookup,
            Outcome = ExternalCallOutcome.Success,
            DurationMs = 120,
            OccurredAt = new DateTime(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc)
        };
    }
}
