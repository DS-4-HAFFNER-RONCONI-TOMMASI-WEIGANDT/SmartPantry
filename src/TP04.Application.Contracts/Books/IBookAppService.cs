using System;
using System.Threading.Tasks;
using TP04.Shared;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;

namespace TP04.Books;

public interface IBookAppService :
    ICrudAppService< //Defines CRUD methods
        BookDto, //Used to show books
        Guid, //Primary key of the book entity
        PagedAndSortedResultRequestDto, //Used for paging/sorting
        CreateUpdateBookDto> //Used to create/update a book
{
    Task<IRemoteStreamContent> GetListAsExcelFileAsync(BookExcelDownloadDto input);

    Task<DownloadTokenResultDto> GetDownloadTokenAsync();
}