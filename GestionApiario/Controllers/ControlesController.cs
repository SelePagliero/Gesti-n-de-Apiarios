using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Controllers
{
    public class ControlesController : ControladorBase
    {
        private readonly GestionApiariosContext _context;
        public ControlesController(GestionApiariosContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult> InsertarControl([FromBody] ControlDto nuevoControl)
        {
            var error = await ValidarReferencias(nuevoControl);
            if (error != null) { return BadRequest(error); }

            Controle control = new()
            {
                FechaAlta = DateTime.Now,
                UsuarioAlta = UsuarioActual
            };
            CopiarDatos(nuevoControl, control);

            _context.Controles.Add(control);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(ObtenerControles), new { Codigo = control.Codigo }, null);
        }

        [HttpGet("{Codigo}")]
        public async Task<ActionResult<ControlDetalleDto>> ObtenerControles([FromRoute] int Codigo)
        {
            var control = await ControlesVisibles()
                .Where(c => c.Codigo == Codigo)
                .Select(c => new ControlDetalleDto()
                {
                    Codigo = c.Codigo,
                    CodCampaña = c.CodCampaña ?? 0,
                    CodApiario = c.CodApiario ?? 0,
                    Fecha = c.Fecha ?? DateOnly.FromDateTime(DateTime.Today),
                    CantDeColmenas = c.CantDeColmenas ?? 0,
                    CodAlimento = c.CodAlimento,
                    CantidadAlimento = c.CantidadAlimento ?? 0,
                    CodEnfermedad = c.CodEnfermedad,
                    CodProductos = c.CodProductos,
                    CantProducto = c.CantProducto ?? 0,
                    Observaciones = c.Observaciones
                })
                .FirstOrDefaultAsync();

            if (control == null) { return NotFound(); }
            return Ok(control);
        }

        [HttpPut("{Codigo}")]
        public async Task<ActionResult> ModificarControl([FromRoute] int Codigo, [FromBody] ControlDto controlModificar)
        {
            var control = await ControlesVisibles().FirstOrDefaultAsync(c => c.Codigo == Codigo);

            if (control == null) { return NotFound(); }

            var error = await ValidarReferencias(controlModificar);
            if (error != null) { return BadRequest(error); }

            CopiarDatos(controlModificar, control);
            control.FechaModificacion = DateTime.Now;
            control.UsuarioModificacion = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("vertodos")]
        public async Task<ActionResult<List<ControlGrillaDto>>> ObtenerTodos([FromQuery] FiltroControlesDto filtro)
        {
            // Los controles de apiarios dados de baja o ajenos no se muestran, aunque se filtre por ese apiario.
            var consulta = ControlesVisibles();

            // El filtro por apicultor solo vale para la Administradora; un apicultor ya ve únicamente lo suyo.
            if (EsAdministrador && !string.IsNullOrEmpty(filtro.UsuarioId))
                consulta = consulta.Where(c => c.CodApiarioNavigation!.UsuarioId == filtro.UsuarioId);

            // El rango de fechas inválido ya lo rechaza [ApiController] con un 400 (FiltroControlesDto.Validate).
            if (filtro.CodApiario is not null)
                consulta = consulta.Where(c => c.CodApiario == filtro.CodApiario);
            if (filtro.CodCampaña is not null)
                consulta = consulta.Where(c => c.CodCampaña == filtro.CodCampaña);
            if (filtro.CodEnfermedad is not null)
                consulta = consulta.Where(c => c.CodEnfermedad == filtro.CodEnfermedad);
            if (filtro.ConAlgunaEnfermedad == true)
                consulta = consulta.Where(c => c.CodEnfermedad != null);
            if (filtro.FechaDesde is not null)
                consulta = consulta.Where(c => c.Fecha >= filtro.FechaDesde);
            if (filtro.FechaHasta is not null)
                consulta = consulta.Where(c => c.Fecha <= filtro.FechaHasta);

            var listaControles = await consulta
                .OrderByDescending(c => c.Fecha)
                .ThenByDescending(c => c.Codigo)
                .Select(c => new ControlGrillaDto()
                {
                    Codigo = c.Codigo,
                    Campaña = c.CodCampañaNavigation!.Año,
                    Apiario = c.CodApiarioNavigation!.Nombre,
                    Fecha = c.Fecha ?? DateOnly.MinValue,
                    CantDeColmenas = c.CantDeColmenas,
                    Alimento = c.CodAlimentoNavigation!.Nombre,
                    CantidadAlimento = c.CantidadAlimento,
                    Enfermedad = c.CodEnfermedadNavigation!.Nombre,
                    Producto = c.CodProductosNavigation!.Nombre,
                    CantProducto = c.CantProducto,
                    Apicultor = c.CodApiarioNavigation!.Usuario!.Email
                })
                .ToListAsync();
            return Ok(listaControles);
        }

        [HttpDelete("{Codigo}")]
        public async Task<ActionResult> Eliminar([FromRoute] int Codigo)
        {
            var control = await ControlesVisibles().FirstOrDefaultAsync(c => c.Codigo == Codigo);

            if (control == null) { return NotFound(); }

            control.FechaBaja = DateTime.Now;
            control.UsuarioBaja = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // Controles activos de apiarios activos que el usuario puede ver: la Administradora ve todos y cada
        // apicultor solo los de sus apiarios. Un control ajeno responde 404, como si no existiera.
        private IQueryable<Controle> ControlesVisibles()
        {
            var activos = _context.Controles.Where(c => c.FechaBaja == null && c.CodApiarioNavigation!.FechaBaja == null);
            return EsAdministrador ? activos : activos.Where(c => c.CodApiarioNavigation!.UsuarioId == UsuarioIdActual);
        }

        // En los DTO, el código 0 significa "sin seleccionar"; en la base se guarda como null.
        private static void CopiarDatos(ControlDto origen, Controle destino)
        {
            destino.CodCampaña = origen.CodCampaña;
            destino.CodApiario = origen.CodApiario;
            destino.Fecha = origen.Fecha;
            destino.CantDeColmenas = origen.CantDeColmenas;
            destino.CodAlimento = origen.CodAlimento == 0 ? null : origen.CodAlimento;
            destino.CantidadAlimento = origen.CodAlimento == 0 ? null : origen.CantidadAlimento;
            destino.CodEnfermedad = origen.CodEnfermedad == 0 ? null : origen.CodEnfermedad;
            destino.CodProductos = origen.CodProductos == 0 ? null : origen.CodProductos;
            destino.CantProducto = origen.CodProductos == 0 ? null : origen.CantProducto;
            destino.Observaciones = origen.Observaciones;
        }

        // Devuelve el mensaje de error si alguna referencia no existe o está dada de baja; null si todo es válido.
        private async Task<string?> ValidarReferencias(ControlDto control)
        {
            // Un apicultor solo puede usar sus propias campañas y cargar controles en sus propios apiarios.
            var campaña = await _context.Campañas
                .Where(c => c.Codigo == control.CodCampaña && c.FechaBaja == null && (EsAdministrador || c.UsuarioId == UsuarioIdActual))
                .Select(c => new { c.UsuarioId })
                .FirstOrDefaultAsync();
            if (campaña is null)
                return "La campaña no existe.";

            var apiario = await _context.Apiarios
                .Where(a => a.Codigo == control.CodApiario && a.FechaBaja == null && (EsAdministrador || a.UsuarioId == UsuarioIdActual))
                .Select(a => new { a.UsuarioId })
                .FirstOrDefaultAsync();
            if (apiario is null)
                return "El apiario no existe.";

            // La campaña tiene que ser del dueño del apiario (relevante para la Administradora, que ve todo).
            if (campaña.UsuarioId != apiario.UsuarioId)
                return "La campaña tiene que ser del mismo apicultor que el apiario.";

            if (control.CodAlimento != 0 && !await _context.Alimentos.AnyAsync(al => al.Codigo == control.CodAlimento && al.FechaBaja == null))
                return "El alimento no existe.";

            if (control.CodEnfermedad != 0 && !await _context.Enfermedades.AnyAsync(e => e.Codigo == control.CodEnfermedad && e.FechaBaja == null))
                return "La enfermedad no existe.";

            if (control.CodProductos != 0 && !await _context.Productos.AnyAsync(p => p.Codigo == control.CodProductos && p.FechaBaja == null))
                return "El producto no existe.";

            return null;
        }
    }
}
