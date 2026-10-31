# Gestión de Apiarios

Sistema web para que los apicultores registren sus apiarios, organicen el trabajo por campañas y lleven el historial de controles de cada apiario: cantidad de colmenas, alimentación, enfermedades detectadas y tratamientos aplicados. Una Administradora mantiene los catálogos (enfermedades, alimentos y productos), ve los datos de todos los apicultores y gestiona sus cuentas.

Está desarrollado en .NET 8: una API REST en ASP.NET Core y un frontend en Blazor Server.

## Tabla de contenidos

- [Funcionalidades](#funcionalidades)
- [Roles: Administradora y apicultor](#roles-administradora-y-apicultor)
- [Arquitectura](#arquitectura)
- [Tecnologías](#tecnologías)
- [Estructura de la solución](#estructura-de-la-solución)
- [Requisitos](#requisitos)
- [Instalación y ejecución](#instalación-y-ejecución)
- [Secretos de usuario](#secretos-de-usuario)
- [Base de datos y migraciones](#base-de-datos-y-migraciones)
- [Pruebas automatizadas](#pruebas-automatizadas)
- [Créditos](#créditos)

---

## Funcionalidades

- **Apiarios, campañas y controles:** alta, consulta, modificación y baja lógica. Un control registra la fecha, el apiario, la campaña, la cantidad de colmenas, el alimento y su cantidad, la enfermedad, el producto aplicado y su cantidad, y observaciones.
- **Catálogos:** enfermedades, alimentos y productos, que se eligen al cargar un control.
- **Tablero de inicio:** apiarios activos, total de colmenas y apiarios con enfermedades, más un gráfico con el porcentaje de apiarios por enfermedad. Se calcula con el último control de cada apiario, porque es el que refleja su estado actual.
- **Filtros en todos los listados:**
  - **Controles:** apiario, campaña, enfermedad (o "con alguna enfermedad") y rango de fechas. Los aplica la API.
  - **Apiarios:** nombre, apicultor y fecha de alta.
  - **Campañas:** año, responsable y apicultor.
  - **Enfermedades, Alimentos y Productos:** nombre y fecha de alta.
  - **Usuarios:** email, rol y contraseña (propia o temporal).

  Salvo los de Controles, se aplican en pantalla y la búsqueda por texto no distingue mayúsculas ni tildes. En todos los listados los filtros quedan en la dirección de la página, así que se conservan al recargar.
- **Cuentas de usuario:** registro, ingreso, "Cambiar contraseña" y recuperación de contraseñas por parte de la Administradora (ver [Contraseñas](#contraseñas)).
- **Auditoría:** cada alta, modificación y baja guarda el email del usuario y la fecha. Esos emails solo los ve la Administradora.
- **Validaciones** en los formularios y en la API, con mensajes en español. Las fechas y los números se muestran con formato argentino.
- **Diseño adaptable:** se puede usar desde el celular.

## Roles: Administradora y apicultor

Quien se registra desde la web es **apicultor**. La **Administradora** es una sola cuenta, la que se indica en los secretos de usuario (ver [Secretos de usuario](#secretos-de-usuario)).

| | Apicultor | Administradora |
|---|---|---|
| Apiarios | Crea; ve, edita y elimina solo los suyos | Ve, crea, edita y elimina todos; puede cambiar el dueño |
| Campañas | Crea; ve, edita y elimina solo las suyas | Ve, crea, edita y elimina todas; puede cambiar el dueño |
| Controles | Solo sobre sus apiarios y con sus campañas | Sobre cualquier apiario, con campañas del dueño de ese apiario |
| Enfermedades, Alimentos, Productos | Solo consulta | Crea, edita y elimina |
| Tablero y filtros | Solo sus datos | Todos, o los de un apicultor |
| Usuarios | No tiene acceso | Ve la lista y restablece contraseñas |
| Emails de auditoría ("creado por", etc.) | No los ve | Los ve |

**Reglas de propiedad:**
- Cada apiario y cada campaña tiene un dueño. Cada control pertenece al dueño de su apiario.
- Las restricciones las aplica la API, no solo la web. Un registro ajeno responde 404, como si no existiera, y una acción sin permiso responde 403.
- **Transferir un apiario** (solo la Administradora): sus controles pasan con él. Si esos controles usan campañas del dueño anterior, se usa la campaña igual del nuevo dueño (mismo año y responsable) o se crea una copia. Las campañas originales no se modifican.
- Si se cambia el rol de un usuario, el cambio se aplica cuando vuelve a iniciar sesión o cuando se renueva su sesión (como máximo en una hora).

### Contraseñas

- La contraseña debe tener al menos 8 caracteres, con una mayúscula, una minúscula y un número.
- Después de 5 intentos fallidos, la cuenta se bloquea por 5 minutos.
- **Cambiar contraseña:** cualquier usuario puede cambiar la suya desde el encabezado. Se pide la actual y la nueva dos veces.
- **Contraseña olvidada:** en la pantalla de ingreso, un aviso indica que hay que pedírsela a la Administradora. Ella entra a **Usuarios** y toca **Restablecer contraseña** en la fila del apicultor. El sistema genera una contraseña temporal, que se muestra una sola vez, y además desbloquea la cuenta si estaba bloqueada.
- **Cambio obligatorio:** quien ingresa con una contraseña temporal tiene que elegir una propia antes de usar el sistema. Mientras tanto, la API rechaza cualquier otra operación.

## Arquitectura

La solución (`GestionApiario.sln`) tiene tres proyectos:

- **GestionApiario:** API REST con ASP.NET Core y Entity Framework Core.
  - Expone un controlador por entidad, el tablero y los usuarios.
  - Los endpoints de cuenta están bajo `/cuenta`: `login`, `register` y `refresh`, que los agrega ASP.NET Core Identity, más `yo` y `cambiar-contrasena`.
  - Todos los controladores exigen haber iniciado sesión.
- **GestionApiario.web:** frontend en Blazor Server, con renderizado interactivo en el servidor. Consume la API con `HttpClient` y envía el token del usuario en cada solicitud. Si el token vence, lo renueva automáticamente.
- **GestionApiario.compartido:** DTO compartidos entre la API y el frontend, con sus validaciones.

La web y la API son dos aplicaciones separadas: primero se inicia la API y después la web.

Las pruebas automatizadas están en otros dos proyectos, que viven en la rama `pruebas-permisos` (ver [Pruebas automatizadas](#pruebas-automatizadas)).

## Tecnologías

| Área | Tecnología |
|---|---|
| Plataforma | .NET 8 (C# con referencias nulas habilitadas) |
| API | ASP.NET Core Web API, Swagger (Swashbuckle 8.1) en desarrollo |
| Usuarios | ASP.NET Core Identity 8 con tokens de acceso (`AddIdentityApiEndpoints`) y roles |
| Datos | Entity Framework Core 8 con SQL Server; migraciones con la herramienta local `dotnet-ef` 8.0.17 |
| Frontend | Blazor Server (.NET 8), sesión guardada cifrada en el navegador (`ProtectedSessionStorage`) |
| Estilos | Bootstrap 5.3.3 (incluido en el proyecto, en `wwwroot/lib/bootstrap`) y una hoja propia, `wwwroot/app.css`, con la paleta del sistema |
| Íconos y tipografías | Bootstrap Icons 1.13.1 (desde CDN), tipografías Figtree y Fraunces (Google Fonts) |
| Gráfico | Chart.js 2.9.4 (incluido en el proyecto) |
| Pruebas | xUnit, `WebApplicationFactory` con base en memoria, y Playwright con Microsoft Edge (rama `pruebas-permisos`) |

Las tipografías y los íconos se cargan desde internet. Sin conexión, el sistema funciona igual, pero se ve con las tipografías del sistema operativo y sin íconos.

## Estructura de la solución

```
SistemaGestionApiarios/
├── GestionApiario.sln
├── .config/dotnet-tools.json             # Herramienta local dotnet-ef
│
├── GestionApiario/                       # API (ASP.NET Core Web API)
│   ├── Controllers/
│   │   ├── ControladorBase.cs            # Base común: ruta, usuario actual, rol y dueño
│   │   ├── ApiarioController.cs          # Incluye la transferencia de apiarios
│   │   ├── CampañaController.cs
│   │   ├── ControlesController.cs        # Incluye los filtros de controles
│   │   ├── AlimentoController.cs
│   │   ├── EnfermedadController.cs
│   │   ├── ProductoController.cs
│   │   ├── DashBoardController.cs        # Indicadores y gráfico del tablero
│   │   ├── UsuariosController.cs         # Lista de usuarios y restablecer contraseñas
│   │   └── CuentaController.cs           # Cambiar la propia contraseña
│   ├── Migrations/                       # Migraciones de Entity Framework Core
│   ├── Models/
│   │   ├── GestionApiariosContext.cs     # Contexto de EF Core e Identity
│   │   ├── InicializadorDatos.cs         # Rol de Administradora al arrancar
│   │   ├── ContraseñaTemporal.cs         # Contraseñas temporales
│   │   └── ...                           # Entidades (Apiario, Campaña, Controle, ...)
│   └── Program.cs                        # Configuración de la API, Identity y Swagger
│
├── GestionApiario.web/                   # Frontend (Blazor Server)
│   ├── Components/
│   │   ├── Compartido/                   # Encabezado de página, tarjeta de filtros, botones de fila, mensajes
│   │   ├── Cuenta/                       # Ingreso, registro y cambio obligatorio de contraseña
│   │   ├── Layout/                       # MainLayout, CuentaLayout y NavMenu
│   │   └── Pages/                        # Una carpeta por pantalla (lista y formulario), Cuenta y Usuarios
│   ├── Servicios/
│   │   ├── Autenticacion/                # Sesión del usuario y llamadas a /cuenta
│   │   ├── Interfaces/IApiariosServicio.cs
│   │   ├── ApiarioServicio.cs            # Cliente HTTP de la API
│   │   ├── ErroresApi.cs                 # Traduce los errores de la API a mensajes legibles
│   │   └── FiltrosLista.cs               # Ayudas para los filtros de los listados
│   ├── wwwroot/                          # app.css, Bootstrap, Chart.js, DashBoard.js e íconos del sitio
│   └── Program.cs
│
└── GestionApiario.compartido/
    └── Dto/                              # DTO de entrada, detalle, grilla, tablero y usuarios
```

## Requisitos

- [SDK de .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0). También sirve un SDK más nuevo, siempre que tenga instalado el entorno de ejecución de ASP.NET Core 8.
- SQL Server. Puede ser SQL Server Express, Developer o LocalDB; LocalDB viene con Visual Studio.
- Opcional: Visual Studio 2022 con la carga de trabajo "Desarrollo de ASP.NET y web".

## Instalación y ejecución

1. **Clonar el repositorio**

   ```
   git clone https://github.com/SelePagliero/Gesti-n-de-Apiarios.git
   cd Gesti-n-de-Apiarios
   ```

2. **Restaurar la herramienta `dotnet-ef` y los paquetes**

   ```
   dotnet tool restore
   dotnet restore
   ```

3. **Configurar los secretos de usuario:** la cadena de conexión y el email de la Administradora. Ver [Secretos de usuario](#secretos-de-usuario).

4. **Crear la base de datos.** Ver [Base de datos y migraciones](#base-de-datos-y-migraciones).

   ```
   dotnet ef database update --project GestionApiario
   ```

5. **Ejecutar.** Iniciá primero la API y después la web, cada una en su propia terminal:

   ```
   dotnet run --project GestionApiario --launch-profile https
   dotnet run --project GestionApiario.web --launch-profile https
   ```

   | Aplicación | Dirección |
   |---|---|
   | Web | `https://localhost:7101` |
   | API (Swagger) | `https://localhost:7035/swagger` |

   **Desde Visual Studio:** clic derecho en la solución → **Configurar proyectos de inicio** → **Varios proyectos de inicio**. Ahí poné `GestionApiario` y `GestionApiario.web` en **Iniciar** y tocá **F5**.

6. **Crear la cuenta de la Administradora.** En la web, tocá **Registrate** con el email que configuraste en `Administracion:Email` y **reiniciá la API una vez**. Al arrancar, la API le asigna el rol. Los demás usuarios se registran solos y quedan como apicultores.

**Otras configuraciones:**
- La dirección de la API que usa la web está en `GestionApiario.web/appsettings.json`, en la clave `Api:UrlBase` (por defecto `https://localhost:7035`).
- **Probar la API desde Swagger:** ejecutá `POST /cuenta/login`, copiá el `accessToken` de la respuesta, tocá **Authorize** y pegalo.

## Secretos de usuario

Los datos sensibles no se guardan en el repositorio. Se usa el almacén de secretos de usuario de .NET, que guarda un archivo `secrets.json` fuera de la carpeta del proyecto:

- **Windows:** `%APPDATA%\Microsoft\UserSecrets\c4a77998-d530-4296-82c5-6f2482dbb44f\secrets.json`
- **macOS y Linux:** `~/.microsoft/usersecrets/c4a77998-d530-4296-82c5-6f2482dbb44f/secrets.json`

Solo los usa la API (`GestionApiario`); la web no tiene secretos.

| Clave | Para qué sirve | Obligatoria |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Cadena de conexión a SQL Server | Sí: sin ella la API no arranca y avisa cómo configurarla |
| `Administracion:Email` | Email de la cuenta que recibe el rol de Administradora | Sí, para tener Administradora. Sin ella nadie tiene ese rol, y la API lo avisa en la consola |

**Desde la terminal**, en la carpeta de la solución:

```
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=TU_SERVIDOR;Database=GestionApiarios;Trusted_Connection=True;TrustServerCertificate=True;" --project GestionApiario
dotnet user-secrets set "Administracion:Email" "email@de-la-administradora" --project GestionApiario
```

- Reemplazá `TU_SERVIDOR` por tu instancia de SQL Server. Por ejemplo: `(localdb)\MSSQLLocalDB` para LocalDB, `.\SQLEXPRESS` para SQL Server Express o `localhost` para una instancia predeterminada.
- **Ver lo que está guardado:** `dotnet user-secrets list --project GestionApiario`.
- **Borrar una clave:** `dotnet user-secrets remove "Clave" --project GestionApiario`.

**Desde Visual Studio:** clic derecho en el proyecto `GestionApiario` → **Administrar secretos de usuario**. Se abre `secrets.json`, que tiene esta forma:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=GestionApiarios;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Administracion": {
    "Email": "email@de-la-administradora"
  }
}
```

**Cuándo se leen:**
- .NET solo lee los secretos de usuario en el entorno **Development**, que es el que usan los perfiles de ejecución del proyecto.
- En otro entorno, por ejemplo un servidor, las mismas claves se configuran con variables de entorno: `ConnectionStrings__DefaultConnection` y `Administracion__Email`.

## Base de datos y migraciones

El esquema se maneja con migraciones de Entity Framework Core. La herramienta `dotnet-ef` está declarada como herramienta local en `.config/dotnet-tools.json`, así que con `dotnet tool restore` queda disponible.

> Antes de aplicar migraciones sobre una base con datos reales, hacé una copia de seguridad.

- **Base nueva:** crea todas las tablas, incluidas las de usuarios.

  ```
  dotnet ef database update --project GestionApiario
  ```

- **Base existente creada con un script**, sin historial de migraciones. Primero hay que marcar la migración inicial como aplicada, porque sus tablas ya existen:

  ```sql
  INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
  VALUES ('20251115134317_eliminacionProductoPorEnfermedad', '8.0.17');
  ```

  Después se aplican las demás:

  ```
  dotnet ef database update --project GestionApiario
  ```

- **Nueva migración** después de cambiar el modelo:

  ```
  dotnet ef migrations add NombreDeLaMigracion --project GestionApiario
  ```

| Migración | Qué agrega |
|---|---|
| `eliminacionProductoPorEnfermedad` | Esquema inicial: apiarios, campañas, controles y catálogos |
| `AgregarUsuarios` | Tablas de usuarios y roles de ASP.NET Core Identity |
| `AgregarDueñoDeApiarioYAuditoria` | Dueño de cada apiario; las columnas de auditoría pasan de 50 a 256 caracteres para guardar el email |
| `AgregarDueñoDeCampaña` | Dueño de cada campaña |

Las marcas de "contraseña temporal" se guardan en la tabla `AspNetUserClaims`, que ya existe, así que no necesitan una migración propia.

## Pruebas automatizadas

Los proyectos de pruebas están en la rama **`pruebas-permisos`**, no en `master`. Para correrlos:

```
git checkout pruebas-permisos
dotnet test                                  # todas
dotnet test GestionApiario.Pruebas           # solo las de la API
dotnet test GestionApiario.PruebasE2E        # solo las de navegador
```

| Proyecto | Qué prueba | Base de datos | Duración aproximada |
|---|---|---|---|
| `GestionApiario.Pruebas` (105 pruebas) | La API: permisos, campañas, transferencia de apiarios, auditoría, filtros, tablero y contraseñas | En memoria (InMemory) | 40 segundos |
| `GestionApiario.PruebasE2E` (26 pruebas) | La web completa en Microsoft Edge, siguiendo los pasos de la lista de pruebas manuales, más los filtros y las contraseñas | `GestionApiarios_PruebasE2E` en LocalDB | 2 a 3 minutos |

Ninguna de las dos usa la base de datos real.

**Desde Visual Studio:** con la rama `pruebas-permisos` abierta, entrá a **Prueba → Explorador de pruebas** (Ctrl+E, T).
- **Ejecutar todas las pruebas** (Ctrl+R, A) las corre todas.
- Con clic derecho en un proyecto o en un paso, corrés solo ese.

**Requisitos de las pruebas de navegador:**
- SQL Server LocalDB y Microsoft Edge instalados. Playwright usa el Edge de la PC, así que no hace falta descargar otro navegador.
- Usan los puertos 5390 (API) y 5391 (web), así que no hace falta cerrar la API y la web que usás normalmente.
- Cada ejecución borra y vuelve a crear `GestionApiarios_PruebasE2E` en LocalDB.
- La API de prueba corre en el entorno "Pruebas". Ahí no se leen los secretos de usuario, así que no puede conectarse a la base real.
- Para ver el navegador mientras corren, definí la variable de entorno `E2E_VISIBLE=1` antes de abrir Visual Studio o la terminal.

**Cómo se mantiene la rama:** cada cambio que se une a `master` se pasa también a `pruebas-permisos`, sin el commit que quita los proyectos de pruebas de `master`, junto con sus pruebas nuevas.

## Créditos

Desarrollado por [SelePagliero](https://github.com/SelePagliero) como trabajo de tesis.
