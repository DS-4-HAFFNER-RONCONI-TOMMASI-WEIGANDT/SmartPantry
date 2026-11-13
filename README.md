# SmartPantry

Repositorio del Trabajo Práctico Integrador de Desarrollo de Software 2026 (UTN FRCU): **Despensa inteligente y perfil nutricional**.

SmartPantry permite que cada usuario busque productos alimenticios en **Open Food Facts**, los guarde en el sistema, los agregue a su despensa con cantidad y fecha de vencimiento, y reciba advertencias de vencimientos próximos.

## Integrantes
- Juan Cruz Weigandt (usuario: JuanWeigandt)
- Martín Ronconi (usuario: MartinRonconi7)
- Federico Haffner (usuario: federicohaffner)
- Camilo Tommasi (usuario: CamiloTommasi)

## Documentación del proyecto
- [Wiki](https://github.com/DS-4-HAFFNER-RONCONI-TOMMASI-WEIGANDT/SmartPantry/wiki): modelo de dominio, arquitectura y decisiones del grupo.
- [CONTRIBUTING.md](CONTRIBUTING.md): ramas, flujo Issue → rama → PR → revisión → merge y convenciones de commits.
- La documentación de cada TP (qué se hizo, cómo se probó y evidencia) está en su Pull Request.

---

## Funcionalidades implementadas

| RF | Funcionalidad | Dónde |
|---|---|---|
| RF-05 / RF-09 | Consulta de un producto por código de barras en Open Food Facts | `ExternalProductLookupAppService` → `GET /api/app/external-product-lookup/by-barcode` |
| RF-08 | Guardar un producto externo sin duplicar el código de barras (CRUD de productos) | `ProductAppService` → `/api/app/product` |
| RF-10 | Agregar un producto guardado a la despensa | `PantryItemAppService.CreateAsync` → `POST /api/app/pantry-item` |
| RF-11 | Pantalla **Mi despensa**: listar, editar, marcar consumido y eliminar ítems propios | `PantryComponent` (`/pantry`) + `PantryItemAppService` |
| RF-15 | Tarea periódica e idempotente que genera advertencias de vencimiento | `ExpirationWarningWorker` + `ExpirationWarningManager` |
| RF-04 | Listado de usuarios (solo administrador) | `IdentityUserAppService` de ABP → `GET /api/identity/users` |

**Seguridad:** el login es el de ABP (Account + Identity + OpenIddict). Los AppServices propios tienen `[Authorize]`: sin sesión responden **401**. Cada usuario solo ve y modifica sus ítems de despensa (un ítem ajeno responde **404**). El worker de vencimientos no depende de un usuario conectado.

---

## Estructura de la solución

Monolito en capas basado en la plantilla **ABP Application (Layered)** con Angular y Entity Framework Core (.NET 10, ABP 10.6, Angular 22).

| Proyecto | Responsabilidad |
|---|---|
| `angular` | Frontend en Angular. Consume la API con los proxies generados por ABP (`angular/src/app/proxy`). |
| `src/TP04.Domain.Shared` | Constantes, enums, códigos de error y textos de localización (es / en) compartidos por todas las capas. |
| `src/TP04.Domain` | Entidades y reglas de negocio (`Product`, `ProductManager`, `PantryItem`, `PantryWarning`, `ExpirationWarningManager`). |
| `src/TP04.Application.Contracts` | DTOs e interfaces de los AppServices (el contrato de la API). |
| `src/TP04.Application` | Casos de uso (AppServices) y worker de vencimientos. Define `IExternalProductCatalogClient`. |
| `src/TP04.EntityFrameworkCore` | `TP04DbContext`, mapeo de entidades y migraciones. |
| `src/TP04.HttpApi` | Controladores de la API (ABP los genera a partir de los AppServices). |
| `src/TP04.HttpApi.Client` | Proxies de cliente C# para consumir la API. |
| `src/TP04.HttpApi.Host` | Aplicación ASP.NET Core que expone la API, Swagger y el servidor de autenticación. Contiene el cliente de Open Food Facts (`ExternalProducts/`), registrado con `IHttpClientFactory`. |
| `src/TP04.DbMigrator` | Aplicación de consola que aplica las migraciones y carga los datos iniciales. |
| `test/TP04.Domain.Tests` | Pruebas de la capa de dominio. |
| `test/TP04.Application.Tests` | Pruebas de los AppServices y de la autorización. |
| `test/TP04.EntityFrameworkCore.Tests` | Pruebas de integración con Entity Framework Core. |
| `test/TP04.TestBase` | Configuración y datos compartidos por las pruebas. |

**Sistema externo:** Open Food Facts. Se usa la Product Opener API para consultar por código de barras. Si el servicio no encuentra el producto, limita las consultas o no responde, la consulta devuelve un resultado de negocio (`NotFound`, `RateLimited`, `Unavailable`), no un error.

---

## Guía de ejecución

### Requisitos
- Visual Studio 2022 o 2026 con la carga de trabajo *Desarrollo de ASP.NET y web* (incluye el SDK de .NET 10).
- Node.js 24.15.0 o superior.
- Yarn 1.22.x.
- SQL Server LocalDB, Developer o Express, y SQL Server Management Studio (SSMS).
- ABP CLI / ABP Studio y Git.

### Configuración local
Las cadenas de conexión están en dos archivos `appsettings.json`, que deben tener el mismo valor:
- `src/TP04.DbMigrator/appsettings.json`
- `src/TP04.HttpApi.Host/appsettings.json`

La cadena local (`ConnectionStrings:Default`) usa LocalDB y la base `TP04`:
```json
"ConnectionStrings": {
  "Default": "Server=(LocalDb)\\MSSQLLocalDB;Database=TP04;Trusted_Connection=True;TrustServerCertificate=true"
}
```

Otras secciones de `src/TP04.HttpApi.Host/appsettings.json`:

| Sección | Para qué sirve |
|---|---|
| `App:SelfUrl` / `App:AngularUrl` | URL del backend (`https://localhost:44384`) y del frontend (`http://localhost:4200`). |
| `OpenFoodFacts` | `BaseUrl`, `TimeoutSeconds` y `UserAgent` del cliente de Open Food Facts. |
| `ExpirationWarnings` | `ThresholdDays` (3 días), `TimeZoneId` (`America/Argentina/Buenos_Aires`, para calcular "hoy") y `WorkerPeriodMinutes` (cada cuánto corre el worker). |

> En el repositorio solo hay configuración local sin credenciales. Los tokens y las contraseñas reales no se versionan.

### Pasos para levantar la solución

1. **Clonar e instalar dependencias** (una sola vez):
   ```cmd
   git clone https://github.com/DS-4-HAFFNER-RONCONI-TOMMASI-WEIGANDT/SmartPantry.git
   cd SmartPantry
   abp install-libs
   dotnet dev-certs https --trust
   cd angular
   yarn install
   ```

2. **Crear o actualizar la base de datos:** ejecutar `TP04.DbMigrator`, desde Visual Studio (*Establecer como proyecto de inicio* → Ejecutar) o por consola:
   ```cmd
   cd src\TP04.DbMigrator
   dotnet run
   ```
   Aplica todas las migraciones y crea el usuario `admin`, los clientes de OpenIddict y los datos de ejemplo de la despensa. Hay que volver a ejecutarlo cada vez que se baja una migración nueva.

3. **Iniciar el backend:** ejecutar `TP04.HttpApi.Host` con el perfil HTTPS, o por consola:
   ```cmd
   cd src\TP04.HttpApi.Host
   dotnet run
   ```
   Swagger queda en <https://localhost:44384/swagger>. Para probar endpoints protegidos, entrar en **Authorize** y elegir:
   - `oidc`: marcar el scope `TP04`, usar el client_id `TP04_Swagger` (sin secret) e iniciar sesión.
   - `Bearer`: pegar un access token, solo lo que viene después de `Bearer`.

4. **Iniciar el frontend** (con el backend corriendo):
   ```cmd
   cd angular
   yarn start
   ```
   La aplicación queda en <http://localhost:4200>.

5. **Iniciar sesión:** en un entorno local recién migrado, el usuario administrador es `admin`, con la contraseña inicial que define ABP al ejecutar el DbMigrator. Para probar como usuario común, crear un usuario sin roles desde *Administración → Identidad → Usuarios*.

Si cambia el contrato del backend, los proxies de Angular se regeneran con el host en ejecución (no se corrigen a mano):
```cmd
cd angular
abp generate-proxy -t ng
```

---

## Pruebas

| Qué | Cómo se ejecuta |
|---|---|
| Backend (dominio, aplicación, EF Core) | Visual Studio → **Test Explorer → Run All**, o desde la raíz: `dotnet test TP04.slnx` |
| Frontend (componentes) | `cd angular` y `yarn test` (Vitest, sin red: el proxy se reemplaza por un mock) |

- Las pruebas usan SQLite en memoria, así que no hace falta tener la base local levantada.
- El worker de vencimientos está deshabilitado en las pruebas: la regla se prueba con fecha controlada mediante `ExpirationWarningManager`.
- Las pruebas que usan `AlwaysAllowAuthorizationService` omiten la autorización. `AppServiceAuthorization_Tests` comprueba que los atributos `[Authorize]` estén declarados, y los 401/403/404 se prueban por HTTP (ver el PR de cada TP).

### Integración continua
`.github/workflows/ci.yml` corre en cada Pull Request y en cada push a `main`, `stg` y `prod`:
- **backend:** `dotnet restore`, `dotnet build` y `dotnet test` sobre `TP04.slnx`.
- **frontend:** `yarn install --frozen-lockfile`, `yarn build` y `yarn test` en `angular`.

Un PR se integra solo con el CI en verde y la aprobación de otro integrante.

---

## Pantalla "Mi despensa" (RF-11)

La pantalla está en `angular/src/app/pantry/` (`PantryComponent`), en la ruta `/pantry` (menú **Mi despensa**). Usa el proxy generado `PantryItemService` (`angular/src/app/proxy/pantry-items`) sobre `PantryItemAppService` (`/api/app/pantry-item`). Si cambia el contrato del backend, el proxy se vuelve a generar con `abp generate-proxy -t ng` (con el host en ejecución) y no se corrige a mano.

### Ítems consumidos
Según la regla de negocio 6, un ítem consumido **no se elimina**: queda como **historial** con estado `Consumed`. La pantalla muestra por defecto los ítems *En la despensa* y un selector **Mostrar** permite ver *Consumidos* o *Todos*. Los consumidos se ven atenuados, con la etiqueta *Consumido*, y ya no se editan ni se vuelven a consumir (el servidor también lo impide). Para quitarlos hay que eliminarlos de forma explícita, con confirmación.

### ListService
El listado (`GetListAsync`) es **paginado en el servidor** (`SkipCount` / `MaxResultCount`), por eso la pantalla usa `ListService` con `hookToQuery`. `ListService` arma la consulta y vuelve a pedir la página cuando cambia la página, el filtro o después de editar, consumir o eliminar.

### Criterio de fechas
La fecha de vencimiento es una **fecha calendario sin hora** (`DateOnly` en el backend, `"yyyy-MM-dd"` en JSON). La pantalla la muestra con `DatePipe` (`mediumDate`) y el `LOCALE_ID` de ABP, es decir, con el formato del idioma elegido en la aplicación. Como no tiene hora, **no se convierte entre UTC y hora local**: se ve el mismo día que está guardado. Al editar, el `<input type="date">` devuelve el mismo formato `yyyy-MM-dd`, que se envía tal cual. Esto **coincide con el criterio del worker de TP08**: el worker compara fechas calendario y calcula "hoy" como la fecha de Argentina (`America/Argentina/Buenos_Aires`) a partir de UTC.

### Manejo de errores
- **Listado:** se llama con `skipHandleError: true` porque la pantalla maneja el error por sí misma. Muestra el estado de error con un mensaje localizado (`Pantry:LoadError`) y el botón **Reintentar**, sin abrir además el diálogo de ABP.
- **Guardar, consumir y eliminar:** se deja el manejo predeterminado de ABP, que muestra el mensaje del servidor (validación, regla de negocio o 404). Si el servidor rechaza una edición, el modal queda abierto con lo que se ingresó.
- No se muestran detalles técnicos ni solo el código de estado.

### Acceso a ítems ajenos
El aislamiento por usuario lo hace el servidor: `PantryItemAppService` tiene `[Authorize]` (sin sesión responde 401) y obtiene el propietario con `ICurrentUser`. Un ítem de otro usuario se trata igual que uno inexistente: **404 en todas las operaciones**, para no revelar que existe. Ocultar o deshabilitar botones en la interfaz no reemplaza esa comprobación.

### Pruebas de la pantalla
- Componente: desde `angular`, `yarn test` (`pantry.component.spec.ts`, con el proxy simulado).
- Backend: Test Explorer (`PantryItemAppService_Tests`, `PantryItem_Tests`, `AppServiceAuthorization_Tests`).

---

## Worker de vencimientos (RF-15)

`ExpirationWarningWorker` (`AsyncPeriodicBackgroundWorkerBase`, registrado en `TP04ApplicationModule`) corre cada `WorkerPeriodMinutes` dentro del Host, sin usuario conectado. Decide **cuándo** ejecutar; `ExpirationWarningManager` (servicio de dominio) decide **qué** advertencias crear, actualizar, reactivar o desactivar.

- Avisa cuando faltan **3 días o menos** para el vencimiento, incluidos el día límite, el mismo día y los vencidos.
- Desactiva la advertencia si el ítem queda fuera del umbral, no tiene fecha o está consumido. No la borra, para poder reactivar la misma fila.
- Es **idempotente**: hay una sola advertencia por ítem y tipo, reforzada con el índice único `IX_AppPantryWarnings_PantryItemId_Type`. Ejecutarlo dos veces no crea duplicados.
- Cada ejecución deja en la consola y en `src/TP04.HttpApi.Host/Logs/logs.txt` una línea `Worker de vencimientos (fecha): creadas=..., ...`.
