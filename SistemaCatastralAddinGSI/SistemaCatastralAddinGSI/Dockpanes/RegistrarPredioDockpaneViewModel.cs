using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using SistemaCatastralAddinGSI.Data;
using System;

namespace SistemaCatastralAddinGSI.Dockpanes
{
    internal class RegistrarPredioDockpaneViewModel : DockPane
    {
        private const string _dockPaneID =
            "SistemaCatastralAddinGSI_RegistrarPredioDockpane";

        protected RegistrarPredioDockpaneViewModel()
        {
        }

        public static RegistrarPredioDockpaneViewModel Show()
        {
            return (RegistrarPredioDockpaneViewModel)
                FrameworkApplication.DockPaneManager.Find(_dockPaneID);
        }

        private string _heading =
            "Formulario Único de Registro Catastral";

        public string Heading
        {
            get => _heading;
            set => SetProperty(ref _heading, value);
        }

        public static bool HayPredioSeleccionado { get; private set; }

        public event Action<PredioInfo> PredioSeleccionado;

        public PredioInfo PredioPendiente { get; private set; }
        public PredioInfo PredioActual { get; private set; }
        public string SeccionPendiente { get; private set; } = "Predio";
        public event Action<string> SolicitarSeccion;
        public void AbrirSeccion(string seccion)
        {
            SeccionPendiente = seccion;
            SolicitarSeccion?.Invoke(seccion);
        }

        public void CargarPredio(PredioInfo info)
        {
            if (info == null)
                return;

            HayPredioSeleccionado = true;

            PredioPendiente = info;
            PredioActual = info;
            PredioSeleccionado?.Invoke(info);
        }

        public PredioInfo ObtenerPredioPendiente()
        {
            var info = PredioPendiente;

            PredioPendiente = null;

            return info;
        }
    }
}