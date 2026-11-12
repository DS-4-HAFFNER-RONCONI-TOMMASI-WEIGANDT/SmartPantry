using Shouldly;
using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;
using Xunit;
using TP04;
using TP04.PantryItems;

namespace TP04.PantryItems
{
    public class PantryItemAppService_Tests : TP04ApplicationTestBase<TP04ApplicationTestModule>
    {
        private readonly IPantryItemAppService _pantryItemAppService;

        public PantryItemAppService_Tests()
        {
            _pantryItemAppService = GetRequiredService<IPantryItemAppService>();
        }

        [Fact]
        public async Task No_Debe_Permitir_Leer_Item_De_Otro_Usuario()
        {
            var idAjeno = Guid.NewGuid();

            await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            {
                await _pantryItemAppService.GetAsync(idAjeno);
            });
        }

        [Fact]
        public async Task No_Debe_Permitir_Eliminar_Item_De_Otro_Usuario()
        {
            var idAjeno = Guid.NewGuid();

            await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            {
                await _pantryItemAppService.DeleteAsync(idAjeno);
            });
        }
    }
}