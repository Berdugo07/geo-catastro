using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using SistemaCatastralAddinGSI.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaCatastralAddinGSI.Tools
{
    // Los overlays son visuales y temporales: para producción, simbolizar atributos
    // ESTADO_CATASTRAL/CODIGO_CATASTRAL en la capa publicada.
    internal static class CatastroVisualManager
    {
        private static readonly Dictionary<long, IDisposable> Overlays = new();
        private static MapView UltimaVista;
        public static async Task PintarRegistradoAsync(long oid)
        {
            var vista = MapView.Active;
            if (vista == null) return;
            if (UltimaVista != vista) { Limpiar(); UltimaVista=vista; }
            await QueuedTask.Run(() => {
                var layer=vista.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
                    .FirstOrDefault(l => l.Name.Equals("PREDIOS", StringComparison.OrdinalIgnoreCase))
                    ?? vista.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
                    .FirstOrDefault(l => l.Name.Contains("PREDIO", StringComparison.OrdinalIgnoreCase));
                if (layer == null) return;
                using var fc=layer.GetFeatureClass();
                using var cursor=fc.Search(new QueryFilter { ObjectIDs=new List<long>{oid},SubFields="*" },false);
                if (!cursor.MoveNext()) return;
                using var feature=cursor.Current as Feature;
                var pol=feature?.GetShape() as Polygon;
                if (pol==null) return;
                if (Overlays.TryGetValue(oid,out var previo)) { previo.Dispose(); Overlays.Remove(oid); }
                var sym=SymbolFactory.Instance.ConstructPolygonSymbol(
                    CIMColor.CreateRGBColor(57,172,115,105), SimpleFillStyle.Solid,
                    SymbolFactory.Instance.ConstructStroke(CIMColor.CreateRGBColor(29,117,76,225),1.2,SimpleLineStyle.Solid));
                Overlays[oid]=vista.AddOverlay(pol,sym.MakeSymbolReference(),-1,0);
            });
        }
        public static async Task RefrescarAsync()
        {
            Limpiar();
            CadastralLabelManager.LimpiarTodas();
            foreach(var f in await RegistroCatastralStore.BuscarAsync(""))
            {
                if (string.IsNullOrWhiteSpace(f.CodigoCatastral)) continue;
                var oid = await CadastralMapService.ObtenerOidPorUidAsync(f.PredioUid);
                if (oid <= 0) continue;
                await PintarRegistradoAsync(oid);
                await CadastralLabelManager.MostrarCodigoAsync(oid,f.CodigoCatastral);
            }
        }
        public static void Limpiar()
        {
            foreach (var overlay in Overlays.Values) overlay.Dispose();
            Overlays.Clear();
        }
    }
}
