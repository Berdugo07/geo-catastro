using System;
using System.Collections.Generic;

namespace SistemaCatastralAddinGSI.Data
{
    public class RegistroCatastralData
    {
        public long ObjectID { get; set; }
        public string PredioUid { get; set; } = "";

        public string CodigoCatastral { get; set; } = "";

        // Datos del propietario
        public string ApellidoPaterno { get; set; } = "";
        public string ApellidoMaterno { get; set; } = "";
        public string Nombres { get; set; } = "";
        public string TipoDocumento { get; set; } = "CI";
        public string NumeroDocumento { get; set; } = "";
        public string Porcentaje { get; set; } = "";

        // Se mantiene el propietario principal para compatibilidad con archivos existentes.
        public List<PropietarioAdicional> PropietariosAdicionales { get; set; } = new List<PropietarioAdicional>();

        // Colindancias
        public string ColindanteNorte { get; set; } = "";
        public string ColindanteSur { get; set; } = "";
        public string ColindanteEste { get; set; } = "";
        public string ColindanteOeste { get; set; } = "";

        // Derecho propietario
        public string Notario { get; set; } = "";
        public string Registro { get; set; } = "";
        public DateTime? FechaEscritura { get; set; }
        public string Matricula { get; set; } = "";
        public string SuperficieTitulo { get; set; } = "";

        // Código catastral (desglose)
        public string Distrito { get; set; } = "";
        public string Zona { get; set; } = "";
        public string Manzana { get; set; } = "";
        public string Predio { get; set; } = "";
        public string Calle { get; set; } = "";
        public string Direccion { get; set; } = "";

        // Tipología
        public string DestinoUso { get; set; } = "VIVIENDA";
        public string Tipologia { get; set; } = "A";
        public string EstadoFisico { get; set; } = "BUENO";
        public string Planta { get; set; } = "";
        public string SuperficieConstruida { get; set; } = "";
        public string AnioConstruccion { get; set; } = "";
        public string SuperficieTotal { get; set; } = "";

        // Servicios
        public bool AguaCorriente { get; set; }
        public bool EnergiaElectrica { get; set; }
        public bool AlumbradoPublico { get; set; }
        public bool Telefono { get; set; }
        public string MaterialVia { get; set; } = "ASFALTO";
        public string Pendiente { get; set; } = "PLANA";
        public string Forma { get; set; } = "REGULAR";
        public string Frente { get; set; } = "";
        public string Fondo { get; set; } = "";

        // Superficies del terreno
        public string SupMensura { get; set; } = "";
        public string SupEscritura { get; set; } = "";
        public string AreaExcedente { get; set; } = "";
        public string AreaAfectada { get; set; } = "";
        public string SuperficieUtil { get; set; } = "";

        // Adjuntos
        public List<string> Fotografias { get; set; } = new List<string>();
        public List<string> Documentos { get; set; } = new List<string>();

        // Observaciones
        public string Observaciones { get; set; } = "";

        // Metadata
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public DateTime FechaModificacion { get; set; } = DateTime.Now;
    }
}