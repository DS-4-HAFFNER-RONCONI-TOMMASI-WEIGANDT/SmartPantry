namespace TP04.ExternalProducts
{
    /// <summary>
    /// Reglas del código de barras aceptado por la consulta externa (TP07 / RF-05).
    ///
    /// Se aceptan los formatos GTIN usuales: EAN-8 (8), UPC-A (12), EAN-13 (13) y
    /// GTIN-14 (14). Solo dígitos 0-9; se toleran espacios al inicio o al final porque
    /// el AppService los recorta antes de consultar. El dígito verificador no se
    /// valida: si el código no existe, el proveedor responde "no encontrado".
    /// </summary>
    public static class ExternalProductConsts
    {
        public const int MinBarcodeLength = 8;
        public const int MaxBarcodeLength = 14;

        public const string BarcodePattern = @"^\s*[0-9]{8,14}\s*$";
    }
}
