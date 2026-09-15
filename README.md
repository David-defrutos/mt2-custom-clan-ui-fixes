# Custom Clan UI Fixes (Monster Train 2)

UI fixes for playing with **many modded clans** installed. It adds no content — no cards, no
units, no clans — it only patches base-game screens with Harmony, so it **does not depend on
Trainworks Reloaded or Conductor**.

![The logbook's champion upgrade page with 18 clans installed](https://raw.githubusercontent.com/David-defrutos/mt2-custom-clan-ui-fixes/main/screenshots/logbook-champion-upgrades.png)

<sub>18 clans in three columns, all visible and clickable. Without the mod the list runs off
the bottom of the page and the last ones cannot be selected at all.</sub>

## What it fixes

### The logbook's champion upgrade page

The **Light Forge Upgrades** page lays the clans out in two fixed columns (regular clans and
crew clans) and **does not paginate**. With a dozen mods installed the column runs off the
page and the last clans are impossible to see or select.

This mod measures the page, works out how many columns fit and lays the clan diamonds out in
a grid that uses the full width, shrinking them only as much as needed. With 18 clans that is
3 columns of 6 at 91% of the original size; the design holds up to about 45 clans before
pagination would be needed.

What it does **not** do: move buttons around in the hierarchy. The game rebuilds those buttons
every time the screen opens, so reparenting them leaves duplicated, dead diamonds behind. Here
every button stays where the game put it and only its drawn position changes.

## Settings

`BepInEx\config\mt2_custom_clan_ui_fixes.Plugin.cfg`, section `[LogbookFit]`:

| key | default | what it does |
|---|---|---|
| `Enabled` | `true` | set to `false` and the screen is left exactly as the game draws it |
| `MinScale` | `0.45` | how far a diamond may be shrunk |
| `MaxAutoColumns` | `3` | column cap. **2 = leave the game's own layout** and only scale it |
| `ColumnSpacing` | `16` | gap between columns once past two (the game's own is 96) |
| `HeightBudget` | `0` | usable page height in px; 0 = detect it (measured: 1000) |
| `WidthBudget` | `0` | usable width in px; 0 = detect it (measured: 400). Set `440` for full-size diamonds |
| `RetryFrames` | `5` | frames the layout pass is retried after the screen opens |
| `Verbose` | `true` | log what it measures and applies to `LogOutput.log` |

The retry is not a blind workaround: the game fills the crew column **one or more frames
after** the screen opens, so the first pass only sees half the clans. The log line is only
written when the result changes, not once per retry.

## Relation to CustomClanHelper

[Custom-Clan-Helper](https://github.com/Monster-Train-2-Modding-Group/Custom-Clan-Helper)
fixes the logbook's **checklist** page and lists this other one in its TODO (*"Champion
Upgrade Page in logbook is wonky"*). Both mods are compatible and deal with different screens.

## Building

See `COMO-COMPILAR.md`. GitHub Actions builds the DLL on every push that touches `src/`.

## License

MIT.

---

# Custom Clan UI Fixes — en castellano

Arreglos de interfaz para jugar con **muchos clanes modeados** instalados. No anade
contenido: ni cartas, ni unidades, ni clanes. Solo parchea pantallas del juego base con
Harmony, asi que **no depende de Trainworks Reloaded ni de Conductor**.

## Que arregla

### La pagina de mejoras de campeon del logbook

La pantalla **Light Forge Upgrades** reparte los clanes en dos columnas fijas (clanes
normales y tripulacion) y **no pagina**: con una decena de mods instalados la columna se sale
de la hoja por abajo y los ultimos clanes no hay forma de verlos ni de seleccionarlos.

Este mod mide la hoja, decide cuantas columnas caben y recoloca los rombos en una rejilla que
aprovecha el ancho, encogiendolos solo lo justo. Con 18 clanes salen 3 columnas de 6 a un 91%
del tamano original; el diseno aguanta hasta unos 45 clanes antes de tener que paginar.

Lo que **no** hace: mover botones de sitio en la jerarquia. El juego reconstruye esos botones
cada vez que se abre la pantalla, asi que cambiarlos de padre deja rombos duplicados y sin
funcionar. Aqui cada boton se queda donde el juego lo puso y solo cambia donde se dibuja.

## Ajustes

`BepInEx\config\mt2_custom_clan_ui_fixes.Plugin.cfg`, seccion `[LogbookFit]`:

| clave | por defecto | que hace |
|---|---|---|
| `Enabled` | `true` | a `false` y la pantalla queda como la deja el juego |
| `MinScale` | `0.45` | hasta donde se deja encoger un rombo |
| `MaxAutoColumns` | `3` | columnas como mucho. **2 = no se toca la rejilla del juego**, solo se escala |
| `ColumnSpacing` | `16` | separacion entre columnas al pasar de dos (la del juego son 96) |
| `HeightBudget` | `0` | alto util de la hoja en px; 0 = detectarlo (medido: 1000) |
| `WidthBudget` | `0` | ancho util en px; 0 = detectarlo (medido: 400). A `440`, rombos a tamano original |
| `RetryFrames` | `5` | frames que se reintenta la colocacion tras abrir la pantalla |
| `Verbose` | `true` | traza en `LogOutput.log`, con las medidas y el factor aplicado |

El reintento no es un parche a ciegas: el juego crea los botones de la columna de tripulacion
**uno o mas frames despues** de abrir la pantalla, asi que la primera colocacion solo ve la
otra mitad. La traza solo se escribe cuando el resultado cambia, no en cada reintento.

## Relacion con CustomClanHelper

[Custom-Clan-Helper](https://github.com/Monster-Train-2-Modding-Group/Custom-Clan-Helper)
arregla la pagina del **checklist** del logbook, y tiene esta otra en su TODO. Los dos mods
son compatibles y se ocupan de pantallas distintas.

## Compilar

Ver `COMO-COMPILAR.md`. El DLL lo compila GitHub Actions con cada push que toque `src/`.

## Licencia

MIT.
