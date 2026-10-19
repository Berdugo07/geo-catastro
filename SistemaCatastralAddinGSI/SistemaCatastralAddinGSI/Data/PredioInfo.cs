using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaCatastralAddinGSI.Data
{
    public class PredioInfo
    {
        public long ObjectID { get; set; }
        public string PredioUid { get; set; } = "";
        public string Manzano { get; set; } = "";
        public string Lote { get; set; } = "";
        public double Superficie { get; set; }
        public string OTB { get; set; } = "";
        public string Funcion { get; set; } = "";

        public string CodigoInterno => $"OID_{ObjectID}";
    }
}