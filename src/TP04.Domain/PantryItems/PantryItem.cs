using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TP04.PantryItems
{
    public class PantryItem : AuditedAggregateRoot<Guid>
    {
        public Guid OwnerId { get; set; }
        public Guid ProductId { get; set; }
        public string Name { get; set; }
        public decimal? Quantity { get; set; }
        public string Unit { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public PantryItemStatus Status { get; set; }

        protected PantryItem()
        {
        }

        // 1. Constructor de 3 argumentos: (id, ownerId, productId)
        public PantryItem(Guid id, Guid ownerId, Guid productId) : base(id)
        {
            OwnerId = ownerId;
            ProductId = productId;
            Status = PantryItemStatus.Active;
        }

        // 2. Constructor de 4 argumentos: (id, ownerId, productId, quantity)
        public PantryItem(Guid id, Guid ownerId, Guid productId, decimal? quantity) : base(id)
        {
            OwnerId = ownerId;
            ProductId = productId;
            Quantity = quantity;
            Status = PantryItemStatus.Active;
        }

        // 3. Constructor de 4 argumentos donde el 3ro es Guid y el 4to es string (unit/name)
        public PantryItem(Guid id, Guid ownerId, Guid productId, string unitOrName) : base(id)
        {
            OwnerId = ownerId;
            ProductId = productId;
            Unit = unitOrName;
            Status = PantryItemStatus.Active;
        }

        // Constructor de 4 argumentos con la fecha de vencimiento (DateOnly?) en el 4to lugar
        public PantryItem(Guid id, Guid ownerId, Guid productId, DateOnly? expirationDate) : base(id)
        {
            OwnerId = ownerId;
            ProductId = productId;
            ExpirationDate = expirationDate;
            Status = PantryItemStatus.Active;
        }

        // 5. Variante con decimal? para las líneas 44 y 55 por si pasan cantidad nula/decimal
        public PantryItem(Guid id, Guid ownerId, Guid productId, decimal? quantity, string unit) : base(id)
        {
            OwnerId = ownerId;
            ProductId = productId;
            Quantity = quantity;
            Unit = unit;
            Status = PantryItemStatus.Active;
        }

        // 6. Constructor completo estándar para servicios de aplicación
        public PantryItem(Guid id, Guid ownerId, string name, decimal? quantity, string unit, DateOnly? expirationDate = null) : base(id)
        {
            OwnerId = ownerId;
            Name = name;
            Quantity = quantity;
            Unit = unit;
            ExpirationDate = expirationDate;
            Status = PantryItemStatus.Active;
        }

        // 7. Constructor completo para Seeders y Managers
        public PantryItem(Guid id, Guid ownerId, Guid productId, string name, decimal? quantity, string unit, DateOnly? expirationDate = null) : base(id)
        {
            OwnerId = ownerId;
            ProductId = productId;
            Name = name;
            Quantity = quantity;
            Unit = unit;
            ExpirationDate = expirationDate;
            Status = PantryItemStatus.Active;
        }

        public void MarkAsConsumed()
        {
            Status = PantryItemStatus.Consumed;
        }

        public void UpdateDetails(decimal quantity, string unit, DateOnly? expirationDate)
        {
            Quantity = quantity;
            Unit = unit;
            ExpirationDate = expirationDate;
        }

        public void UpdateDetails(decimal? quantity, string unit, DateOnly? expirationDate)
        {
            Quantity = quantity;
            Unit = unit;
            ExpirationDate = expirationDate;
        }

        public void ChangeExpirationDate(DateOnly? expirationDate)
        {
            ExpirationDate = expirationDate;
        }
    }
}