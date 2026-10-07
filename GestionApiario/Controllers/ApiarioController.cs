using GestionApiario.compartido.Dto;
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionApiario.Controllers
{
    // Cada apiario pertenece al usuario que lo creó. Un apicultor solo ve y modifica los suyos;
    // la Administradora ve y modifica todos, y es la única que puede elegir o cambiar el dueño.
    public class ApiarioController : ControladorBase
    {
        private readonly GestionApiariosContext _context;
        public ApiarioController(GestionApiariosContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult> InsertarApiario([FromBody] ApiarioDto nuevoApiario)
        {
            var (dueño, error) = await ResolverDueñoAsync(_context, nuevoApiario.UsuarioId, dueñoActual: UsuarioIdActual);
            if (error != null) { return error; }

            Apiario apiario = new()
            {
                Nombre = nuevoApiario.Nombre,
                Empresa = nuevoApiario.Empresa,
                Longitud = nuevoApiario.Longitud,
                Latitud = nuevoApiario.Latitud,
                UsuarioId = dueño,
                FechaAlta = DateTime.Now,
                UsuarioAlta = UsuarioActual
            };

            _context.Apiarios.Add(apiario);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(ObtenerApiario), new { Codigo = apiario.Codigo }, null);
        }

        [HttpGet("{Codigo}")]
        public async Task<ActionResult<ApiarioDetalleDto>> ObtenerApiario([FromRoute] int Codigo)
        {
            // Los emails de auditoría ("creado por", "modificado por" y "dado de baja por") solo los ve la Administradora.
            var mostrarAuditoria = EsAdministrador;
            var apiario = await ApiariosVisibles()
                .Where(a => a.Codigo == Codigo)
                .Select(a => new ApiarioDetalleDto()
                {
                    Codigo = a.Codigo,
                    Nombre = a.Nombre,
                    Empresa = a.Empresa,
                    FechaAlta = a.FechaAlta,
                    FechaBaja = a.FechaBaja,
                    Latitud = a.Latitud,
                    Longitud = a.Longitud,
                    UsuarioAlta = mostrarAuditoria ? a.UsuarioAlta : null,
                    UsuarioBaja = mostrarAuditoria ? a.UsuarioBaja : null,
                    FechaModificacion = a.FechaModificacion,
                    UsuarioModificacion = mostrarAuditoria ? a.UsuarioModificacion : null,
                    UsuarioId = a.UsuarioId,
                    Apicultor = a.Usuario!.Email
                })
                .FirstOrDefaultAsync();

            if (apiario == null) { return NotFound(); }
            return Ok(apiario);
        }

        [HttpPut("{Codigo}")]
        public async Task<ActionResult> Modificar([FromRoute] int Codigo, [FromBody] ApiarioDto apiarioModificar)
        {
            var apiario = await ApiariosVisibles().FirstOrDefaultAsync(a => a.Codigo == Codigo);

            if (apiario == null) { return NotFound(); }

            // Si no se indica dueño se mantiene el actual. Sus controles cambian de dueño junto con el apiario.
            var (dueño, error) = await ResolverDueñoAsync(_context, apiarioModificar.UsuarioId, dueñoActual: apiario.UsuarioId);
            if (error != null) { return error; }

            if (dueño != apiario.UsuarioId)
                await PasarCampañasAlNuevoDueñoAsync(apiario.Codigo, dueño!);

            apiario.Nombre = apiarioModificar.Nombre;
            apiario.Empresa = apiarioModificar.Empresa;
            apiario.Latitud = apiarioModificar.Latitud;
            apiario.Longitud = apiarioModificar.Longitud;
            apiario.UsuarioId = dueño;
            apiario.FechaModificacion = DateTime.Now;
            apiario.UsuarioModificacion = UsuarioActual;
            // Un solo SaveChanges: el cambio de dueño, las campañas copiadas y los controles se guardan juntos o no se guarda nada.
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("vertodos")]
        public async Task<ActionResult<List<ApiarioGrillaDto>>> ObtenerTodos()
        {
            var listaApiarios = await ApiariosVisibles()
                .OrderBy(a => a.Nombre)
                .Select(apiario => new ApiarioGrillaDto()
                {
                    Codigo = apiario.Codigo,
                    Nombre = apiario.Nombre,
                    FechaModificacion = apiario.FechaModificacion,
                    FechaAlta = apiario.FechaAlta,
                    UsuarioId = apiario.UsuarioId,
                    Apicultor = apiario.Usuario!.Email
                })
                .ToListAsync();

            return Ok(listaApiarios);
        }

        [HttpDelete("{Codigo}")]
        public async Task<ActionResult> Eliminar([FromRoute] int Codigo)
        {
            var apiario = await ApiariosVisibles().FirstOrDefaultAsync(a => a.Codigo == Codigo);

            if (apiario == null) { return NotFound(); }

            apiario.FechaBaja = DateTime.Now;
            apiario.UsuarioBaja = UsuarioActual;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // Apiarios activos que el usuario actual puede ver. Los ajenos responden 404, como si no existieran.
        private IQueryable<Apiario> ApiariosVisibles()
        {
            var activos = _context.Apiarios.Where(a => a.FechaBaja == null);
            return EsAdministrador ? activos : activos.Where(a => a.UsuarioId == UsuarioIdActual);
        }

        // Al reasignar un apiario, sus controles (también los dados de baja) tienen que quedar con campañas del nuevo dueño.
        // Solo se procesan las campañas que usan esos controles. Para cada una: si el nuevo dueño ya tiene una campaña activa
        // igual (mismo año y responsable) se usa esa; si no, se crea una copia, una sola vez aunque varios controles la usen.
        // Las campañas originales no se modifican, porque pueden estar usándolas otros apiarios.
        private async Task PasarCampañasAlNuevoDueñoAsync(int codApiario, string nuevoDueño)
        {
            var controles = await _context.Controles
                .Include(c => c.CodCampañaNavigation)
                .Where(c => c.CodApiario == codApiario && c.CodCampaña != null && c.CodCampañaNavigation!.UsuarioId != nuevoDueño)
                .ToListAsync();

            var reemplazos = new Dictionary<int, Campaña>();
            var copiasNuevas = new List<Campaña>();
            foreach (var control in controles)
            {
                var original = control.CodCampañaNavigation!;
                if (!reemplazos.TryGetValue(original.Codigo, out var destino))
                {
                    destino = copiasNuevas.FirstOrDefault(c => c.Año == original.Año && c.Responsable == original.Responsable)
                        ?? await _context.Campañas.FirstOrDefaultAsync(c => c.UsuarioId == nuevoDueño && c.FechaBaja == null
                            && c.Año == original.Año && c.Responsable == original.Responsable);

                    if (destino is null)
                    {
                        destino = new Campaña
                        {
                            Año = original.Año,
                            Responsable = original.Responsable,
                            UsuarioId = nuevoDueño,
                            FechaAlta = DateTime.Now,
                            UsuarioAlta = UsuarioActual
                        };
                        _context.Campañas.Add(destino);
                        copiasNuevas.Add(destino);
                    }
                    reemplazos[original.Codigo] = destino;
                }

                control.CodCampañaNavigation = destino;
                control.FechaModificacion = DateTime.Now;
                control.UsuarioModificacion = UsuarioActual;
            }
        }
    }
}
