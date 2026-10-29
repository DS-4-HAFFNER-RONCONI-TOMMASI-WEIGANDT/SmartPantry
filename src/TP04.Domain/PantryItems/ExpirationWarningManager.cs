using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;

namespace TP04.PantryItems
{
    /// <summary>
    /// &lt;&lt;DomainService&gt;&gt; RF-15: decide QUÉ advertencias de vencimiento crear,
    /// actualizar, reactivar o desactivar. El worker sólo decide CUÁNDO ejecutarlo,
    /// así la regla se prueba con una fecha controlada sin esperar el temporizador.
    ///
    /// Regla (umbral U = ThresholdDays, por defecto 3; días = vencimiento - hoy):
    ///  - ítem activo con fecha y días &lt;= U  → advertencia activa.
    ///      * incluye el día límite (días == U), el mismo día (días == 0)
    ///        y los ya vencidos (días &lt; 0, IsExpired = true).
    ///  - días &gt; U, sin fecha o consumido     → la advertencia (si existe) se desactiva.
    ///
    /// Idempotencia:
    ///  - Clave lógica (PantryItemId, Type): se busca la advertencia existente y se
    ///    reutiliza; nunca se crea una segunda.
    ///  - Si la advertencia ya está como debe, no se escribe nada.
    ///  - El índice único de la base impide el duplicado aunque dos ejecuciones
    ///    cercanas intenten crearla a la vez: la segunda falla al guardar, su
    ///    unidad de trabajo se revierte y la siguiente ejecución la encuentra.
    ///
    /// No usa ICurrentUser: en segundo plano no hay sesión HTTP. El propietario de
    /// la advertencia se copia del ítem persistido.
    /// </summary>
    public class ExpirationWarningManager : DomainService
    {
        private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
        private readonly IRepository<PantryWarning, Guid> _pantryWarningRepository;
        private readonly IOptions<ExpirationWarningOptions> _options;

        public ExpirationWarningManager(
            IRepository<PantryItem, Guid> pantryItemRepository,
            IRepository<PantryWarning, Guid> pantryWarningRepository,
            IOptions<ExpirationWarningOptions> options)
        {
            _pantryItemRepository = pantryItemRepository;
            _pantryWarningRepository = pantryWarningRepository;
            _options = options;
        }

        /// <summary>
        /// Procesa todos los ítems con la fecha "hoy" recibida. [UnitOfWork]: si se
        /// llama fuera de una unidad de trabajo abre una propia; si el worker ya
        /// abrió una, participa de ella y todo se guarda junto al completarla.
        /// </summary>
        [UnitOfWork]
        public virtual async Task<ExpirationWarningProcessResult> ProcessAsync(
            DateOnly today,
            CancellationToken cancellationToken = default)
        {
            var thresholdDays = _options.Value.ThresholdDays;
            if (thresholdDays < 0)
            {
                throw new InvalidOperationException("ExpirationWarnings:ThresholdDays no puede ser negativo.");
            }

            var limitDate = today.AddDays(thresholdDays);

            // 1) Ítems que deben tener advertencia activa.
            var itemQuery = await _pantryItemRepository.GetQueryableAsync();
            var dueItems = await AsyncExecuter.ToListAsync(
                itemQuery.Where(i =>
                    i.Status == PantryItemStatus.Active &&
                    i.ExpirationDate != null &&
                    i.ExpirationDate <= limitDate),
                cancellationToken);

            // 2) Advertencias de vencimiento existentes (una por ítem, por el índice único).
            var warningQuery = await _pantryWarningRepository.GetQueryableAsync();
            var existingWarnings = await AsyncExecuter.ToListAsync(
                warningQuery.Where(w => w.Type == PantryWarningType.Expiration),
                cancellationToken);

            var warningsByItem = existingWarnings.ToDictionary(w => w.PantryItemId);
            var dueItemIds = new HashSet<Guid>();
            var result = new ExpirationWarningProcessResult();

            // 3) Crear / actualizar / reactivar las que corresponden.
            foreach (var item in dueItems)
            {
                dueItemIds.Add(item.Id);

                var expirationDate = item.ExpirationDate!.Value;
                var isExpired = expirationDate < today;

                if (!warningsByItem.TryGetValue(item.Id, out var warning))
                {
                    warning = new PantryWarning(
                        GuidGenerator.Create(),
                        item.Id,
                        item.OwnerId,
                        PantryWarningType.Expiration,
                        expirationDate,
                        isExpired);

                    await _pantryWarningRepository.InsertAsync(warning, cancellationToken: cancellationToken);
                    result.Created++;
                    continue;
                }

                var wasActive = warning.IsActive;
                if (!warning.Refresh(expirationDate, isExpired))
                {
                    result.Unchanged++;
                    continue;
                }

                await _pantryWarningRepository.UpdateAsync(warning, cancellationToken: cancellationToken);
                if (wasActive)
                {
                    result.Updated++;
                }
                else
                {
                    result.Reactivated++;
                }
            }

            // 4) Desactivar las que dejaron de corresponder (fuera de umbral, sin fecha,
            //    consumido o ítem eliminado de la lista de candidatos).
            foreach (var warning in existingWarnings.Where(w => !dueItemIds.Contains(w.PantryItemId)))
            {
                if (warning.Deactivate())
                {
                    await _pantryWarningRepository.UpdateAsync(warning, cancellationToken: cancellationToken);
                    result.Deactivated++;
                }
                else
                {
                    result.Unchanged++;
                }
            }

            return result;
        }
    }
}
