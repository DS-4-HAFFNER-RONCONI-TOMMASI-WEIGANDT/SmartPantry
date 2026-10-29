using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TP04.PantryItems
{
    /// <summary>
    /// TP08 / RF-15: advertencia persistida sobre un ítem de despensa.
    ///
    /// Clave lógica estable: (PantryItemId, Type). Hay a lo sumo UNA advertencia
    /// por ítem y tipo; la base lo refuerza con un índice único. Cuando la
    /// advertencia deja de corresponder no se borra: se desactiva (IsActive =
    /// false) y, si vuelve a corresponder, se reactiva la MISMA fila.
    ///
    /// Constructor y métodos de cambio son internal: la única forma de crearla o
    /// modificarla es ExpirationWarningManager, que aplica la regla completa.
    /// </summary>
    public class PantryWarning : AuditedAggregateRoot<Guid>
    {
        public virtual Guid PantryItemId { get; private set; }

        /// <summary>
        /// Copia del propietario del ítem (dato persistido, no del usuario HTTP).
        /// Permite que, cuando exista la API privada, cada usuario consulte sólo
        /// sus advertencias sin hacer join con los ítems.
        /// </summary>
        public virtual Guid OwnerId { get; private set; }

        public virtual PantryWarningType Type { get; private set; }

        /// <summary>Fecha de vencimiento sobre la que se está avisando.</summary>
        public virtual DateOnly ExpirationDate { get; private set; }

        /// <summary>true si la fecha ya pasó ("vencido"); false si está "por vencer".</summary>
        public virtual bool IsExpired { get; private set; }

        public virtual bool IsActive { get; private set; }

        // Requerido por EF Core.
        protected PantryWarning() { }

        internal PantryWarning(
            Guid id,
            Guid pantryItemId,
            Guid ownerId,
            PantryWarningType type,
            DateOnly expirationDate,
            bool isExpired
        ) : base(id)
        {
            PantryItemId = pantryItemId;
            OwnerId = ownerId;
            Type = type;
            ExpirationDate = expirationDate;
            IsExpired = isExpired;
            IsActive = true;
        }

        /// <summary>
        /// Deja la advertencia activa con los datos actuales del ítem. Devuelve
        /// false si ya estaba exactamente así: en ese caso no hay nada que
        /// escribir y repetir el proceso no modifica la base (idempotencia).
        /// </summary>
        internal bool Refresh(DateOnly expirationDate, bool isExpired)
        {
            if (IsActive && ExpirationDate == expirationDate && IsExpired == isExpired)
            {
                return false;
            }

            ExpirationDate = expirationDate;
            IsExpired = isExpired;
            IsActive = true;
            return true;
        }

        /// <summary>
        /// Desactiva la advertencia porque dejó de corresponder (fecha fuera del
        /// umbral, sin fecha o ítem consumido). Devuelve false si ya estaba inactiva.
        /// </summary>
        internal bool Deactivate()
        {
            if (!IsActive)
            {
                return false;
            }

            IsActive = false;
            return true;
        }
    }
}
