using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace GestionApiario.Servicios
{
    // Configuración de la sección "Correo" (los datos de la cuenta van en los secretos de usuario, ver README).
    public class OpcionesCorreo
    {
        public string? Servidor { get; set; }
        public int Puerto { get; set; } = 587;
        public string? Usuario { get; set; }
        public string? Contraseña { get; set; }

        // Dirección que aparece como remitente. Si no se configura, se usa Usuario.
        public string? Remitente { get; set; }
        public string NombreRemitente { get; set; } = "Gestión de Apiarios";

        // Para pruebas: en lugar de enviar, guarda cada correo como archivo .eml en esta carpeta.
        public string? CarpetaDeSalida { get; set; }

        public bool Configurado => !string.IsNullOrWhiteSpace(CarpetaDeSalida)
            || (!string.IsNullOrWhiteSpace(Servidor) && !string.IsNullOrWhiteSpace(Usuario) && !string.IsNullOrWhiteSpace(Contraseña));
    }

    public interface IEnviadorCorreo
    {
        bool Configurado { get; }
        Task EnviarAsync(string para, string asunto, string cuerpoHtml, string cuerpoTexto);
    }

    // Envía correos por SMTP (por ejemplo, Gmail con una contraseña de aplicación).
    public class EnviadorCorreo : IEnviadorCorreo
    {
        private readonly OpcionesCorreo _opciones;

        public EnviadorCorreo(IOptions<OpcionesCorreo> opciones)
        {
            _opciones = opciones.Value;
        }

        public bool Configurado => _opciones.Configurado;

        public async Task EnviarAsync(string para, string asunto, string cuerpoHtml, string cuerpoTexto)
        {
            if (!Configurado)
                throw new InvalidOperationException("El envío de correos no está configurado (sección \"Correo\").");

            var remitente = _opciones.Remitente ?? _opciones.Usuario ?? "no-responder@gestion-apiarios.local";
            using var mensaje = new MailMessage
            {
                From = new MailAddress(remitente, _opciones.NombreRemitente),
                Subject = asunto,
                Body = cuerpoTexto,
                IsBodyHtml = false
            };
            mensaje.To.Add(para);
            mensaje.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(cuerpoHtml, null, "text/html"));

            using var cliente = new SmtpClient();
            if (!string.IsNullOrWhiteSpace(_opciones.CarpetaDeSalida))
            {
                Directory.CreateDirectory(_opciones.CarpetaDeSalida);
                cliente.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                cliente.PickupDirectoryLocation = Path.GetFullPath(_opciones.CarpetaDeSalida);
            }
            else
            {
                cliente.Host = _opciones.Servidor!;
                cliente.Port = _opciones.Puerto;
                cliente.EnableSsl = true;
                cliente.Credentials = new NetworkCredential(_opciones.Usuario, _opciones.Contraseña);
            }

            await cliente.SendMailAsync(mensaje);
        }
    }
}
