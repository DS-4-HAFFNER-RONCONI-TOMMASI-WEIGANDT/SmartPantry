# SmartPantry

Repositorio del Trabajo Práctico Integrador de Desarrollo de Software 2026.

## Integrantes
- Juan Cruz Weigandt (usuario: JuanWeigandt)
- Martín Ronconi (usuario: MartinRonconi7)
- Federico Haffner (usuario: federicohaffner)
- Camilo Tomassi (usuario: CamiloTommasi)

---

## Estructura de la Solución
Esta es una aplicación monolítica en capas basada en ABP Framework que consta de:
- `angular`: Aplicación frontend en Angular.
- `TP04.DbMigrator`: Aplicación de consola que aplica las migraciones y carga los datos iniciales.
- `TP04.HttpApi.Host`: Aplicación ASP.NET Core API que expone los servicios a los clientes.
- **Proyectos de prueba (`test/`):**
  - `TP04.Application.Tests`: Pruebas de la capa de aplicación.
  - `TP04.Domain.Tests`: Pruebas de la capa de dominio.
  - `TP04.EntityFrameworkCore.Tests`: Pruebas de integración de Entity Framework Core.

---

## Guía de Ejecución

### Requisitos
- Visual Studio 2022 o 2026 con Desarrollo de ASP.NET y web.
- Node.js 24.15.0 o superior.
- Yarn 1.22.x.
- SQL Server Developer o Express.
- SQL Server Management Studio (SSMS).
- ABP Studio y Git.

### Configuración local
Las cadenas de conexión se encuentran en los siguientes dos archivos `appsettings.json`:
- `src/TP04.DbMigrator/appsettings.json`
- `src/TP04.HttpApi.Host/appsettings.json`

La cadena de conexión local (`ConnectionStrings:Default`) utilizada para LocalDB es la siguiente:
```json
"ConnectionStrings": {
  "Default": "Server=(localdb)\\MSSQLLocalDB; Database=SmartPantry; Trusted_Connection=True"
}
```

---

## Pantalla "Mi despensa" (RF-11)

La pantalla está en `angular/src/app/pantry/` (`PantryComponent`), en la ruta `/pantry` (menú **Mi despensa**). Usa el proxy generado `PantryItemService` (`angular/src/app/proxy/pantry-items`) sobre `PantryItemAppService` (`/api/app/pantry-item`). Si cambia el contrato del BackEnd, el proxy se vuelve a generar con `abp generate-proxy -t ng` (con el host en ejecución) y no se corrige a mano.

### Ítems consumidos
Según la regla de negocio 6, un ítem consumido **no se elimina**: queda como **historial** con estado `Consumed`. La pantalla muestra por defecto los ítems *En la despensa* y un selector **Mostrar** permite ver *Consumidos* o *Todos*. Los consumidos se ven atenuados, con la etiqueta *Consumido*, y ya no se editan ni se vuelven a consumir (el servidor también lo impide). Para quitarlos hay que eliminarlos de forma explícita, con confirmación.

### ListService
El listado (`GetListAsync`) es **paginado en el servidor** (`SkipCount` / `MaxResultCount`), por eso la pantalla usa `ListService` con `hookToQuery`. `ListService` arma la consulta y vuelve a pedir la página cuando cambia la página, el filtro o después de editar, consumir o eliminar.

### Criterio de fechas
La fecha de vencimiento es una **fecha calendario sin hora** (`DateOnly` en el BackEnd, `"yyyy-MM-dd"` en JSON). La pantalla la muestra con `DatePipe` (`mediumDate`) y el `LOCALE_ID` de ABP, es decir, con el formato del idioma elegido en la aplicación. Como no tiene hora, **no se convierte entre UTC y hora local**: se ve el mismo día que está guardado. Al editar, el `<input type="date">` devuelve el mismo formato `yyyy-MM-dd`, que se envía tal cual. Esto **coincide con el criterio del worker de TP08**: el worker compara fechas calendario y calcula "hoy" como la fecha de Argentina (`America/Argentina/Buenos_Aires`) a partir de UTC.

### Manejo de errores
- **Listado:** se llama con `skipHandleError: true` porque la pantalla maneja el error por sí misma. Muestra el estado de error con un mensaje localizado (`Pantry:LoadError`) y el botón **Reintentar**, sin abrir además el diálogo de ABP.
- **Guardar, consumir y eliminar:** se deja el manejo predeterminado de ABP, que muestra el mensaje del servidor (validación, regla de negocio o 404). Si el servidor rechaza una edición, el modal queda abierto con lo que se ingresó.
- No se muestran detalles técnicos ni solo el código de estado.

### Acceso a ítems ajenos
El aislamiento por usuario lo hace el servidor: `PantryItemAppService` tiene `[Authorize]` (sin sesión responde 401) y obtiene el propietario con `ICurrentUser`. Un ítem de otro usuario se trata igual que uno inexistente: **404 en todas las operaciones**, para no revelar que existe. Ocultar o deshabilitar botones en la interfaz no reemplaza esa comprobación.

### Pruebas
- Componente: desde `angular`, `yarn test` (Vitest, sin red: el proxy se reemplaza por un mock).
- BackEnd: Test Explorer (`PantryItemAppService_Tests`, `PantryItem_Tests`, `AppServiceAuthorization_Tests`).
