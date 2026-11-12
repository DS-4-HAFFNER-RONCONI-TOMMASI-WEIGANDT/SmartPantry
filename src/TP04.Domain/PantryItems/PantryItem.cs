using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace TP04.PantryItems
{
    public class PantryItem : AuditedAggregateRoot<Guid>
    {
        public Guid OwnerId { get; set; }
        public Guid ProductId { get; set; }
        public decimal? Quantity { get; set; }
        public string? Unit { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public PantryItemStatus Status { get; set; }

        protected PantryItem()
        {
        }

        // 1. (id, ownerId, productId)
        public PantryItem(Guid id, Guid ownerId, Guid productId) : base(id)
        {
            OwnerId = ownerId;
            ProductId = productId;
            Status = PantryItemStatus.Active;
        }

        // 2. (id, ownerId, productId, quantity)
        public PantryItem(Guid id, Guid ownerId, Guid productId, decimal? quantity) : base(id)
        {
            OwnerId = ownerId;
            ProductId = productId;
            Quantity = quantity;
            Status = PantryItemStatus.Active;
        }

        // 3. (id, ownerId, productId, unit)
        public PantryItem(Guid id, Guid ownerId, Guid productId, string? unit) : base(id)
        {
            OwnerId = ownerId;
            ProductId = productId;
            Unit = unit;
            Status = PantryItemStatus.Active;
        }

        // 4. (id, ownerId, productId, expirationDate)
        public PantryItem(Guid id, Guid ownerId, Guid productId, DateOnly? expirationDate) : base(id)
        {
            OwnerId = ownerId;
            ProductId = productId;
            ExpirationDate = expirationDate;
            Status = PantryItemStatus.Active;
        }

        // 5. (id, ownerId, productId, quantity, unit)
        public PantryItem(Guid id, Guid ownerId, Guid productId, decimal? quantity, string? unit) : base(id)
        {
            OwnerId = ownerId;
            ProductId = productId;
            Quantity = quantity;
            Unit = unit;
            Status = PantryItemStatus.Active;
        }

        // 6. Completo: para seeders, managers y servicios
        public PantryItem(Guid id, Guid ownerId, Guid productId, decimal? quantity, string? unit, DateOnly? expirationDate = null) : base(id)
        {
            OwnerId = ownerId;
            ProductId = productId;
            Quantity = quantity;
            Unit = unit;
            ExpirationDate = expirationDate;
            Status = PantryItemStatus.Active;
        }

        public void MarkAsConsumed()
        {
            Status = PantryItemStatus.Consumed;
        }

        public void UpdateDetails(decimal quantity, string? unit, DateOnly? expirationDate)
        {
            Quantity = quantity;
            Unit = unit;
            ExpirationDate = expirationDate;
        }

        public void UpdateDetails(decimal? quantity, string? unit, DateOnly? expirationDate)
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