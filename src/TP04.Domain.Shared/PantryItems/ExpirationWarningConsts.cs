namespace TP04.PantryItems
{
    /// <summary>
    /// Valores por defecto de la regla de vencimientos (RF-13 / RF-15), fijados
    /// y documentados por el equipo. Se pueden sobrescribir desde appsettings
    /// (sección "ExpirationWarnings").
    /// </summary>
    public static class ExpirationWarningConsts
    {
        /// <summary>
        /// Umbral de aviso: se advierte cuando faltan 3 días o menos para el
        /// vencimiento (el día límite está incluido) y también si ya venció.
        /// </summary>
        public const int DefaultThresholdDays = 3;

        /// <summary>
        /// "Hoy" se calcula como la fecha calendario de Argentina a partir de UTC.
        /// </summary>
        public const string DefaultTimeZoneId = "America/Argentina/Buenos_Aires";

        /// <summary>
        /// Cada cuánto se ejecuta el worker periódico.
        /// </summary>
        public const int DefaultWorkerPeriodMinutes = 60;
    }
}
