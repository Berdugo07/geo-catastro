<?php
$dir = 'datos/mapa/';
$archivos = scandir($dir);

$geojson = array();

foreach ($archivos as $archivo) {
    if (pathinfo($archivo, PATHINFO_EXTENSION) === 'geojson') {
        $geojson[] = $archivo;
    }
}

header('Content-Type: application/json');
echo json_encode($geojson);
?>