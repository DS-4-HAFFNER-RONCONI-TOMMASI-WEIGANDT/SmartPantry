import type { AuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { PantryItemStatus } from './pantry-item-status.enum';

export interface CreatePantryItemDto extends PantryItemDetailsDto {
  productId: string;
}

export interface GetPantryItemListDto extends PagedAndSortedResultRequestDto {
  status?: PantryItemStatus | null;
}

export interface PantryItemDetailsDto {
  quantity?: number | null;
  unit?: string | null;
  expirationDate?: string | null;
}

export interface PantryItemDto extends AuditedEntityDto<string> {
  productId?: string;
  productName?: string;
  productBrand?: string | null;
  quantity?: number | null;
  unit?: string | null;
  expirationDate?: string | null;
  status?: PantryItemStatus;
}

export interface UpdatePantryItemDto extends PantryItemDetailsDto {
}
