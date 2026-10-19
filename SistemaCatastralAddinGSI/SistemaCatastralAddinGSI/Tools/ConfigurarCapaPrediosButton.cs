using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using SistemaCatastralAddinGSI.Data;
using ArcGIS.Core.CIM;
using System;
using System.Linq;
using System.Windows;

namespace SistemaCatastralAddinGSI.Tools
{
    internal class ConfigurarCapaPrediosButton : Button
    {
        protected override async void OnClick()
        {
            var vista = MapView.Active;
            if (vista == null || Project.Current == null)
            {
                MessageBox.Show("Abre primero un proyecto y una vista de mapa.",
                    "Catastro - Configurar capa");
                return;
            }

            try
            {
                // No se elige automaticamente por nombre para evitar
                // vincular manzanos, construcciones o una capa duplicada.
                var seleccionada = await QueuedTask.Run(() =>
                {
                    var capas = vista.GetSelectedLayers();
                    if (capas.Count != 1 ||
                        capas[0] is not FeatureLayer capa ||
                        capa.ShapeType != esriGeometryType.esriGeometryPolygon)
                        return null;

                    return new CapaPrediosAsignada
                    {
                        CapaUri = capa.URI,
                        NombreCapa = capa.Name,
                        MapaUri = vista.Map.URI
                    };
                });

                if (seleccionada == null)
                {
                    MessageBox.Show(
                        "En Contents, haz UN clic sobre UNA sola capa de " +
                        "poligonos de predios; despues vuelve a pulsar " +
                        "'Configurar capa'.",
                        "Catastro - Configurar capa");
                    return;
                }

                var confirmacion = MessageBox.Show(
                    "Se configurara esta capa de POLIGONOS como predios:\n\n" +
                    seleccionada.NombreCapa + "\n\n" +
                    "¿Confirmas que contiene lotes/predios y NO manzanos?",
                    "Catastro - Confirmar capa principal",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (confirmacion != MessageBoxResult.Yes)
                    return;

                // No escribir datos catastrales ni modificar la geodatabase.
                CapaPrediosConfig.Guardar(
                    Project.Current.URI, seleccionada.MapaUri,
                    seleccionada.CapaUri, seleccionada.NombreCapa);

                MessageBox.Show(
                    "Capa principal configurada para este mapa:\n\n" +
                    seleccionada.NombreCapa + "\n\n" +
                    "Desde ahora puedes hacer clic en 'Seleccionar Predio' " +
                    "sin volver a elegir la capa en Contents.",
                    "Catastro - Configurar capa");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo guardar la configuracion:\n" + ex.Message,
                    "Catastro - Configurar capa");
            }
        }
    }
}
