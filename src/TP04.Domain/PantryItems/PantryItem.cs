using System;
using Volo.Abp;
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
            SetQuantity(quantity);
            Status = PantryItemStatus.Active;
        }

        // 3. (id, ownerId, productId, unit)
        public PantryItem(Guid id, Guid ownerId, Guid productId, string? unit) : base(id)
        {
            OwnerId = ownerId;
            ProductId = productId;
            SetUnit(unit);
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
            SetQuantity(quantity);
            SetUnit(unit);
            Status = PantryItemStatus.Active;
        }

        // 6. Completo: para seeders, managers y servicios
        public PantryItem(Guid id, Guid ownerId, Guid productId, decimal? quantity, string? unit, DateOnly? expirationDate = null) : base(id)
        {
            OwnerId = ownerId;
            ProductId = productId;
            SetQuantity(quantity);
            SetUnit(unit);
            ExpirationDate = expirationDate;
            Status = PantryItemStatus.Active;
        }

        public void MarkAsConsumed()
        {
            Status = PantryItemStatus.Consumed;
        }

        public void UpdateDetails(decimal quantity, string? unit, DateOnly? expirationDate)
        {
            UpdateDetails((decimal?)quantity, unit, expirationDate);
        }

        public void UpdateDetails(decimal? quantity, string? unit, DateOnly? expirationDate)
        {
            if (Status == PantryItemStatus.Consumed)
            {
                throw new BusinessException("TP04:PantryItem:AlreadyConsumed")
                    .WithData("Id", Id);
            }

            SetQuantity(quantity);
            SetUnit(unit);
            ExpirationDate = expirationDate;
        }

        public void ChangeExpirationDate(DateOnly? expirationDate)
        {
            if (Status == PantryItemStatus.Consumed)
            {
                throw new BusinessException("TP04:PantryItem:AlreadyConsumed")
                    .WithData("Id", Id);
            }

            ExpirationDate = expirationDate;
        }

        private void SetQuantity(decimal? quantity)
        {
            if (quantity.HasValue && quantity.Value < 0)
            {
                throw new BusinessException("TP04:PantryItem:NegativeQuantity");
            }

            Quantity = quantity;
        }

        private void SetUnit(string? unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
            {
                Unit = null;
                return;
            }

            var trimmedUnit = unit.Trim();
            Check.Length(trimmedUnit, nameof(unit), PantryItemConsts.MaxUnitLength);
            Unit = trimmedUnit;
        }
    }
}