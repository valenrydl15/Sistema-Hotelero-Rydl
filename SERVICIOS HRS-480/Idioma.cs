using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using SEGURIDAD_HRS_480;

namespace SERVICIOS_HRS_480
{
    public class Idioma
    {
        public interface IObservable
        {
            public void AgregarForm(IObserver control);
            public void EliminarForm(IObserver control);
            public void Notificar();
        }

        public interface IObserver
        {
            public void ActualizarTraducciones();
        }

        public class GestorTraducciones: IObservable
        {
            private static readonly GestorTraducciones _instancia = new GestorTraducciones();

            public List<IObserver> formularios;

            private GestorTraducciones()
            {
                formularios = new List<IObserver>();
            }

            public static GestorTraducciones Instancia => _instancia;

            public void AgregarForm(IObserver control)
            {

                formularios.Add(control);
            }

            public void EliminarForm(IObserver control)
            {
                formularios.Remove(control);
            }

            public void Notificar()
            {
                foreach (var item in formularios)
                {
                    item.ActualizarTraducciones();
                }
            }

        }            
    }
}
