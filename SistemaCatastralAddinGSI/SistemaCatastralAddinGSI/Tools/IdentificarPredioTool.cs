using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using SistemaCatastralAddinGSI.Dockpanes;
using System.Threading.Tasks;

namespace SistemaCatastralAddinGSI.Tools
{
    internal class IdentificarPredioTool : MapTool
    {
        public IdentificarPredioTool() { IsSketchTool = true; SketchType = SketchGeometryType.Point; SketchOutputMode = SketchOutputMode.Map; UseSnapping = false; }
        protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            var predio = await CadastralMapService.SeleccionarPuntoAsync(geometry as MapPoint);
            if (predio == null) return false;
            var panel = InformacionPredioDockpaneViewModel.Show();
            if (panel == null) return false;
            panel.CargarPredio(predio);
            panel.Activate();
            return true;
        }
    }
}
