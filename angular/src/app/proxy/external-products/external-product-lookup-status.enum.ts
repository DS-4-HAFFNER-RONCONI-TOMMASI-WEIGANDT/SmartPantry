import { mapEnumToOptions } from '@abp/ng.core';

export enum ExternalProductLookupStatus {
  Found = 'Found',
  NotFound = 'NotFound',
  RateLimited = 'RateLimited',
  Unavailable = 'Unavailable',
}

export const externalProductLookupStatusOptions = mapEnumToOptions(ExternalProductLookupStatus);
