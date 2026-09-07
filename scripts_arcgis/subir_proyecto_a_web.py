import arcpy
import ftplib
import os
import datetime
import json
from arcpy import env
from arcpy import mp

# Datos FTP
SERVIDOR_FTP = "ftp.geospatialindustry.net"
USUARIO_FTP = "catastro@geospatialindustry.net"
CLAVE_FTP = "Catastro12."

CARPETA_TRABAJO = os.environ.get('TEMP', r'C:\temp')

def log(mensaje):
    arcpy.AddMessage(f"[{datetime.datetime.now().strftime('%H:%M:%S')}] {mensaje}")

def subir_archivo_ftp(ruta_local, nombre_remoto):
    """Sube el archivo a la carpeta 'mapa' ubicada en public_html"""
    try:
        with ftplib.FTP(SERVIDOR_FTP) as ftp:
            ftp.login(user=USUARIO_FTP, passwd=CLAVE_FTP)
            
            try:
                ftp.cwd("public_html")
            except:
                ftp.cwd("/")
            
            try:
                ftp.cwd("mapa")
                log(" En la carpeta 'mapa' (dentro de public_html)")
            except:
                # Si no existe, crear la carpeta 'mapa' ahí mismo
                try:
                    ftp.mkd("mapa")
                    ftp.cwd("mapa")
                    log(" Carpeta 'mapa' creada dentro de public_html")
                except:
                    log(f" ERROR: Se intentó crear 'mapa' pero no se pudo encontrar o crear. Ruta actual: {ftp.pwd()}")
                    return False
            
            with open(ruta_local, 'rb') as file:
                ftp.storbinary(f'STOR {nombre_remoto}', file)
            log(f" Subido: {nombre_remoto}")
            return True
    except Exception as e:
        log(f" ERROR al subir {nombre_remoto}: {str(e)}")
        return False

def get_style_from_layer(layer):
    """Extrae el color de relleno y línea de la capa"""
    try:
        if layer.supports("symbology"):
            sym = layer.symbology
            if hasattr(sym, "renderer"):
                renderer = sym.renderer
                # Si es simbolo unico (un solo color)
                if renderer.type == "SIMPLE":
                    symbol = renderer.symbol
                    fill_color = "#" + symbol.color.getRGB() if hasattr(symbol, "color") else "#3388ff"
                    outline_color = "#" + symbol.outlineColor.getRGB() if hasattr(symbol, "outlineColor") else fill_color
                    outline_width = symbol.outlineWidth if hasattr(symbol, "outlineWidth") else 2
                    
                    return {
                        "fillColor": fill_color,
                        "color": outline_color,
                        "weight": outline_width,
                        "fillOpacity": 0.3
                    }
        return None
    except:
        return None

def main():
    try:
        capas_seleccionadas = arcpy.GetParameterAsText(0).split(";")
        
        if not capas_seleccionadas or capas_seleccionadas == ['']:
            arcpy.AddError(" ERROR: Debes seleccionar al menos una capa.")
            return False

        log(f" SUBIENDO {len(capas_seleccionadas)} CAPA(S) SELECCIONADA(S)")
        
        estilos_capas = {}
        
        for capa in capas_seleccionadas:
            capa = capa.strip()
            
            try:
                progetto = arcpy.mp.ArcGISProject("CURRENT")
                mapa = progetto.activeMap
                
                capa_encontrada = None
                for layer in mapa.listLayers():
                    if layer.name == capa:
                        capa_encontrada = layer
                        break
                
                if not capa_encontrada:
                    if arcpy.Exists(capa):
                        capa_encontrada = arcpy.Describe(capa).catalogPath
                    else:
                        log(f" AVISO: La capa '{capa}' não existe. Se omite.")
                        continue
                
                estilo = get_style_from_layer(capa_encontrada)
                if estilo:
                    nombre_base = os.path.basename(str(capa_encontrada)).replace(" ", "_").replace("-", "_")
                    if len(nombre_base) > 40:
                        nombre_base = nombre_base[:40]
                    estilos_capas[nombre_base] = estilo
                
                nombre_base = os.path.basename(str(capa_encontrada)).replace(" ", "_").replace("-", "_")
                if len(nombre_base) > 40:
                    nombre_base = nombre_base[:40]
                
                archivo_local = os.path.join(CARPETA_TRABAJO, f"{nombre_base}.geojson")
                archivo_remoto = f"{nombre_base}.geojson"

                log(f" Exportando: {nombre_base} a GeoJSON (WGS84)...")
                arcpy.SetProgressorLabel(f"Exportando {nombre_base}...")
                env.overwriteOutput = True
                
                try:
                    arcpy.conversion.FeaturesToJSON(
                        in_features=capa_encontrada,
                        out_json_file=archivo_local,
                        format_json="NOT_FORMATTED",
                        include_z_values="NO_Z_VALUES",
                        include_m_values="NO_M_VALUES",
                        geoJSON="GEOJSON",
                        outputToWGS84="WGS84"
                    )
                    
                    if not os.path.exists(archivo_local):
                        log(f" ERROR: No se pudo crear el archivo para {nombre_base}")
                        continue
                    
                    log(f" GeoJSON creado. Tamanho: {os.path.getsize(archivo_local) / 1024:.2f} KB")
                    
                    subir_archivo_ftp(archivo_local, archivo_remoto)
                    
                    if os.path.exists(archivo_local):
                        os.remove(archivo_local)
                        
                except Exception as e_capa:
                    log(f" AVISO: Error en la capa {nombre_base}: {str(e_capa)}. Se omite.")
                    if os.path.exists(archivo_local):
                        os.remove(archivo_local)
                    continue
                    
            except Exception as e_capa:
                log(f" AVISO: Error en la capa {capa}: {str(e_capa)}. Se omite.")
                continue
        
        if estilos_capas:
            archivo_estilos = os.path.join(CARPETA_TRABAJO, "estilos.json")
            with open(archivo_estilos, 'w') as f:
                json.dump(estilos_capas, f)
            log(" Subiendo archivo de estilos...")
            subir_archivo_ftp(archivo_estilos, "estilos.json")
            if os.path.exists(archivo_estilos):
                os.remove(archivo_estilos)
        
        log(" ¡TODAS LAS CAPAS FUERON SUBIDAS EXITOSAMENTE!")
        return True
        
    except Exception as e:
        arcpy.AddError(f" ERROR: {str(e)}")
        return False

if __name__ == "__main__":
    main()