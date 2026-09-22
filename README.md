# Custom Clan UI Fixes (Monster Train 2)

UI fixes for playing with **many clans** installed. It adds no content: no cards, no units,
no clans. It patches base-game screens with Harmony.

![The logbook's artifacts page with 19 clans installed](https://raw.githubusercontent.com/David-defrutos/mt2-custom-clan-ui-fixes/main/screenshots/logbook-artifacts-page1.png)

<sub>The artifacts page with 19 clans installed: the clan columns split into two pages,
turned with the game's own arrows. Without the mod the last ones are drawn past the right
edge of the sheet.</sub>

## What it fixes

### The logbook's champion upgrade page

![The logbook's champion upgrade page with 19 clans installed](https://raw.githubusercontent.com/David-defrutos/mt2-custom-clan-ui-fixes/main/screenshots/logbook-champion-upgrades.png)

<sub>19 clans in three columns, all visible and clickable, with the page heading left
alone.</sub>

The **Light Forge Upgrades** page lays the clans out in two fixed columns (regular clans and
crew clans) and **does not paginate**: with a dozen mods installed the column runs off the
bottom of the page and the last clans cannot be seen or selected.

This mod measures the page, works out how many columns fit and lays the clan diamonds out in
a grid that uses the full width, shrinking them only as much as needed and keeping the page
heading clear. With 21 clans that is 3 columns of 7 at 87% of the original size, and the
diamonds keep shrinking on their own as clans are added — around 30 they are at 61%.

What it does **not** do: move buttons around in the hierarchy. The game rebuilds those
buttons every time the screen opens, so reparenting them leaves duplicated, dead diamonds
behind. Here every button stays where the game put it and only its drawn position changes.

### The logbook's artifacts page

The **Artifacts** page gives every clan a column of its own and, like the page above, does
not paginate: past a dozen clans the last columns run off the right edge of the sheet and
there is no way to reach them.

This mod splits the columns into pages that fit the width of the sheet and **reuses the
game's own page-turn arrows** — the ones the compendium already draws for the card and
checklist pages. No new UI, no reparenting: paging is whole columns being switched on and
off, and the game's layout closes the gaps. When every column already fits, nothing is
hidden and no arrow appears.

The pages come out evenly filled rather than cramming the first one: with 24 columns that is
12 and 12, not 17 and 7. It balances by width, not by column count — the generic artifacts
take four times the width of a clan column.

![Page two of the artifacts page](https://raw.githubusercontent.com/David-defrutos/mt2-custom-clan-ui-fixes/main/screenshots/logbook-artifacts-page2.png)

<sub>Page two, reached with the arrow on the right. Without the mod these columns are drawn
past the edge of the sheet and cannot be reached at all.</sub>

### The logbook's progress record

![The logbook's progress record, page 3 of 5, with 19 clans installed](https://raw.githubusercontent.com/David-defrutos/mt2-custom-clan-ui-fixes/main/screenshots/logbook-progress-record.png)

<sub>Page 3 of 5 with 19 clans: one clan per row, the ally banners at full size, and the same
card mastery grid for every clan.</sub>

The **Progress Record** page lays the clans out in a fixed grid of 2 x 5 and does not
paginate either: from the eleventh clan on, the sections are drawn below the bottom edge of
the sheet and cannot be seen at all. On top of that, each row of ally banners is only 354 px
wide, so with a dozen allied clans the game squeezes the banners to half their width and
they overlap.

This mod puts one clan per row and pages the sheet with the game's own arrows, five clans
per page; the "Page N of M" label counts those pages. With the full width of the sheet:

- the ally banners keep their full size;
- the clan's colour ribbon runs all the way to the card mastery meter;
- the card mastery meter grows in columns instead of rows — a clan with more than 42 cards
  used to spill out of its section — and every clan gets the same 12 x 5 grid, so the meters
  line up.

The DLC page (Railforged, Wurmkin) gets the same layout.

### The card filter's search box

![The card filters with the search box readable](https://raw.githubusercontent.com/David-defrutos/mt2-custom-clan-ui-fixes/main/screenshots/card-filter-search.png)

<sub>The logbook's card filters with 19 clans: three rows of clan buttons, and a search box
you can read.</sub>

Once there are enough clans for a third row of clan buttons, the ornament that closes the
bottom of the filter panel — a line with a diamond in the middle — is drawn right across the
search box, over the text you type. This mod switches that ornament off, and only when it
actually overlaps the box: with fewer clans the panel is left as the game draws it.

## Settings

`BepInEx\config\mt2_custom_clan_ui_fixes.Plugin.cfg`, section `[LogbookFit]`:

| key | default | what it does |
|---|---|---|
| `Enabled` | `true` | set to `false` and the screen is left exactly as the game draws it |
| `MinScale` | `0.45` | how far a diamond may be shrunk |
| `HeaderReserve` | `130` | px kept free at the top for the page heading. The measured area includes it, so without this the first row of diamonds climbs over the title once there are enough clans |
| `ScaleMultiplier` | `1` | multiplies the computed factor, to leave the diamonds a little smaller than strictly needed (0.9 = 10% smaller) |
| `MaxAutoColumns` | `3` | column cap. **2 = leave the game's own layout** and only scale it |
| `ColumnSpacing` | `16` | gap between columns once past two (the game's own is 96) |
| `HeightBudget` | `0` | usable page height in px; 0 = detect it (measured: 1000) |
| `WidthBudget` | `0` | usable width in px; 0 = detect it (measured: 400). At `440` the diamonds keep their original size, spilling slightly outside the nominal area |
| `RetryFrames` | `5` | frames the layout pass is retried after the screen opens |
| `Verbose` | `false` | log what it measures and applies to `LogOutput.log` |

The retry is not a blind workaround: the game fills the crew column **one or more frames
after** the screen opens, so the first pass only sees half the clans. The log line is only
written when the result changes, not once per retry.

Section `[ArtifactsPaging]`:

| key | default | what it does |
|---|---|---|
| `Enabled` | `true` | set to `false` and the artifacts page is left exactly as the game draws it |
| `ColumnsPerPage` | `0` | columns per page; 0 = as many as the measured width fits |
| `WidthBudget` | `0` | usable sheet width in px; 0 = detect it. If the split comes out short or long, take the width from the `zona:` line in the log and set it here |
| `Balance` | `true` | evenly filled pages (24 columns: 12 + 12) instead of filling the first one (17 + 7) |
| `RetryFrames` | `10` | frames the split is retried after the screen opens |
| `Verbose` | `false` | log what it measures and how it splits |

Section `[ProgressGrid]`:

| key | default | what it does |
|---|---|---|
| `Enabled` | `true` | set to `false` and the progress record is left exactly as the game draws it |
| `Columns` | `1` | clan columns. 1 = one clan per row, which is what makes room for the banners; 2 = the game's own grid |
| `RowsPerPage` | `0` | clans per page; 0 = as many as fit (5) |
| `PlaqueWidth` | `280` | width of the portrait plaque in px; 0 = whatever is left |
| `FlagOffsetX` | `-280` | px the ally banners are moved inside the section; negative = to the left |
| `BalanceFlagRows` | `true` | splits the ally banners evenly between their two rows. It moves objects between parents, as the game itself does: if duplicated or unresponsive banners show up, set it to `false` |
| `FixMasteryMeter` | `true` | the card mastery meter grows in columns instead of rows |
| `MeterColumns` | `12` | meter columns, the same for every clan |
| `MeterRows` | `5` | rows that fit; with 12 columns, room for 60 cards. If a clan does not fit, columns are added for every clan |
| `Verbose` | `false` | log the grid, the paging and what it adjusts |

The remaining keys (`Layout`, `WidenSections`, `FreeFlagWidth`, `FlagAlignLeft`,
`FlagSpacing`, `StretchPlaqueFill`, `RibbonExtra`, `DetailSections`, `DumpTree`) are there
for troubleshooting; their defaults are the values tuned in game.

Section `[CardFilter]`:

| key | default | what it does |
|---|---|---|
| `Enabled` | `true` | set to `false` and the filter panel is left exactly as the game draws it |
| `MinOverlap` | `0.25` | how much of the search box's height something has to cover to count as the ornament. Raise it if anything else gets switched off |
| `AlsoInside` | `false` | also look inside the search box itself. Off because the box's own frame and background live there |
| `Levels` | `1` | how far up to look for the ornament; 1 = the Search section and its neighbours |
| `Verbose` | `false` | log what it switched off |
| `DumpTree` | `false` | dump the panel's hierarchy once, for troubleshooting |

## Relation to CustomClanHelper

[Custom-Clan-Helper](https://github.com/Monster-Train-2-Modding-Group/Custom-Clan-Helper)
also works on the logbook: it lists the champion upgrade and artifacts pages in its TODO
(*"Champion Upgrade Page in logbook is wonky"*, *"Artifact page needs pagination support"*),
and it gives modded clans a progress record page of their own. Both mods work together: this
one has been tested with Custom-Clan-Helper installed, progress record included.

## Building

See `COMO-COMPILAR.md`. GitHub Actions builds the DLL on every push that touches `src/`.

## License

MIT.

---

# Custom Clan UI Fixes — en castellano

Arreglos de interfaz para jugar con **muchos clanes** instalados. No añade
contenido: ni cartas, ni unidades, ni clanes. Parchea pantallas del juego base con
Harmony.

## Que arregla

### La pagina de mejoras de campeon del logbook

La pantalla **Light Forge Upgrades** del logbook reparte los clanes en dos columnas fijas
(clanes normales y tripulacion) y **no pagina**: con una decena de mods instalados la
columna se sale de la hoja por abajo y los ultimos clanes no hay forma de verlos ni de
seleccionarlos.

Este mod mide la hoja, decide cuantas columnas caben y recoloca los rombos en una rejilla
que aprovecha el ancho, encogiendolos solo lo justo y dejando libre el titulo de la hoja.
Con 21 clanes salen 3 columnas de 7 a un 87% del tamano original, y los rombos siguen
encogiendo solos segun se anaden clanes: sobre los 30 van al 61%.

Lo que **no** hace: mover botones de sitio en la jerarquia. El juego reconstruye esos
botones cada vez que se abre la pantalla, asi que cambiarlos de padre deja rombos
duplicados y sin funcionar. Aqui cada boton se queda donde el juego lo puso y solo se
cambia donde se dibuja.

### La pagina de artefactos del logbook

La pagina de **artefactos** le da una columna a cada clan y, como la anterior, **no pagina**:
pasada la docena de clanes las ultimas columnas se salen de la hoja por la derecha y no hay
forma de llegar a ellas.

Este mod reparte las columnas en paginas que caben a lo ancho de la hoja y **reutiliza las
flechas de paso de pagina del propio juego**, las mismas que el compendio ya pinta en las
paginas de cartas y de coleccion. Ni UI nueva ni cambios de jerarquia: pasar de pagina es
encender y apagar columnas enteras, y el layout del juego cierra los huecos. Si todas las
columnas caben, no se esconde ninguna y no aparece ninguna flecha.

Las paginas salen igual de llenas en vez de llenar la primera: con 24 columnas, 12 y 12, no
17 y 7. El equilibrio es por ancho y no por numero de columnas, porque la de artefactos
genericos ocupa cuatro veces lo que una de clan.

### La hoja de progreso del logbook

La hoja **Progress Record** reparte los clanes en una rejilla fija de 2 x 5 y tampoco pagina:
a partir del undecimo clan, las secciones se dibujan por debajo del borde de la hoja y no hay
forma de verlas. Ademas, cada fila de banderas de aliados mide solo 354 px, asi que con una
docena de clanes aliados el juego aplasta las banderas a la mitad y se solapan.

Este mod pone un clan por fila y pagina la hoja con las flechas del propio juego, cinco
clanes por pagina; el rotulo "Page N of M" cuenta esas paginas. Con todo el ancho de la hoja:

- las banderas de aliados conservan su tamano;
- la cinta de color del clan llega hasta el medidor de cartas;
- el medidor de cartas dominadas crece en columnas y no en filas -un clan con mas de 42
  cartas se salia de su seccion- y todos los clanes llevan la misma rejilla de 12 x 5, asi
  que los medidores quedan alineados.

La hoja de DLC (Railforged, Wurmkin) recibe el mismo formato.

### La caja de busqueda del filtro de cartas

En cuanto hay clanes para una tercera fila de botones de clan, el adorno que cierra el panel
de filtros por abajo -una raya con un rombo en medio- se dibuja justo encima de la caja de
busqueda, sobre el texto que escribes. Este mod apaga ese adorno, y solo cuando de verdad
tapa la caja: con menos clanes el panel queda como lo pinta el juego.

## Ajustes

`BepInEx\config\mt2_custom_clan_ui_fixes.Plugin.cfg`, seccion `[LogbookFit]`:

| clave | por defecto | que hace |
|---|---|---|
| `Enabled` | `true` | a `false` y la pantalla queda como la deja el juego |
| `MinScale` | `0.45` | hasta donde se deja encoger un rombo |
| `HeaderReserve` | `130` | pixeles que se reservan arriba para el titulo de la hoja. La zona medida lo incluye, asi que sin esto los rombos se le suben encima en cuanto hay bastantes clanes |
| `ScaleMultiplier` | `1` | multiplica el factor calculado, para dejar los rombos algo mas pequenos de lo justo (0,9 = un 10% mas pequenos) |
| `MaxAutoColumns` | `3` | columnas como mucho. **2 = no se toca la rejilla del juego**, solo se escala |
| `ColumnSpacing` | `16` | separacion entre columnas al pasar de dos (la del juego son 96) |
| `HeightBudget` | `0` | alto util de la hoja en px; 0 = detectarlo (medido: 1000) |
| `WidthBudget` | `0` | ancho util en px; 0 = detectarlo (medido: 400). A `440` los rombos quedan a tamano original, saliendose un poco del area nominal |
| `RetryFrames` | `5` | frames que se reintenta la colocacion tras abrir la pantalla |
| `Verbose` | `false` | traza en `LogOutput.log`, con las medidas y el factor aplicado |

El reintento no es un parche a ciegas: el juego crea los botones de la columna de tripulacion
**uno o mas frames despues** de abrir la pantalla, asi que la primera colocacion solo ve la
otra mitad. La traza solo se escribe cuando el resultado cambia, no en cada reintento.

Seccion `[ArtifactsPaging]`:

| clave | por defecto | que hace |
|---|---|---|
| `Enabled` | `true` | a `false` y la pagina de artefactos queda como la deja el juego |
| `ColumnsPerPage` | `0` | columnas por pagina; 0 = las que quepan segun el ancho medido |
| `WidthBudget` | `0` | ancho util de la hoja en px; 0 = detectarlo. Si el reparto se queda corto o largo, coge el ancho de la linea `zona:` del log y fijalo aqui |
| `Balance` | `true` | paginas igual de llenas (24 columnas: 12 + 12) en vez de llenar la primera (17 + 7) |
| `RetryFrames` | `10` | frames que se reintenta el reparto tras abrir la pantalla |
| `Verbose` | `false` | traza en `LogOutput.log` de lo que mide y de como reparte |

Seccion `[ProgressGrid]`:

| clave | por defecto | que hace |
|---|---|---|
| `Enabled` | `true` | a `false` y la hoja de progreso queda como la deja el juego |
| `Columns` | `1` | columnas de clanes. 1 = un clan por fila, que es lo que deja sitio a las banderas; 2 = la rejilla del juego |
| `RowsPerPage` | `0` | clanes por pagina; 0 = los que quepan (5) |
| `PlaqueWidth` | `280` | ancho de la placa del retrato en px; 0 = lo que sobre |
| `FlagOffsetX` | `-280` | px que se mueven las banderas de aliados dentro de la seccion; negativo = a la izquierda |
| `BalanceFlagRows` | `true` | reparte las banderas de aliados a partes iguales entre sus dos filas. Mueve objetos de padre, como hace el propio juego: si aparecen banderas duplicadas o que no responden, ponlo a `false` |
| `FixMasteryMeter` | `true` | el medidor de cartas dominadas crece en columnas y no en filas |
| `MeterColumns` | `12` | columnas del medidor, las mismas para todos los clanes |
| `MeterRows` | `5` | filas que caben; con 12 columnas, sitio para 60 cartas. Si un clan no cabe, se anaden columnas para todos |
| `Verbose` | `false` | traza de la rejilla, el paginado y lo que ajusta |

El resto de claves (`Layout`, `WidenSections`, `FreeFlagWidth`, `FlagAlignLeft`,
`FlagSpacing`, `StretchPlaqueFill`, `RibbonExtra`, `DetailSections`, `DumpTree`) son para
diagnosticar; sus valores por defecto son los ajustados en partida.

Seccion `[CardFilter]`:

| clave | por defecto | que hace |
|---|---|---|
| `Enabled` | `true` | a `false` y el panel de filtros queda como lo deja el juego |
| `MinOverlap` | `0.25` | cuanto del alto de la caja de busqueda tiene que tapar algo para darlo por adorno. Subelo si se apaga algo mas |
| `AlsoInside` | `false` | buscar tambien dentro de la propia caja. Apagado porque ahi viven su marco y su fondo |
| `Levels` | `1` | hasta donde se sube a buscar el adorno; 1 = la seccion Search y sus vecinas |
| `Verbose` | `false` | traza de lo que ha apagado |
| `DumpTree` | `false` | vuelca una vez el arbol del panel, para diagnosticar |

## Relacion con CustomClanHelper

[Custom-Clan-Helper](https://github.com/Monster-Train-2-Modding-Group/Custom-Clan-Helper)
tambien trabaja sobre el logbook: tiene las paginas de mejoras y de artefactos en su TODO
(*"Champion Upgrade Page in logbook is wonky"*, *"Artifact page needs pagination support"*)
y da a los clanes modeados una hoja de progreso propia. Los dos mods funcionan juntos: este se
ha probado con Custom-Clan-Helper instalado, hoja de progreso incluida.

## Compilar

Ver `COMO-COMPILAR.md`. El DLL lo compila GitHub Actions con cada push que toque `src/`.

## Licencia

MIT.
<!-- 2026-09-22-2327||claude-mt2-CustomClanUIFixes||plugins/frutos-CustomClanUIFixes/README.md||Verbose true->false en las cuatro tablas de ajustes (ingles y espanol, pagina de mejoras y de artefactos) -->
<!-- 2026-09-23-0040||claude-mt2-CustomClanUIFixes||plugins/frutos-CustomClanUIFixes/README.md||0.3.0: secciones nuevas de la hoja de progreso y de la caja de busqueda (ingles y espanol) con sus dos capturas, tablas [ProgressGrid] y [CardFilter], [ArtifactsPaging] RetryFrames 5->10 y Balance anadido, pies de captura a 19 clanes, relacion con Custom-Clan-Helper reescrita -->
<!-- 2026-09-23-0058||claude-mt2-CustomClanUIFixes||plugins/frutos-CustomClanUIFixes/README.md||relacion con Custom-Clan-Helper (ingles y espanol): fuera la advertencia de "no probado juntos"; probado el 23-sep con CCH activo, hoja de progreso incluida -->
