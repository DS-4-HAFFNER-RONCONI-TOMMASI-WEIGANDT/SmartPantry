# SmartPantry

Repositorio del Trabajo Práctico Integrador de Desarrollo de Software 2026.

## Integrantes
- Juan Cruz Weigandt (usuario: JuanWeigandt)
- Martín Ronconi (usuario: MartinRonconi7)
- Federico Haffner (usuario: federicohaffner)
- Camilo Tomassi (usuario: CamiloTommasi)

### Cómo ejecutar

### Pre-requirements

* [.NET10.0+ SDK](https://dotnet.microsoft.com/download/dotnet)
* [Node v18 or 20](https://nodejs.org/en)

### Configurations

The solution comes with a default configuration that works out of the box. However, you may consider to change the following configuration before running your solution:

* Check the `ConnectionStrings` in `appsettings.json` files under the `TP04.HttpApi.Host` and `TP04.DbMigrator` projects and change it if you need.

### Before running the application

* Run `abp install-libs` command on your solution folder to install client-side package dependencies. This step is automatically done when you create a new solution, if you didn't especially disabled it. However, you should run it yourself if you have first cloned this solution from your source control, or added a new client-side package dependency to your solution.
* Run `TP04.DbMigrator` to create the initial database. This step is also automatically done when you create a new solution, if you didn't especially disabled it. This should be done in the first run. It is also needed if a new database migration is added to the solution later.

### Solution structure

This is a layered monolith application that consists of the following applications:

* `angular`: Angular application.
* `TP04.DbMigrator`: A console application which applies the migrations and also seeds the initial data. It is useful on development as well as on production environment.
* `TP04.HttpApi.Host`: ASP.NET Core API application that is used to expose the APIs to the clients.

#### Test Projects

The `test` folder contains the following test projects:

* `TP04.Application.Tests`: Application layer tests.
* `TP04.Domain.Tests`: Domain layer tests.
* `TP04.EntityFrameworkCore.Tests`: Entity Framework Core integration tests.

###  Ejecución del Proyecto (Local)

El proyecto está configurado como un monolito en capas basado en ABP Framework. Para levantar el entorno de desarrollo local, siga estos pasos:

### 1. Configuración de Base de Datos
* Las cadenas de conexión se encuentran en los archivos `appsettings.json` de los proyectos `src/TP04.DbMigrator` y `src/TP04.HttpApi.Host`.
* Están configuradas por defecto para usar autenticación integrada de Windows en un servidor local (ej. `localhost\SQLEXPRESS`).
* Para inicializar la base de datos `SmartPantry` con las tablas de ABP, establezca `TP04.DbMigrator` como proyecto de inicio en Visual Studio y ejecútelo (F5).

### 2. Ejecutar el Backend (API)
1. Establezca el proyecto **`TP04.HttpApi.Host`** como proyecto de inicio en Visual Studio.
2. Presione **F5** (asegúrese de aceptar el certificado SSL de desarrollo si se le solicita).
3. La API quedará disponible y expondrá la documentación de Swagger en: `https://localhost:44384`.
* **Para detenerlo:** Cierre la consola de ejecución o presione `Shift + F5` en Visual Studio.

### 3. Ejecutar el Frontend (Angular)
1. Abra una terminal y ubíquese en el directorio `angular` del repositorio.
2. Ejecute el comando `yarn start`.
3. La interfaz web estará disponible en su navegador en: `http://localhost:4200`
* **Para detenerlo:** En la terminal donde se está ejecutando, presione `Ctrl + C` y confirme con `S`.

