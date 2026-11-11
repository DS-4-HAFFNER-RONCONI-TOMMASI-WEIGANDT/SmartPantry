using System;

namespace TP04.PantryItems
{
    /// <summary>
    /// TP10 (RF-10 / RF-11): límites de los datos editables de un ítem de despensa.
    /// Los usan el dominio, el mapeo de EF Core y los DTO, así la regla está en un
    /// solo lugar. La interfaz Angular repite las mismas reglas por usabilidad.
    /// </summary>
    public static class PantryItemConsts
    {
        /// <summary>Largo máximo de la unidad ("kg", "g", "l", "unidades", ...).</summary>
        public const int MaxUnitLength = 20;

        /// <summary>Cantidad máxima admitida. La mínima es 0 (no negativa).</summary>
        public const double MaxQuantity = 999999;

        /// <summary>Precisión de la columna Quantity: decimal(18,3).</summary>
        public const int QuantityPrecision = 18;
        public const int QuantityScale = 3;

        /// <summary>Rango de fechas de vencimiento aceptadas (fecha calendario).</summary>
        public static readonly DateOnly MinExpirationDate = new DateOnly(2000, 1, 1);
        public static readonly DateOnly MaxExpirationDate = new DateOnly(2100, 12, 31);
    }
}
