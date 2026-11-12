import type { ExternalProductLookupStatus } from './external-product-lookup-status.enum';

export interface ExternalProductLookupResultDto {
  status?: ExternalProductLookupStatus;
  barcode?: string;
  name?: string | null;
  brand?: string | null;
  imageUrl?: string | null;
  missingFields?: string[];
}

export interface ExternalProductSearchDto {
  barcode: string;
}
