
using GestionApiario.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionApiario.compartido.Dto;

namespace GestionApiario.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ControlesController : Controller
    {
        private readonly GestionApiariosContext _context;
        public ControlesController(GestionApiariosContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult> InsertarControl([FromBody] ControlDto NuevoControl)
        {
            ValidarCampaña(NuevoControl.CodCampaña);
            ValidarApiario(NuevoControl.CodApiario);
            ValidarAlimento(NuevoControl.CodAlimento);
            ValidarEnfermeadad(NuevoControl.CodEnfermedad);
            ValidarProducto(NuevoControl.CodProductos);

            Controle controles = new()
            {
                CodCampaña = NuevoControl.CodCampaña,
                CodApiario = NuevoControl.CodApiario,
                Fecha = NuevoControl.Fecha,
                CantDeColmenas = NuevoControl.CantDeColmenas,
                CodAlimento = NuevoControl.CodAlimento == 0 ? null : NuevoControl.CodAlimento,
                CantidadAlimento=NuevoControl.CodAlimento== 0 ? null : NuevoControl.CantidadAlimento,
                CodEnfermedad = NuevoControl.CodEnfermedad==0?null:NuevoControl.CodEnfermedad,
                CodProductos = NuevoControl.CodProductos==0?null:NuevoControl.CodProductos,
                CantProducto = NuevoControl.CodProductos==0?null:NuevoControl.CantProducto,
                Obsevaciones = NuevoControl.Obsevaciones,
                FechaAlta = DateTime.Now
            };

            _context.Controles.Add(controles);
            await _context.SaveChangesAsync();
            return Created();
        }

        private void ValidarProducto(int codProductos)
        {
            if (codProductos == 0)
                return;
            bool existe = _context.Productos.Any(p => p.Codigo == codProductos);
            if (!existe) { throw new Exception("El producto no existe"); }
        }

        private void ValidarEnfermeadad(int codEnfermedad)
        {
            if (codEnfermedad == 0)
                return;
            bool existe = _context.Enfermedads.Any(e => e.Codigo == codEnfermedad);
            if (!existe) { throw new Exception("La enfermedad no existe."); }
        }

        private void ValidarAlimento(int codAlimento)
        {
            if (codAlimento == 0)
                return;
            bool existe = _context.Alimentos.Any(al => al.Codigo == codAlimento);
            if (!existe) { throw new Exception("El alimento no existe."); }
        }

        private void ValidarApiario(int codApiario)
        {
            bool existe = _context.Apiarios.Any(a => a.Codigo == codApiario);
            if (!existe) { throw new Exception("El apiario no existe."); }
        }

        private void ValidarCampaña(int codCampaña)
        {
           bool existe = _context.Campañas.Any(c => c.Codigo == codCampaña);
            if (!existe) { throw new Exception("La campaña no existe."); }
        }

        [HttpGet("{Codigo}")]
        public async Task<ActionResult<Controle>> ObtenerControles([FromRoute] int Codigo)
        {

            var control = await _context.Controles.Where(a => a.Codigo == Codigo).FirstOrDefaultAsync();

            if (control == null) { return NotFound(); }
            control.CantidadAlimento = control.CantidadAlimento ?? 0;
            control.CantProducto = control.CantProducto ?? 0;
            return Ok(control);
        }

        [HttpPut("{Codigo}")]
        public async Task<ActionResult> ModificarControl([FromRoute] int Codigo, [FromBody] ControlDto controlModificar)
        {
            var control = await _context.Controles.Where(a => a.Codigo == Codigo).FirstOrDefaultAsync();

            if (control == null) { return NotFound(); }

            control.CodCampaña = controlModificar.CodCampaña;
            control.CodApiario = controlModificar.CodApiario;
            control.Fecha = controlModificar.Fecha;
            control.CantDeColmenas = controlModificar.CantDeColmenas;
            control.CodAlimento = controlModificar.CodAlimento == 0 ? null : controlModificar.CodAlimento;
            control.CantidadAlimento = controlModificar.CodAlimento == 0 ? null : controlModificar.CantidadAlimento;
            control.CodEnfermedad = controlModificar.CodEnfermedad == 0 ? null : controlModificar.CodEnfermedad;
            control.CodProductos = controlModificar.CodProductos == 0 ? null : controlModificar.CodProductos;
            control.CantProducto = controlModificar.CodProductos == 0 ? null : controlModificar.CantProducto;
            control.FechaModificacion = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok();
        }


        [HttpGet("vertodos")]
        public async Task<ActionResult<List<ControlGrillaDto>>> ObtenerTodos()
        {
            var listaControles = await _context.Controles.Where(a => a.FechaBaja == null)
                                                         .Select(c => new ControlGrillaDto() 
                                                         { 
                                                            Codigo = c.Codigo,
                                                            Campaña = c.CodCampañaNavigation.Año,
                                                            Apiario = c.CodApiarioNavigation.Nombre,
                                                             Fecha = c.Fecha.Value,
                                                             CantDeColmenas = c.CantDeColmenas,
                                                             Alimento = c.CodAlimentoNavigation.Nombre,
                                                             CantidadAlimento = c.CantidadAlimento,
                                                             Enfermedad = c.CodEnfermedadNavigation.Nombre,
                                                             Producto = c.CodProductosNavigation.Nombre,
                                                             CantProducto = c.CantProducto
                                                         })
                                                         .ToListAsync();
            return Ok(listaControles);
        }

        [HttpDelete("{Codigo}")]
        public async Task<ActionResult> Eliminar([FromRoute] int Codigo)
        {

            var borrarControles = await _context.Controles.Where(a => a.Codigo == Codigo).FirstOrDefaultAsync();

            if (borrarControles == null) { return NotFound(); }

            borrarControles.FechaBaja = DateTime.Now;
            borrarControles.FechaModificacion = DateTime.Now;
            _context.SaveChanges();
            return Ok();
        }
    }
}

