[English follows ↓](#english)

# Historial de cambios

## 0.5.0

- Move Soul Savior Souls and Clans to the independent SoulSaviorLogbook plugin.
- Remove the embedded Soul Savior tab and direct ExpandedWinTracker package dependency.
- Keep UI fixes and card-usage tooltip tracking; defer the experimental Progress · Cards explorer without deleting its stored counts.


## Español

### 0.4.8

**Nota de desarrollo:** La página **Progress · Cards** de Soul Savior sigue en desarrollo. He decidido publicar esta actualización porque ya incluye numerosas mejoras en el seguimiento de Soul Savior.

- Menú de clanes con fondo opaco y navegación independiente: la tabla y el buscador dejan de recibir selección mientras el desplegable está abierto.
- Corregida la sustitución del trofeo en los estados normal y seleccionado de la pestaña Soul Savior, también tras las animaciones.
- Selector de clan desplegable con lista alfabética y desplazamiento, para elegir directamente en lugar de recorrer todos los clanes. Icono de alma propio para la pestaña Soul Savior, evitando repetir el trofeo.
- Texto de información más ligero y espaciado: peso regular, sin contorno ni sombra, con mayor separación de letras, palabras y líneas.
- Cartas, Clanes y Almas conservan por separado búsqueda y página mientras el libro está abierto, incluido el detalle de aliados. Limpiar afecta solo a la vista actual; pulsar la pestaña activa conserva su posición.
- Corregido un fallo al crear los tooltips de los encabezados que podía impedir mostrar la nueva pestaña del logbook.
- Menos huecos en el panel lateral, más espacio para nombres en las tablas y tooltips para títulos y ayudas completos. La navegación compacta también se activa cuando falta altura.
- Filtros activos destacados, ayudas localizadas para recorrer opciones, contador de resultados y mensajes centrados para tablas vacías. Volver del detalle de clan conserva la página y la búsqueda de la lista.
- Tarjetas de almas con el mismo estilo del libro, tablas con filas alternas y valores alineados, marcas de objetivo y buscador enmarcado. Los tooltips de almas se limpian si falla la lectura del tracker.
- Interfaz de Soul Savior y Progreso más integrada con el libro: botones enmarcados, selección dorada, texto en tinta y bloques separados.
- Accesos Almas, Clanes y Progreso dentro de Soul Savior, con memoria de página durante la sesión.
- Ranking de cartas más jugadas, con búsqueda y filtros por clan y modo; usos directos y automáticos separados.
- Objetivo de dificultad para combinaciones y almas, con contadores completos, porcentaje y barra de progreso.
- Filtros de almas: todas, sin victoria y por debajo del objetivo.
- Pendientes por campeón: seleccionar un clan muestra los aliados que le faltan al campeón elegido.
- Búsqueda de clanes y orden por nombre, progreso o cantidad de pendientes. Las tablas usan las flechas del juego.

### 0.4.7

- Panel del clan seleccionado: aliados pendientes y mejor dificultad de cada campeón. Sigue el cursor y la selección con mando.
- Navegación nativa en el catálogo de almas, tooltips y texto adaptable; los aliados pendientes se recorren por bloques.
- Filtro «Sin victoria» para las combinaciones de Soul Savior, conservando los contadores completos.
- Contador histórico de cartas en sus tooltips, con jugadas directas y automáticas separadas y subtotal Soul Savior. Agrupa copias y mejoras de la misma carta.
- El recuento empieza al instalar esta función. Excluye habilidades, simulaciones y replays; conserva las jugadas reales previas a deshacer o reiniciar un combate.

### 0.4.6

- Seguimiento por alma en la pestaña Soul Savior, con iconos y nombres en el idioma del juego.
- Quince almas por página y resumen de almas con victoria; las combinaciones de clanes siguen después del catálogo.
- Nivel máximo del alma y dificultad máxima mostrados como récords independientes de ExpandedWinTracker.

### 0.4.5

- Nuevo resumen de Soul Savior en el panel izquierdo: combinaciones ganadas, clanes con victorias y máxima dificultad alcanzada.
- Los textos de Soul Savior siguen el idioma seleccionado en el juego.
- Icono de Soul Savior ajustado para encajar mejor en las pestañas del logbook.
- ExpandedWinTracker by Conductor se instala automáticamente como dependencia. Gracias a Brandon por el seguimiento de victorias.

### 0.4.4

- Nueva pestaña de Soul Savior con acceso directo y cinco clanes por página.
- Seguimiento de victorias por combinación de clan principal y aliado, con máxima dificultad ganada y detalles por campeón al pasar el cursor sobre las banderas.
- Usa los registros de **ExpandedWinTracker by Conductor**, sin modificar sus guardados. Gracias a Brandon por ExpandedWinTracker.
- Las banderas de aliados se reparten de forma equilibrada y dejan libre el retrato del clan.
- Los clanes normales y del DLC comparten páginas para aprovechar mejor el espacio.
- Ajustes de alineación de pestañas, cintas y medidores, y mejoras de navegación con mando.

### 0.3.0

- Registro de progreso con un clan por fila y cinco clanes por página, usando las flechas del juego.
- Banderas de aliados a tamaño completo y medidores de maestría alineados, también para los clanes del DLC.
- El adorno inferior del filtro de cartas deja de tapar la caja de búsqueda cuando hay muchos clanes.
- Nuevos ajustes para el registro de progreso y los filtros de cartas. Registro detallado desactivado por defecto.

### 0.2.1

- Nuevas capturas y documentación en la página del mod. Sin cambios en su funcionamiento.

### 0.2.0

- Artefactos repartidos en páginas equilibradas, con las flechas del juego. Si todas las columnas caben, se mantiene una sola página.
- Las mejoras de campeón respetan el espacio del título y permiten ajustar el tamaño de los iconos.

### 0.1.0

- Primera versión.
- Rejilla de mejoras de campeón que permite ver y seleccionar todos los clanes instalados.
- Opciones de configuración para ajustar o desactivar los cambios de interfaz.

## English

### 0.4.8

**Development note:** The **Progress · Cards** page inside Soul Savior is still under development. I'm releasing this update now because it already brings substantial improvements to Soul Savior tracking.

- Clan dropdown now has an opaque background and isolated navigation; the table and search field cannot receive selection while the menu is open.
- Fixed Soul Savior tab artwork in both normal and selected states, including after button animations.
- Scrollable alphabetical clan dropdown for direct selection instead of cycling through every clan. Soul Savior now uses a soul icon instead of repeating the trophy.
- Lighter, more spacious information text: regular weight, no outline or shadow, and increased letter, word and line spacing.
- Cards, Clans and Souls keep separate searches and pages while the logbook is open, including ally details. Clear affects only the current view; selecting the active tab preserves its position.
- Fixed heading tooltip initialization that could prevent the new logbook tab from displaying.
- Tighter sidebar spacing, more room for table names and full-text tooltips for headings and help. Compact navigation also handles insufficient height.
- Highlighted active filters, localized option hints, result counts and centered empty states. Returning from clan details restores the list page and search.
- Matching soul cards, alternating table rows, aligned values, goal markers and a framed search field. Soul tooltips clear stale results when tracker records cannot be read.
- Refined Soul Savior and Progress styling with framed buttons, gold selection, ink text and clearer sections matching the logbook.
- Souls, Clans and Progress shortcuts within Soul Savior, with page memory during the session.
- Most-played card ranking with search, clan and mode filters, and separate direct and automatic counts.
- Difficulty goals for clan combinations and souls, with complete totals, percentage and a progress bar.
- Soul filters: all, without a win, or below the selected goal.
- Champion-specific missing wins: select a clan to inspect the allies still needed by the chosen champion.
- Clan search and sorting by name, progress or missing wins. Tables support the game's page arrows.

### 0.4.7

- Selected-clan panel with missing allies and each champion’s highest difficulty. Follows mouse focus and controller selection.
- Native navigation for soul entries, tooltips and adaptive text; missing allies can be browsed in batches.
- “Without a win” filter for Soul Savior combinations, while keeping full progress totals.
- Historical card-play counts in card tooltips, separating direct and automatic plays and showing a Soul Savior subtotal. Copies and upgrades share the same base-card count.
- Counts start when this feature is installed. Abilities, previews and replays are excluded; real plays before an undo or battle restart remain counted.

### 0.4.6

- Soul tracking in the Soul Savior tab, with icons and names in the selected game language.
- Fifteen souls per page and a summary of souls with a win; clan combinations follow the soul catalog.
- Highest soul tier and highest difficulty shown as independent records from ExpandedWinTracker.

### 0.4.5

- New Soul Savior overview in the left panel: winning clan combinations, clans with wins and highest winning difficulty.
- Soul Savior text follows the language selected in the game.
- Refined Soul Savior icon to fit the logbook tabs.
- ExpandedWinTracker by Conductor is now installed automatically as a dependency. Thanks to Brandon for the win tracking.

### 0.4.4

- New Soul Savior tab with direct access and five clans per page.
- Track wins by main/ally clan combination, including highest winning difficulty and champion details when hovering over banners.
- Uses records from **ExpandedWinTracker by Conductor** without modifying its save files. Thanks to Brandon for ExpandedWinTracker.
- Ally banners are evenly distributed and no longer cover clan portraits.
- Normal and DLC clans share pages to make better use of the available space.
- Tab, ribbon and meter alignment adjustments, plus improved controller navigation.

### 0.3.0

- Progress record with one clan per row and five clans per page, using the game's arrows.
- Full-size ally banners and aligned mastery meters, including DLC clans.
- The card filter's bottom ornament no longer covers the search box when many clans are installed.
- New progress-record and card-filter settings. Detailed logging is off by default.

### 0.2.1

- Updated screenshots and documentation on the mod page. No changes to mod behaviour.

### 0.2.0

- Artifacts split into balanced pages using the game's arrows. Keep a single page when all columns fit.
- Champion upgrades leave room for the heading and allow icon-size adjustments.

### 0.1.0

- First release.
- Champion-upgrade grid that makes all installed clans visible and selectable.
- Configuration options to adjust or disable the interface changes.

<!-- Documento generado el 2026-09-30-2221 -->

<!-- 2026-09-23-0040||claude-mt2-CustomClanUIFixes||plugins/frutos-CustomClanUIFixes/CHANGELOG.md||entrada v0.3.0: hoja de progreso, caja de busqueda del filtro, secciones [ProgressGrid] y [CardFilter], traza apagada por defecto -->

<!-- 2026-09-30-1937||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Integracion Soul Savior 0.4.0 completada; consultas validadas y presentacion separada de victorias normales -->

<!-- 2026-09-30-2025||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||0.4.1: FlagOffsetX por defecto cero y negativos antiguos ignorados; documentar reparto y condicion de espacio -->

<!-- 2026-09-30-2121||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Añadir cambios y validaciones de 0.4.2 -->

<!-- 2026-09-30-2128||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Documentar entrega 0.4.2, validaciones y pendientes; distinguir push de código de publicación Thunderstore -->

<!-- 2026-09-30-2132||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Documentar corrección de pestaña 0.4.3 y validación visual pendiente -->

<!-- 2026-09-30-2209||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Documentar causa confirmada por log y corrección 0.4.4 con prueba de componente -->

<!-- 2026-09-30-2221||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Changelog bilingüe completo: español primero, enlace English follows al inicio; actualizar instalación 0.4.4 y fallos confirmados 0.4.3 -->

<!-- 2026-09-30-2240||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Reescribir changelog para jugadores; agrupar iteraciones internas bajo 0.4.4; mantener español/inglés y añadir novedades 0.4.5 -->

<!-- 2026-10-01-2326||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Changelog público bilingüe 0.4.6 centrado en seguimiento de almas -->

<!-- 2026-10-02-0049||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Changelog público bilingüe 0.4.7: cuatro mejoras y reglas del contador, sin notas de pruebas internas -->

<!-- 2026-10-02-0931||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Notas de próxima versión para jugadores en español primero e inglés; cinco mejoras y accesos directos -->

<!-- 2026-10-02-0952||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Documentar pulido visual en español e inglés en sección próxima versión -->

<!-- 2026-10-02-1046||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Añadir refinamientos de tarjetas, tablas, búsqueda y errores a notas bilingües -->

<!-- 2026-10-02-1052||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Añadir mejoras de claridad de controles y retorno de detalle al changelog bilingüe -->

<!-- 2026-10-02-1107||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Registrar ajuste de espacio y textos largos en changelog bilingüe -->

<!-- 2026-10-02-1559||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Registrar corrección de pestaña vacía en notas bilingües -->

<!-- 2026-10-02-1615||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Añadir memoria y limpieza por vista al changelog bilingüe -->

<!-- 2026-10-04-0522||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Documentar mejora de legibilidad en notas públicas bilingües -->

<!-- 2026-10-04-0538||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Documentar desplegable de clanes e icono distinto en notas bilingües -->

<!-- 2026-10-04-0601||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Añadir correccion del icono duplicado a novedades ES/EN -->

<!-- 2026-10-04-0607||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Añadir correccion de superposicion y navegacion del menu en ES/EN -->

<!-- 2026-10-04-1713||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Cerrar novedades como 0.4.8 y explicar en ES/EN publicacion por mejoras Soul Savior con Cards en desarrollo -->

<!-- 2026-10-04-1714||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Conservar separacion CommonMark entre titulos/nota de desarrollo y listas -->

<!-- 2026-10-04-2148||codex-soulsavior-split||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Notas UI 0.5.0 centradas en separación -->
