using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Shouldly;
using Xunit;

namespace TP04.ExternalProducts
{
    /// <summary>
    /// TP07: validación del DTO de entrada de la consulta por código de barras.
    /// En la API, ABP ejecuta esta misma validación antes de invocar al AppService y
    /// responde HTTP 400 si el código es inválido (no se consulta al proveedor).
    /// </summary>
    public class ExternalProductSearchDto_Tests
    {
        [Theory]
        [InlineData("3017620422003")]   // EAN-13
        [InlineData("12345670")]        // EAN-8
        [InlineData("012345678905")]    // UPC-A
        [InlineData("10012345678902")]  // GTIN-14
        [InlineData("0000000000000")]   // válido en formato aunque el producto no exista
        [InlineData("  3017620422003 ")] // espacios externos: el AppService los recorta
        public void Should_Accept_Valid_Barcodes(string barcode)
        {
            var errors = Validate(new ExternalProductSearchDto { Barcode = barcode });

            errors.ShouldBeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("1234567")]          // menos de 8 dígitos
        [InlineData("123456789012345")]  // más de 14 dígitos
        [InlineData("30176204220O3")]    // letra O en lugar de cero
        [InlineData("abcdefghijk")]
        [InlineData("3017620-422003")]
        [InlineData("3017 620422003")]   // espacio interno
        public void Should_Reject_Invalid_Barcodes(string? barcode)
        {
            var errors = Validate(new ExternalProductSearchDto { Barcode = barcode! });

            errors.ShouldNotBeEmpty();
            errors.ShouldContain(e => e.MemberNames.Contains(nameof(ExternalProductSearchDto.Barcode)));
        }

        private static List<ValidationResult> Validate(ExternalProductSearchDto input)
        {
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(
                input,
                new ValidationContext(input),
                results,
                validateAllProperties: true);

            return results;
        }
    }
}
