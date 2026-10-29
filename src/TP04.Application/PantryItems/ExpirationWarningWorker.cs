using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace TP04.PantryItems
{
    /// <summary>
    /// TP08 / RF-15: worker periódico de vencimientos.
    ///
    /// Sólo decide CUÁNDO ejecutar; QUÉ hacer lo decide ExpirationWarningManager.
    ///  - Es singleton: no guarda repositorios ni servicios scoped. En cada
    ///    ejecución resuelve el servicio desde workerContext.ServiceProvider, que es
    ///    el alcance (scope) creado por ABP para esa ejecución.
    ///  - Abre una unidad de trabajo nueva y transaccional para las escrituras.
    ///  - Si algo falla, registra el error y la siguiente ejecución vuelve a
    ///    intentar (el timer de ABP no se detiene).
    ///  - Un mismo worker no se superpone consigo mismo: AbpAsyncTimer espera a que
    ///    termine una ejecución antes de programar la siguiente. Entre instancias
    ///    distintas, la protección es el índice único de la base.
    ///  - No usa ICurrentUser, ni llama a Open Food Facts, ni envía correos.
    /// </summary>
    public class ExpirationWarningWorker : AsyncPeriodicBackgroundWorkerBase
    {
        private readonly ExpirationWarningOptions _options;

        public ExpirationWarningWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<ExpirationWarningOptions> options)
            : base(timer, serviceScopeFactory)
        {
            _options = options.Value;

            var periodMinutes = _options.WorkerPeriodMinutes > 0
                ? _options.WorkerPeriodMinutes
                : ExpirationWarningConsts.DefaultWorkerPeriodMinutes;

            Timer.Period = (int)TimeSpan.FromMinutes(periodMinutes).TotalMilliseconds;
            Timer.RunOnStart = true; // primera ejecución al iniciar la aplicación
        }

        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            var today = ExpirationCalendar.GetToday(DateTime.UtcNow, _options.TimeZoneId);

            try
            {
                var unitOfWorkManager = workerContext.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var expirationWarningManager = workerContext.ServiceProvider.GetRequiredService<ExpirationWarningManager>();

                using (var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true))
                {
                    var result = await expirationWarningManager.ProcessAsync(today);
                    await uow.CompleteAsync();

                    Logger.LogInformation(
                        "Worker de vencimientos ({Today}): {Result}",
                        today.ToString("yyyy-MM-dd"),
                        result);
                }
            }
            catch (Exception ex)
            {
                // La unidad de trabajo no se completó, así que no queda nada a medias.
                Logger.LogError(ex, "Error al procesar vencimientos del {Today}. Se reintentará en la próxima ejecución.",
                    today.ToString("yyyy-MM-dd"));
            }
        }
    }
}
