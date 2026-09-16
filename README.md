# SmartPantry

Repositorio del Trabajo Práctico Integrador de Desarrollo de Software 2026.

## Integrantes
- Juan Cruz Weigandt (usuario: JuanWeigandt)
- Martín Ronconi (usuario: MartinRonconi7)
- Federico Haffner (usuario: federicohaffner)
- Camilo Tomassi (usuario: CamiloTommasi)

---

## 🏗️ Estructura de la Solución
Esta es una aplicación monolítica en capas basada en ABP Framework que consta de:
- `angular`: Aplicación frontend en Angular.
- `TP04.DbMigrator`: Aplicación de consola que aplica las migraciones y carga los datos iniciales en la base de datos.
- `TP04.HttpApi.Host`: Aplicación ASP.NET Core API que expone los servicios y endpoints.
- **Proyectos de Testing (`test/`):** Incluye pruebas para las distintas capas (`TP04.Application.Tests`, `TP04.Domain.Tests`, `TP04.EntityFrameworkCore.Tests`).

---

## 🚀 Guía de Puesta en Marcha (TP 04)

### 📋 Requisitos Previos
- Visual Studio 2022 o 2026 (con la carga de trabajo *Desarrollo de ASP.NET y web*).
- Node.js 24 LTS (versión 24.15.0 o superior).
- Yarn 1.22.x.
- SQL Server Developer o SQL Server Express.
- SQL Server Management Studio (SSMS).
- Git y ABP CLI.

### ⚙️ Configuración de la Base de Datos Local
Antes de ejecutar la aplicación, debes configurar la cadena de conexión local en los siguientes archivos:
- `src/TP04.DbMigrator/appsettings.json`
- `src/TP04.HttpApi.Host/appsettings.json`

Ubica la sección `ConnectionStrings` y reemplaza el valor de `Default` por tu instancia local de SQL Server. Por ejemplo:
```json
{
  "ConnectionStrings": {
    "Default": "Server=(localdb)\\MSSQLLocalDB; Database=SmartPantry; Trusted_Connection=True"
  }
}
