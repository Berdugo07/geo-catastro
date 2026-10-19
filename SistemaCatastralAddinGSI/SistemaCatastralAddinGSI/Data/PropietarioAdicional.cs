namespace SistemaCatastralAddinGSI.Data
{
    public class PropietarioAdicional
    {
        public string NombreCompleto { get; set; } = "";
        public string Documento { get; set; } = "";
        public string Porcentaje { get; set; } = "";
        public string Resumen => $"{NombreCompleto} | Doc: {Documento} | {Porcentaje}%";
    }
}
