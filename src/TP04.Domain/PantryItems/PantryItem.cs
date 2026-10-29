using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TP04.PantryItems
{
    /// <summary>
    /// TP08: modelo MÍNIMO de ítem de despensa, sólo lo necesario para que el
    /// worker de vencimientos (RF-15) procese datos propios del sistema:
    /// propietario, producto, estado y fecha de vencimiento opcional.
    ///
    /// Cantidad, unidad y la administración por el usuario (RF-10 / RF-11) llegan
    /// en un TP posterior, junto con la seguridad. Por eso todavía NO hay una API
    /// pública que permita crear ítems ni elegir/modificar su propietario.
    ///
    /// Regla de negocio 3: cada ítem pertenece a un único usuario. El OwnerId es
    /// un dato persistido; nunca se toma del usuario HTTP actual (ICurrentUser).
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

        // Requerido por EF Core.
        protected PantryItem() { }

        public PantryItem(
            Guid id,
            Guid ownerId,
            Guid productId,
            DateOnly? expirationDate = null
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
            Status = PantryItemStatus.Active;
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
        /// de generar advertencias de vencimiento.
        /// </summary>
        public void MarkAsConsumed()
        {
            Status = PantryItemStatus.Consumed;
        }
    }
}
