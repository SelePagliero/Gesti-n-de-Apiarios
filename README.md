# Gestión de Apiarios

Sistema para la gestión de apiarios, campañas, controles, enfermedades, alimentos y productos apícolas. Está desarrollado en .NET 8, con una API REST en ASP.NET Core y un frontend en Blazor Server.

## Tabla de contenidos

- [Características](#características)
- [Arquitectura](#arquitectura)
- [Tecnologías](#tecnologías)
- [Estructura de la solución](#estructura-de-la-solución)
- [Configuración y ejecución](#configuración-y-ejecución)
- [Usuarios e inicio de sesión](#usuarios-e-inicio-de-sesión)
- [Base de datos y migraciones](#base-de-datos-y-migraciones)
- [Pruebas automatizadas](#pruebas-automatizadas)
- [Créditos](#créditos)

---

## Características

- Alta, consulta, modificación y baja lógica de apiarios, campañas, controles, enfermedades, alimentos y productos.
- Inicio de sesión con usuario y contraseña (ASP.NET Core Identity). Cada alta, modificación y baja registra qué usuario la hizo.
- Varios apicultores: cada uno ve y modifica solo sus apiarios y controles. La Administradora ve y modifica todo, administra los catálogos y puede transferir apiarios entre apicultores.
- Filtros de controles por apiario, campaña, enfermedad (o "con alguna enfermedad") y rango de fechas. Los aplica la API y quedan en la URL.
- Tablero con indicadores (apiarios activos, colmenas, apiarios con enfermedades) y gráfico de enfermedades.
- Validaciones en los formularios y en la API, con mensajes en español.
- Pruebas automatizadas de la API.

## Arquitectura

La solución tiene cuatro proyectos:

- **GestionApiario**: API REST con ASP.NET Core y Entity Framework Core. Expone los endpoints de cada entidad y los de cuenta de usuario (`/cuenta/login`, `/cuenta/register`, `/cuenta/refresh`). Todos los controladores exigen haber iniciado sesión.
- **GestionApiario.web**: frontend en Blazor Server. Consume la API con `HttpClient` y envía el token del usuario en cada solicitud.
- **GestionApiario.compartido**: DTO compartidos entre la API y el frontend, con sus validaciones.
- **GestionApiario.Pruebas**: pruebas de integración de la API con xUnit y una base de datos en memoria.

## Tecnologías

- .NET 8
- ASP.NET Core Web API y ASP.NET Core Identity (tokens de acceso)
- Blazor Server
- Entity Framework Core 8 (SQL Server)
- Bootstrap 4 (tema SB Admin 2) y Bootstrap Icons
- Chart.js 2.9 para el gráfico del tablero
- xUnit para las pruebas

## Estructura de la solución

```
SistemaGestionApiarios/
├── GestionApiario.sln
├── GestionApiario/                       # API (ASP.NET Core Web API)
│   ├── Controllers/
│   │   ├── ControladorBase.cs            # Base común: ruta, [ApiController] y usuario actual
│   │   ├── AlimentoController.cs
│   │   ├── ApiarioController.cs
│   │   ├── CampañaController.cs
│   │   ├── ControlesController.cs
│   │   ├── DashBoardController.cs        # Indicadores y gráfico del tablero
│   │   ├── EnfermedadController.cs
│   │   └── ProductoController.cs
│   ├── Migrations/                       # Migraciones de Entity Framework Core
│   ├── Models/                           # Entidades y GestionApiariosContext
│   └── Program.cs                        # Configuración de la API, Identity y Swagger
│
├── GestionApiario.web/                   # Frontend (Blazor Server)
│   ├── Components/
│   │   ├── Compartido/MensajeError.razor
│   │   ├── Cuenta/                       # Login.razor y Registro.razor
│   │   ├── Layout/                       # MainLayout, CuentaLayout, NavMenu
│   │   └── Pages/                        # Una carpeta por entidad (Lista y Actualizar)
│   ├── Servicios/
│   │   ├── Autenticacion/                # Sesión del usuario y llamadas a /cuenta
│   │   ├── Interfaces/IApiariosServicio.cs
│   │   ├── ApiarioServicio.cs            # Cliente HTTP de la API
│   │   └── ErroresApi.cs                 # Traduce los errores de la API a mensajes legibles
│   ├── wwwroot/                          # CSS, íconos, Chart.js y DashBoard.js
│   └── Program.cs
│
├── GestionApiario.compartido/
│   └── Dto/                              # DTO de entrada, detalle, grilla y tablero
│
└── GestionApiario.Pruebas/               # Pruebas de integración de la API
```

## Configuración y ejecución

1. **Clonar el repositorio**

   ```
   git clone https://github.com/SelePagliero/Gesti-n-de-Apiarios.git
   ```

2. **Restaurar las herramientas y paquetes**

   ```
   dotnet tool restore
   dotnet restore
   ```

3. **Configurar la cadena de conexión.** No se guarda en el repositorio; se usa el almacén de secretos de usuario de .NET:

   ```
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=TU_SERVIDOR;Database=GestionApiarios;Trusted_Connection=True;TrustServerCertificate=True;" --project GestionApiario
   ```

   Reemplazá `TU_SERVIDOR` por el nombre de tu instancia de SQL Server.

4. **Crear o actualizar la base de datos** (ver [Base de datos y migraciones](#base-de-datos-y-migraciones)).

5. **Ejecutar la solución.** Iniciá primero la API y después la web:

   ```
   dotnet run --project GestionApiario --launch-profile https
   dotnet run --project GestionApiario.web --launch-profile https
   ```

   - API y Swagger: `https://localhost:7035/swagger`
   - Aplicación web: `https://localhost:7101`

   La dirección de la API que usa la web se configura en `GestionApiario.web/appsettings.json`, en la clave `Api:UrlBase`.

## Usuarios e inicio de sesión

- La primera vez, entrá a la web y usá **Registrate** para crear un usuario. La contraseña debe tener al menos 8 caracteres, con mayúscula, minúscula y número.
- Después de 5 intentos fallidos, la cuenta se bloquea por 5 minutos.
- Desde Swagger: ejecutá `POST /cuenta/login`, copiá el `accessToken` de la respuesta, tocá **Authorize** y pegalo.
- El email del usuario se guarda en las columnas `UsuarioAlta`, `UsuarioModificacion` y `UsuarioBaja` de cada tabla.

### Apicultores y Administradora

| | Apicultor | Administradora |
|---|---|---|
| Apiarios | crea; ve, edita y elimina solo los suyos | ve, crea, edita y elimina todos; puede cambiar el dueño |
| Controles | solo sobre sus apiarios | sobre cualquier apiario |
| Alimentos, Enfermedades, Productos, Campañas | solo consulta | todo |
| Tablero, gráfico y filtros | solo sus datos | todos, o los de un apicultor |

- Las restricciones las aplica la API: un apiario o control ajeno responde 404 y modificar un catálogo sin permiso responde 403.
- Cada control pertenece al dueño de su apiario. Al transferir un apiario, sus controles pasan con él.
- **Configurar la Administradora:** guardá su email en los secretos de usuario:

  ```
  dotnet user-secrets set "Administracion:Email" "email@de-la-administradora" --project GestionApiario
  ```

  Al arrancar, la API le asigna el rol y le pasa los apiarios que no tienen dueño. Si la cuenta se registra después de arrancar la API, reiniciala una vez.
- Si se cambia el rol de un usuario, el cambio se aplica cuando vuelve a iniciar sesión o cuando se renueva su token (como máximo en una hora).

## Base de datos y migraciones

El esquema se maneja con migraciones de Entity Framework Core. La herramienta `dotnet-ef` está declarada como herramienta local en `.config/dotnet-tools.json`, así que con `dotnet tool restore` queda disponible.

- **Base nueva:** crea todas las tablas, incluidas las de usuarios.

  ```
  dotnet ef database update --project GestionApiario
  ```

- **Base existente creada con un script** (sin historial de migraciones): primero hay que marcar la migración inicial como aplicada, porque sus tablas ya existen. Después se aplican las demás:

  ```sql
  INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
  VALUES ('20251115134317_eliminacionProductoPorEnfermedad', '8.0.17');
  ```

  ```
  dotnet ef database update --project GestionApiario
  ```

- **Nueva migración** después de cambiar el modelo:

  ```
  dotnet ef migrations add NombreDeLaMigracion --project GestionApiario
  ```

## Pruebas automatizadas

```
dotnet test
```

Las pruebas levantan la API en memoria con una base de datos InMemory, así que no necesitan SQL Server. Verifican:
- el inicio de sesión;
- las validaciones;
- la auditoría de usuarios;
- los cálculos del tablero.

## Créditos

Desarrollado por [SelePagliero](https://github.com/SelePagliero).
