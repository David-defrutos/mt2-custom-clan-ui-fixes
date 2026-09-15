# Custom Clan UI Fixes (Monster Train 2)

Arreglos de interfaz para jugar con **muchos clanes modeados** instalados. No anade
contenido: ni cartas, ni unidades, ni clanes. Solo parchea pantallas del juego base con
Harmony, asi que **no depende de Trainworks Reloaded ni de Conductor**.

## Que arregla

### La pagina de mejoras de campeon del logbook

La pantalla **Light Forge Upgrades** del logbook reparte los clanes en dos columnas fijas
(clanes normales y tripulacion) y **no pagina**: con una decena de mods instalados la
columna se sale de la hoja por abajo y los ultimos clanes no hay forma de verlos ni de
seleccionarlos.

Este mod mide la hoja, decide cuantas columnas caben y recoloca los rombos en una rejilla
que aprovecha el ancho, encogiendolos solo lo justo. Con 18 clanes salen 3 columnas de 6 a
un 91% del tamano original; el diseno aguanta hasta unos 45 clanes antes de tener que
paginar.

Lo que **no** hace: mover botones de sitio en la jerarquia. El juego reconstruye esos
botones cada vez que se abre la pantalla, asi que cambiarlos de padre deja rombos
duplicados y sin funcionar. Aqui cada boton se queda donde el juego lo puso y solo se
cambia donde se dibuja.

## Ajustes

`BepInEx\config\mt2_custom_clan_ui_fixes.Plugin.cfg`, seccion `[LogbookFit]`:

| clave | por defecto | que hace |
|---|---|---|
| `Enabled` | `true` | a `false` y la pantalla queda como la deja el juego |
| `MinScale` | `0.45` | hasta donde se deja encoger un rombo |
| `MaxAutoColumns` | `3` | columnas como mucho. **2 = no se toca la rejilla del juego**, solo se escala |
| `ColumnSpacing` | `16` | separacion entre columnas al pasar de dos (la del juego son 96) |
| `HeightBudget` | `0` | alto util de la hoja en px; 0 = detectarlo (medido: 1000) |
| `WidthBudget` | `0` | ancho util en px; 0 = detectarlo (medido: 400). A `440` los rombos quedan a tamano original, saliendose un poco del area nominal |
| `Verbose` | `true` | traza en `LogOutput.log`, con las medidas y el factor aplicado |

## Relacion con CustomClanHelper

[Custom-Clan-Helper](https://github.com/Monster-Train-2-Modding-Group/Custom-Clan-Helper)
arregla la pagina del **checklist** del logbook, y tiene esta otra en su TODO
(*"Champion Upgrade Page in logbook is wonky"*). Los dos mods son compatibles y se ocupan
de pantallas distintas.

## Compilar

Ver `COMO-COMPILAR.md`. El DLL lo compila GitHub Actions con cada push que toque `src/`.

## Licencia

MIT.
