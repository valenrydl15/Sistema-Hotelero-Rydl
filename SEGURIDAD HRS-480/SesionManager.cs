using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTIDADES_HRS_480;
namespace SEGURIDAD_HRS_480
{
    public sealed class SesionManager
    {
        private static SesionManager _instancia;
        private static readonly object _lock = new object();

        private SesionManager() { Idioma = "espanol"; }

        public static SesionManager Instancia
        {
            get
            {
                lock (_lock)
                {
                    if (_instancia == null) _instancia = new SesionManager();
                    return _instancia;
                }
            }
        }

        public Usuario UsuarioActual { get; private set; }
        public bool HaySesionActiva => UsuarioActual != null;

        public void IniciarSesion(Usuario u) => UsuarioActual = u;
        public void CerrarSesion() => UsuarioActual = null;
        public string Idioma { get; set; }
    }
}
