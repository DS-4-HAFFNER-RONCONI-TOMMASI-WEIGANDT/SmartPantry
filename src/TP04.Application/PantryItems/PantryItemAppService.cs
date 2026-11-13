using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TP04.Products;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace TP04.PantryItems
{
    /// <summary>
    /// TP10: despensa del usuario autenticado (RF-10 alta, RF-11 listar / editar /
    /// marcar consumido / eliminar).
    ///
    /// Seguridad:
    ///  - [Authorize] en la clase: sin sesión ni token la API responde 401. No pide
    ///    permisos: cualquier usuario autenticado administra SU despensa.
    ///  - El propietario sale de ICurrentUser, nunca del cliente. El alta lo fija y
    ///    las demás operaciones lo comprueban antes de leer o modificar.
    ///  - Un ítem de otro usuario se trata igual que uno inexistente:
    ///    EntityNotFoundException -> 404 en TODAS las operaciones. Así la API no
    ///    revela si el Id existe (decisión del equipo, documentada en el PR).
    ///
    /// No hereda de CrudAppService porque cada operación debe filtrar por propietario
    /// y el listado necesita el nombre del producto (join con Product).
    /// </summary>
    [Authorize]
    public class PantryItemAppService : TP04AppService, IPantryItemAppService
    {
        private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
        private readonly IRepository<Product, Guid> _productRepository;

        public PantryItemAppService(
            IRepository<PantryItem, Guid> pantryItemRepository,
            IRepository<Product, Guid> productRepository)
        {
            _pantryItemRepository = pantryItemRepository;
            _productRepository = productRepository;
        }

        public virtual async Task<PagedResultDto<PantryItemDto>> GetListAsync(GetPantryItemListDto input)
        {
            var ownerId = CurrentUser.GetId();

            // 1) Sólo ítems propios (regla de negocio 3) y, si se pide, de un estado.
            var items = (await _pantryItemRepository.GetQueryableAsync())
                .Where(i => i.OwnerId == ownerId);

            if (input.Status.HasValue)
            {
                items = items.Where(i => i.Status == input.Status.Value);
            }

            var totalCount = await AsyncExecuter.CountAsync(items);

            // 2) Join con el producto para mostrar su nombre.
            var products = await _productRepository.GetQueryableAsync();
            var joined = items.Join(
                products,
                item => item.ProductId,
                product => product.Id,
                (item, product) => new { Item = item, Product = product });

            // 3) Orden. Se admiten claves conocidas; cualquier otra usa el orden por defecto.
            var sorting = input.Sorting?.Trim().ToLowerInvariant();
            var ordered = sorting switch
            {
                "productname" or "productname asc" =>
                    joined.OrderBy(x => x.Product.Name),
                "productname desc" =>
                    joined.OrderByDescending(x => x.Product.Name),
                "expirationdate" or "expirationdate asc" =>
                    joined.OrderBy(x => x.Item.ExpirationDate == null)
                        .ThenBy(x => x.Item.ExpirationDate)
                        .ThenBy(x => x.Product.Name),
                "expirationdate desc" =>
                    joined.OrderBy(x => x.Item.ExpirationDate == null)
                        .ThenByDescending(x => x.Item.ExpirationDate)
                        .ThenBy(x => x.Product.Name),
                // Por defecto: activos primero, los que vencen antes arriba, sin fecha al final.
                _ => joined.OrderBy(x => x.Item.Status)
                        .ThenBy(x => x.Item.ExpirationDate == null)
                        .ThenBy(x => x.Item.ExpirationDate)
                        .ThenBy(x => x.Product.Name)
            };

            // 4) Página pedida (SkipCount / MaxResultCount).
            var page = await AsyncExecuter.ToListAsync(
                ordered.Skip(input.SkipCount).Take(input.MaxResultCount));

            return new PagedResultDto<PantryItemDto>(
                totalCount,
                page.Select(x => MapToDto(x.Item, x.Product)).ToList());
        }

        public virtual async Task<PantryItemDto> GetAsync(Guid id)
        {
            var item = await GetOwnedItemAsync(id);
            return await MapToDtoAsync(item);
        }

        public virtual async Task<PantryItemDto> CreateAsync(CreatePantryItemDto input)
        {
            // RF-10: el producto tiene que estar guardado (RF-08). Si no existe -> 404.
            var product = await _productRepository.GetAsync(input.ProductId);

            var item = new PantryItem(
            GuidGenerator.Create(),
            CurrentUser.GetId(), // el propietario lo decide el servidor
            product.Id,
            input.Quantity,
            input.Unit,
            input.ExpirationDate);

            await _pantryItemRepository.InsertAsync(item, autoSave: true);

            return MapToDto(item, product);
        }

        public virtual async Task<PantryItemDto> UpdateAsync(Guid id, UpdatePantryItemDto input)
        {
            var item = await GetOwnedItemAsync(id);

            // Si algún dato es inválido, la entidad lanza la excepción antes de cambiar
            // nada y la unidad de trabajo no persiste.
            item.UpdateDetails(input.Quantity, input.Unit, input.ExpirationDate);

            await _pantryItemRepository.UpdateAsync(item, autoSave: true);

            return await MapToDtoAsync(item);
        }

        public virtual async Task<PantryItemDto> MarkAsConsumedAsync(Guid id)
        {
            var item = await GetOwnedItemAsync(id);

            // Regla de negocio 6: no se elimina; queda como historial con estado Consumed.
            item.MarkAsConsumed();

            await _pantryItemRepository.UpdateAsync(item, autoSave: true);

            return await MapToDtoAsync(item);
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            var item = await GetOwnedItemAsync(id);

            // Borrado físico explícito del usuario. Sus advertencias (TP08) se borran en
            // cascada por la FK configurada en el DbContext.
            await _pantryItemRepository.DeleteAsync(item, autoSave: true);
        }

        /// <summary>
        /// Busca el ítem y comprueba que sea del usuario actual. Inexistente o ajeno:
        /// la misma EntityNotFoundException (404).
        /// </summary>
        protected virtual async Task<PantryItem> GetOwnedItemAsync(Guid id)
        {
            var item = await _pantryItemRepository.FindAsync(id);

            if (item == null || item.OwnerId != CurrentUser.GetId())
            {
                throw new EntityNotFoundException(typeof(PantryItem), id);
            }

            return item;
        }

        private async Task<PantryItemDto> MapToDtoAsync(PantryItem item)
        {
            var product = await _productRepository.GetAsync(item.ProductId);
            return MapToDto(item, product);
        }

        private static PantryItemDto MapToDto(PantryItem item, Product product)
        {
            return new PantryItemDto
            {
                Id = item.Id,
                CreationTime = item.CreationTime,
                CreatorId = item.CreatorId,
                LastModificationTime = item.LastModificationTime,
                LastModifierId = item.LastModifierId,
                ProductId = product.Id,
                ProductName = product.Name,
                ProductBrand = product.Brand,
                Quantity = item.Quantity,
                Unit = item.Unit,
                ExpirationDate = item.ExpirationDate,
                Status = item.Status
            };
        }
    }
}
