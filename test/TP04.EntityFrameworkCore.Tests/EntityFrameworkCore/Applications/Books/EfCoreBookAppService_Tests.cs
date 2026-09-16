using TP04.Books;
using Xunit;

namespace TP04.EntityFrameworkCore.Applications.Books;

[Collection(TP04TestConsts.CollectionDefinitionName)]
public class EfCoreBookAppService_Tests : BookAppService_Tests<TP04EntityFrameworkCoreTestModule>
{

}