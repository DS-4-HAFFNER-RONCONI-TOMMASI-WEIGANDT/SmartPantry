using System.ComponentModel.DataAnnotations;

namespace TP04.ExternalProducts
{
    /// <summary>
    /// Entrada de la consulta por código de barras (RF-05).
    /// El código es el dato de consulta, no un identificador de usuario.
    /// </summary>
    public class ExternalProductSearchDto
    {
        [Required(ErrorMessage = "El código de barras es obligatorio.")]
        [RegularExpression(
            ExternalProductConsts.BarcodePattern,
            ErrorMessage = "El código de barras debe tener entre 8 y 14 dígitos numéricos.")]
        public string Barcode { get; set; } = string.Empty;
    }
}
