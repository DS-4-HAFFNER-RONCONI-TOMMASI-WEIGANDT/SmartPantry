import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

const oAuthConfig = {
  issuer: 'https://localhost:44384/',
  redirectUri: baseUrl,
  clientId: 'TP04_App',
  responseType: 'code',
  scope: 'offline_access TP04',
  requireHttps: true,
};

export const environment = {
  production: true,
  application: {
    baseUrl,
    name: 'TP04',
  },
  oAuthConfig,
  apis: {
    default: {
      url: 'https://localhost:44384',
      rootNamespace: 'TP04',
    },
    AbpAccountPublic: {
      url: oAuthConfig.issuer,
      rootNamespace: 'AbpAccountPublic',
    },
  },
  remoteEnv: {
    url: '/getEnvConfig',
    mergeStrategy: 'deepmerge'
  }
} as Environment;
