using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TP04.PantryItems
{
    /// <summary>
    /// TP10: operaciones de la despensa del usuario autenticado (RF-10 / RF-11).
    ///
    /// Todas trabajan sólo sobre ítems propios. Un ítem de otro usuario se trata igual
    /// que uno inexistente: 404 en todas las operaciones (decisión del equipo, para no
    /// revelar que el ítem existe).
    ///
    /// Endpoints que genera ABP (convención de Auto API Controllers):
    ///   GET    /api/app/pantry-item                        GetListAsync
    ///   GET    /api/app/pantry-item/{id}                   GetAsync
    ///   POST   /api/app/pantry-item                        CreateAsync
    ///   PUT    /api/app/pantry-item/{id}                   UpdateAsync
    ///   POST   /api/app/pantry-item/{id}/mark-as-consumed  MarkAsConsumedAsync
    ///   DELETE /api/app/pantry-item/{id}                   DeleteAsync
    /// </summary>
    public interface IPantryItemAppService : IApplicationService
    {
        Task<PagedResultDto<PantryItemDto>> GetListAsync(GetPantryItemListDto input);

        Task<PantryItemDto> GetAsync(Guid id);

        Task<PantryItemDto> CreateAsync(CreatePantryItemDto input);

        Task<PantryItemDto> UpdateAsync(Guid id, UpdatePantryItemDto input);

        Task<PantryItemDto> MarkAsConsumedAsync(Guid id);

        Task DeleteAsync(Guid id);
    }
}
