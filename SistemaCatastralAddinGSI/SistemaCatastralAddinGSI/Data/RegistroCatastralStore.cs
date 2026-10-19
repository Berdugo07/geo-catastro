using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SistemaCatastralAddinGSI.Data
{
    /// <summary>
    /// BETA: fichas persistidas DENTRO de la file geodatabase de la capa PREDIOS.
    /// Requiere ejecutar previamente HerramientasGDB/preparar_geodatabase.py desde ArcGIS Pro.
    /// No escribe en JSON suelto, servidores web, FTP ni MySQL.
    /// </summary>
    public static class RegistroCatastralStore
    {
        private const string TablaRegistros = "CAT_REGISTROS";
        private const int LongitudFichaJson = 16000;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        public static string NormalizarUid(string valor) =>
            Guid.TryParse(valor, out var guid) ? guid.ToString("B").ToUpperInvariant() : "";

        private static FeatureLayer ObtenerCapaPredios()
        {
            var vista = MapView.Active ?? throw new InvalidOperationException("Abre una vista de mapa de ArcGIS Pro.");
            var capas = vista.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
                .Where(l => l.Name.Equals("PREDIOS", StringComparison.OrdinalIgnoreCase)).ToList();
            if (capas.Count != 1)
                throw new InvalidOperationException("Debe existir exactamente una capa denominada PREDIOS en el mapa activo.");
            return capas[0];
        }

        private static (FeatureClass fc, Datastore datastore, Table tabla) Abrir()
        {
            // El llamador está dentro de QueuedTask.Run y dispone TODOS los objetos retornados.
            var fc = ObtenerCapaPredios().GetFeatureClass();
            Datastore datastore = null;
            try
            {
                datastore = fc.GetDatastore();
                if (datastore is not Geodatabase gdb)
                    throw new InvalidOperationException("La capa PREDIOS debe proceder de una geodatabase.");
                var tabla = gdb.OpenDataset<Table>(TablaRegistros);
                return (fc, datastore, tabla);
            }
            catch
            {
                datastore?.Dispose();
                fc.Dispose();
                throw new InvalidOperationException(
                    "No se pudo abrir CAT_REGISTROS dentro de la geodatabase de PREDIOS. " +
                    "Ejecuta primero HerramientasGDB/preparar_geodatabase.py sobre una COPIA de tu proyecto.");
            }
        }

        private static string Texto(Row row, string campo) => row[campo]?.ToString() ?? "";

        private static RegistroCatastralData Leer(Row row)
        {
            var json = Texto(row, "FICHA_JSON");
            if (string.IsNullOrWhiteSpace(json)) return null;
            var ficha = JsonSerializer.Deserialize<RegistroCatastralData>(json, JsonOptions);
            if (ficha != null)
            {
                ficha.PredioUid = NormalizarUid(Texto(row, "PREDIO_UID"));
                ficha.CodigoCatastral = Texto(row, "CODIGO_CATASTRAL");
            }
            return ficha;
        }

        private static string CodigoValido(string valor)
        {
            var codigo = (valor ?? "").Trim().ToUpperInvariant();
            if (codigo.Length == 0 || codigo.Length > 80 ||
                codigo.Any(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_'))
                throw new InvalidOperationException("Código catastral inválido (1–80 caracteres: letras, números, guiones y _). ");
            return codigo;
        }

        public static Task<RegistroCatastralData> CargarPorUidAsync(string uid) => QueuedTask.Run(() =>
        {
            uid = NormalizarUid(uid);
            if (uid.Length == 0) return null;
            var (fc, datastore, tabla) = Abrir();
            using (fc) using (datastore) using (tabla)
            using (var cursor = tabla.Search(new QueryFilter { SubFields = "*" }, false))
                while (cursor.MoveNext())
                {
                    using var row = cursor.Current;
                    if (NormalizarUid(Texto(row, "PREDIO_UID")) == uid) return Leer(row);
                }
            return null;
        });

        public static Task<List<RegistroCatastralData>> BuscarAsync(string texto) => QueuedTask.Run(() =>
        {
            var criterio = (texto ?? "").Trim();
            var lista = new List<RegistroCatastralData>();
            var (fc, datastore, tabla) = Abrir();
            using (fc) using (datastore) using (tabla)
            using (var cursor = tabla.Search(new QueryFilter { SubFields = "*" }, false))
                while (cursor.MoveNext())
                {
                    using var row = cursor.Current;
                    RegistroCatastralData ficha;
                    try { ficha = Leer(row); }
                    catch (JsonException) { continue; }
                    if (ficha == null) continue;
                    var campos = new[] { ficha.CodigoCatastral, ficha.Nombres, ficha.ApellidoPaterno,
                        ficha.ApellidoMaterno, ficha.NumeroDocumento, ficha.Manzana, ficha.Predio,
                        ficha.Zona, ficha.Distrito, ficha.Direccion, ficha.Matricula };
                    if (criterio.Length == 0 || campos.Any(c =>
                        (c ?? "").Contains(criterio, StringComparison.OrdinalIgnoreCase))) lista.Add(ficha);
                }
            return lista.OrderBy(f => f.CodigoCatastral).ToList();
        });

        public static Task GuardarAsync(RegistroCatastralData ficha) => QueuedTask.Run(() =>
        {
            if (ficha == null) throw new ArgumentNullException(nameof(ficha));
            var uid = NormalizarUid(ficha.PredioUid);
            if (uid.Length == 0 || ficha.ObjectID <= 0)
                throw new InvalidOperationException("El predio carece de GlobalID. Ejecuta la preparación de geodatabase.");
            var codigo = CodigoValido(ficha.CodigoCatastral);
            var capa = ObtenerCapaPredios();
            var (fc, datastore, tabla) = Abrir();
            using (fc) using (datastore) using (tabla)
            {
                // No permite grabar la ficha de un OID cuyo GlobalID ya cambió.
                using (var cursor = fc.Search(new QueryFilter
                {
                    ObjectIDs = new List<long> { ficha.ObjectID },
                    SubFields = "*"
                }, false))
                {
                    if (!cursor.MoveNext()) throw new InvalidOperationException("El predio seleccionado ya no existe.");
                    using var feature = cursor.Current;
                    if (NormalizarUid(Texto(feature, "GlobalID")) != uid)
                        throw new InvalidOperationException("El identificador del predio cambió. Vuelve a seleccionarlo.");
                }

                long? oidFicha = null;
                using (var cursor = tabla.Search(new QueryFilter { SubFields = "*" }, false))
                    while (cursor.MoveNext())
                    {
                        using var row = cursor.Current;
                        var uidGuardado = NormalizarUid(Texto(row, "PREDIO_UID"));
                        var codigoGuardado = Texto(row, "CODIGO_CATASTRAL");
                        if (uidGuardado != uid && codigo.Equals(codigoGuardado, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("Ese código catastral ya está asignado a otro predio.");
                        if (uidGuardado == uid)
                        {
                            if (oidFicha.HasValue)
                                throw new InvalidOperationException("La geodatabase contiene fichas duplicadas para este GlobalID.");
                            oidFicha = row.GetObjectID();
                        }
                    }

                ficha.PredioUid = uid;
                ficha.CodigoCatastral = codigo;
                ficha.FechaModificacion = DateTime.Now;
                var json = JsonSerializer.Serialize(ficha, JsonOptions);
                if (json.Length > LongitudFichaJson)
                    throw new InvalidOperationException("La ficha supera 16.000 caracteres. Revise adjuntos u observaciones.");
                var ahora = DateTime.Now;
                var atributosFicha = new Dictionary<string, object>
                {
                    ["PREDIO_UID"] = uid,
                    ["CODIGO_CATASTRAL"] = codigo,
                    ["ESTADO_CATASTRAL"] = "REGISTRADO",
                    ["FICHA_JSON"] = json,
                    ["FECHA_MOD"] = ahora
                };
                var operacion = new EditOperation { Name = "Guardar ficha catastral en geodatabase" };
                if (oidFicha.HasValue) operacion.Modify(tabla, oidFicha.Value, atributosFicha);
                else operacion.Create(tabla, atributosFicha);
                operacion.Modify(capa, ficha.ObjectID, new Dictionary<string, object>
                {
                    ["CODIGO_CATASTRAL"] = codigo,
                    ["ESTADO_CATASTRAL"] = "REGISTRADO"
                });
                if (!operacion.Execute())
                    throw new InvalidOperationException("No se pudieron guardar los cambios en la geodatabase: " + operacion.ErrorMessage);
            }
        });
    }
}
