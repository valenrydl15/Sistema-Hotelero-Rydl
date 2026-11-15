using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ENTIDADES_HRS_480;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// CUN13 – DES-SERIALIZAR (DES_SERIALIZAR_R_BLL del enunciado). Lee un archivo XML de clientes
    /// elegido del disco y devuelve la lista para cargarla en la grilla de la pantalla.
    /// No guarda nada en la base: para pasar un cliente del XML a la base se usa Añadir.
    /// </summary>
    public class DesSerializarBLL
    {
        public ResultadoOperacion<List<ClienteBE>> DesSerializar(string rutaArchivo)
        {
            const string OP = "Des-serializar clientes";
            if (!ControlAcceso.PuedeGestionarClientes()) return MaestrosComun.Denegado<List<ClienteBE>>(OP);
            rutaArchivo = MaestrosComun.T(rutaArchivo);
            if (rutaArchivo.Length == 0)
                return MaestrosComun.Rechazo<List<ClienteBE>>(OP, "Elegí primero el archivo XML a des-serializar (botón de la carpeta).");
            if (!File.Exists(rutaArchivo))
                return MaestrosComun.Rechazo<List<ClienteBE>>(OP, "El archivo elegido no existe.");
            if (!rutaArchivo.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                return MaestrosComun.Rechazo<List<ClienteBE>>(OP, "El archivo debe ser un XML (.xml).");
            try
            {
                List<ClienteBE>? lista;
                using (var fs = new FileStream(rutaArchivo, FileMode.Open, FileAccess.Read))
                    lista = SerializarBLL.Serializador().Deserialize(fs) as List<ClienteBE>;
                if (lista == null || lista.Count == 0)
                    return MaestrosComun.Rechazo<List<ClienteBE>>(OP, "El archivo no tiene clientes.");

                // Se cargan solo los clientes con datos válidos; los demás se informan
                var validos = new List<ClienteBE>();
                int invalidos = 0;
                foreach (var c in lista)
                {
                    if (MaestrosComun.ValidarCliente(c) == null && validos.All(v => v.DNI != c.DNI))
                    { c.DesdeXml = true; validos.Add(c); }
                    else invalidos++;
                }
                if (validos.Count == 0)
                    return MaestrosComun.Rechazo<List<ClienteBE>>(OP, "Ningún cliente del archivo tiene datos válidos.");

                BitacoraModulo.Registrar(BitacoraModulo.MOD_MAESTROS, BitacoraModulo.DESSERIALIZAR, 4,
                    $"{validos.Count} cliente/s des-serializado/s desde {Path.GetFileName(rutaArchivo)}" +
                    (invalidos > 0 ? $"; {invalidos} descartado/s por datos inválidos o DNI repetido." : "."), "EXITOSO");
                string msg = $"Se cargaron {validos.Count} cliente/s desde {Path.GetFileName(rutaArchivo)}.";
                if (invalidos > 0) msg += $" Se descartaron {invalidos} con datos inválidos o DNI repetido.";
                return ResultadoOperacion<List<ClienteBE>>.Ok(validos, msg);
            }
            catch (InvalidOperationException)
            {
                return MaestrosComun.Rechazo<List<ClienteBE>>(OP, "El archivo no es un XML de clientes válido.");
            }
            catch (IOException ex) { return MaestrosComun.Rechazo<List<ClienteBE>>(OP, "No se pudo leer el archivo: " + ex.Message); }
            catch (Exception ex) { return MaestrosComun.Error<List<ClienteBE>>(OP, ex); }
        }
    }
}
