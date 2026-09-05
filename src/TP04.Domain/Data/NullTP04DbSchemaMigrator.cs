using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace TP04.Data;

/* This is used if database provider does't define
 * ITP04DbSchemaMigrator implementation.
 */
public class NullTP04DbSchemaMigrator : ITP04DbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
