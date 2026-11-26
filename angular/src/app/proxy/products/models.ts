import type { AuditedEntityDto } from '@abp/ng.core';

export interface CreateProductDto {
  barcode: string;
  name: string;
  brand?: string | null;
}

export interface ProductDto extends AuditedEntityDto<string> {
  barcode?: string;
  name?: string;
  brand?: string | null;
  imageUrl?: string | null;
  categories?: string[] | null;
  ingredientsText?: string | null;
  allergenTags?: string[] | null;
  nutriScore?: string | null;
  novaGroup?: number | null;
  externalDataUpdatedAt?: string | null;
}

export interface UpdateProductDto {
  barcode: string;
  name: string;
  brand?: string | null;
}
