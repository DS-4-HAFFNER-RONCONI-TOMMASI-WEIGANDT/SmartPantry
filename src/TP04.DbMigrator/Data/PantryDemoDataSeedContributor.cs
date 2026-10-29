using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TP04.PantryItems;
using TP04.Products;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;

namespace TP04.DbMigrator.Data
{
    /// <summary>
    /// TP08: datos de ejemplo LOCALES para ver el worker de vencimientos en acción.
    ///
    /// Vive en el proyecto DbMigrator (y no en Domain) a propósito:
    ///  - sólo corre cuando se ejecuta el migrador en la máquina de desarrollo;
    ///  - no lo carga la suite de pruebas, así los tests arrancan con la base vacía.
    ///
    /// No existe todavía API pública para crear ítems ni para elegir su propietario
    /// (eso llega con Seguridad). El propietario se toma de un dato persistido: el
    /// usuario "admin" que crea ABP al sembrar Identity.
    ///
    /// Es idempotente: si ya hay ítems de despensa, no hace nada.
    /// </summary>
    public class PantryDemoDataSeedContributor : IDataSeedContributor, ITransientDependency
    {
        private const string OwnerUserName = "admin";

        public ILogger<PantryDemoDataSeedContributor> Logger { get; set; }

        private readonly IRepository<Product, Guid> _productRepository;
        private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
        private readonly ProductManager _productManager;
        private readonly IIdentityUserRepository _userRepository;
        private readonly IGuidGenerator _guidGenerator;

        public PantryDemoDataSeedContributor(
            IRepository<Product, Guid> productRepository,
            IRepository<PantryItem, Guid> pantryItemRepository,
            ProductManager productManager,
            IIdentityUserRepository userRepository,
            IGuidGenerator guidGenerator)
        {
            _productRepository = productRepository;
            _pantryItemRepository = pantryItemRepository;
            _productManager = productManager;
            _userRepository = userRepository;
            _guidGenerator = guidGenerator;

            Logger = NullLogger<PantryDemoDataSeedContributor>.Instance;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            // Sólo para el host (sin multi-tenancy).
            if (context.TenantId != null)
            {
                return;
            }

            if (await _pantryItemRepository.GetCountAsync() > 0)
            {
                return;
            }

            var owner = await _userRepository.FindByNormalizedUserNameAsync(OwnerUserName.ToUpperInvariant());
            if (owner == null)
            {
                Logger.LogWarning("No se encontró el usuario '{UserName}'; no se cargan ítems de despensa de ejemplo.", OwnerUserName);
                return;
            }

            // Fecha local de la máquina que corre el migrador (Argentina). Sólo se
            // usa para que los ejemplos queden alrededor de "hoy".
            var today = DateOnly.FromDateTime(DateTime.Now);

            // Un caso de cada tipo que distingue la regla (umbral = 3 días).
            await AddItemAsync(owner.Id, "7790000008001", "Leche entera (vence hoy)", today);
            await AddItemAsync(owner.Id, "7790000008002", "Yogur (vence en 2 días)", today.AddDays(2));
            await AddItemAsync(owner.Id, "7790000008003", "Queso (vence en 3 días, día límite)", today.AddDays(3));
            await AddItemAsync(owner.Id, "7790000008004", "Manteca (vence en 10 días)", today.AddDays(10));
            await AddItemAsync(owner.Id, "7790000008005", "Jamón (vencido ayer)", today.AddDays(-1));
            await AddItemAsync(owner.Id, "7790000008006", "Arroz (sin fecha)", null);
            await AddItemAsync(owner.Id, "7790000008007", "Pan (consumido, vencía mañana)", today.AddDays(1), consumed: true);

            Logger.LogInformation("Se cargaron 7 ítems de despensa de ejemplo para el usuario '{UserName}'.", OwnerUserName);
        }

        private async Task AddItemAsync(
            Guid ownerId,
            string barcode,
            string productName,
            DateOnly? expirationDate,
            bool consumed = false)
        {
            var product = await GetOrCreateProductAsync(barcode, productName);

            var item = new PantryItem(_guidGenerator.Create(), ownerId, product.Id, expirationDate);
            if (consumed)
            {
                item.MarkAsConsumed();
            }

            await _pantryItemRepository.InsertAsync(item, autoSave: true);
        }

        private async Task<Product> GetOrCreateProductAsync(string barcode, string name)
        {
            var existing = await _productRepository.FindAsync(p => p.Barcode == barcode);
            if (existing != null)
            {
                return existing;
            }

            var product = await _productManager.CreateAsync(barcode, name, "Demo SmartPantry");
            return await _productRepository.InsertAsync(product, autoSave: true);
        }
    }
}
