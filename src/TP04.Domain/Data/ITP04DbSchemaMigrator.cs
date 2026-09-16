using System.Threading.Tasks;

namespace TP04.Data;

public interface ITP04DbSchemaMigrator
{
    Task MigrateAsync();
}
