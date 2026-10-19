using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SistemaCatastralAddinGSI.Data
{
 
    internal sealed class CapaPrediosAsignada
    {
        public CapaPrediosAsignada() { }
        public string ProyectoUri { get; set; } = "";
        public string MapaUri { get; set; } = "";
        public string CapaUri { get; set; } = "";
        public string NombreCapa { get; set; } = "";
    }

    internal static class CapaPrediosConfig
    {
        private static string Ruta(string proyectoUri, string mapaUri)
        {
            if (string.IsNullOrWhiteSpace(proyectoUri) ||
                string.IsNullOrWhiteSpace(mapaUri))
                throw new InvalidOperationException(
                    "Guarda primero el proyecto .aprx y abre un mapa valido.");


            string clave = proyectoUri.Trim().ToUpperInvariant() + "\n" +
                           mapaUri.Trim().ToUpperInvariant();
            string hash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(clave)));

            string directorio = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "GSI", "Catastro", "CapasPrincipales");

            return Path.Combine(directorio, hash + ".json");
        }

        public static void Guardar(
            string proyectoUri, string mapaUri,
            string capaUri, string nombreCapa)
        {
            if (string.IsNullOrWhiteSpace(capaUri))
                throw new ArgumentException("La capa no tiene una URI valida.");

            string ruta = Ruta(proyectoUri, mapaUri);
            Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
            var datos = new CapaPrediosAsignada
            {
                ProyectoUri = proyectoUri,
                MapaUri = mapaUri,
                CapaUri = capaUri,
                NombreCapa = nombreCapa
            };

            string temporal = ruta + ".tmp";
            File.WriteAllText(temporal,
                JsonSerializer.Serialize(datos, new JsonSerializerOptions
                {
                    WriteIndented = true
                }), Encoding.UTF8);
            File.Move(temporal, ruta, true);
        }

        public static CapaPrediosAsignada Obtener(
            string proyectoUri, string mapaUri)
        {
            string ruta = Ruta(proyectoUri, mapaUri);
            if (!File.Exists(ruta)) return null;

            var datos = JsonSerializer.Deserialize<CapaPrediosAsignada>(
                File.ReadAllText(ruta, Encoding.UTF8));

            if (datos == null ||
                !string.Equals(datos.ProyectoUri, proyectoUri,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(datos.MapaUri, mapaUri,
                    StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(datos.CapaUri))
                return null;

            return datos;
        }
    }
}
