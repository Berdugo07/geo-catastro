using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace SistemaCatastralAddinGSI.Dockpanes
{
    internal class BuscarPredioDockpaneViewModel : DockPane
    {
        private const string ID="SistemaCatastralAddinGSI_BuscarPredioDockpane";
        protected BuscarPredioDockpaneViewModel() { }
        public static BuscarPredioDockpaneViewModel Show() =>
            (BuscarPredioDockpaneViewModel)FrameworkApplication.DockPaneManager.Find(ID);
    }
}
