using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using TP04.Products;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Volo.Abp.Uow;
using Xunit;

namespace TP04.PantryItems
{
    /// <summary>
    /// TP08 / RF-15: pruebas de la regla de vencimientos SIN esperar el temporizador.
    ///
    /// Criterio de fecha: "hoy" se pasa fijo (Today). Las fechas son calendario
    /// (DateOnly), igual que en el código. Umbral por defecto: 3 días.
    ///
    /// Igual que ProductManager_Tests, la clase es abstracta y la ejecuta
    /// EfCoreExpirationWarningManager_Tests contra la base SQLite en memoria de la
    /// suite ABP (aislada y nueva para cada prueba), así también se prueba el
    /// índice único real.
    /// </summary>
    public abstract class ExpirationWarningManager_Tests<TStartupModule> : TP04DomainTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        private static readonly DateOnly Today = new DateOnly(2026, 11, 10);
        private static readonly Guid OwnerId = new Guid("2f3c9b4e-0d6a-4f7e-9a51-6c0e8b1d2a10");

        private readonly ExpirationWarningManager _expirationWarningManager;
        private readonly ProductManager _productManager;
        private readonly IRepository<Product, Guid> _productRepository;
        private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
        private readonly IRepository<PantryWarning, Guid> _pantryWarningRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        private int _nextBarcode = 1;

        protected ExpirationWarningManager_Tests()
        {
            _expirationWarningManager = GetRequiredService<ExpirationWarningManager>();
            _productManager = GetRequiredService<ProductManager>();
            _productRepository = GetRequiredService<IRepository<Product, Guid>>();
            _pantryItemRepository = GetRequiredService<IRepository<PantryItem, Guid>>();
            _pantryWarningRepository = GetRequiredService<IRepository<PantryWarning, Guid>>();
            _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        }

        // ---------------------------------------------------------------------
        // Umbral: dentro, día límite, fuera, hoy y vencido
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Item_Within_Threshold_Should_Get_Active_Warning()
        {
            var item = await CreateItemAsync(Today.AddDays(2));

            var result = await _expirationWarningManager.ProcessAsync(Today);

            result.Created.ShouldBe(1);
            var warning = await GetSingleWarningAsync(item.Id);
            warning.IsActive.ShouldBeTrue();
            warning.IsExpired.ShouldBeFalse();
            warning.Type.ShouldBe(PantryWarningType.Expiration);
            warning.ExpirationDate.ShouldBe(Today.AddDays(2));
            warning.OwnerId.ShouldBe(item.OwnerId); // propietario tomado del ítem persistido
        }

        [Fact]
        public async Task Item_On_Threshold_Limit_Day_Should_Get_Warning()
        {
            var item = await CreateItemAsync(Today.AddDays(ExpirationWarningConsts.DefaultThresholdDays));

            await _expirationWarningManager.ProcessAsync(Today);

            var warning = await GetSingleWarningAsync(item.Id);
            warning.IsActive.ShouldBeTrue();
            warning.IsExpired.ShouldBeFalse();
        }

        [Fact]
        public async Task Item_Outside_Threshold_Should_Not_Get_Warning()
        {
            var item = await CreateItemAsync(Today.AddDays(ExpirationWarningConsts.DefaultThresholdDays + 1));

            var result = await _expirationWarningManager.ProcessAsync(Today);

            result.TotalChanges.ShouldBe(0);
            (await GetWarningsAsync(item.Id)).ShouldBeEmpty();
        }

        [Fact]
        public async Task Item_Expiring_Today_Should_Get_Warning_Not_Yet_Expired()
        {
            var item = await CreateItemAsync(Today);

            await _expirationWarningManager.ProcessAsync(Today);

            var warning = await GetSingleWarningAsync(item.Id);
            warning.IsActive.ShouldBeTrue();
            warning.IsExpired.ShouldBeFalse();
        }

        [Fact]
        public async Task Expired_Item_Should_Get_Warning_Marked_As_Expired()
        {
            var item = await CreateItemAsync(Today.AddDays(-1));

            await _expirationWarningManager.ProcessAsync(Today);

            var warning = await GetSingleWarningAsync(item.Id);
            warning.IsActive.ShouldBeTrue();
            warning.IsExpired.ShouldBeTrue();
        }

        // ---------------------------------------------------------------------
        // Sin fecha y consumido
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Item_Without_Expiration_Date_Should_Not_Get_Warning()
        {
            var item = await CreateItemAsync(expirationDate: null);

            await _expirationWarningManager.ProcessAsync(Today);

            (await GetWarningsAsync(item.Id)).ShouldBeEmpty();
        }

        [Fact]
        public async Task Consumed_Item_Should_Not_Get_Warning()
        {
            var item = await CreateItemAsync(Today.AddDays(1), consumed: true);

            await _expirationWarningManager.ProcessAsync(Today);

            (await GetWarningsAsync(item.Id)).ShouldBeEmpty();
        }

        // ---------------------------------------------------------------------
        // Idempotencia
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Running_Twice_Should_Keep_A_Single_Warning_And_Change_Nothing()
        {
            var item = await CreateItemAsync(Today.AddDays(1));

            var first = await _expirationWarningManager.ProcessAsync(Today);
            var warningAfterFirstRun = await GetSingleWarningAsync(item.Id);

            var second = await _expirationWarningManager.ProcessAsync(Today);

            first.Created.ShouldBe(1);
            second.TotalChanges.ShouldBe(0);
            second.Unchanged.ShouldBe(1);

            var warningAfterSecondRun = await GetSingleWarningAsync(item.Id);
            warningAfterSecondRun.Id.ShouldBe(warningAfterFirstRun.Id);
            warningAfterSecondRun.ConcurrencyStamp.ShouldBe(warningAfterFirstRun.ConcurrencyStamp); // no se reescribió
        }

        [Fact]
        public async Task Two_Overlapping_Executions_Should_Not_Duplicate_The_Warning()
        {
            var item = await CreateItemAsync(Today.AddDays(1));
            Exception? overlappingSaveError = null;

            try
            {
                await WithUnitOfWorkAsync(async () =>
                {
                    // Ejecución A: lee (todavía no hay advertencia) y deja la inserción pendiente.
                    await _expirationWarningManager.ProcessAsync(Today);

                    // Ejecución B: empieza y termina mientras A sigue abierta.
                    using (var uowB = _unitOfWorkManager.Begin(requiresNew: true))
                    {
                        await _expirationWarningManager.ProcessAsync(Today);
                        await uowB.CompleteAsync();
                    }

                    // Al completarse A, el índice único rechaza la segunda advertencia.
                });
            }
            catch (Exception ex)
            {
                overlappingSaveError = ex;
            }

            overlappingSaveError.ShouldNotBeNull();
            (await GetWarningsAsync(item.Id)).Count.ShouldBe(1);

            // La ejecución siguiente encuentra la advertencia y no cambia nada.
            var next = await _expirationWarningManager.ProcessAsync(Today);
            next.TotalChanges.ShouldBe(0);
            (await GetWarningsAsync(item.Id)).Count.ShouldBe(1);
        }

        [Fact]
        public async Task Database_Should_Reject_A_Second_Warning_For_The_Same_Item_And_Type()
        {
            var item = await CreateItemAsync(Today.AddDays(1));

            await _pantryWarningRepository.InsertAsync(
                NewExpirationWarning(item), autoSave: true);

            Exception? duplicateError = null;
            try
            {
                await _pantryWarningRepository.InsertAsync(
                    NewExpirationWarning(item), autoSave: true);
            }
            catch (Exception ex)
            {
                duplicateError = ex; // violación del índice único (PantryItemId, Type)
            }

            duplicateError.ShouldNotBeNull();
            (await GetWarningsAsync(item.Id)).Count.ShouldBe(1);
        }

        // ---------------------------------------------------------------------
        // Cambio de fecha: actualización, desactivación y reactivación
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Changing_Date_Within_Threshold_Should_Update_The_Same_Warning()
        {
            var item = await CreateItemAsync(Today.AddDays(3));
            await _expirationWarningManager.ProcessAsync(Today);
            var original = await GetSingleWarningAsync(item.Id);

            await ChangeItemAsync(item.Id, i => i.ChangeExpirationDate(Today.AddDays(1)));
            var result = await _expirationWarningManager.ProcessAsync(Today);

            result.Updated.ShouldBe(1);
            result.Created.ShouldBe(0);
            var updated = await GetSingleWarningAsync(item.Id);
            updated.Id.ShouldBe(original.Id);
            updated.ExpirationDate.ShouldBe(Today.AddDays(1));
            updated.IsActive.ShouldBeTrue();
        }

        [Fact]
        public async Task Moving_Date_Out_And_Back_In_Should_Deactivate_And_Reactivate_The_Same_Warning()
        {
            var item = await CreateItemAsync(Today.AddDays(2));
            await _expirationWarningManager.ProcessAsync(Today);
            var original = await GetSingleWarningAsync(item.Id);

            // Fuera del umbral → se desactiva (no se borra).
            await ChangeItemAsync(item.Id, i => i.ChangeExpirationDate(Today.AddDays(20)));
            var deactivation = await _expirationWarningManager.ProcessAsync(Today);

            deactivation.Deactivated.ShouldBe(1);
            var inactive = await GetSingleWarningAsync(item.Id);
            inactive.Id.ShouldBe(original.Id);
            inactive.IsActive.ShouldBeFalse();

            // De nuevo dentro del umbral → se reactiva la MISMA advertencia.
            await ChangeItemAsync(item.Id, i => i.ChangeExpirationDate(Today.AddDays(1)));
            var reactivation = await _expirationWarningManager.ProcessAsync(Today);

            reactivation.Reactivated.ShouldBe(1);
            reactivation.Created.ShouldBe(0);
            var reactivated = await GetSingleWarningAsync(item.Id);
            reactivated.Id.ShouldBe(original.Id);
            reactivated.IsActive.ShouldBeTrue();
            reactivated.ExpirationDate.ShouldBe(Today.AddDays(1));
        }

        [Fact]
        public async Task Removing_The_Date_Should_Deactivate_The_Warning()
        {
            var item = await CreateItemAsync(Today.AddDays(1));
            await _expirationWarningManager.ProcessAsync(Today);

            await ChangeItemAsync(item.Id, i => i.ChangeExpirationDate(null));
            await _expirationWarningManager.ProcessAsync(Today);

            (await GetSingleWarningAsync(item.Id)).IsActive.ShouldBeFalse();
        }

        [Fact]
        public async Task Consuming_The_Item_Should_Deactivate_The_Warning()
        {
            var item = await CreateItemAsync(Today.AddDays(1));
            await _expirationWarningManager.ProcessAsync(Today);

            await ChangeItemAsync(item.Id, i => i.MarkAsConsumed());
            var result = await _expirationWarningManager.ProcessAsync(Today);

            result.Deactivated.ShouldBe(1);
            (await GetSingleWarningAsync(item.Id)).IsActive.ShouldBeFalse();
        }

        [Fact]
        public async Task When_The_Day_Passes_The_Same_Warning_Should_Be_Marked_As_Expired()
        {
            var item = await CreateItemAsync(Today);
            await _expirationWarningManager.ProcessAsync(Today);
            var original = await GetSingleWarningAsync(item.Id);
            original.IsExpired.ShouldBeFalse();

            var nextDay = await _expirationWarningManager.ProcessAsync(Today.AddDays(1));

            nextDay.Updated.ShouldBe(1);
            var updated = await GetSingleWarningAsync(item.Id);
            updated.Id.ShouldBe(original.Id);
            updated.IsExpired.ShouldBeTrue();
        }

        [Fact]
        public async Task Mixed_Items_Should_Only_Warn_The_Ones_That_Apply()
        {
            await CreateItemAsync(Today.AddDays(-2));                // vencido      → sí
            await CreateItemAsync(Today);                            // vence hoy    → sí
            await CreateItemAsync(Today.AddDays(3));                 // día límite   → sí
            await CreateItemAsync(Today.AddDays(4));                 // fuera        → no
            await CreateItemAsync(expirationDate: null);             // sin fecha    → no
            await CreateItemAsync(Today.AddDays(1), consumed: true); // consumido    → no

            var result = await _expirationWarningManager.ProcessAsync(Today);

            result.Created.ShouldBe(3);
            (await _pantryWarningRepository.GetCountAsync()).ShouldBe(3);
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------

        private async Task<PantryItem> CreateItemAsync(DateOnly? expirationDate, bool consumed = false)
        {
            var product = await _productManager.CreateAsync(NextBarcode(), "Producto de prueba");
            await _productRepository.InsertAsync(product, autoSave: true);

            var item = new PantryItem(Guid.NewGuid(), OwnerId, product.Id, expirationDate);
            if (consumed)
            {
                item.MarkAsConsumed();
            }

            return await _pantryItemRepository.InsertAsync(item, autoSave: true);
        }

        private Task ChangeItemAsync(Guid itemId, Action<PantryItem> change)
        {
            return WithUnitOfWorkAsync(async () =>
            {
                var item = await _pantryItemRepository.GetAsync(itemId);
                change(item);
                await _pantryItemRepository.UpdateAsync(item);
            });
        }

        private Task<List<PantryWarning>> GetWarningsAsync(Guid itemId)
        {
            return _pantryWarningRepository.GetListAsync(w => w.PantryItemId == itemId);
        }

        private async Task<PantryWarning> GetSingleWarningAsync(Guid itemId)
        {
            var warnings = await GetWarningsAsync(itemId);
            warnings.Count.ShouldBe(1);
            return warnings[0];
        }

        private static PantryWarning NewExpirationWarning(PantryItem item)
        {
            return new PantryWarning(
                Guid.NewGuid(),
                item.Id,
                item.OwnerId,
                PantryWarningType.Expiration,
                item.ExpirationDate!.Value,
                isExpired: false);
        }

        private string NextBarcode()
        {
            return "77900000090" + (_nextBarcode++).ToString("D2");
        }
    }
}
