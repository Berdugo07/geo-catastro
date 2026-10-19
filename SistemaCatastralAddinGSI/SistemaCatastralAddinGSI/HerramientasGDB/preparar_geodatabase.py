# -*- coding: utf-8 -*-
"""
FASE 1 / BETA. Ejecutar DENTRO de ArcGIS Pro (Vista -> Python).
Primero crear copia de seguridad del proyecto y de su file geodatabase.
La capa de polígonos activa debe llamarse exactamente PREDIOS y proceder de una .gdb.
Es repetible: crea únicamente los campos que faltan.
No modifica ni publica ningún archivo FTP ni los JSON locales anteriores.
"""
import arcpy
import os

TABLA = "CAT_REGISTROS"


def avisar(msg):
    arcpy.AddMessage(msg)
    print(msg)


def agregar_campo_si_falta(dataset, nombre, tipo, longitud=None, alias=None):
    existentes = {f.name.upper() for f in arcpy.ListFields(dataset)}
    if nombre.upper() not in existentes:
        parametros = {"field_alias": alias or nombre}
        if longitud is not None:
            parametros["field_length"] = longitud
        arcpy.management.AddField(dataset, nombre, tipo, **parametros)
        avisar("+ Campo: " + nombre)
    else:
        avisar("= Ya existe: " + nombre)


def ejecutar():
    aprx = arcpy.mp.ArcGISProject("CURRENT")
    mapa = aprx.activeMap
    if mapa is None:
        raise RuntimeError("Abre primero un mapa con la capa PREDIOS.")
    capas = [x for x in mapa.listLayers() if x.name.upper() == "PREDIOS" and x.isFeatureLayer]
    if len(capas) != 1:
        raise RuntimeError("Debe haber exactamente una capa de entidades llamada PREDIOS.")
    capa = capas[0]
    fuente = arcpy.Describe(capa).catalogPath
    if not arcpy.Exists(fuente):
        raise RuntimeError("No existe la fuente de datos: " + fuente)
    # No cambiar a la geodatabase predeterminada por error: se usa LA MISMA gdb que PREDIOS.
    ruta_norm = os.path.normpath(fuente)
    pos = ruta_norm.lower().find(".gdb" + os.sep)
    if pos == -1:
        raise RuntimeError("PREDIOS debe estar almacenada en una file geodatabase (*.gdb), no en shapefile ni servicio web.")
    gdb = ruta_norm[:pos + len(".gdb")]
    if not os.path.isdir(gdb):
        raise RuntimeError("No se encontró la geodatabase origen.")
    if arcpy.Describe(capa).shapeType != "Polygon":
        raise RuntimeError("PREDIOS debe ser una capa de polígonos.")
    avisar("Geodatabase destino: " + gdb)
    avisar("Capa: " + fuente)
    if "GLOBALID" not in {f.name.upper() for f in arcpy.ListFields(fuente)}:
        arcpy.management.AddGlobalIDs(fuente)
        avisar("+ GlobalID permanente generado para los predios")
    else:
        avisar("= PREDIOS ya dispone de GlobalID")

    agregar_campo_si_falta(fuente, "CODIGO_CATASTRAL", "TEXT", 80, "Código catastral")
    agregar_campo_si_falta(fuente, "ESTADO_CATASTRAL", "TEXT", 20, "Estado del registro")

    tabla = os.path.join(gdb, TABLA)
    if not arcpy.Exists(tabla):
        arcpy.management.CreateTable(gdb, TABLA)
        avisar("+ Tabla: " + tabla)
    agregar_campo_si_falta(tabla, "PREDIO_UID", "TEXT", 38, "GlobalID del predio")
    agregar_campo_si_falta(tabla, "CODIGO_CATASTRAL", "TEXT", 80, "Código catastral")
    agregar_campo_si_falta(tabla, "ESTADO_CATASTRAL", "TEXT", 20, "Estado del registro")
    agregar_campo_si_falta(tabla, "FICHA_JSON", "TEXT", 16000, "Formulario catastral serializado")
    agregar_campo_si_falta(tabla, "FECHA_MOD", "DATE", alias="Modificado el")

    # En las FileGDB el parámetro de unicidad de AddIndex se IGNORA.
    # Los controles de repetición de código y GlobalID los hace el add-in.
    indices = {idx.name.upper() for idx in arcpy.ListIndexes(tabla)}
    for campo, nombre in (("PREDIO_UID", "IX_CAT_UID"), ("CODIGO_CATASTRAL", "IX_CAT_COD")):
        if nombre.upper() not in indices:
            arcpy.management.AddIndex(tabla, campo, nombre, "NON_UNIQUE")
            avisar("+ Índice de búsqueda: " + nombre)

    avisar("LISTO. Ahora recompila y abre el complemento GeoCatastro Fase 1.")
    avisar("IMPORTANTE: esta preparación no traslada los archivos JSON anteriores.")
    return gdb


if __name__ == "__main__":
    ejecutar()
