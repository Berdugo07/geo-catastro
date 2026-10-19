using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Reports;
using SistemaCatastralAddinGSI.Data;
using SistemaCatastralAddinGSI.Dockpanes;
using SistemaCatastralAddinGSI.Tools;
using System;

namespace SistemaCatastralAddinGSI
{
    internal class Module1 : Module
    {
        private static Module1 _this;
        public static Module1 Current => _this;
        public Module1() { _this = this; }
        protected override bool Initialize() => true;
    }
    internal class RegistrarPredioButton : Button
    {
        protected override void OnClick()
        {
            var pane = RegistrarPredioDockpaneViewModel.Show();
            if (pane == null) return;
            if (!RegistrarPredioDockpaneViewModel.HayPredioSeleccionado)
            {
                MessageBox.Show("Seleccione primero un predio en el mapa con 'Seleccionar Predio'.", "Catastro");
                return;
            }
            pane.Activate();
        }
    }
    
    internal class BuscarPredioButton : Button
    {
        protected override async void OnClick() =>
            await FrameworkApplication.SetCurrentToolAsync("SistemaCatastralAddinGSI_SelectPredioTool");
    }
    internal class IdentificarButton : Button
    {
        protected override async void OnClick() =>
            await FrameworkApplication.SetCurrentToolAsync("SistemaCatastralAddinGSI_IdentificarPredioTool");
    }
    internal class BuscarCatastralButton : Button
    {
        protected override void OnClick() => BuscarPredioDockpaneViewModel.Show()?.Activate();
    }
    internal class EditarPredioButton : Button
    {
        protected override void OnClick() => AbrirEdicion("Predio");
        internal static void AbrirEdicion(string seccion)
        {
            var seleccionado = InformacionPredioDockpaneViewModel.Show()?.PredioActual;
            var registro = RegistrarPredioDockpaneViewModel.Show();
            if (registro == null) return;
            if (seleccionado != null) registro.CargarPredio(seleccionado);
            if (!RegistrarPredioDockpaneViewModel.HayPredioSeleccionado)
                MessageBox.Show("Primero selecciona un predio.", "Catastro");
            else { registro.Activate(); registro.AbrirSeccion(seccion); }
        }
    }
    internal class EditarColindantesButton : Button
    {
        protected override void OnClick() => EditarPredioButton.AbrirEdicion("Colindantes");
    }
    internal class EditarPropietariosButton : Button
    {
        protected override void OnClick() => EditarPredioButton.AbrirEdicion("Propietarios");
    }
    internal class GenerarPDFButton : Button
    {
        protected override async void OnClick()
        {
            var info = InformacionPredioDockpaneViewModel.Show()?.PredioActual
                ?? RegistrarPredioDockpaneViewModel.Show()?.PredioActual;
            if (info == null) { MessageBox.Show("Selecciona un predio registrado antes de generar el PDF."); return; }
            RegistroCatastralData ficha;
            try { ficha = await RegistroCatastralStore.CargarPorUidAsync(info.PredioUid); }
            catch (Exception ex) { MessageBox.Show("Error leyendo CAT_REGISTROS: " + ex.Message); return; }
            if (ficha == null) { MessageBox.Show("Este predio aún no tiene un formulario guardado."); return; }
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Exportar Formulario Único Catastral",
                Filter = "PDF|*.pdf",
                FileName = "FURC_" + ficha.CodigoCatastral + ".pdf"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                Reports.FormularioPdfGenerator.Crear(dlg.FileName, ficha);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
            catch (Exception ex) { MessageBox.Show("No se pudo exportar el PDF: " + ex.Message); }
        }
    }
    internal class ActualizarMapaButton : Button
    {
        protected override async void OnClick()
        {
            try { await CatastroVisualManager.RefrescarAsync(); }
            catch (Exception ex) { MessageBox.Show("Error al actualizar el mapa: " + ex.Message); }
        }
    }
    internal class CertificadosButton : Button
    {
        protected override void OnClick() => MessageBox.Show("Certificados: sección reservada para una siguiente etapa.", "Catastro");
    }
}
