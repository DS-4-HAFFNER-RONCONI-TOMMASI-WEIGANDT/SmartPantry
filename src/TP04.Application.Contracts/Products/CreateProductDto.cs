using System.ComponentModel.DataAnnotations;

namespace TP04.Products
{
    public class CreateProductDto
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