using System;
using Volo.Abp.Application.Dtos;

namespace TP04.Products
{
    public class ProductDto : AuditedEntityDto<Guid>
    {
        public string Barcode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Brand { get; set; }
    }
}