using TP04.EntityFrameworkCore;

namespace TP04.PantryItems
{
    /// <summary>
    /// TP10: ejecuta las pruebas de la despensa contra la base SQLite en memoria de la
    /// suite ABP (aislada y nueva para cada prueba).
    /// </summary>
    public class EfCorePantryItemAppService_Tests : PantryItemAppService_Tests<TP04EntityFrameworkCoreTestModule>
    {
    }
}
