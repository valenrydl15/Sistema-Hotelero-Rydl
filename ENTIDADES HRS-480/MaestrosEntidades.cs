using System;
using System.Xml.Serialization;

namespace ENTIDADES_HRS_480
{
    // ══════════════════════════════════════════════════════════════════════
    // CUN13 Maestro de Clientes (ABM + Serializar / Des-serializar XML)
    // CLIENTES del enunciado = HUÉSPEDES del hotel (tabla Huespedes).
    // ══════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Cliente del hotel (un huésped). Es la clase que se serializa a XML: las propiedades
    /// públicas son las columnas de la grilla (DNI, Apellidos, Nombres, Email, Celular, Dirección).
    /// </summary>
    [Serializable]
    [XmlType("Cliente")]
    public class ClienteBE
    {
        /// <summary>Id del huésped en la base. No viaja en el XML.</summary>
        [XmlIgnore] public int IdHuesped { get; set; }

        [XmlElement("DNI")]       public string DNI { get; set; } = "";
        [XmlElement("Apellidos")] public string Apellido { get; set; } = "";
        [XmlElement("Nombres")]   public string Nombre { get; set; } = "";
        [XmlElement("Email")]     public string Email { get; set; } = "";
        [XmlElement("Celular")]   public string Celular { get; set; } = "";
        [XmlElement("Direccion")] public string Direccion { get; set; } = "";

        /// <summary>true si la fila de la grilla vino de un XML y no de la base.</summary>
        [XmlIgnore] public bool DesdeXml { get; set; }

        public override string ToString() => $"{Apellido}, {Nombre} (DNI {DNI})";
    }

    // ══════════════════════════════════════════════════════════════════════
    // CUN14 Bitácora de cambios de Insumos (PRODUCTOS_C del enunciado = Insumos_C)
    // ══════════════════════════════════════════════════════════════════════
    /// <summary>Un registro de la tabla Insumos_C: una versión de un insumo en el tiempo.</summary>
    public class InsumoCBE
    {
        public int IdCambio { get; set; }
        public int IdInsumo { get; set; }
        public string Codigo => $"INS-{IdInsumo:D3}";
        public DateTime Fecha { get; set; }
        public TimeSpan Hora { get; set; }
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string UnidadMedida { get; set; } = "";
        public decimal StockActual { get; set; }
        public decimal StockMinimo { get; set; }
        /// <summary>Estado del insumo en esa versión (false = dado de baja, borrado lógico).</summary>
        public bool Activo { get; set; }
        /// <summary>Act: true = versión vigente del insumo (una sola por insumo).</summary>
        public bool Act { get; set; }
        public string Operacion { get; set; } = "";
        public string Usuario { get; set; } = "";
    }

    /// <summary>Filtros de la bitácora de cambios (todos opcionales).</summary>
    public class FiltroCambiosBE
    {
        public int? IdInsumo { get; set; }
        public string? Nombre { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }

    /// <summary>Resultado de las operaciones de los maestros (la BLL no lanza excepciones a la GUI).</summary>
    public class ResultadoOperacion<T>
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = "";
        public T Dato { get; set; } = default!;

        public static ResultadoOperacion<T> Ok(T dato, string mensaje = "")
            => new ResultadoOperacion<T> { Exito = true, Dato = dato, Mensaje = mensaje };

        public static ResultadoOperacion<T> Fallo(string mensaje)
            => new ResultadoOperacion<T> { Exito = false, Mensaje = mensaje };
    }
}
