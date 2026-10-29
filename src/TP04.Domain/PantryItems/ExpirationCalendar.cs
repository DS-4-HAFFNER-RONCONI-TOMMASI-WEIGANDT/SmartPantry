using System;

namespace TP04.PantryItems
{
    /// <summary>
    /// Criterio de fecha del equipo para vencimientos (documentado en el README):
    ///
    ///  - Las fechas de vencimiento son fechas CALENDARIO, sin hora (DateOnly).
    ///  - "Hoy" es la fecha local de Argentina, calculada a partir de la hora UTC.
    ///    Así el resultado no depende de la zona horaria del servidor (por ejemplo,
    ///    CI corre en UTC) y entre las 21:00 y las 24:00 de Argentina "hoy" sigue
    ///    siendo el día argentino y no el día siguiente de UTC.
    ///
    /// La regla (ExpirationWarningManager) recibe "hoy" como parámetro, por eso las
    /// pruebas usan una fecha fija sin depender del reloj.
    /// </summary>
    public static class ExpirationCalendar
    {
        public static DateOnly GetToday(DateTime utcNow, string timeZoneId)
        {
            var utc = utcNow.Kind switch
            {
                DateTimeKind.Local => utcNow.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(utcNow, DateTimeKind.Utc),
                _ => utcNow
            };

            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utc, FindTimeZone(timeZoneId));
            return DateOnly.FromDateTime(localNow);
        }

        private static TimeZoneInfo FindTimeZone(string timeZoneId)
        {
            try
            {
                // En .NET 6+ los Id IANA ("America/Argentina/Buenos_Aires") funcionan
                // también en Windows.
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                // Respaldo por si el sistema sólo conoce el Id de Windows.
                if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId))
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
                }

                throw;
            }
        }
    }
}
