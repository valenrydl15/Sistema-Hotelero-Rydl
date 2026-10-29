using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SEGURIDAD_HRS_480;
using SERVICIOS_HRS_480;

namespace GUI_HRS_480
{
    public partial class FrmIdiomas : Form
    {
        public FrmIdiomas()
        {
            InitializeComponent();
            Tema.Aplicar(this);

            cboIdioma.Items.Add("Español");
            cboIdioma.Items.Add("Inglés");
            cboIdioma.Items.Add("Alemán");

            cboIdioma.SelectedIndex = 0;
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            switch (cboIdioma.Text)
            {
                case "Español":
                    SesionManager.Instancia.Idioma = "espanol";
                    break;

                case "Inglés":
                    SesionManager.Instancia.Idioma = "ingles";
                    break;

                case "Alemán":
                    SesionManager.Instancia.Idioma = "aleman";
                    break;
            }

            Idioma.GestorTraducciones
                  .Instancia
                  .Notificar();

            Close();
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
