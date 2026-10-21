using System.ComponentModel.DataAnnotations;

namespace TP04.Products
{
    public class ExternalProductSearchDto
    {
        [Required(ErrorMessage = "El código de barras es obligatorio.")]
        [StringLength(50, ErrorMessage = "El código de barras no puede superar los 50 caracteres.")]
        public string Barcode { get; set; } = null!;
    }
}
