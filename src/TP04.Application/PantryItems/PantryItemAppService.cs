using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace TP04.PantryItems
{
    [Authorize]
    public class PantryItemAppService : ApplicationService, IPantryItemAppService
    {
        private readonly IRepository<PantryItem, Guid> _pantryItemRepository;

        public PantryItemAppService(IRepository<PantryItem, Guid> pantryItemRepository)
        {
            _pantryItemRepository = pantryItemRepository;
        }

        private async Task<PantryItem> GetOwnItemAsync(Guid id)
        {
            var item = await _pantryItemRepository.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == CurrentUser.Id);

            if (item == null)
            {
                throw new EntityNotFoundException(typeof(PantryItem), id);
            }

            return item;
        }

        public async Task<PantryItemDto> GetAsync(Guid id)
        {
            var item = await GetOwnItemAsync(id);
            return ObjectMapper.Map<PantryItem, PantryItemDto>(item);
        }

        public async Task<PagedResultDto<PantryItemDto>> GetListAsync(GetPantryItemListDto input)
        {
            var query = await _pantryItemRepository.GetQueryableAsync();

            query = query.Where(x => x.OwnerId == CurrentUser.Id);

            var totalCount = await AsyncExecuter.CountAsync(query);
            var items = await AsyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));

            return new PagedResultDto<PantryItemDto>(
    totalCount,
    items.Select(i => ObjectMapper.Map<PantryItem, PantryItemDto>(i)).ToList()
);
        }

        // Se agrega para cumplir con la interfaz de tu compañero
        public async Task<PantryItemDto> CreateAsync(CreatePantryItemDto input)
        {
            throw new NotImplementedException("La creación no es parte de los RFs de esta entrega.");
        }

        public async Task<PantryItemDto> UpdateAsync(Guid id, UpdatePantryItemDto input)
        {
            var item = await GetOwnItemAsync(id);

            item.UpdateDetails(input.Quantity, input.Unit, input.ExpirationDate);
            await _pantryItemRepository.UpdateAsync(item);

            return ObjectMapper.Map<PantryItem, PantryItemDto>(item);
        }

        public async Task<PantryItemDto> MarkAsConsumedAsync(Guid id)
        {
            var item = await GetOwnItemAsync(id);

            item.MarkAsConsumed();
            await _pantryItemRepository.UpdateAsync(item);

            return ObjectMapper.Map<PantryItem, PantryItemDto>(item);
        }

        public async Task DeleteAsync(Guid id)
        {
            var item = await GetOwnItemAsync(id);
            await _pantryItemRepository.DeleteAsync(item);
        }
    }
}