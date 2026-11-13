using System;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace TP04.PantryItems
{
    /// <summary>
    /// TP10: reglas de la entidad PantryItem para cantidad, unidad y edición (RF-11).
    /// Son pruebas unitarias puras: no usan base de datos ni contenedor de ABP.
    /// </summary>
    public class PantryItem_Tests
    {
        private static readonly Guid OwnerId = Guid.NewGuid();
        private static readonly Guid ProductId = Guid.NewGuid();

        [Fact]
        public void New_Item_Without_Optional_Data_Should_Keep_Nulls()
        {
            // RF-09: lo que no se informa queda en null, no se inventa un valor.
            var item = new PantryItem(Guid.NewGuid(), OwnerId, ProductId);

            item.Quantity.ShouldBeNull();
            item.Unit.ShouldBeNull();
            item.ExpirationDate.ShouldBeNull();
            item.Status.ShouldBe(PantryItemStatus.Active);
        }

        [Fact]
        public void UpdateDetails_Should_Change_Quantity_Unit_And_Date()
        {
            var item = new PantryItem(Guid.NewGuid(), OwnerId, ProductId);

            item.UpdateDetails(2.5m, "  kg  ", new DateOnly(2026, 12, 1));

            item.Quantity.ShouldBe(2.5m);
            item.Unit.ShouldBe("kg"); // se normaliza con Trim
            item.ExpirationDate.ShouldBe(new DateOnly(2026, 12, 1));
        }

        [Fact]
        public void UpdateDetails_With_Blank_Unit_Should_Store_Null()
        {
            var item = new PantryItem(Guid.NewGuid(), OwnerId, ProductId, quantity: 1, unit: "kg");

            item.UpdateDetails(1, "   ", null);

            item.Unit.ShouldBeNull();
            item.ExpirationDate.ShouldBeNull();
        }

        [Fact]
        public void UpdateDetails_With_Negative_Quantity_Should_Throw_And_Not_Change_Item()
        {
            var item = new PantryItem(Guid.NewGuid(), OwnerId, ProductId, quantity: 3, unit: "u");

            var ex = Should.Throw<BusinessException>(() => item.UpdateDetails(-1, "kg", null));

            ex.Code.ShouldBe(TP04DomainErrorCodes.PantryItemNegativeQuantity);
            item.Quantity.ShouldBe(3m);
            item.Unit.ShouldBe("u");
        }

        [Fact]
        public void UpdateDetails_With_Too_Long_Unit_Should_Throw()
        {
            var item = new PantryItem(Guid.NewGuid(), OwnerId, ProductId);
            var tooLong = new string('x', PantryItemConsts.MaxUnitLength + 1);

            Should.Throw<ArgumentException>(() => item.UpdateDetails(1, tooLong, null));
        }

        [Fact]
        public void Consumed_Item_Should_Not_Be_Edited()
        {
            // Regla de negocio 6: el consumido queda como historial.
            var item = new PantryItem(Guid.NewGuid(), OwnerId, ProductId, quantity: 1);
            item.MarkAsConsumed();

            var ex = Should.Throw<BusinessException>(() => item.UpdateDetails(5, "kg", null));

            ex.Code.ShouldBe(TP04DomainErrorCodes.PantryItemAlreadyConsumed);
            item.Quantity.ShouldBe(1m);
            item.Status.ShouldBe(PantryItemStatus.Consumed);
        }
    }
}
