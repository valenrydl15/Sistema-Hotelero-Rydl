using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DAL_HRS_480;
using ENTIDADES_HRS_480;
using SEGURIDAD_HRS_480;
using SERVICIOS_HRS_480;

namespace GUI_HRS_480
{
    public partial class FrmBitacora : Form, Idioma.IObserver
    {
        private readonly Bitacora _dal = new Bitacora();
        private List<RegistroBitacora> _datos;

        public FrmBitacora()
        {
            InitializeComponent();
            Tema.Aplicar(this);
            CargarCombos();
            AplicarFiltro();
            Idioma.GestorTraducciones.Instancia.AgregarForm(this);

            ActualizarTraducciones();
        }
        public void ActualizarTraducciones()
        {
            new Traductor()
                .ActualizarTraducciones(
                    "FrmBitacora",
                    this);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Idioma.GestorTraducciones.Instancia.EliminarForm(this);
            base.OnFormClosing(e);
        }

        private void CargarCombos()
        {
            // Módulos (objetos: ToString() devuelve el Nombre)
            cmbModulo.Items.Clear();
            cmbModulo.Items.Add(new Modulo { IdModulo = 0, Nombre = "(Todos)" });
            foreach (var m in _dal.ObtenerModulos())
                cmbModulo.Items.Add(m);
            cmbModulo.SelectedIndex = 0;
            // OJO: no volver a suscribir SelectedIndexChanged, ya lo hace el Designer

            // Criticidad (se queda con strings)
            cmbCriticidad.Items.Clear();
            cmbCriticidad.Items.Add("(Todas)");
            for (int i = 1; i <= 5; i++) cmbCriticidad.Items.Add(i.ToString());
            cmbCriticidad.SelectedIndex = 0;

            // Login: lista completa de usuarios, se carga una sola vez
            cmbLogin.DisplayMember = "NombreUsuario";
            cmbLogin.Items.Clear();
            cmbLogin.Items.Add(new Usuario { IdUsuario = 0, NombreUsuario = "(Todos)" });
            foreach (var u in new UsuarioServicio().ObtenerTodos(false))
                cmbLogin.Items.Add(u);
            cmbLogin.SelectedIndex = 0;

            CargarComboEventos(null);
        }

        private void CargarComboEventos(int? idModulo)
        {
            cmbEvento.Items.Clear();
            cmbEvento.Items.Add(new TipoEvento { IdTipoEvento = 0, Nombre = "(Todos)" });
            foreach (var ev in _dal.ObtenerTiposEvento(idModulo))
                cmbEvento.Items.Add(ev);
            cmbEvento.SelectedIndex = 0;
        }

        private void cmbModulo_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (cmbModulo.SelectedItem is Modulo m && m.IdModulo != 0)
                CargarComboEventos(m.IdModulo);
            else
                CargarComboEventos(null);
        }

        private void AplicarFiltro()
        {
            int? idUsuario = null;
            int? idModulo = null;
            int? idTipoEvento = null;
            int? criticidad = null;
            DateTime? fechaIni = null;
            DateTime? fechaFin = null;

            if (cmbLogin.SelectedItem is Usuario u && u.IdUsuario != 0)
                idUsuario = u.IdUsuario;

            if (dtpFechaIni.Checked) fechaIni = dtpFechaIni.Value.Date;
            if (dtpFechaFin.Checked) fechaFin = dtpFechaFin.Value.Date;

            if (cmbModulo.SelectedItem is Modulo m && m.IdModulo != 0)
                idModulo = m.IdModulo;

            if (cmbEvento.SelectedItem is TipoEvento te && te.IdTipoEvento != 0)
                idTipoEvento = te.IdTipoEvento;

            if (cmbCriticidad.SelectedIndex > 0 &&
                int.TryParse(cmbCriticidad.SelectedItem?.ToString(), out int crit))
                criticidad = crit;

            _datos = _dal.ObtenerFiltrado(idUsuario, fechaIni, fechaFin, idModulo, idTipoEvento, criticidad);
            CargarGrilla();
            // Ya no se llama a CargarComboLogin(): el combo tiene todos los usuarios
        }

        private void CargarGrilla()
        {
            dgv.Rows.Clear();
            int numero = 1;   

            foreach (var r in _datos)
            {
                int idx = dgv.Rows.Add(
                    numero,                             
                    r.Login ?? "-",                  
                    r.Fecha.ToString("dd/MM/yyyy"),    
                    r.Hora.ToString(@"hh\:mm"),         
                    r.NombreModulo ?? "-",             
                    r.NombreEvento ?? "-",              
                    r.Criticidad.ToString()             
                );
                dgv.Rows[idx].Tag = r;

                if (r.Resultado == "FALLIDO")
                    dgv.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(180, 0, 0);

                numero++;  
            }

            if (dgv.Rows.Count > 0)
            {
                dgv.Rows[0].Selected = true;
                MostrarNombreApellido(dgv.Rows[0].Tag as RegistroBitacora);
            }
            else
            {
                lblNombreVal.Text = "";
                lblApellidoVal.Text = "";
            }
        }

      
        private void dgv_SelectionChanged_1(object sender, EventArgs e)
        {
            if (dgv.CurrentRow == null) return;
            MostrarNombreApellido(dgv.CurrentRow.Tag as RegistroBitacora);
        }

        private void MostrarNombreApellido(RegistroBitacora r)
        {
            if (r == null) return;
            lblNombreVal.Text = r.Nombre ?? "";
            lblApellidoVal.Text = r.Apellido ?? "";
        }

        
        private void btnLimpiar_Click_1(object sender, EventArgs e)
        {
            cmbLogin.SelectedIndex = 0;
            dtpFechaIni.Checked = false;
            dtpFechaFin.Checked = false;
            cmbModulo.SelectedIndex = 0;
            cmbCriticidad.SelectedIndex = 0;
            if (cmbEvento.Items.Count > 0) cmbEvento.SelectedIndex = 0;
            lblNombreVal.Text = "";
            lblApellidoVal.Text = "";
        }

        private void btnAplicar_Click_1(object sender, EventArgs e) => AplicarFiltro();


        private void btnImprimir_Click_1(object sender, EventArgs e)
        {
            
            var pd = new PrintDocument();
            pd.PrintPage += (s, ev) =>
            {
                float y = 40;
                ev.Graphics.DrawString("BITÁCORA DE EVENTOS - Hotel Rydl",
                    new Font("Arial", 12, FontStyle.Bold), Brushes.Black, 40, y);
                y += 30;
                ev.Graphics.DrawString($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}",
                    new Font("Arial", 9), Brushes.Gray, 40, y);
                y += 25;

                var encabezado = "Login        Fecha        Hora    Módulo        Evento            Crit.";
                ev.Graphics.DrawString(encabezado, new Font("Courier New", 8, FontStyle.Bold),
                    Brushes.Black, 40, y);
                y += 18;

                foreach (var r in _datos)
                {
                    string linea = $"{(r.Login ?? "-"),-12} {r.Fecha:dd/MM/yy}  {r.Hora:hh\\:mm}  {(r.NombreModulo ?? "-"),-14}{(r.NombreEvento ?? "-"),-20}{r.Criticidad}";
                    ev.Graphics.DrawString(linea, new Font("Courier New", 7.5f),
                        Brushes.Black, 40, y);
                    y += 15;
                    if (y > ev.PageBounds.Height - 60) { ev.HasMorePages = true; return; }
                }
            };

            using (var ppd = new PrintPreviewDialog { Document = pd, Width = 800, Height = 600 })
                ppd.ShowDialog(this);
        }

        private void btnSalir_Click_1(object sender, EventArgs e) => Close();
        

        
    }
}

