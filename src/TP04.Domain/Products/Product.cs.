using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace TP04.Products
{
    /// <summary>
    /// RF-08 (alcance preparatorio de TP05): producto guardado internamente a partir
    /// de un producto externo de Open Food Facts. Identificado de forma única por su
    /// código de barras (Regla de negocio 4 de la especificación funcional).
    ///
    /// En este TP05 la búsqueda en Open Food Facts todavía NO está integrada: el
    /// barcode/nombre/marca se cargan a mano (simulando lo que en el TP final va a
    /// venir de la API externa) para poder probar la persistencia interna aislada.
    /// </summary>
    public class Product : AuditedAggregateRoot<Guid>
    {
        public virtual string Barcode { get; private set; } = null!;
        public virtual string Name { get; private set; } = null!;
        public virtual string? Brand { get; private set; }

        // Requerido por EF Core.
        protected Product() { }

        // Constructor internal: la regla "no duplicar código de barras" NO se puede
        // validar acá adentro, porque para eso hace falta consultar el repositorio y
        // una Entity no debe depender de un repositorio. Esa regla vive en
        // ProductManager (Domain Service): los productos se crean siempre a través de
        // ProductManager.CreateAsync, nunca con "new Product(...)" desde Application.
        internal Product(
            Guid id,
            string barcode,
            string name,
            string? brand = null
        ) : base(id)
        {
            SetBarcode(barcode);
            SetName(name);
            SetBrand(brand);
        }

        public void SetBarcode(string barcode)
        {
            Barcode = Check.NotNullOrWhiteSpace(
                barcode,
                nameof(barcode),
                maxLength: ProductConsts.MaxBarcodeLength
            ).Trim();
        }

        public void SetName(string name)
        {
            Name = Check.NotNullOrWhiteSpace(
                name,
                nameof(name),
                maxLength: ProductConsts.MaxNameLength
            ).Trim();
        }

        public void SetBrand(string? brand)
        {
            Brand = string.IsNullOrWhiteSpace(brand)
                ? null
                : Check.Length(brand.Trim(), nameof(brand), ProductConsts.MaxBrandLength);
        }
    }
}

