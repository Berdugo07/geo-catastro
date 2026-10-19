using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SistemaCatastralAddinGSI.Data;
using SistemaCatastralAddinGSI.Tools;

namespace SistemaCatastralAddinGSI.Dockpanes
{
    public partial class BuscarPredioDockpaneView : UserControl
    {
        private sealed class Resultado
        {
            public RegistroCatastralData Registro { get; set; }
            public override string ToString() =>
                $"{Registro.CodigoCatastral}   |   Mz {Registro.Manzana} - Lt {Registro.Predio}\n"+
                $"{Registro.Nombres} {Registro.ApellidoPaterno} {Registro.ApellidoMaterno}";
        }
        public BuscarPredioDockpaneView() { InitializeComponent(); Loaded += (s,e) => Buscar(); }
        private void Consulta_KeyDown(object sender, KeyEventArgs e) { if(e.Key == Key.Enter) Buscar(); }
        private void Buscar_Click(object sender, RoutedEventArgs e) => Buscar();
        private async void Buscar()
        {
            System.Collections.Generic.List<RegistroCatastralData> fichas;
            try { fichas=(await RegistroCatastralStore.BuscarAsync(txtConsulta.Text)).Take(200).ToList(); }
            catch(Exception ex) { MessageBox.Show("Error buscando en CAT_REGISTROS: " + ex.Message); return; }
            lstResultados.ItemsSource=fichas.Select(x => new Resultado{Registro=x}).ToList();
            txtCantidad.Text=$"{fichas.Count} resultado(s). Doble clic para ubicar en el mapa (máximo 200).";
        }
        private async void Resultado_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(lstResultados.SelectedItem is Resultado item)) return;
            try
            {
                var info=await CadastralMapService.IrAUidAsync(item.Registro.PredioUid);
                if (info == null) { MessageBox.Show("No se encontró el GlobalID en la capa PREDIOS activa. Verifica que la geodatabase sea la correcta."); return; }
                var pane=InformacionPredioDockpaneViewModel.Show();
                pane?.CargarPredio(info);
                pane?.Activate();
            }
            catch(Exception ex) { MessageBox.Show("Error al ubicar el predio: " + ex.Message); }
        }
    }
}
