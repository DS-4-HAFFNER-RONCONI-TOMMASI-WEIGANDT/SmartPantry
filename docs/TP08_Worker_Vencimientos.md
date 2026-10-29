# TP08 — Worker de vencimientos (RF-15)

## Qué se implementó

Un proceso periódico que, **sin petición HTTP**, revisa los ítems de despensa y genera, actualiza o desactiva advertencias de vencimiento **sin duplicarlas**.

| Pieza | Capa | Responsabilidad |
|---|---|---|
| `PantryItem` | Domain | Modelo mínimo: propietario, producto, estado (`Active`/`Consumed`) y fecha de vencimiento opcional |
| `PantryWarning` | Domain | Advertencia persistida con clave lógica `(PantryItemId, Type)` |
| `ExpirationWarningManager` | Domain (servicio de dominio) | Decide **qué** advertencias crear, actualizar, reactivar o desactivar |
| `ExpirationWarningWorker` | Application | Decide **cuándo** ejecutar (`AsyncPeriodicBackgroundWorkerBase`) |
| `PantryDemoDataSeedContributor` | DbMigrator | Datos de ejemplo locales |

Separar el *cuándo* (worker) del *qué* (servicio) permite probar la regla con una fecha fija, sin esperar al temporizador.

## Regla de vencimientos

- **Umbral: 3 días** (`ExpirationWarnings:ThresholdDays` en `appsettings.json`).
- `días = fecha de vencimiento − hoy`

| Caso | ¿Advertencia activa? | `IsExpired` |
|---|---|---|
| Faltan 0 a 3 días (incluye el **día límite** y **hoy**) | Sí | `false` |
| Ya venció (días < 0) | Sí | `true` |
| Faltan más de 3 días | No (se desactiva si existía) | — |
| Sin fecha de vencimiento | No (se desactiva si existía) | — |
| Ítem consumido | No (se desactiva si existía) | — |

Las advertencias **no se borran**: se desactivan cuando dejan de corresponder y, si vuelven a corresponder, se reactiva **la misma fila**. Los ítems consumidos tampoco se borran (regla de negocio 6).

## Criterio de fecha

- Las fechas de vencimiento son **fechas calendario sin hora** (`DateOnly`, columna `date`).
- "Hoy" es la **fecha local de Argentina** (`America/Argentina/Buenos_Aires`), calculada a partir de la hora **UTC** (`ExpirationCalendar.GetToday`). Así el resultado no depende de la zona horaria del servidor (CI corre en UTC).
- El servicio de dominio recibe `today` como parámetro; las pruebas usan una fecha fija.

## Idempotencia

1. **Clave lógica** `(PantryItemId, Type)`: se busca la advertencia existente y se reutiliza.
2. **Sin escrituras innecesarias**: si la advertencia ya está como debe, no se actualiza (la segunda ejecución da 0 cambios).
3. **Índice único** `IX_AppPantryWarnings_PantryItemId_Type` en la base: aunque dos ejecuciones cercanas intenten crear la misma advertencia, la segunda falla al guardar, su unidad de trabajo se revierte, el error queda en el log y la ejecución siguiente encuentra la advertencia ya creada.
4. Un mismo worker no se superpone consigo mismo (`AbpAsyncTimer` espera a que termine una ejecución antes de programar la siguiente).

## Worker

- Se registra en `TP04ApplicationModule.OnApplicationInitializationAsync`, así que corre en `TP04.HttpApi.Host` (no en el DbMigrator).
- Resuelve `ExpirationWarningManager` desde el scope de cada ejecución (no guarda repositorios scoped).
- Abre una unidad de trabajo nueva y transaccional por ejecución.
- Captura y registra los errores; la próxima ejecución vuelve a intentar.
- No usa `ICurrentUser`, no llama a Open Food Facts y no envía correos.
- Configuración (`src/TP04.HttpApi.Host/appsettings.json`):

```json
"ExpirationWarnings": {
  "ThresholdDays": 3,
  "TimeZoneId": "America/Argentina/Buenos_Aires",
  "WorkerPeriodMinutes": 60
}
```

Se ejecuta al iniciar la aplicación y luego cada `WorkerPeriodMinutes`. En las pruebas está deshabilitado (`AbpBackgroundWorkerOptions.IsEnabled = false`), así CI no espera ningún temporizador.

## Por qué todavía no hay API privada

Todavía no se trabajó la seguridad (autenticación/autorización por dueño). Una API para listar o editar ítems y advertencias permitiría que un usuario lea o modifique datos de otro (regla de negocio 3). Por eso en este TP sólo existe el modelo mínimo, cargado por seeding local, y la consulta/edición privada (RF-10, RF-11, RF-13) se implementará cuando se incorpore la autorización.

## Base de datos

Migración `AddPantryItemsAndWarnings`:

- `AppPantryItems`: `OwnerId`, `ProductId` (FK a `AppProducts`, `Restrict`), `Status`, `ExpirationDate` (`date`, nullable) + auditoría.
- `AppPantryWarnings`: `PantryItemId` (FK a `AppPantryItems`, `Cascade`), `OwnerId`, `Type`, `ExpirationDate`, `IsExpired`, `IsActive` + auditoría.
- Índice **único** `(PantryItemId, Type)`.

## Datos de ejemplo (sólo local)

Al correr `TP04.DbMigrator` se cargan 7 ítems para el usuario `admin` (si no hay ítems todavía): vence hoy, en 2 días, en 3 días (límite), en 10 días, vencido ayer, sin fecha y consumido. Al levantar `TP04.HttpApi.Host` el worker genera 4 advertencias activas (hoy, 2 días, 3 días y vencido).

Consulta para verificar en SSMS:

```sql
SELECT p.Name, i.Status, i.ExpirationDate, w.IsActive, w.IsExpired, w.ExpirationDate AS WarningDate
FROM AppPantryItems i
JOIN AppProducts p ON p.Id = i.ProductId
LEFT JOIN AppPantryWarnings w ON w.PantryItemId = i.Id
ORDER BY i.ExpirationDate;
```

## Pruebas

`ExpirationWarningManager_Tests` (ejecutadas por `EfCoreExpirationWarningManager_Tests` contra la SQLite en memoria de la suite ABP) y `ExpirationCalendar_Tests` cubren: dentro y fuera del umbral, día límite, hoy, vencido, sin fecha, consumido, doble ejecución, ejecuciones superpuestas, índice único, actualización, desactivación, reactivación y cambio de día.
