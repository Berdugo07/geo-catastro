using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Core;
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
        // Relleno temporal: no representa que el predio este registrado.
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

            CapaPrediosAsignada configuracion;
            try
            {
                configuracion = CapaPrediosConfig.Obtener(
                    Project.Current?.URI, vista.Map.URI);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo leer la capa principal: " + ex.Message,
                    "Catastro");
                return false;
            }

            if (configuracion == null)
            {
                MessageBox.Show(
                    "No hay una capa principal configurada para este mapa.\n\n" +
                    "1. Selecciona la capa de predios en Contents.\n" +
                    "2. Haz clic en 'Catastro > Configurar capa'.\n" +
                    "3. Vuelve a usar 'Seleccionar Predio'.",
                    "Catastro - Configurar capa");
                return false;
            }

            try
            {
                await QueuedTask.Run(() =>
                {
                    // Buscar la capa exacta que el usuario configuro
                    // mediante su URI, NUNCA por nombre u ObjectID.
                    var capa = vista.Map.GetLayersAsFlattenedList()
                        .OfType<FeatureLayer>()
                        .FirstOrDefault(c => string.Equals(
                            c.URI, configuracion.CapaUri,
                            StringComparison.OrdinalIgnoreCase));

                    if (capa == null ||
                        capa.ShapeType != esriGeometryType.esriGeometryPolygon)
                    {
                        aviso = "La capa principal configurada ya no existe " +
                                "en este mapa o dejo de ser de poligonos. " +
                                "Selecciona la capa correcta en Contents y " +
                                "usa 'Configurar capa' nuevamente.";
                        return;
                    }
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

                            // Evitar elegir arbitrariamente un predio
                            // si dos poligonos de esta capa se superponen.
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

                    // Selección nativa de ArcGIS: se usa OID solo temporalmente.
                    vista.Map.ClearSelection();
                    using (var seleccion = capa.Select(
                        new QueryFilter { ObjectIDs = new List<long> { oid } },
                        SelectionCombinationMethod.New))
                    {
                    }

                    // Overlay encima del mapa para garantizar un relleno visible,
                    // incluso cuando hay otras capas dibujadas encima.
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
