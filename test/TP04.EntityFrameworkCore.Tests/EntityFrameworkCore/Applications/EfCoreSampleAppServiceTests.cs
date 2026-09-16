using TP04.Samples;
using Xunit;

namespace TP04.EntityFrameworkCore.Applications;

[Collection(TP04TestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<TP04EntityFrameworkCoreTestModule>
{

}
