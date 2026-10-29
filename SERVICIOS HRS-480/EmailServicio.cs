using System.Net;
using System.Net.Mail;
using System.Text;
using ENTIDADES_HRS_480;

namespace SERVICIOS_HRS_480
{
    /// <summary>
    /// Envío de la orden de compra al proveedor (CUN08).
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

        public ResultadoEnvio EnviarOrdenAProveedor(OrdenCompraBE orden, string direccionEntrega)
        {
            string destino = orden.EmailProveedor?.Trim() ?? "";
            if (destino.Length == 0 || !destino.Contains('@'))
                return new ResultadoEnvio { Exito = false, Mensaje = $"El proveedor {orden.NombreProveedor} no tiene un correo válido cargado." };

            string asunto = $"Orden de compra {orden.NroOrden} - Hotelería Rydl";
            string cuerpo = ArmarCuerpo(orden, direccionEntrega);
            var cfg = LeerConfig();

            if (cfg == null)
                return Simular(orden, destino, asunto, cuerpo);

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
                return new ResultadoEnvio { Exito = true, Mensaje = $"Orden enviada por correo a {destino}." };
            }
            catch (Exception ex)
            {
                return new ResultadoEnvio { Exito = false, Mensaje = $"No se pudo enviar el correo a {destino}: {ex.Message}" };
            }
        }

        private static string ArmarCuerpo(OrdenCompraBE o, string direccionEntrega)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Estimado/a {o.NombreProveedor}:");
            sb.AppendLine();
            sb.AppendLine("Le enviamos la siguiente orden de compra:");
            sb.AppendLine();
            sb.AppendLine($"  Nro de orden     : {o.NroOrden}");
            sb.AppendLine($"  Fecha de emisión : {o.FechaEmision:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"  Insumo           : {o.NombreInsumo}");
            sb.AppendLine($"  Cantidad         : {o.Cantidad:0.##}");
            sb.AppendLine($"  Precio unitario  : $ {o.PrecioUnitario:N2}");
            sb.AppendLine($"  Total            : $ {o.PrecioTotal:N2}");
            if (o.PlazoDias.HasValue)
                sb.AppendLine($"  Plazo de entrega : {o.PlazoDias} días (hasta el {o.FechaEmision.AddDays(o.PlazoDias.Value):dd/MM/yyyy})");
            sb.AppendLine();
            sb.AppendLine("Dirección de entrega:");
            sb.AppendLine($"  {direccionEntrega}");
            sb.AppendLine();
            sb.AppendLine("Por favor confirme la recepción de esta orden. Si hay diferencias en la entrega,");
            sb.AppendLine("el hotel solicitará el reembolso correspondiente.");
            sb.AppendLine();
            sb.AppendLine("Saludos cordiales,");
            sb.AppendLine("Departamento de Compras - Hotelería Rydl");
            return sb.ToString();
        }

        private static ResultadoEnvio Simular(OrdenCompraBE orden, string destino, string asunto, string cuerpo)
        {
            try
            {
                string carpeta = Path.Combine(Base, "Correos_Enviados");
                Directory.CreateDirectory(carpeta);
                string archivo = Path.Combine(carpeta, $"{orden.NroOrden}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(archivo,
                    $"PARA: {destino}\r\nASUNTO: {asunto}\r\n(correo simulado: falta configurar correo.txt)\r\n\r\n{cuerpo}",
                    Encoding.UTF8);
                return new ResultadoEnvio
                {
                    Exito = true,
                    Simulado = true,
                    Mensaje = $"Modo simulado (sin correo.txt): el correo para {destino} quedó guardado en Correos_Enviados\\{Path.GetFileName(archivo)}."
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
