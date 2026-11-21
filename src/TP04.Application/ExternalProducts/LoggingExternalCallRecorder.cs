using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace TP04.ExternalProducts;

/// <summary>
/// TP final (HU-05): implementación por defecto de <see cref="IExternalCallRecorder"/>.
/// Escribe un log estructurado con operación, resultado y duración (regla de negocio 5:
/// sin datos del usuario ni del pedido).
///
/// ABP la registra sola como IExternalCallRecorder (ITransientDependency). El Frente 4
/// la reemplaza por una que persiste, con [Dependency(ReplaceServices = true)].
/// </summary>
public class LoggingExternalCallRecorder : IExternalCallRecorder, ITransientDependency
{
    private readonly ILogger<LoggingExternalCallRecorder> _logger;

    public LoggingExternalCallRecorder(ILogger<LoggingExternalCallRecorder> logger)
    {
        _logger = logger;
    }

    public Task RecordAsync(ExternalCallRecord record)
    {
        try
        {
            _logger.LogInformation(
                "Llamada externa {Operation}: {Outcome} en {DurationMs} ms ({OccurredAt:O})",
                record.Operation,
                record.Outcome,
                record.DurationMs,
                record.OccurredAt);
        }
        catch (Exception)
        {
            // Las métricas nunca rompen la consulta del usuario: si falla el log,
            // se pierde este registro y la búsqueda sigue normalmente.
        }

        return Task.CompletedTask;
    }
}
