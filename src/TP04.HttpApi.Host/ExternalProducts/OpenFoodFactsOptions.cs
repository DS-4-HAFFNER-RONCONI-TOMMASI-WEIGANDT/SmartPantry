namespace TP04.ExternalProducts
{
    /// <summary>
    /// Configuración del cliente HTTP de Open Food Facts (sección "OpenFoodFacts" de appsettings).
    /// No contiene claves ni credenciales: la consulta de productos es pública, pero el
    /// proveedor exige un User-Agent que identifique al cliente ("AppName/Version (contacto)").
    /// </summary>
    public class OpenFoodFactsOptions
    {
        public const string SectionName = "OpenFoodFacts";

        public string BaseUrl { get; set; } = "https://world.openfoodfacts.org/";

        public int TimeoutSeconds { get; set; } = 10;

        public string UserAgent { get; set; } =
            "SmartPantry/1.0 (+https://github.com/DS-4-HAFFNER-RONCONI-TOMMASI-WEIGANDT/SmartPantry)";
    }
}
