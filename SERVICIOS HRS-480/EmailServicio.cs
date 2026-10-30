using System.Net;
using System.Net.Mail;
using System.Text;
using ENTIDADES_HRS_480;

namespace SERVICIOS_HRS_480
{
    /// <summary>
    /// Envío de la solicitud de cotización (CUN06) y de la orden de compra pagada (CUN11) al proveedor.
    /// Configuración opcional: archivo "correo.txt" junto al .exe, con líneas clave=valor:
    ///     servidor=smtp.gmail.com
    ///     puerto=587
    ///     usuario=hotel@ejemplo.com
    ///     clave=********
    ///     ssl=true
    ///     remitente=compras@hotel.com      (opcional; por defecto = usuario)
    /// Sin ese archivo el sistema funciona en MODO SIMULADO: no envía nada por internet,
    /// guarda el correo completo en la carpeta "Correos_Enviados" y lo informa.
    /// </summary>
    public class EmailServicio
    {
        public class ResultadoEnvio
        {
            public bool Exito { get; set; }
            public bool Simulado { get; set; }
            public string Mensaje { get; set; } = "";
        }

        private static string Base => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>CUN06: envía la solicitud de cotización al proveedor.</summary>
        public ResultadoEnvio EnviarSolicitudCotizacion(SolicitudCotizacionBE s)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Estimado/a {s.NombreProveedor}:");
            sb.AppendLine();
            sb.AppendLine("Le solicitamos cotización de los siguientes insumos:");
            sb.AppendLine();
            sb.AppendLine($"  Nro de solicitud : {s.NroSolicitud}");
            sb.AppendLine($"  Fecha            : {s.FechaSolicitud:dd/MM/yyyy}");
            sb.AppendLine();
            foreach (var i in s.Items)
                sb.AppendLine($"  {i.Codigo}  {i.NombreInsumo,-30} cantidad: {i.Cantidad:0.##} {i.UnidadMedida}");
            sb.AppendLine();
            sb.AppendLine("Por favor indíquenos, para cada insumo, la cantidad disponible y el precio unitario,");
            sb.AppendLine("junto con su CUIT, banco, CBU y titular de la cuenta para el pago.");
            sb.AppendLine();
            sb.AppendLine("Saludos cordiales,");
            sb.AppendLine("Departamento de Compras - Hotelería Rydl");
            return Enviar(s.EmailProveedor, s.NombreProveedor, $"Solicitud de cotización {s.NroSolicitud} - Hotelería Rydl", sb.ToString(), s.NroSolicitud);
        }

        /// <summary>CUN11: envía la orden de compra pagada al proveedor.</summary>
        public ResultadoEnvio EnviarOrdenAProveedor(OrdenCompraBE o, string direccionEntrega)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Estimado/a {o.NombreProveedor}:");
            sb.AppendLine();
            sb.AppendLine("Le enviamos la orden de compra, ya pagada:");
            sb.AppendLine();
            sb.AppendLine($"  Nro de orden       : {o.NroOrden}");
            sb.AppendLine($"  Nro de operación   : {o.NroOperacionPago}" + (o.FechaPago.HasValue ? $" ({o.FechaPago:dd/MM/yyyy HH:mm})" : ""));
            sb.AppendLine();
            foreach (var i in o.Items)
                sb.AppendLine($"  {i.Codigo}  {i.NombreInsumo,-30} {i.Cantidad,8:0.##} x $ {i.PrecioUnitario,10:N2} = $ {i.Subtotal,12:N2}");
            sb.AppendLine($"  {"TOTAL",-41} $ {o.Total,35:N2}");
            sb.AppendLine();
            sb.AppendLine("Dirección de entrega:");
            sb.AppendLine($"  {direccionEntrega}");
            sb.AppendLine();
            sb.AppendLine("Por favor entregue la mercadería junto con la factura correspondiente.");
            sb.AppendLine();
            sb.AppendLine("Saludos cordiales,");
            sb.AppendLine("Gerencia de Compras - Hotelería Rydl");
            return Enviar(o.EmailProveedor, o.NombreProveedor, $"Orden de compra {o.NroOrden} - Hotelería Rydl", sb.ToString(), o.NroOrden);
        }

        private ResultadoEnvio Enviar(string? destino, string nombre, string asunto, string cuerpo, string clave)
        {
            destino = destino?.Trim() ?? "";
            if (destino.Length == 0 || !destino.Contains('@'))
                return new ResultadoEnvio { Exito = false, Mensaje = $"El proveedor {nombre} no tiene un correo válido cargado." };

            var cfg = LeerConfig();
            if (cfg == null) return Simular(clave, destino, asunto, cuerpo);
            try
            {
                using var msg = new MailMessage
                {
                    From = new MailAddress(cfg.GetValueOrDefault("remitente", cfg["usuario"])),
                    Subject = asunto,
                    Body = cuerpo,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };
                msg.To.Add(destino);
                using var smtp = new SmtpClient(cfg["servidor"], int.TryParse(cfg.GetValueOrDefault("puerto", "587"), out int p) ? p : 587)
                {
                    EnableSsl = !cfg.GetValueOrDefault("ssl", "true").Equals("false", StringComparison.OrdinalIgnoreCase),
                    Credentials = new NetworkCredential(cfg["usuario"], cfg["clave"]),
                    Timeout = 15000
                };
                smtp.Send(msg);
                return new ResultadoEnvio { Exito = true, Mensaje = $"Enviado por correo a {destino}." };
            }
            catch (Exception ex)
            {
                return new ResultadoEnvio { Exito = false, Mensaje = $"No se pudo enviar el correo a {destino}: {ex.Message}" };
            }
        }

        private static ResultadoEnvio Simular(string clave, string destino, string asunto, string cuerpo)
        {
            try
            {
                string carpeta = Path.Combine(Base, "Correos_Enviados");
                Directory.CreateDirectory(carpeta);
                string archivo = Path.Combine(carpeta, $"{clave}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(archivo,
                    $"PARA: {destino}\r\nASUNTO: {asunto}\r\n(correo simulado: falta configurar correo.txt)\r\n\r\n{cuerpo}", Encoding.UTF8);
                return new ResultadoEnvio
                {
                    Exito = true,
                    Simulado = true,
                    Mensaje = $"Modo simulado: el correo para {destino} quedó guardado en Correos_Enviados\\{Path.GetFileName(archivo)}."
                };
            }
            catch (Exception ex)
            {
                return new ResultadoEnvio { Exito = false, Mensaje = "No se pudo registrar el correo simulado: " + ex.Message };
            }
        }

        private static Dictionary<string, string>? LeerConfig()
        {
            try
            {
                string ruta = Path.Combine(Base, "correo.txt");
                if (!File.Exists(ruta)) return null;

                var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var linea in File.ReadAllLines(ruta))
                {
                    string l = linea.Trim();
                    if (l.Length == 0 || l.StartsWith("#")) continue;
                    int i = l.IndexOf('=');
                    if (i > 0) d[l.Substring(0, i).Trim()] = l.Substring(i + 1).Trim();
                }
                return d.ContainsKey("servidor") && d.ContainsKey("usuario") && d.ContainsKey("clave") ? d : null;
            }
            catch { return null; }
        }
    }
}
