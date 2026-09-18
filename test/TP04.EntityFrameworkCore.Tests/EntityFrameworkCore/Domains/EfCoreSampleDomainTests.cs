using TP04.Samples;
using Xunit;

namespace TP04.EntityFrameworkCore.Domains;

[Collection(TP04TestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<TP04EntityFrameworkCoreTestModule>
{

}
