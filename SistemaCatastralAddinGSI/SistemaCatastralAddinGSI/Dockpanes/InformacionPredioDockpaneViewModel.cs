using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using SistemaCatastralAddinGSI.Data;
using System;

namespace SistemaCatastralAddinGSI.Dockpanes
{
    internal class InformacionPredioDockpaneViewModel : DockPane
    {
        private const string ID = "SistemaCatastralAddinGSI_InformacionPredioDockpane";
        protected InformacionPredioDockpaneViewModel() { }
        public static InformacionPredioDockpaneViewModel Show() =>
            (InformacionPredioDockpaneViewModel)FrameworkApplication.DockPaneManager.Find(ID);
        public PredioInfo PredioActual { get; private set; }
        public event Action Actualizado;
        public void CargarPredio(PredioInfo info) { PredioActual=info; Actualizado?.Invoke(); }
    }
}
