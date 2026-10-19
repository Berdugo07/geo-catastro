using System;
using System.Windows;
using System.Windows.Controls;
using SistemaCatastralAddinGSI.Data;

namespace SistemaCatastralAddinGSI.Dockpanes
{
    public partial class InformacionPredioDockpaneView : UserControl
    {
        public InformacionPredioDockpaneView()
        {
            InitializeComponent();
            Loaded += (s,e) => {
                var vm=InformacionPredioDockpaneViewModel.Show();
                if (vm == null) return;
                vm.Actualizado -= Actualizar;
                vm.Actualizado += Actualizar;
                Actualizar();
            };
        }
        private async void Actualizar()
        {
            var info=InformacionPredioDockpaneViewModel.Show()?.PredioActual;
            if (info == null) { txtEstado.Text="Selecciona un predio";txtContenido.Text=""; return; }
            RegistroCatastralData ficha;
            try { ficha=await RegistroCatastralStore.CargarPorUidAsync(info.PredioUid); }
            catch(Exception ex) { txtEstado.Text="ERROR DE GEODATABASE"; txtContenido.Text=ex.Message; return; }
            if (InformacionPredioDockpaneViewModel.Show()?.PredioActual?.PredioUid != info.PredioUid) return;
            txtEstado.Text=ficha == null ? "SIN REGISTRAR" : "REGISTRADO";
            if (ficha == null) {
                txtContenido.Text=$"ObjectID: {info.ObjectID}\nManzana: {info.Manzano}\nLote: {info.Lote}\nSuperficie: {info.Superficie:N2} m²\n\nAún no se ha completado la ficha catastral.";
                return;
            }
            var nombre=$"{ficha.Nombres} {ficha.ApellidoPaterno} {ficha.ApellidoMaterno}".Trim();
            txtContenido.Text=$"Código: {ficha.CodigoCatastral}\nObjectID: {info.ObjectID}\n\n"+
                $"Distrito: {ficha.Distrito}\nZona: {ficha.Zona}\nManzana: {ficha.Manzana}\nLote: {ficha.Predio}\n"+
                $"Dirección: {ficha.Direccion}\nSuperficie: {ficha.SuperficieTotal} m²\n\n"+
                $"Propietario: {nombre}\nDocumento: {ficha.TipoDocumento} {ficha.NumeroDocumento}\n"+
                $"Copropietarios: {ficha.PropietariosAdicionales?.Count ?? 0}\nMatrícula: {ficha.Matricula}\n\n"+
                $"Colinda al norte: {ficha.ColindanteNorte}\nSur: {ficha.ColindanteSur}\nEste: {ficha.ColindanteEste}\nOeste: {ficha.ColindanteOeste}\n\n"+
                $"Última actualización: {ficha.FechaModificacion:dd/MM/yyyy HH:mm}";
        }
        private void Editar_Click(object sender, RoutedEventArgs e)
        {
            var info=InformacionPredioDockpaneViewModel.Show()?.PredioActual;
            if (info == null) return;
            var registro=RegistrarPredioDockpaneViewModel.Show();
            if (registro == null) return;
            registro.CargarPredio(info);
            registro.Activate();
        }
    }
}
