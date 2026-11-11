using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace TP04.PantryItems
{
    /// <summary>
    /// Ítem de la despensa de un usuario.
    ///
    /// TP08: modelo mínimo para el worker de vencimientos (RF-15): propietario,
    /// producto, estado y fecha de vencimiento opcional.
    /// TP10: se agregan cantidad y unidad (opcionales) y la edición por el usuario
    /// (RF-11). La regla del worker y su idempotencia no cambian.
    ///
    /// Regla de negocio 3: cada ítem pertenece a un único usuario. El OwnerId se fija
    /// al crear el ítem y no se puede cambiar. Quien decide que el ítem es del
    /// usuario actual es el AppService (ICurrentUser), nunca el cliente.
    /// </summary>
    public class PantryItem : AuditedAggregateRoot<Guid>
    {
        /// <summary>Id del usuario dueño del ítem (AbpUsers.Id).</summary>
        public virtual Guid OwnerId { get; private set; }

        /// <summary>Producto guardado del catálogo interno (AppProducts.Id).</summary>
        public virtual Guid ProductId { get; private set; }

        public virtual PantryItemStatus Status { get; private set; }

        /// <summary>
        /// Fecha calendario de vencimiento, sin hora. Es opcional: hay productos
        /// sin fecha (RF-10 la pide de forma opcional).
        /// </summary>
        public virtual DateOnly? ExpirationDate { get; private set; }

        /// <summary>TP10: cantidad opcional, nunca negativa. Null = dato no informado (RF-09).</summary>
        public virtual decimal? Quantity { get; private set; }

        /// <summary>TP10: unidad opcional ("kg", "unidades"...). Null = dato no informado (RF-09).</summary>
        public virtual string? Unit { get; private set; }

        // Requerido por EF Core.
        protected PantryItem() { }

        public PantryItem(
            Guid id,
            Guid ownerId,
            Guid productId,
            DateOnly? expirationDate = null,
            decimal? quantity = null,
            string? unit = null
        ) : base(id)
        {
            if (ownerId == Guid.Empty)
            {
                throw new ArgumentException("El ítem de despensa debe tener un propietario.", nameof(ownerId));
            }

            if (productId == Guid.Empty)
            {
                throw new ArgumentException("El ítem de despensa debe referenciar un producto.", nameof(productId));
            }

            OwnerId = ownerId;
            ProductId = productId;
            ExpirationDate = expirationDate;
            Quantity = NormalizeQuantity(quantity);
            Unit = NormalizeUnit(unit);
            Status = PantryItemStatus.Active;
        }

        /// <summary>
        /// TP10 (RF-11): actualiza cantidad, unidad y fecha de vencimiento.
        ///
        /// Primero valida todos los valores y recién después asigna: si algo es
        /// inválido, la entidad no queda modificada a medias. Un ítem consumido es
        /// historial (regla de negocio 6) y ya no se edita.
        /// </summary>
        public void UpdateDetails(decimal? quantity, string? unit, DateOnly? expirationDate)
        {
            if (Status == PantryItemStatus.Consumed)
            {
                throw new BusinessException(TP04DomainErrorCodes.PantryItemAlreadyConsumed);
            }

            var normalizedQuantity = NormalizeQuantity(quantity);
            var normalizedUnit = NormalizeUnit(unit);

            Quantity = normalizedQuantity;
            Unit = normalizedUnit;
            ExpirationDate = expirationDate;
        }

        /// <summary>
        /// Cambia (o quita, con null) la fecha de vencimiento. El worker detecta el
        /// cambio en su próxima ejecución y actualiza o desactiva la advertencia.
        /// </summary>
        public void ChangeExpirationDate(DateOnly? expirationDate)
        {
            ExpirationDate = expirationDate;
        }

        /// <summary>
        /// Marca el ítem como consumido. No se elimina (regla de negocio 6) y deja
        /// de generar advertencias de vencimiento. Marcarlo dos veces no cambia nada.
        /// </summary>
        public void MarkAsConsumed()
        {
            Status = PantryItemStatus.Consumed;
        }

        private static decimal? NormalizeQuantity(decimal? quantity)
        {
            if (quantity < 0)
            {
                throw new BusinessException(TP04DomainErrorCodes.PantryItemNegativeQuantity);
            }

            return quantity;
        }

        private static string? NormalizeUnit(string? unit)
        {
            // Unidad vacía o con espacios = dato no informado.
            return string.IsNullOrWhiteSpace(unit)
                ? null
                : Check.Length(unit.Trim(), nameof(unit), PantryItemConsts.MaxUnitLength);
        }
    }
}
