# Iluminación de referencia — Nivel-1_Rhythm

La escena guarda la iluminación bajo `Reference Lighting`. Está preparada para el perfil PC de URP existente, sin cambiar los assets globales de renderizado.

## Ajustes en Unity

- `Overhead violet key`: foco superior, intensidad 400, rango 32, apertura 115°, sombras suaves.
- `Audience soft fill`: relleno frontal frío, intensidad 150, sin sombras.
- `Left/Right column uplight`: intensidad 16 cada uno, sin sombras.
- `Muted red floor bounce`: intensidad 3, sin sombras.
- Volume global: `Assets/Content/ReferenceLighting/TheaterReference.asset`. ACES, exposición +0.15, contraste +8, saturación −22, bloom 0.2, viñeta 0.27.
- Ambiente plano azul grisáceo y niebla exponencial cuadrática de densidad 0.022.
- `CeilingHaze.mat` y `SoftShaft.mat`: color y opacidad de las dos superficies transparentes. El halo y el haz son una aproximación artística, sin dispersión volumétrica ni sombras volumétricas. Su forma depende del encuadre.
- `TheaterReferenceEye.mat`: acabado frío, borde inferior rojizo y recepción de niebla para los ojos flotantes. El material original conserva su apariencia por defecto.

Las 260 luces pequeñas se conservan con intensidad 0.025, tono frío y sin sombras. Se desactiva el antiguo foco `Area Light` y se reduce la linterna a 8. No se cambian posiciones, animaciones, rutas ni colisiones de los personajes.

## Herramientas

`Tools > Mentality > Lighting > Apply Reference Atmosphere` reconstruye el grupo y restablece los valores anteriores. Úsalo con la escena guardada y fuera de Play Mode; sobrescribe los ajustes manuales de este ambiente.

`Tools > Mentality > Lighting > Verify In Play Mode` inicia una comprobación del juego, materiales, volumen, ojos y contribución visible de la bruma. Sale de Play al terminar y escribe el resultado en `Logs/ReferenceLighting/PlayChecks.txt`.

Las capturas y el respaldo inicial están en `Logs/ReferenceLighting/`, carpeta local excluida de Git. `Gameplay.png` muestra la cámara real y el HUD; `Play-Front.png` usa un encuadre frontal fijo para revisar el ambiente. La captura inicial `Before.png` se tomó en un destino HDR lineal y no sirve para una comparación directa de exposición con las capturas finales sRGB.

La medición de fotogramas corresponde al editor; no sustituye una prueba de rendimiento de la compilación final o del perfil Mobile.
