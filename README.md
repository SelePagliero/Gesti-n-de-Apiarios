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
- [Créditos](#créditos)

---

## Características

- Alta, consulta, modificación y baja lógica de apiarios, campañas, controles, enfermedades, alimentos y productos.
- Inicio de sesión con usuario y contraseña (ASP.NET Core Identity). Cada alta, modificación y baja registra qué usuario la hizo.
- Varios apicultores: cada uno ve y modifica solo sus apiarios y controles. La Administradora ve y modifica todo, administra Alimentos, Enfermedades y Productos, y puede transferir apiarios y campañas entre apicultores. Cada apicultor tiene sus propias campañas.
- Filtros de controles por apiario, campaña, enfermedad (o "con alguna enfermedad") y rango de fechas. Los aplica la API y quedan en la URL.
- Tablero con indicadores (apiarios activos, colmenas, apiarios con enfermedades) y gráfico de enfermedades.
- Validaciones en los formularios y en la API, con mensajes en español.

## Arquitectura

La solución tiene tres proyectos:

- **GestionApiario**: API REST con ASP.NET Core y Entity Framework Core. Expone los endpoints de cada entidad y los de cuenta de usuario (`/cuenta/login`, `/cuenta/register`, `/cuenta/refresh`). Todos los controladores exigen haber iniciado sesión.
- **GestionApiario.web**: frontend en Blazor Server. Consume la API con `HttpClient` y envía el token del usuario en cada solicitud.
- **GestionApiario.compartido**: DTO compartidos entre la API y el frontend, con sus validaciones.

## Tecnologías

- .NET 8
- ASP.NET Core Web API y ASP.NET Core Identity (tokens de acceso)
- Blazor Server
- Entity Framework Core 8 (SQL Server)
- Bootstrap 4 (tema SB Admin 2) y Bootstrap Icons
- Chart.js 2.9 para el gráfico del tablero

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
│   │   ├── CuentaController.cs           # Cambiar y recuperar la contraseña
│   │   ├── ControlesController.cs
│   │   ├── DashBoardController.cs        # Indicadores y gráfico del tablero
│   │   ├── EnfermedadController.cs
│   │   ├── ProductoController.cs
│   │   └── UsuariosController.cs         # Lista de usuarios (solo la Administradora)
│   ├── Migrations/                       # Migraciones de Entity Framework Core
│   ├── Models/                           # Entidades y GestionApiariosContext
│   ├── Servicios/EnviadorCorreo.cs        # Envío de correos por SMTP
│   └── Program.cs                        # Configuración de la API, Identity y Swagger
│
├── GestionApiario.web/                   # Frontend (Blazor Server)
│   ├── Components/
│   │   ├── Compartido/MensajeError.razor
│   │   ├── Cuenta/                       # Ingreso, registro y recuperar la contraseña
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
└── GestionApiario.compartido/
    └── Dto/                              # DTO de entrada, detalle, grilla y tablero
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
| Campañas | crea; ve, edita y elimina solo las suyas | ve, crea, edita y elimina todas; puede cambiar el dueño |
| Controles | solo sobre sus apiarios y con sus campañas | sobre cualquier apiario, con campañas del dueño del apiario |
| Alimentos, Enfermedades, Productos | solo consulta | todo |
| Tablero, gráfico y filtros | solo sus datos | todos, o los de un apicultor |

- Las restricciones las aplica la API: un apiario o control ajeno responde 404 y modificar un catálogo sin permiso responde 403.
- Cada control pertenece al dueño de su apiario. Al transferir un apiario, sus controles pasan con él. Si usan campañas de otro dueño, se usa la campaña igual (mismo año y responsable) del nuevo dueño o se crea una copia; las campañas originales no se modifican.
- **Configurar la Administradora:** guardá su email en los secretos de usuario:

  ```
  dotnet user-secrets set "Administracion:Email" "email@de-la-administradora" --project GestionApiario
  ```

  Al arrancar, la API le asigna el rol y le pasa los apiarios que no tienen dueño. Si la cuenta se registra después de arrancar la API, reiniciala una vez.
- Si se cambia el rol de un usuario, el cambio se aplica cuando vuelve a iniciar sesión o cuando se renueva su token (como máximo en una hora).

### Contraseñas

- **Cambiar contraseña:** cualquier usuario puede cambiar la suya desde el encabezado. Pide la actual y la nueva dos veces.
- **¿Olvidaste tu contraseña?:** desde la pantalla de ingreso se escribe el email y llega un correo con un link para elegir una contraseña nueva. El link vence en una hora y sirve una sola vez. La API responde lo mismo aunque el email no esté registrado, para que no se pueda averiguar quién tiene cuenta, y limita los pedidos por dirección IP (10 cada 15 minutos).
- **Configurar el envío de correos.** Por defecto se usa Gmail (`smtp.gmail.com`, puerto 587, en `GestionApiario/appsettings.json`). La cuenta tiene que tener la verificación en dos pasos activada; en <https://myaccount.google.com/apppasswords> se crea una *contraseña de aplicación* y se guarda en los secretos de usuario junto con el email:

  ```
  dotnet user-secrets set "Correo:Usuario" "cuenta@gmail.com" --project GestionApiario
  dotnet user-secrets set "Correo:Contraseña" "la-contraseña-de-aplicación" --project GestionApiario
  ```

  El link del correo apunta a la web, cuya dirección se configura en `Web:UrlBase` (por defecto `https://localhost:7101`). Si el correo no está configurado, la pantalla avisa que por ahora no se pueden enviar correos.

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

## Créditos

Desarrollado por [SelePagliero](https://github.com/SelePagliero).
