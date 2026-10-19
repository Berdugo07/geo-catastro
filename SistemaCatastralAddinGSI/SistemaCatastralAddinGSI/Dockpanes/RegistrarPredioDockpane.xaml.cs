using Microsoft.Win32;
using SistemaCatastralAddinGSI.Data;
using SistemaCatastralAddinGSI.Tools;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Linq;

namespace SistemaCatastralAddinGSI.Dockpanes
{
    public partial class RegistrarPredioDockpaneView : UserControl
    {
        private RegistroCatastralData _dataActual;
        private PredioInfo _predioActual;
        private readonly ObservableCollection<PropietarioAdicional> _copropietarios = new();

        public RegistrarPredioDockpaneView()
        {
            InitializeComponent();
            lstOtrosPropietarios.ItemsSource = _copropietarios;

            Loaded += RegistrarPredioDockpaneView_Loaded;
        }

        private void RegistrarPredioDockpaneView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            var vm = RegistrarPredioDockpaneViewModel.Show();

            if (vm == null)
                return;

            vm.SolicitarSeccion -= IrASeccion;
            vm.SolicitarSeccion += IrASeccion;
            vm.PredioSeleccionado -= CargarPredioEnFormulario;
            vm.PredioSeleccionado += CargarPredioEnFormulario;

            var predioPendiente = vm.ObtenerPredioPendiente();

            if (predioPendiente != null)
            {
                CargarPredioEnFormulario(predioPendiente);
            }
            IrASeccion(vm.SeccionPendiente);
        }
        private void IrASeccion(string seccion)
        {
            System.Windows.FrameworkElement element = seccion switch
            {
                "Colindantes" => gbColindantes,
                "Propietarios" => gbPropietarios,
                _ => gbPredio
            };
            Dispatcher.BeginInvoke(new System.Action(() => element.BringIntoView()));
        }

        private async void CargarPredioEnFormulario(PredioInfo info)
        {
            if (info == null)
                return;

            _predioActual = info;
            _dataActual = null; // Bloquea guardado con la ficha del predio anterior mientras carga.

            // En beta, el GlobalID identifica a la ficha dentro de la misma geodatabase.
            RegistroCatastralData ficha;
            try { ficha = await RegistroCatastralStore.CargarPorUidAsync(info.PredioUid); }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error leyendo CAT_REGISTROS: " + ex.Message, "Geodatabase");
                return;
            }
            // Ignora cargas asíncronas de un predio que ya dejó de estar seleccionado.
            if (_predioActual?.PredioUid != info.PredioUid) return;
            _dataActual = ficha;
            if (_dataActual != null)
            {
                // La ficha se vincula por GlobalID: el ObjectID almacenado es solo informativo.
                _dataActual.ObjectID = info.ObjectID;
                _dataActual.PredioUid = info.PredioUid;
            }

            // Limpiar primero
            LimpiarFormulario();

            // =====================================================
            // PREDIO YA REGISTRADO
            // =====================================================

            if (_dataActual != null)
            {
                txtCodigoCatastral.Text =
                    _dataActual.CodigoCatastral;

                txtManzana.Text =
                    _dataActual.Manzana;

                txtPredio.Text =
                    _dataActual.Predio;

                txtDireccion.Text =
                    _dataActual.Direccion;

                txtSupTotal.Text =
                    _dataActual.SuperficieTotal;

                txtApellidoPaterno.Text =
                    _dataActual.ApellidoPaterno;

                txtApellidoMaterno.Text =
                    _dataActual.ApellidoMaterno;

                txtNombres.Text =
                    _dataActual.Nombres;

                cmbTipoDocumento.Text =
                    _dataActual.TipoDocumento;

                txtDocumento.Text =
                    _dataActual.NumeroDocumento;

                txtPorcentaje.Text =
                    _dataActual.Porcentaje;

                txtNotario.Text =
                    _dataActual.Notario;

                txtRegistro.Text =
                    _dataActual.Registro;

                if (_dataActual.FechaEscritura.HasValue)
                    dpFechaEscritura.SelectedDate =
                        _dataActual.FechaEscritura;

                txtMatricula.Text =
                    _dataActual.Matricula;

                txtSuperficieTitulo.Text =
                    _dataActual.SuperficieTitulo;

                txtDistrito.Text =
                    _dataActual.Distrito;

                txtZona.Text =
                    _dataActual.Zona;

                txtCalle.Text =
                    _dataActual.Calle;

                cmbDestinoUso.Text =
                    _dataActual.DestinoUso;

                cmbTipologia.Text =
                    _dataActual.Tipologia;

                cmbEstadoFisico.Text =
                    _dataActual.EstadoFisico;

                txtPlanta.Text =
                    _dataActual.Planta;

                txtSupConstruida.Text =
                    _dataActual.SuperficieConstruida;

                txtAnioConstruccion.Text =
                    _dataActual.AnioConstruccion;

                rbAguaSi.IsChecked =
                    _dataActual.AguaCorriente;

                rbAguaNo.IsChecked =
                    !_dataActual.AguaCorriente;

                rbEnergiaSi.IsChecked =
                    _dataActual.EnergiaElectrica;

                rbEnergiaNo.IsChecked =
                    !_dataActual.EnergiaElectrica;

                rbAlumbradoSi.IsChecked =
                    _dataActual.AlumbradoPublico;

                rbAlumbradoNo.IsChecked =
                    !_dataActual.AlumbradoPublico;

                rbTelefonoSi.IsChecked =
                    _dataActual.Telefono;

                rbTelefonoNo.IsChecked =
                    !_dataActual.Telefono;

                cmbMaterialVia.Text =
                    _dataActual.MaterialVia;

                cmbPendiente.Text =
                    _dataActual.Pendiente;

                cmbForma.Text =
                    _dataActual.Forma;

                txtFrente.Text =
                    _dataActual.Frente;

                txtFondo.Text =
                    _dataActual.Fondo;

                txtSupMensura.Text =
                    _dataActual.SupMensura;

                txtSupEscritura.Text =
                    _dataActual.SupEscritura;

                txtAreaExcedente.Text =
                    _dataActual.AreaExcedente;

                txtAreaAfectada.Text =
                    _dataActual.AreaAfectada;

                txtSuperficieUtil.Text =
                    _dataActual.SuperficieUtil;

                txtObservaciones.Text =
                    _dataActual.Observaciones;
                txtColindanteNorte.Text = _dataActual.ColindanteNorte;
                txtColindanteSur.Text = _dataActual.ColindanteSur;
                txtColindanteEste.Text = _dataActual.ColindanteEste;
                txtColindanteOeste.Text = _dataActual.ColindanteOeste;
                foreach (var co in _dataActual.PropietariosAdicionales ?? new System.Collections.Generic.List<PropietarioAdicional>())
                    _copropietarios.Add(co);

                return;
            }

            // =====================================================
            // PREDIO NUEVO
            // =====================================================

            _dataActual = new RegistroCatastralData
            {
                ObjectID = info.ObjectID,
                PredioUid = info.PredioUid,

                CodigoCatastral = "",

                Manzana = info.Manzano,

                Predio = info.Lote,

                Direccion = info.OTB,

                SuperficieTotal =
                    info.Superficie.ToString("F2")
            };

            txtCodigoCatastral.Text = "";

            txtManzana.Text =
                info.Manzano;

            txtPredio.Text =
                info.Lote;

            txtDireccion.Text =
                info.OTB;

            txtSupTotal.Text =
                info.Superficie.ToString("F2");
        }

        private void LimpiarFormulario()
        {
            txtCodigoCatastral.Text = "";

            txtApellidoPaterno.Text = "";
            txtApellidoMaterno.Text = "";
            txtNombres.Text = "";
            txtDocumento.Text = "";
            txtPorcentaje.Text = "";
            txtNotario.Text = "";
            txtRegistro.Text = "";
            txtMatricula.Text = "";
            txtSuperficieTitulo.Text = "";

            txtDistrito.Text = "";
            txtZona.Text = "";
            txtManzana.Text = "";
            txtPredio.Text = "";
            txtCalle.Text = "";
            txtDireccion.Text = "";

            txtPlanta.Text = "";
            txtSupConstruida.Text = "";
            txtAnioConstruccion.Text = "";
            txtSupTotal.Text = "";

            txtFrente.Text = "";
            txtFondo.Text = "";
            txtSupMensura.Text = "";
            txtSupEscritura.Text = "";
            txtAreaExcedente.Text = "";
            txtAreaAfectada.Text = "";
            txtSuperficieUtil.Text = "";

            txtObservaciones.Text = "";
            txtColindanteNorte.Text = "";
            txtColindanteSur.Text = "";
            txtColindanteEste.Text = "";
            txtColindanteOeste.Text = "";
            txtOtroNombre.Text = "";
            txtOtroDocumento.Text = "";
            txtOtroPorcentaje.Text = "";
            _copropietarios.Clear();

            dpFechaEscritura.SelectedDate = null;

            rbAguaSi.IsChecked = false;
            rbAguaNo.IsChecked = false;

            rbEnergiaSi.IsChecked = false;
            rbEnergiaNo.IsChecked = false;

            rbAlumbradoSi.IsChecked = false;
            rbAlumbradoNo.IsChecked = false;

            rbTelefonoSi.IsChecked = false;
            rbTelefonoNo.IsChecked = false;
        }

        private void btnAgregarPropietario_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtOtroNombre.Text) || string.IsNullOrWhiteSpace(txtOtroDocumento.Text))
            {
                MessageBox.Show("Complete el nombre y el documento del copropietario.", "Catastro");
                return;
            }
            _copropietarios.Add(new PropietarioAdicional {
                NombreCompleto = txtOtroNombre.Text.Trim(),
                Documento = txtOtroDocumento.Text.Trim(),
                Porcentaje = txtOtroPorcentaje.Text.Trim()
            });
            txtOtroNombre.Clear();txtOtroDocumento.Clear();txtOtroPorcentaje.Clear();
        }
        private void btnQuitarPropietario_Click(object sender, RoutedEventArgs e)
        {
            if (lstOtrosPropietarios.SelectedItem is PropietarioAdicional item)
                _copropietarios.Remove(item);
        }

        private void btnCargarFoto_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Seleccionar Fotografía",
                Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (dlg.ShowDialog() == true)
            {
                _dataActual?.Fotografias.Add(dlg.FileName);

                MessageBox.Show(
                    $"Foto agregada:\n{dlg.FileName}",
                    "Fotografía");
            }
        }

        private void btnCargarDocumento_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Seleccionar Documentos",
                Filter = "Documentos|*.pdf;*.docx;*.doc;*.jpg;*.png",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                if (_dataActual != null)
                {
                    foreach (var f in dlg.FileNames)
                        _dataActual.Documentos.Add(f);
                }

                MessageBox.Show(
                    $"Documentos agregados: {dlg.FileNames.Length}",
                    "Documentos");
            }
        }

        private async void btnGuardar_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_dataActual == null)
            {
                MessageBox.Show(
                    "No hay un predio seleccionado.",
                    "Error");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                txtCodigoCatastral.Text))
            {
                MessageBox.Show(
                    "⚠️ Debe ingresar el CÓDIGO CATASTRAL antes de guardar.\n\n" +
                    "Es la clave principal del registro.",
                    "Código Catastral obligatorio");

                txtCodigoCatastral.Focus();

                return;
            }

            if (string.IsNullOrWhiteSpace(txtNombres.Text) || string.IsNullOrWhiteSpace(txtDocumento.Text))
            {
                MessageBox.Show("Para registrar se requiere al menos nombre/razón social y número de documento del propietario.", "Datos obligatorios");
                return;
            }

            // =====================================================
            // COPIAR DATOS DEL FORMULARIO
            // =====================================================

            _dataActual.CodigoCatastral =
                txtCodigoCatastral.Text.Trim();

            _dataActual.ApellidoPaterno =
                txtApellidoPaterno.Text;

            _dataActual.ApellidoMaterno =
                txtApellidoMaterno.Text;

            _dataActual.Nombres =
                txtNombres.Text;

            _dataActual.TipoDocumento =
                cmbTipoDocumento.Text;

            _dataActual.NumeroDocumento =
                txtDocumento.Text;

            _dataActual.Porcentaje =
                txtPorcentaje.Text;

            _dataActual.Notario =
                txtNotario.Text;

            _dataActual.Registro =
                txtRegistro.Text;

            _dataActual.FechaEscritura =
                dpFechaEscritura.SelectedDate;

            _dataActual.Matricula =
                txtMatricula.Text;

            _dataActual.SuperficieTitulo =
                txtSuperficieTitulo.Text;

            _dataActual.Distrito =
                txtDistrito.Text;

            _dataActual.Zona =
                txtZona.Text;

            _dataActual.Manzana =
                txtManzana.Text;

            _dataActual.Predio =
                txtPredio.Text;

            _dataActual.Calle =
                txtCalle.Text;

            _dataActual.Direccion =
                txtDireccion.Text;

            _dataActual.DestinoUso =
                cmbDestinoUso.Text;

            _dataActual.Tipologia =
                cmbTipologia.Text;

            _dataActual.EstadoFisico =
                cmbEstadoFisico.Text;

            _dataActual.Planta =
                txtPlanta.Text;

            _dataActual.SuperficieConstruida =
                txtSupConstruida.Text;

            _dataActual.AnioConstruccion =
                txtAnioConstruccion.Text;

            _dataActual.SuperficieTotal =
                txtSupTotal.Text;

            _dataActual.AguaCorriente =
                rbAguaSi.IsChecked == true;

            _dataActual.EnergiaElectrica =
                rbEnergiaSi.IsChecked == true;

            _dataActual.AlumbradoPublico =
                rbAlumbradoSi.IsChecked == true;

            _dataActual.Telefono =
                rbTelefonoSi.IsChecked == true;

            _dataActual.MaterialVia =
                cmbMaterialVia.Text;

            _dataActual.Pendiente =
                cmbPendiente.Text;

            _dataActual.Forma =
                cmbForma.Text;

            _dataActual.Frente =
                txtFrente.Text;

            _dataActual.Fondo =
                txtFondo.Text;

            _dataActual.SupMensura =
                txtSupMensura.Text;

            _dataActual.SupEscritura =
                txtSupEscritura.Text;

            _dataActual.AreaExcedente =
                txtAreaExcedente.Text;

            _dataActual.AreaAfectada =
                txtAreaAfectada.Text;

            _dataActual.SuperficieUtil =
                txtSuperficieUtil.Text;

            _dataActual.Observaciones =
                txtObservaciones.Text;
            _dataActual.ColindanteNorte = txtColindanteNorte.Text;
            _dataActual.ColindanteSur = txtColindanteSur.Text;
            _dataActual.ColindanteEste = txtColindanteEste.Text;
            _dataActual.ColindanteOeste = txtColindanteOeste.Text;
            _dataActual.PropietariosAdicionales = _copropietarios.ToList();

            // =====================================================
            // GUARDAR
            // =====================================================

            try { await RegistroCatastralStore.GuardarAsync(_dataActual); }
            catch (System.Exception ex) { MessageBox.Show("No se guardó el registro: " + ex.Message, "Validación"); return; }

            // =====================================================
            // MOSTRAR CÓDIGO EN EL MAPA
            // =====================================================

            if (_dataActual.ObjectID > 0 &&
                !string.IsNullOrWhiteSpace(
                    _dataActual.CodigoCatastral))
            {
                try
                {
                    await CatastroVisualManager.PintarRegistradoAsync(_dataActual.ObjectID);
                    await CadastralLabelManager
                        .MostrarCodigoAsync(
                            _dataActual.ObjectID,
                            _dataActual.CodigoCatastral);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(
                        "El registro se guardó correctamente,\n" +
                        "pero no se pudo mostrar el código sobre el mapa.\n\n" +
                        ex.Message,
                        "Aviso");
                }
            }

            // =====================================================
            // CERRAR FORMULARIO
            // =====================================================

            var dockpane =
                RegistrarPredioDockpaneViewModel.Show();

            if (dockpane != null)
                dockpane.Activate();
            var info = InformacionPredioDockpaneViewModel.Show();
            if (info?.PredioActual?.ObjectID == _dataActual.ObjectID)
                info.CargarPredio(_predioActual);
            MessageBox.Show("Ficha guardada correctamente. El predio fue marcado como registrado.", "Catastro");
        }

        private void btnSalir_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dockpane =
                RegistrarPredioDockpaneViewModel.Show();

            if (dockpane != null)
                dockpane.Hide();
        }

        private void txtApellidoPaterno_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
        }
    }
}