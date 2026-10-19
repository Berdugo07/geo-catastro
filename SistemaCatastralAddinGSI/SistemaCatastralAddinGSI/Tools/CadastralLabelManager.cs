using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaCatastralAddinGSI.Tools
{
    internal static class CadastralLabelManager
    {
        private static readonly Dictionary<long, IDisposable>
            _labels = new Dictionary<long, IDisposable>();


        public static async Task MostrarCodigoAsync(
            long objectID,
            string codigoCatastral)
        {
            if (string.IsNullOrWhiteSpace(codigoCatastral))
                return;

            var mapView = MapView.Active;

            if (mapView == null)
                return;


            await QueuedTask.Run(() =>
            {
                var predioLayer = mapView.Map
                    .GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .FirstOrDefault(l =>
                        l.Name.Equals(
                            "PREDIOS",
                            StringComparison.OrdinalIgnoreCase));

                if (predioLayer == null)
                {
                    predioLayer = mapView.Map
                        .GetLayersAsFlattenedList()
                        .OfType<FeatureLayer>()
                        .FirstOrDefault(l =>
                            l.Name.ToUpper()
                                .Contains("PREDIO"));
                }

                if (predioLayer == null)
                    return;


                Polygon polygon = null;


                // ============================================
                // BUSCAR GEOMETRÍA POR OBJECTID
                // ============================================

                using (var fc = predioLayer.GetFeatureClass())
                {
                    var query = new QueryFilter
                    {
                        ObjectIDs =
                            new List<long>
                            {
                                objectID
                            },
                        SubFields = "*"
                    };

                    using (var cursor =
                           fc.Search(query, false))
                    {
                        if (cursor.MoveNext())
                        {
                            using (var feature =
                                   cursor.Current as Feature)
                            {
                                polygon =
                                    feature?.GetShape()
                                    as Polygon;
                            }
                        }
                    }
                }


                if (polygon == null)
                    return;


                // ============================================
                // PUNTO CENTRAL DEL PREDIO
                // ============================================

                MapPoint labelPoint =
                    GeometryEngine.Instance
                        .LabelPoint(polygon);

                if (labelPoint == null)
                    return;


                // ============================================
                // SI YA EXISTE UNA ETIQUETA PARA ESTE OID
                // LA ELIMINAMOS
                // ============================================

                if (_labels.TryGetValue(
                    objectID,
                    out IDisposable anterior))
                {
                    anterior?.Dispose();
                    _labels.Remove(objectID);
                }


                // ============================================
                // SÍMBOLO DEL TEXTO
                // ============================================

                var textSymbol =
                    SymbolFactory.Instance
                        .ConstructTextSymbol(
                            CIMColor.CreateRGBColor(
                                20, 60, 100, 255),
                            10,
                            "Arial",
                            "Bold");


                // Halo blanco para que sea legible
                textSymbol.HaloSymbol =
                    SymbolFactory.Instance
                        .ConstructPolygonSymbol(
                            CIMColor.CreateRGBColor(
                                255, 255, 255, 255));

                textSymbol.HaloSize = 1.5;


                // ============================================
                // CREAR TEXTO
                // ============================================

                var textGraphic =
                    new CIMTextGraphic
                    {
                        Shape = labelPoint,
                        Text = codigoCatastral,
                        Symbol =
                            textSymbol.MakeSymbolReference()
                    };


                // ============================================
                // MOSTRAR SOBRE EL MAPA
                // ============================================

                IDisposable overlay =
                    mapView.AddOverlay(
                        textGraphic);

                _labels[objectID] = overlay;
            });
        }


        public static void LimpiarEtiqueta(
            long objectID)
        {
            if (_labels.TryGetValue(
                objectID,
                out IDisposable overlay))
            {
                overlay?.Dispose();
                _labels.Remove(objectID);
            }
        }


        public static void LimpiarTodas()
        {
            foreach (var overlay in _labels.Values)
            {
                overlay?.Dispose();
            }

            _labels.Clear();
        }
    }
}