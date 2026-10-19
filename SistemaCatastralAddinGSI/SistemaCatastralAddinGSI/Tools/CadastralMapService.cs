using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using SistemaCatastralAddinGSI.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaCatastralAddinGSI.Tools
{
    internal static class CadastralMapService
    {
        private static FeatureLayer BuscarCapa(MapView view) =>
            view.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
                .FirstOrDefault(l => l.Name.Equals("PREDIOS", StringComparison.OrdinalIgnoreCase));

        private static string Campo(Feature feature, string campo)
        {
            try
            {
                var f = feature.GetFields().FirstOrDefault(x => x.Name.Equals(campo, StringComparison.OrdinalIgnoreCase));
                return f == null ? "" : feature[f.Name]?.ToString() ?? "";
            }
            catch { return ""; }
        }
        private static PredioInfo Extraer(Feature feature)
        {
            var sup = Campo(feature, "SUPERFICIE");
            if (!double.TryParse(sup, NumberStyles.Any, CultureInfo.CurrentCulture, out var area))
                double.TryParse(sup, NumberStyles.Any, CultureInfo.InvariantCulture, out area);
            return new PredioInfo {
                ObjectID = feature.GetObjectID(), PredioUid = RegistroCatastralStore.NormalizarUid(Campo(feature, "GlobalID")),
                Manzano = Campo(feature, "N_MANZANO"), Lote = Campo(feature, "N_LOTE"),
                OTB = Campo(feature, "OTB_URB_JV"), Funcion = Campo(feature, "FUNCION"),
                Superficie = area
            };
        }
        public static async Task<PredioInfo> SeleccionarPuntoAsync(MapPoint punto)
        {
            var vista = MapView.Active;
            if (vista == null || punto == null) return null;
            return await QueuedTask.Run(() =>
            {
                var capa = BuscarCapa(vista);
                if (capa == null) return null;
                using var fc = capa.GetFeatureClass();
                using var cursor = fc.Search(new SpatialQueryFilter {
                    FilterGeometry = punto, SpatialRelationship = SpatialRelationship.Intersects,
                    SubFields = "*" }, false);
                if (!cursor.MoveNext()) return null;
                using var feature = cursor.Current as Feature;
                if (feature == null) return null;
                var info = Extraer(feature);
                vista.Map.ClearSelection();
                using var selection = capa.Select(new QueryFilter { ObjectIDs = new List<long>{info.ObjectID} }, SelectionCombinationMethod.New);
                return info;
            });
        }
        public static Task<long> ObtenerOidPorUidAsync(string uid) => QueuedTask.Run(() =>
        {
            uid = RegistroCatastralStore.NormalizarUid(uid);
            var vista = MapView.Active;
            if (uid.Length == 0 || vista == null) return -1L;
            var capa = BuscarCapa(vista);
            if (capa == null) return -1L;
            using var fc = capa.GetFeatureClass();
            using var cursor = fc.Search(new QueryFilter { SubFields = "*" }, false);
            while (cursor.MoveNext())
            {
                using var row = cursor.Current as Feature;
                if (row != null && RegistroCatastralStore.NormalizarUid(Campo(row, "GlobalID")) == uid)
                    return row.GetObjectID();
            }
            return -1L;
        });

        public static async Task<PredioInfo> IrAUidAsync(string uid)
        {
            uid = RegistroCatastralStore.NormalizarUid(uid);
            var vista = MapView.Active;
            if (uid.Length == 0 || vista == null) return null;
            var seleccionado = await QueuedTask.Run(() =>
            {
                var capa = BuscarCapa(vista);
                if (capa == null) return (PredioInfo)null;
                using var fc = capa.GetFeatureClass();
                using var cursor = fc.Search(new QueryFilter { SubFields = "*" }, false);
                while (cursor.MoveNext())
                {
                    using var feature = cursor.Current as Feature;
                    if (feature == null ||
                        RegistroCatastralStore.NormalizarUid(Campo(feature, "GlobalID")) != uid) continue;
                    var info = Extraer(feature);
                    vista.Map.ClearSelection();
                    using var sel = capa.Select(new QueryFilter { ObjectIDs = new List<long> { info.ObjectID } }, SelectionCombinationMethod.New);
                    return info;
                }
                return null;
            });
            if (seleccionado != null) await vista.ZoomToSelectedAsync(TimeSpan.FromSeconds(1));
            return seleccionado;
        }

        public static async Task<PredioInfo> IrAOidAsync(long oid)
        {
            var vista = MapView.Active;
            if (vista == null || oid <= 0) return null;
            var encontrado = await QueuedTask.Run(() =>
            {
                var capa = BuscarCapa(vista);
                if (capa == null) return (info: (PredioInfo)null, capa: (FeatureLayer)null);
                using var fc = capa.GetFeatureClass();
                using var cursor = fc.Search(new QueryFilter { ObjectIDs = new List<long>{oid}, SubFields="*" }, false);
                if (!cursor.MoveNext()) return (info: (PredioInfo)null, capa: capa);
                using var feature = cursor.Current as Feature;
                if (feature == null) return (info: (PredioInfo)null, capa: capa);
                var info = Extraer(feature);
                vista.Map.ClearSelection();
                using var selection = capa.Select(new QueryFilter { ObjectIDs=new List<long>{oid} }, SelectionCombinationMethod.New);
                return (info: info, capa: capa);
            });
            if (encontrado.info != null && encontrado.capa != null)
                await vista.ZoomToSelectedAsync(TimeSpan.FromSeconds(1));
            return encontrado.info;
        }
    }
}
