import type { ExternalProductLookupResultDto, ExternalProductSearchDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ExternalProductLookupService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  getByBarcode = (input: ExternalProductSearchDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ExternalProductLookupResultDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/external-product-lookup/by-barcode',
      params: { barcode: input.barcode },
    },
    { apiName: this.apiName,...config });
}