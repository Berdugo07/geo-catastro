using SistemaCatastralAddinGSI.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SistemaCatastralAddinGSI.Reports
{
    // Generador de PDF textual, sin paquetes externos.
    // Formato inicial: la plantilla oficial municipal requiere una fase de maquetación.
    internal static class FormularioPdfGenerator
    {
        public static void Crear(string destino, RegistroCatastralData ficha)
        {
            if (ficha == null) throw new ArgumentNullException(nameof(ficha));
            var r = new List<string>
            {
                "GEOCATASTRO GSI - FORMULARIO UNICO DE REGISTRO CATASTRAL",
                "Codigo catastral: " + ficha.CodigoCatastral,
                "", "1. IDENTIFICACION DEL PREDIO",
                "Distrito: " + ficha.Distrito + "    Zona: " + ficha.Zona,
                "Manzana: " + ficha.Manzana + "    Predio: " + ficha.Predio,
                "Direccion: " + ficha.Direccion + "    Calle: " + ficha.Calle,
                "Superficie total (m2): " + ficha.SuperficieTotal,
                "", "2. PROPIEDAD",
                "Titular: " + (ficha.Nombres + " " + ficha.ApellidoPaterno + " " + ficha.ApellidoMaterno).Trim(),
                "Documento: " + ficha.TipoDocumento + " " + ficha.NumeroDocumento,
                "Porcentaje: " + ficha.Porcentaje,
                "Notaria: " + ficha.Notario + "    Registro: " + ficha.Registro,
                "Matricula: " + ficha.Matricula,
                "Fecha de escritura: " + (ficha.FechaEscritura?.ToString("dd/MM/yyyy") ?? ""),
                "Superficie titulo: " + ficha.SuperficieTitulo,
                "", "3. COPROPIETARIOS"
            };
            var copropietarios = ficha.PropietariosAdicionales ?? new List<PropietarioAdicional>();
            if (copropietarios.Count == 0) r.Add("Sin copropietarios adicionales registrados.");
            foreach (var p in copropietarios)
                r.Add(p.NombreCompleto + " | " + p.Documento + " | " + p.Porcentaje + "%");
            r.AddRange(new [] {
                "", "4. CARACTERISTICAS Y SERVICIOS",
                "Uso: " + ficha.DestinoUso + "    Tipologia: " + ficha.Tipologia + "    Estado: " + ficha.EstadoFisico,
                "Planta: " + ficha.Planta + "    Superficie construida: " + ficha.SuperficieConstruida,
                "Anio construccion: " + ficha.AnioConstruccion,
                "Agua: " + SiNo(ficha.AguaCorriente) + "  Electricidad: " + SiNo(ficha.EnergiaElectrica),
                "Alumbrado: " + SiNo(ficha.AlumbradoPublico) + "  Telefono: " + SiNo(ficha.Telefono),
                "Material via: " + ficha.MaterialVia + "  Pendiente: " + ficha.Pendiente + "  Forma: " + ficha.Forma,
                "Frente: " + ficha.Frente + "  Fondo: " + ficha.Fondo,
                "", "5. AREAS",
                "Mensura: " + ficha.SupMensura + "  Escritura: " + ficha.SupEscritura,
                "Excedente: " + ficha.AreaExcedente + "  Afectada: " + ficha.AreaAfectada,
                "Superficie util: " + ficha.SuperficieUtil,
                "", "6. COLINDANTES",
                "Norte: " + ficha.ColindanteNorte,
                "Sur: " + ficha.ColindanteSur,
                "Este: " + ficha.ColindanteEste,
                "Oeste: " + ficha.ColindanteOeste,
                "", "7. OBSERVACIONES",
                ficha.Observaciones,
                "", "Fecha de registro: " + ficha.FechaRegistro.ToString("dd/MM/yyyy"),
                "Ultima actualizacion: " + ficha.FechaModificacion.ToString("dd/MM/yyyy HH:mm"),
                "Generado desde GeoCatastro GSI. Documento referencial no certificado."
            });
            var lineas = r.SelectMany(Envolver).ToList();
            var paginas = new List<List<string>>();
            for (int i=0; i<lineas.Count; i+=48) paginas.Add(lineas.Skip(i).Take(48).ToList());
            if (paginas.Count == 0) paginas.Add(new List<string>());
            using var outStream = File.Create(destino);
            var offsets = new List<long>{0};
            void Write(string s)
            {
                var b = Encoding.Latin1.GetBytes(s);
                outStream.Write(b,0,b.Length);
            }
            void Object(int id, string content)
            {
                offsets.Add(outStream.Position);
                Write(id.ToString(CultureInfo.InvariantCulture)+" 0 obj\n"+content+"\nendobj\n");
            }
            Write("%PDF-1.4\n%GSI\n");
            Object(1,"<< /Type /Catalog /Pages 2 0 R >>");
            var kids=string.Join(" ",Enumerable.Range(0,paginas.Count).Select(i => (4+i*2)+" 0 R"));
            Object(2,"<< /Type /Pages /Kids ["+kids+"] /Count "+paginas.Count+" >>");
            Object(3,"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
            for (int i=0;i<paginas.Count;i++)
            {
                var pageId=4+i*2;var contentId=pageId+1;
                Object(pageId,"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents "+contentId+" 0 R >>");
                var contenido="BT\n/F1 10 Tf\n48 800 Td\n14 TL\n" +
                    string.Join("\n", paginas[i].Select(t => "("+Escape(t)+") Tj T*")) +
                    "\nET\n";
                var len=Encoding.Latin1.GetByteCount(contenido);
                Object(contentId,"<< /Length "+len+" >>\nstream\n"+contenido+"endstream");
            }
            var xref=outStream.Position;
            Write("xref\n0 "+offsets.Count+"\n0000000000 65535 f \n");
            foreach(var o in offsets.Skip(1)) Write(o.ToString("D10",CultureInfo.InvariantCulture)+" 00000 n \n");
            Write("trailer\n<< /Size "+offsets.Count+" /Root 1 0 R >>\nstartxref\n"+xref+"\n%%EOF");
        }
        private static string SiNo(bool v) => v ? "SI" : "NO";
        private static IEnumerable<string> Envolver(string linea)
        {
            linea=(linea ?? "").Replace('\r',' ').Replace('\n',' ');
            if(linea.Length==0) {yield return "";yield break;}
            while(linea.Length>98) { yield return linea.Substring(0,98);linea=linea.Substring(98); }
            yield return linea;
        }
        private static string Escape(string valor)
        {
            var ascii=valor.Replace('’','\'').Replace('“','"').Replace('”','"').Replace('–','-').Replace('²','2');
            return ascii.Replace("\\","\\\\").Replace("(","\\(").Replace(")","\\)");
        }
    }
}
