using System;
using System.Linq;
using System.Threading.Tasks;
using TP04.PantryItems;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace TP04.Products
{
    /// <summary>
    /// <<DomainService>>
    /// Regla de negocio 4 / Invariante 4: no puede haber dos productos con el mismo
    /// código de barras. El criterio de aceptación de RF-08 pide explícitamente
    /// evitar duplicados para el mismo código de barras.
    ///
    /// Esta regla necesita consultar el repositorio (¿ya existe ese barcode?), y un
    /// Aggregate Root no debería depender de un repositorio. Por eso es un Domain
    /// Service: coordina una regla que no pertenece a un único Aggregate.
    ///
    /// TP final: también la regla "no se puede borrar un producto en uso", que
    /// consulta otro Aggregate (PantryItem).
    /// </summary>
    public class ProductManager : DomainService
    {
        private readonly IRepository<Product, Guid> _productRepository;
        private readonly IRepository<PantryItem, Guid> _pantryItemRepository;

        public ProductManager(
            IRepository<Product, Guid> productRepository,
            IRepository<PantryItem, Guid> pantryItemRepository)
        {
            _productRepository = productRepository;
            _pantryItemRepository = pantryItemRepository;
        }

        public async Task<Product> CreateAsync(string barcode, string name, string? brand = null)
        {
            // Se normaliza ANTES de chequear unicidad: si no, "  123  " y "123" pasan
            // el chequeo como distintos y el duplicado lo termina rechazando el índice
            // único de la base con una excepción de infraestructura (500) en lugar de
            // esta BusinessException.
            var normalizedBarcode = Product.NormalizeBarcode(barcode);

            await CheckBarcodeIsUniqueAsync(normalizedBarcode);

            return new Product(
                GuidGenerator.Create(),
                normalizedBarcode,
                name,
                brand
            );
        }

        /// <summary>
        /// TP06: modifica un producto existente. El barcode es editable para poder
        /// corregir errores de carga sin borrar el producto (cuando exista la
        /// despensa, los PantryItem lo referencian por Id y el borrado deja de ser
        /// una opción). Si el barcode cambia, se vuelve a verificar la unicidad
        /// excluyendo al propio producto.
        ///
        /// Todas las verificaciones ocurren antes de modificar la entidad: una
        /// entrada inválida o un barcode duplicado no dejan cambios parciales.
        /// </summary>
        public async Task UpdateAsync(Product product, string barcode, string name, string? brand)
        {
            Check.NotNull(product, nameof(product));

            var normalizedBarcode = Product.NormalizeBarcode(barcode);

            if (normalizedBarcode != product.Barcode)
            {
                await CheckBarcodeIsUniqueAsync(normalizedBarcode, product.Id);
            }

            product.Update(normalizedBarcode, name, brand);
        }

        /// <summary>
        /// RF-08: guarda un producto de Open Food Facts en el catálogo interno sin
        /// duplicar el código de barras (regla de negocio 4).
        ///
        ///   - Si ya existe un producto con ese código: se actualizan solo sus datos
        ///     externos. Nombre y marca se conservan porque pueden haber sido corregidos
        ///     a mano (TP06).
        ///   - Si no existe: se crea. Si el proveedor no informa el nombre, se usa el
        ///     código de barras como nombre visible (Name es obligatorio y no se inventa
        ///     un nombre, RF-09).
        ///
        /// Como CreateAsync, no inserta ni actualiza en el repositorio: lo hace quien
        /// llama, según <see cref="ExternalProductSaveResult.AlreadyExisted"/>. El índice
        /// único de la base sigue de respaldo si dos usuarios guardan el mismo código a
        /// la vez.
        /// </summary>
        public async Task<ExternalProductSaveResult> SaveFromExternalAsync(
            string barcode,
            string? name,
            string? brand,
            ProductExternalData externalData)
        {
            Check.NotNull(externalData, nameof(externalData));

            // Se normaliza antes de buscar, igual que en CreateAsync: "  123  " y "123"
            // son el mismo producto.
            var normalizedBarcode = Product.NormalizeBarcode(barcode);

            var existing = await _productRepository.FindAsync(p => p.Barcode == normalizedBarcode);
            if (existing != null)
            {
                existing.UpdateExternalData(externalData, DateTime.UtcNow);
                return new ExternalProductSaveResult(existing, alreadyExisted: true);
            }

            var visibleName = string.IsNullOrWhiteSpace(name) ? normalizedBarcode : name;

            var product = new Product(GuidGenerator.Create(), normalizedBarcode, visibleName, brand);
            product.UpdateExternalData(externalData, DateTime.UtcNow);

            return new ExternalProductSaveResult(product, alreadyExisted: false);
        }

        /// <summary>
        /// TP final (RF-08): un producto que está en alguna despensa no se puede borrar.
        ///
        /// La base ya lo impide (FK Restrict entre PantryItems y Products), pero ese
        /// rechazo llega como un error de base de datos y la API responde 500. Con esta
        /// verificación previa el usuario recibe un error de negocio claro.
        ///
        /// Cuentan también los ítems consumidos: son historial y no se borran solos
        /// (regla de negocio 6), así que siguen referenciando al producto.
        /// </summary>
        public async Task EnsureNotInUseAsync(Guid productId)
        {
            // Se cuenta en vez de usar FindAsync(condición): FindAsync espera un único
            // resultado y falla si el producto está en varias despensas.
            var itemQuery = await _pantryItemRepository.GetQueryableAsync();
            var itemsUsingProduct = await AsyncExecuter.CountAsync(
                itemQuery.Where(i => i.ProductId == productId));

            if (itemsUsingProduct > 0)
            {
                throw new BusinessException(TP04DomainErrorCodes.ProductInUse);
            }
        }

        private async Task CheckBarcodeIsUniqueAsync(string barcode, Guid? excludedProductId = null)
        {
            var existing = await _productRepository.FindAsync(p => p.Barcode == barcode);
            if (existing != null && existing.Id != excludedProductId)
            {
                throw new BusinessException(TP04DomainErrorCodes.ProductDuplicateBarcode)
                    .WithData("Barcode", barcode);
            }
        }
    }
}
