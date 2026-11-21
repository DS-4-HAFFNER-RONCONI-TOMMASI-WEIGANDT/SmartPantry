import type { ExternalProductLookupStatus } from './external-product-lookup-status.enum';

export interface ExternalProductLookupResultDto {
  status?: ExternalProductLookupStatus;
  barcode?: string;
  name?: string | null;
  brand?: string | null;
  imageUrl?: string | null;
  categories?: string[] | null;
  ingredientsText?: string | null;
  allergenTags?: string[] | null;
  nutriScore?: string | null;
  novaGroup?: number | null;
  missingFields?: string[];
}

export interface ExternalProductSearchDto {
  barcode: string;
}
