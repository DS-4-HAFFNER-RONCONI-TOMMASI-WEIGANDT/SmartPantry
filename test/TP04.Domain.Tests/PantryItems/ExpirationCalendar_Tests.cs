using System;
using Shouldly;
using Xunit;

namespace TP04.PantryItems
{
    /// <summary>
    /// TP08: el criterio de fecha del equipo es "hoy = fecha local de Argentina,
    /// calculada desde UTC". Estas pruebas no dependen de la zona horaria de la
    /// máquina (CI corre en UTC; las PCs del equipo en Argentina).
    /// </summary>
    public class ExpirationCalendar_Tests
    {
        private const string ArgentinaTimeZone = ExpirationWarningConsts.DefaultTimeZoneId;

        [Fact]
        public void Late_Night_In_Argentina_Should_Still_Be_The_Argentinian_Day()
        {
            // 10/11 01:00 UTC = 09/11 22:00 en Argentina (UTC-3).
            var utcNow = new DateTime(2026, 11, 10, 1, 0, 0, DateTimeKind.Utc);

            ExpirationCalendar.GetToday(utcNow, ArgentinaTimeZone)
                .ShouldBe(new DateOnly(2026, 11, 9));
        }

        [Fact]
        public void Midnight_In_Argentina_Should_Change_The_Day()
        {
            // 10/11 03:00 UTC = 10/11 00:00 en Argentina.
            var utcNow = new DateTime(2026, 11, 10, 3, 0, 0, DateTimeKind.Utc);

            ExpirationCalendar.GetToday(utcNow, ArgentinaTimeZone)
                .ShouldBe(new DateOnly(2026, 11, 10));
        }

        [Fact]
        public void Unspecified_Kind_Should_Be_Treated_As_Utc()
        {
            var utcNow = new DateTime(2026, 11, 10, 2, 59, 0, DateTimeKind.Unspecified);

            ExpirationCalendar.GetToday(utcNow, ArgentinaTimeZone)
                .ShouldBe(new DateOnly(2026, 11, 9));
        }
    }
}
