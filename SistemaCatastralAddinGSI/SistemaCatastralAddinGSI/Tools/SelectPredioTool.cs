using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using SistemaCatastralAddinGSI.Data;
using SistemaCatastralAddinGSI.Dockpanes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SistemaCatastralAddinGSI.Tools
{
    internal class SelectPredioTool : MapTool
    {
        private static IDisposable _resaltadoActual;

        public SelectPredioTool()
        {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Point;
            SketchOutputMode = SketchOutputMode.Map;
            UseSnapping = false;
        }

        protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            if (geometry is not MapPoint punto || MapView.Active == null)
                return false;

            var vista = MapView.Active;
            PredioInfo predio = null;
            string aviso = null;

            try
            {
                await QueuedTask.Run(() =>
                {
 
                    var capas = vista.GetSelectedLayers()
                        .OfType<FeatureLayer>()
                        .Where(c => c.ShapeType == esriGeometryType.esriGeometryPolygon)
                        .ToList();

                    if (capas.Count != 1)
                    {
                        aviso = "Seleccione UNA sola capa de polígonos de predios " +
                                "en el panel Contents y vuelva a hacer clic en el mapa.";
                        return;
                    }

                    var capa = capas[0];
                    if (!capa.IsVisible)
                    {
                        aviso = "La capa seleccionada no está visible. " +
                                "Active su casilla de visibilidad en Contents.";
                        return;
                    }

                    Polygon poligono = null;
                    long oid = -1;

                    using (var fc = capa.GetFeatureClass())
                    {
                        SpatialReference srCapa;
                        using (var definicion = fc.GetDefinition())
                            srCapa = definicion.GetSpatialReference();

                        var puntoCapa = srCapa == null
                            ? punto
                            : GeometryEngine.Instance.Project(punto, srCapa);

                        var filtro = new SpatialQueryFilter
                        {
                            FilterGeometry = puntoCapa,
                            SpatialRelationship = SpatialRelationship.Intersects,
                            SubFields = "*"
                        };

                        using (var cursor = fc.Search(filtro, false))
                        {
                            if (!cursor.MoveNext())
                            {
                                aviso = "No se encontró un predio en ese punto " +
                                        "de la capa seleccionada.";
                                return;
                            }

                            using (var entidad = cursor.Current as Feature)
                            {
                                if (entidad == null)
                                    return;

                                oid = entidad.GetObjectID();
                                poligono = entidad.GetShape() as Polygon;
                                predio = new PredioInfo
                                {
                                    ObjectID = oid,
                                    Manzano = LeerCampo(entidad, "N_MANZANO"),
                                    Lote = LeerCampo(entidad, "N_LOTE"),
                                    OTB = LeerCampo(entidad, "OTB_URB_JV"),
                                    Funcion = LeerCampo(entidad, "FUNCION")
                                };

                                var valorSup = LeerCampo(entidad, "SUPERFICIE");
                                if (double.TryParse(valorSup, out double superficie))
                                    predio.Superficie = superficie;
                            }

                            if (cursor.MoveNext())
                            {
                                predio = null;
                                aviso = "Hay polígonos superpuestos en esta ubicación. " +
                                        "Seleccione un punto interior inequívoco.";
                                return;
                            }
                        }
                    }

                    if (poligono == null || oid < 0 || predio == null)
                    {
                        predio = null;
                        aviso = "No se pudo obtener la geometría del predio.";
                        return;
                    }

                    vista.Map.ClearSelection();
                    using (var seleccion = capa.Select(
                        new QueryFilter { ObjectIDs = new List<long> { oid } },
                        SelectionCombinationMethod.New))
                    {
                    }

                    _resaltadoActual?.Dispose();
                    var contorno = SymbolFactory.Instance.ConstructStroke(
                        CIMColor.CreateRGBColor(0, 105, 210, 100),
                        2.0, SimpleLineStyle.Solid);
                    var simbolo = SymbolFactory.Instance.ConstructPolygonSymbol(
                        CIMColor.CreateRGBColor(0, 140, 240, 45),
                        SimpleFillStyle.Solid, contorno);

                    var geometriaMapa = vista.Map.SpatialReference == null
                        ? poligono
                        : GeometryEngine.Instance.Project(
                            poligono, vista.Map.SpatialReference);

                    _resaltadoActual = vista.AddOverlay(
                        geometriaMapa, simbolo.MakeSymbolReference());
                });
            }
            catch (Exception ex)
            {
                predio = null;
                aviso = "No se pudo seleccionar el predio: " + ex.Message;
            }

            if (predio == null)
            {
                if (!string.IsNullOrWhiteSpace(aviso))
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                        MessageBox.Show(aviso, "Catastro - Seleccionar predio"));
                return false;
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var panel = RegistrarPredioDockpaneViewModel.Show();
                if (panel == null)
                {
                    MessageBox.Show("No se encontró el panel de registro.", "Catastro");
                    return;
                }
                panel.CargarPredio(predio);
                panel.Activate();
            });

            return true;
        }

        private static string LeerCampo(Feature entidad, string nombre)
        {
            try
            {
                var campo = entidad.GetFields().FirstOrDefault(c =>
                    string.Equals(c.Name, nombre,
                        StringComparison.OrdinalIgnoreCase));
                return campo == null ? "" : entidad[campo.Name]?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }
    }
}
