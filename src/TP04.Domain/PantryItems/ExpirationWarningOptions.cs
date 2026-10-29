namespace TP04.PantryItems
{
    /// <summary>
    /// Configuración de la regla y del worker de vencimientos.
    /// Se lee de la sección "ExpirationWarnings" de appsettings.json; si no está,
    /// se usan los valores por defecto de ExpirationWarningConsts.
    /// </summary>
    public class ExpirationWarningOptions
    {
        public const string ConfigurationSection = "ExpirationWarnings";

        /// <summary>Días de anticipación con los que se avisa (día límite incluido).</summary>
        public int ThresholdDays { get; set; } = ExpirationWarningConsts.DefaultThresholdDays;

        /// <summary>Zona horaria con la que el worker calcula la fecha de "hoy".</summary>
        public string TimeZoneId { get; set; } = ExpirationWarningConsts.DefaultTimeZoneId;

        /// <summary>Período del worker en minutos.</summary>
        public int WorkerPeriodMinutes { get; set; } = ExpirationWarningConsts.DefaultWorkerPeriodMinutes;
    }
}
