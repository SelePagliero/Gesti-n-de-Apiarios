# Gestión de Apiarios

Sistema integral para la gestión de apiarios, colmenas, campañas, controles, enfermedades, alimentos y productos apícolas. Desarrollado en .NET 8 con arquitectura multicapa y frontend Blazor Server.

## Tabla de Contenidos

- [Características](#características)
- [Arquitectura](#arquitectura)
- [Tecnologías](#tecnologías)
- [Estructura de la Solución](#estructura-de-la-solución)
- [Configuración y Ejecución](#configuración-y-ejecución)
- [Migraciones y Base de Datos](#migraciones-y-base-de-datos)
- [Buenas Prácticas](#buenas-prácticas)
- [Créditos](#créditos)

---

## Características

- Gestión de apiarios, colmenas, campañas, controles, enfermedades, alimentos y productos.
- Dashboard con métricas clave y gráficos interactivos.
- CRUD completo para todas las entidades principales.
- Arquitectura desacoplada con DTOs y servicios.
- Interfaz moderna y responsiva con Blazor Server.

## Arquitectura

La solución está compuesta por tres proyectos principales:

- **GestionApiario.web**: Frontend Blazor Server y servicios de integración.
- **GestionApiario**: API RESTful y lógica de negocio, basada en ASP.NET Core y Entity Framework Core.
- **GestionApiario.compartido**: Definición de DTOs y modelos compartidos entre los proyectos.

## Tecnologías

- .NET 8
- Blazor Server
- ASP.NET Core Web API
- Entity Framework Core (SQL Server)
- Bootstrap 4/5
- JavaScript (para gráficos con Chart.js)

## Estructura de la Solución

```
SistemaGestionApiarios/
?
??? GestionApiario/                      # ?? API Backend (ASP.NET Core Web API)
?   ??? Controllers/                     # Controladores REST
?   ?   ??? AlimentoController.cs       # CRUD de alimentos
?   ?   ??? ApiarioController.cs        # CRUD de apiarios
?   ?   ??? CampañaController.cs        # CRUD de campañas
?   ?   ??? ControlesController.cs      # CRUD de controles
?   ?   ??? DashBoardController.cs      # Métricas y datos del dashboard
?   ?   ??? EnfermedadController.cs     # CRUD de enfermedades
?   ?   ??? ProductoController.cs       # CRUD de productos
?   ?
?   ??? Models/                          # Modelos de Entity Framework Core
?   ?   ??? GestionApiariosContext.cs   # Contexto de base de datos
?   ?   ??? Apiario.cs                  # Entidad Apiario
?   ?   ??? Alimento.cs                 # Entidad Alimento
?   ?   ??? Campaña.cs                  # Entidad Campaña
?   ?   ??? Controle.cs                 # Entidad Control
?   ?   ??? Enfermedad.cs               # Entidad Enfermedad
?   ?   ??? Producto.cs                 # Entidad Producto
?   ?   ??? Productosporenfermedad.cs   # Relación N:N
?   ?
?   ??? Program.cs                       # Configuración de la API
?   ??? appsettings.json                # Configuración y cadena de conexión
?   ??? GestionApiario.csproj           # Archivo de proyecto
?
??? GestionApiario.web/                  # ?? Frontend (Blazor Server)
?   ??? Components/
?   ?   ??? Layout/                      # Componentes de diseño
?   ?   ?   ??? MainLayout.razor        # Layout principal
?   ?   ?   ??? MainLayout.razor.css    # Estilos del layout
?   ?   ?   ??? NavMenu.razor           # Menú de navegación
?   ?   ?   ??? NavMenu.razor.css       # Estilos del menú
?   ?   ?
?   ?   ??? Pages/                       # Páginas Blazor
?   ?   ?   ??? Home.razor              # Dashboard principal
?   ?   ?   ??? Home.razor.css          # Estilos del dashboard
?   ?   ?   ?
?   ?   ?   ??? Apiario/                # Módulo de Apiarios
?   ?   ?   ?   ??? ListaApiarios.razor
?   ?   ?   ?   ??? ActualizarApiario.razor
?   ?   ?   ?
?   ?   ?   ??? Alimento/               # Módulo de Alimentos
?   ?   ?   ?   ??? ListaAlimentos.razor
?   ?   ?   ?   ??? ActualizarAlimento.razor
?   ?   ?   ?
?   ?   ?   ??? Campaña/                # Módulo de Campañas
?   ?   ?   ?   ??? ListaCampañas.razor
?   ?   ?   ?   ??? ActualizarCampaña.razor
?   ?   ?   ?
?   ?   ?   ??? Controles/              # Módulo de Controles
?   ?   ?   ?   ??? ListaControles.razor
?   ?   ?   ?   ??? ActualizarControles.razor
?   ?   ?   ?
?   ?   ?   ??? Enfermedad/             # Módulo de Enfermedades
?   ?   ?   ?   ??? ListaEnfermedades.razor
?   ?   ?   ?   ??? ActualizarEnfermedad.razor
?   ?   ?   ?
?   ?   ?   ??? Producto/               # Módulo de Productos
?   ?   ?   ?   ??? ListaProductos.razor
?   ?   ?   ?   ??? ActualizarProducto.razor
?   ?   ?   ?
?   ?   ?   ??? Counter.razor           # Página de ejemplo
?   ?   ?   ??? Error.razor             # Página de error
?   ?   ?
?   ?   ??? App.razor                    # Componente raíz
?   ?   ??? Routes.razor                 # Configuración de rutas
?   ?   ??? _Imports.razor               # Imports globales
?   ?
?   ??? Servicios/                       # Servicios de integración
?   ?   ??? Interfaces/
?   ?   ?   ??? IApiariosServicio.cs    # Interfaz del servicio
?   ?   ??? ApiarioServicio.cs          # Implementación HTTP
?   ?
?   ??? wwwroot/                         # Archivos estáticos
?   ?   ??? bootstrap/
?   ?   ?   ??? bootstrap.min.css       # Framework CSS
?   ?   ??? js/
?   ?   ?   ??? Chart.min.js            # Librería de gráficos
?   ?   ?   ??? DashBoard.js            # Lógica de gráficos
?   ?   ??? app.css                      # Estilos personalizados
?   ?   ??? icons8-*.png                 # Iconos del menú
?   ?   ??? favicon.png
?   ?
?   ??? Program.cs                       # Configuración de Blazor
?   ??? GestionApiario.web.csproj       # Archivo de proyecto
?
??? GestionApiario.compartido/          # ?? Librería compartida
?   ??? Dto/                             # Data Transfer Objects
?   ?   ??? AlimentoDto.cs              # DTOs de Alimento
?   ?   ??? AlimentoBaseDto.cs
?   ?   ??? AlimentoDetalleDto.cs
?   ?   ??? AlimentoGrillaDto.cs
?   ?   ?
?   ?   ??? ApiarioDto.cs               # DTOs de Apiario
?   ?   ??? ApiarioBaseDto.cs
?   ?   ??? ApiarioDetalleDto.cs
?   ?   ??? ApiarioGrillaDto.cs
?   ?   ?
?   ?   ??? CampañaDto.cs               # DTOs de Campaña
?   ?   ??? CampañaBaseDto.cs
?   ?   ??? CampañaDetalleDto.cs
?   ?   ??? CampañaGrillaDto.cs
?   ?   ?
?   ?   ??? ControlDto.cs               # DTOs de Control
?   ?   ??? ControlDetalleDto.cs
?   ?   ??? ControlGrillaDto.cs
?   ?   ?
?   ?   ??? EnfermedadDto.cs            # DTOs de Enfermedad
?   ?   ??? EnfermedadBaseDto.cs
?   ?   ??? EnfermedadDetalleDto.cs
?   ?   ??? EnfermedadGrillaDto.cs
?   ?   ?
?   ?   ??? ProductoDto.cs              # DTOs de Producto
?   ?   ??? ProductoBaseDto.cs
?   ?   ??? ProductoDetalleDto.cs
?   ?   ??? ProductoGrillaDto.cs
?   ?   ?
?   ?   ??? DashBoardDto.cs             # DTO del Dashboard
?   ?   ??? EnfermedadesGraficoResponse.cs  # DTO para gráficos
?   ?
?   ??? GestionApiario.compartido.csproj
?
??? .gitignore                           # Archivos ignorados por Git
??? README.md                            # Este archivo
```

### Descripción de Capas

#### ?? **GestionApiario (Backend API)**
- Expone endpoints RESTful para todas las operaciones CRUD.
- Utiliza Entity Framework Core para el acceso a datos.
- Implementa el patrón Repository implícitamente a través de DbContext.

#### ?? **GestionApiario.web (Frontend Blazor)**
- Interfaz de usuario interactiva con Blazor Server.
- Consumo de API mediante HttpClient.
- Componentes reutilizables y páginas modulares por entidad.
- Dashboard con visualizaciones usando Chart.js.

#### ?? **GestionApiario.compartido (Capa de Contratos)**
- Contiene los DTOs (Data Transfer Objects) compartidos.
- Facilita la comunicación entre frontend y backend.
- Permite mantener una separación clara de responsabilidades.

## Configuración y Ejecución

1. **Clonar el repositorio**
``` git clone https://github.com/SelePagliero/Gesti-n-de-Apiarios.git ```

2. **Configurar la base de datos**
   - Modifica la cadena de conexión en `appsettings.json` o en el contexto de EF Core según tu entorno.

3. **Restaurar paquetes y compilar**
``` dotnet restore dotnet build ```


4. **Ejecutar la solución**
   - Inicia primero el proyecto `GestionApiario` (API).
   - Luego ejecuta `GestionApiario.web` (Blazor Server).

5. **Acceder a la aplicación**
   - Navega a `https://localhost:7035` (o el puerto configurado).

## Migraciones y Base de Datos

Para actualizar el modelo desde la base de datos, utiliza el siguiente comando en la consola de NuGet:
``` Scaffold-DbContext "Server=TU_SERVIDOR;Database=GestionApiarios;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Force ```


> **Nota:** Cambia `TU_SERVIDOR` por el nombre de tu servidor SQL.

## Buenas Prácticas

- Separación de responsabilidades: DTOs, servicios, controladores y vistas bien definidos.
- Uso de inyección de dependencias para servicios.
- Código limpio y comentado.
- Manejo de errores y validaciones en backend y frontend.
- Uso de componentes reutilizables en Blazor.

## Créditos

Desarrollado por [SelePagliero](https://github.com/SelePagliero) y colaboradores.

---
