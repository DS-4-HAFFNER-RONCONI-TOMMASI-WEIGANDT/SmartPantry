# SmartPantry

Repositorio del Trabajo Práctico Integrador de Desarrollo de Software 2026.

## Integrantes
- Juan Cruz Weigandt (usuario: JuanWeigandt)
- Martín Ronconi (usuario: MartinRonconi7)
- Federico Haffner (usuario: federicohaffner)
- Camilo Tomassi (usuario: CamiloTommasi)

## Cómo ejecutar
Pendiente de TP 03.

---

## 🚀 Guía de Puesta en Marcha (TP 04)

### 📋 Requisitos Previos
- Visual Studio 2022 o 2026 (con la carga de trabajo de ASP.NET y web).
- Node.js (Versión 24.15.0 LTS o superior).
- Yarn (Versión 1.22.x).
- SQL Server Developer o SQL Server Express junto con SSMS.
- Git y ABP CLI.

### ⚙️ Configuración Local
Configurar la cadena de conexión local en los siguientes archivos:
- `src/SmartPantry.DbMigrator/appsettings.json`
- `src/SmartPantry.HttpApi.Host/appsettings.json`

En la sección `ConnectionStrings`, reemplazar el valor de `Default` por:
```json
{
  "ConnectionStrings": {
    "Default": "Server=(localdb)\\MSSQLLocalDB; Database=SmartPantry; Trusted_Connection=True"
  }
}

### 🏃‍♂️ Procedimiento de Ejecución
1. **Backend:** Abrir `SmartPantry.slnx` con Visual Studio, restaurar dependencias y compilar.
2. **Migraciones:** Ejecutar el proyecto `SmartPantry.DbMigrator` (F5) para crear las tablas en SQL Server.
3. **API:** Ejecutar el proyecto `SmartPantry.HttpApi.Host`.
4. **Frontend:** Ir a la carpeta `angular` en la terminal y ejecutar:
   ```bash
   yarn install
   yarn start
