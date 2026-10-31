# TP09 — Login y seguridad con ABP (RF-05, RF-08, RF-04)

## Qué se implementó

El backend usa el login que ya trae la plantilla de ABP: **Account** para la pantalla de cuenta, **Identity** para usuarios y roles y **OpenIddict** para emitir y validar los access tokens. No se desarrolló un login propio ni se tocó Angular.

| Cambio | Archivo | Detalle |
|---|---|---|
| RF-05 requiere autenticación | `src/TP04.Application/ExternalProducts/ExternalProductLookupAppService.cs` | `[Authorize]` en la clase (no tenía `[AllowAnonymous]`) |
| RF-08 requiere autenticación | `src/TP04.Application/Products/ProductAppService.cs` | Se reemplaza el `[AllowAnonymous]` temporal de TP05/TP06 por `[Authorize]`. Se conserva la herencia de `CrudAppService` y todos los métodos |
| Swagger con Bearer | `src/TP04.HttpApi.Host/TP04HttpApiHostModule.cs` | `AddSecurityDefinition("Bearer")` y `AddSecurityRequirement` dentro de `AddAbpSwaggerGenWithOidc`. Se conservan `oidc`, `Authority`, el scope `TP04` y el flujo `AuthorizationCode` |
| Prueba de atributos | `test/TP04.Application.Tests/Security/AppServiceAuthorization_Tests.cs` | Comprueba que RF-05 y RF-08 tienen `[Authorize]` sin política y sin `[AllowAnonymous]`, y que el worker no tiene `[Authorize]` |

`[Authorize]` **sin política** significa "cualquier usuario autenticado". Así el usuario común puede consultar (RF-05) y guardar productos (RF-08), y quien no inició sesión recibe **401**.

## Correspondencia con los requisitos

| RF | AppService | Método | Endpoint |
|---|---|---|---|
| RF-05 consulta externa | `ExternalProductLookupAppService` | `GetByBarcodeAsync(ExternalProductSearchDto)` | `GET /api/app/external-product-lookup/by-barcode?Barcode={codigo}` |
| RF-08 guardado interno | `ProductAppService` | `CreateAsync(CreateProductDto)` | `POST /api/app/product` |
| RF-04 listado de usuarios | `IdentityUserAppService` (ABP) | `GetListAsync` | `GET /api/identity/users` |

El resto de `ProductAppService` (`GET /api/app/product`, `GET /api/app/product/{id}`, `PUT /api/app/product/{id}`, `DELETE /api/app/product/{id}`) queda protegido por el mismo `[Authorize]` de la clase.

RF-04 no requiere código propio: `GET /api/identity/users` exige el permiso `AbpIdentity.Users`, que solo tiene el rol `admin`. Por eso responde **403** al usuario común y **200** al administrador.

## Otros AppServices

- `AuthorAppService` y `BookAppService` (ejemplo de la plantilla) ya tenían `[Authorize(TP04Permissions.*.Default)]`, que es más restrictivo que `[Authorize]`, y se dejan como están. Su `GetListAsExcelFileAsync` sigue con `[AllowAnonymous]` porque la descarga del Excel se valida con un token de un solo uso de 30 segundos.
- `TP04AppService` es abstracta y no se expone como API.
- Por ahora no hay operaciones sobre datos personales expuestas como API (los ítems de despensa solo los usa el worker), así que todavía no hace falta comprobar el propietario con `ICurrentUser`. Eso se hará cuando se implementen RF-10, RF-11 y RF-13.

## Worker de vencimientos (TP08)

- `ExpirationWarningWorker` **no** tiene `[Authorize]` y sigue funcionando sin usuario conectado.
- Se registra en `TP04ApplicationModule.OnApplicationInitializationAsync`. El módulo del host no sobrescribe `OnApplicationInitializationAsync`, así que el pipeline (`OnApplicationInitialization`) se inicializa normalmente y no hace falta agregar `await base.OnApplicationInitializationAsync(context)`.

## Cómo probarlo

1. Compilar, ejecutar `TP04.DbMigrator` (desde `src/TP04.DbMigrator` si se usa terminal) y luego `TP04.HttpApi.Host` con HTTPS.
2. Abrir `https://localhost:44384/swagger`. En **Authorize** aparecen `oidc` y `Bearer`.
3. **oidc como admin:** marcar el scope `TP04`, usar el client_id `TP04_Swagger`, dejar el secret vacío e iniciar sesión con `admin` y `1q2w3E*`. Ejecutar RF-05 con un código de TP07 (por ejemplo `3017620422003`) y copiar el token del Curl (solo lo que viene después de `Bearer`).
4. **Usuario común:** como admin, ejecutar `POST /api/identity/users` con `roleNames: []`:
   ```json
   {
     "userName": "usuario.comun",
     "name": "Usuario",
     "surname": "Comun",
     "email": "usuario.comun@smartpantry.local",
     "phoneNumber": null,
     "isActive": true,
     "lockoutEnabled": true,
     "roleNames": [],
     "password": "<contraseña local>"
   }
   ```
5. En **otro perfil del navegador**, iniciar sesión por `oidc` con el usuario común. Repetir RF-05, copiar su token y guardar un producto con `POST /api/app/product`:
   ```json
   { "barcode": "7790070410130", "name": "Producto de prueba TP09", "brand": "Marca" }
   ```
6. En un **tercer perfil sin sesión** (las pestañas de un mismo perfil comparten cookies), hacer las pruebas con Bearer de la tabla siguiente.

Una respuesta de RF-05 con `Status = NotFound`, `RateLimited` o `Unavailable` es un **resultado de negocio de TP07** (HTTP 200), no un error de autenticación.

## Resultados

| Funcionalidad | Condición | Esperado | Obtenido |
|---|---|---|---|
| RF-05 consulta externa | Perfil sin sesión y sin token | 401 | ✅ 401 |
| RF-05 consulta externa | Bearer inválido (`token-invalido`) | 401 | ✅ 401 (`invalid_token`) |
| RF-05 consulta externa | Bearer válido de usuario común | Consulta permitida | ✅ 200 `Found` |
| RF-08 guardado interno | Perfil sin sesión y sin token | 401 | ✅ 401 |
| RF-08 guardado interno | Usuario común con login oidc | Guardado permitido con DTO válido | ✅ 200, producto creado con `creatorId` del usuario común |
| RF-04 listado de usuarios | Bearer válido de usuario común | 403 | ✅ 403 (`insufficient_access`) |
| RF-04 listado de usuarios | Bearer válido de administrador | 200 | ✅ 200 |

Pruebas automáticas: las pruebas existentes se ejecutan desde Test Explorer y en el CI. Las que usan `AlwaysAllowAuthorizationService` omiten la autorización, así que **no reemplazan** esta matriz HTTP. `AppServiceAuthorization_Tests` solo revisa que los atributos estén declarados.

> Los tokens, las cookies y las contraseñas **no** se guardan en el código, en este documento ni en el PR. En las capturas se ocultan.
