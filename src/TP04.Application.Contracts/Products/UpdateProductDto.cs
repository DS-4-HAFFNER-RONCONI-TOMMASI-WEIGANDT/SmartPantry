using System.ComponentModel.DataAnnotations;

namespace TP04.Products
{
    // TP06: datos que el modelo admite modificar. Las mismas validaciones que
    // CreateProductDto; la normalización (Trim, marca vacía -> null) y la unicidad
    // del barcode las resuelve el dominio.
    public class UpdateProductDto
    {
        [Required]
        [StringLength(ProductConsts.MaxBarcodeLength)]
        public string Barcode { get; set; } = string.Empty;

        [Required]
        [StringLength(ProductConsts.MaxNameLength)]
        public string Name { get; set; } = string.Empty;

        [StringLength(ProductConsts.MaxBrandLength)]
        public string? Brand { get; set; }
    }
}
