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
