using TP04.EntityFrameworkCore;

namespace TP04.PantryItems
{
    /// <summary>
    /// Ejecuta las pruebas de la regla de vencimientos contra la base SQLite en
    /// memoria de la suite ABP (aislada por prueba, con el índice único real).
    /// </summary>
    public class EfCoreExpirationWarningManager_Tests : ExpirationWarningManager_Tests<TP04EntityFrameworkCoreTestModule>
    {
    }
}
