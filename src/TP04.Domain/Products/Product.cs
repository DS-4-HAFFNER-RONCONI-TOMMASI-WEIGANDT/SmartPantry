using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace TP04.Products
{
    /// <summary>
    /// RF-08: producto del catálogo base guardado internamente a partir de un
    /// producto externo de Open Food Facts. Identificado de forma única por su
    /// código de barras (Regla de negocio 4 / Invariante 4 del Modelo de Dominio).
    ///
    /// Todavía NO está integrada la búsqueda en Open Food Facts (TP07): el
    /// barcode/nombre/marca se cargan a mano para probar la persistencia interna.
    /// </summary>
    public class Product : AuditedAggregateRoot<Guid>
    {
        public virtual string Barcode { get; private set; } = null!;
        public virtual string Name { get; private set; } = null!;
        public virtual string? Brand { get; private set; }

        // Requerido por EF Core.
        protected Product() { }

        // Constructor internal: la regla "no duplicar código de barras" necesita
        // consultar el repositorio, por eso vive en ProductManager (Domain Service).
        // Los productos se crean siempre con ProductManager.CreateAsync.
        internal Product(
            Guid id,
            string barcode,
            string name,
            string? brand = null
        ) : base(id)
        {
            Barcode = NormalizeBarcode(barcode);
            Name = NormalizeName(name);
            Brand = NormalizeBrand(brand);
        }

        /// <summary>
        /// Modifica los datos editables del producto (TP06).
        ///
        /// Es internal por la misma razón que el constructor: si cambia el barcode
        /// hay que verificar unicidad contra el repositorio, así que la única
        /// entrada es ProductManager.UpdateAsync.
        ///
        /// Primero valida y normaliza TODOS los valores en variables locales y recién
        /// después asigna. Si cualquiera es inválido se lanza la excepción antes de
        /// tocar el estado: la entidad nunca queda modificada a medias.
        /// </summary>
        internal void Update(string barcode, string name, string? brand)
        {
            var normalizedBarcode = NormalizeBarcode(barcode);
            var normalizedName = NormalizeName(name);
            var normalizedBrand = NormalizeBrand(brand);

            Barcode = normalizedBarcode;
            Name = normalizedName;
            Brand = normalizedBrand;
        }

        internal static string NormalizeBarcode(string barcode)
        {
            return Check.NotNullOrWhiteSpace(
                barcode,
                nameof(barcode),
                maxLength: ProductConsts.MaxBarcodeLength
            ).Trim();
        }

        private static string NormalizeName(string name)
        {
            return Check.NotNullOrWhiteSpace(
                name,
                nameof(name),
                maxLength: ProductConsts.MaxNameLength
            ).Trim();
        }

        private static string? NormalizeBrand(string? brand)
        {
            return string.IsNullOrWhiteSpace(brand)
                ? null
                : Check.Length(brand.Trim(), nameof(brand), ProductConsts.MaxBrandLength);
        }
    }
}
