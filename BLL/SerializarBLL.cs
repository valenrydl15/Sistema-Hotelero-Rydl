using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ENTIDADES_HRS_480;
using SERVICIOS_HRS_480;

namespace BLL_HRS_480
{
    /// <summary>
    /// CUN13 – SERIALIZAR (SERIALIZAR_B_BLL del enunciado). Convierte los clientes que están en la
    /// grilla a un archivo XML en la carpeta y con el nombre que elige el operador.
    /// No usa la base de datos: el archivo queda en un directorio del disco.
    /// </summary>
    public class SerializarBLL
    {
        /// <summary>El mismo formato lo lee DesSerializarBLL: raíz &lt;Clientes&gt;, un &lt;Cliente&gt; por fila.</summary>
        internal static XmlSerializer Serializador() => new XmlSerializer(typeof(List<ClienteBE>), new XmlRootAttribute("Clientes"));

        public ResultadoOperacion<string> Serializar(List<ClienteBE> clientesDeLaGrilla, string rutaArchivo)
        {
            const string OP = "Serializar clientes";
            if (!ControlAcceso.PuedeGestionarClientes()) return MaestrosComun.Denegado<string>(OP);
            rutaArchivo = MaestrosComun.T(rutaArchivo);
            if (clientesDeLaGrilla == null || clientesDeLaGrilla.Count == 0)
                return MaestrosComun.Rechazo<string>(OP, "La grilla está vacía: no hay clientes para serializar.");
            if (rutaArchivo.Length == 0)
                return MaestrosComun.Rechazo<string>(OP, "Elegí primero la ubicación y el nombre del archivo XML (botón de la carpeta).");
            if (!rutaArchivo.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) rutaArchivo += ".xml";
            string? carpeta = Path.GetDirectoryName(rutaArchivo);
            if (string.IsNullOrEmpty(carpeta) || !Directory.Exists(carpeta))
                return MaestrosComun.Rechazo<string>(OP, "La carpeta elegida no existe.");
            try
            {
                var datos = clientesDeLaGrilla.Select(c => new ClienteBE
                {
                    DNI = c.DNI, Apellido = c.Apellido, Nombre = c.Nombre, Email = c.Email, Celular = c.Celular, Direccion = c.Direccion
                }).ToList();
                using (var fs = new FileStream(rutaArchivo, FileMode.Create, FileAccess.Write))
                    Serializador().Serialize(fs, datos);
                BitacoraModulo.Registrar(BitacoraModulo.MOD_MAESTROS, BitacoraModulo.SERIALIZAR, 4,
                    $"{datos.Count} cliente/s serializado/s al archivo {Path.GetFileName(rutaArchivo)} ({carpeta}).", "EXITOSO");
                return ResultadoOperacion<string>.Ok(rutaArchivo, $"Se serializaron {datos.Count} cliente/s en {Path.GetFileName(rutaArchivo)}.");
            }
            catch (UnauthorizedAccessException) { return MaestrosComun.Rechazo<string>(OP, "No hay permiso para escribir en esa carpeta."); }
            catch (IOException ex) { return MaestrosComun.Rechazo<string>(OP, "No se pudo escribir el archivo: " + ex.Message); }
            catch (Exception ex) { return MaestrosComun.Error<string>(OP, ex); }
        }
    }
}
