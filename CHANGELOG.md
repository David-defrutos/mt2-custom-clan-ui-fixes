[English follows ↓](#english)

<!-- Documento generado el 2026-09-30-2221 -->

# Historial de cambios

## Español

### 0.4.4 — instalada localmente; sin publicar en Thunderstore

- Ajuste de la pestaña Soul Savior: se miden los gráficos visibles de los botones y se espera a que el encabezado tenga posiciones válidas. La 0.4.3 había registrado extremos 0/0 y una separación de cero.
- El reparto se mantiene tras las animaciones de selección y los cambios de ancho, sin aceptar una medición de cero.
- Pruebas del componente con layout tardío, gráficos desplazados respecto a su controlador, ajustes repetidos, animaciones y cambios de ancho: correctas. Compilación local sin errores ni avisos.
- Instalada y verificada por versión y SHA256. **Pendiente de confirmar visualmente en el juego que los ocho iconos quedan separados.**

### 0.4.3 — prueba local; sin publicar en Thunderstore

- Primer intento de corregir el solapamiento de Soul Savior con el trofeo: reparto por centros y mantenimiento de posiciones antes de renderizar.
- Conservación de anclajes y pivotes; protección del ancho del encabezado frente al ajuste automático.
- La prueba en el juego confirmó que el solapamiento continuaba. El registro mostró una separación calculada de cero; la 0.4.4 aborda esa causa.

### 0.4.2 — sin publicar en Thunderstore

- Nueva pestaña superior Soul Savior con acceso directo, contador y paginación independientes del registro normal.
- Cinco clanes por página Soul Savior. Se elimina la cabecera que restaba espacio a la quinta fila.
- Los clanes normales y del DLC comparten paginación: con 23 clanes quedan cinco páginas; Yokai, Railforged y Wurmkin aparecen juntos en la última.
- Se conservan las páginas de datos del DLC para aplicar cambios y animaciones de victoria.
- La navegación con mando selecciona el primer clan visible.
- Compilación y pruebas de geometría, lectura y paginación correctas. Las capturas posteriores detectaron el solapamiento del nuevo botón con el trofeo.

### 0.4.1 — sin publicar en Thunderstore

- Reparto de banderas también al preparar páginas ocultas: 22 aliados quedan en dos filas de 11.
- Las banderas se sitúan después del retrato, tanto en el registro normal como en Soul Savior. Los antiguos valores negativos de `FlagOffsetX` se ignoran para evitar tapar la imagen.
- La punta de la cinta mantiene 20 píxeles de separación respecto al medidor de cartas.
- El título Soul Savior deja de heredar el localizador del nombre del clan, que podía sustituirlo por «Banished».
- El registro normal y Soul Savior conservan vistas separadas: ambos grupos de 22 aliados no caben juntos a tamaño original.
- Disposición confirmada visualmente por David.

### 0.4.0 — sin publicar en Thunderstore

- Primera integración de Soul Savior en el registro de progreso: combinaciones de clan principal y aliado, máxima dificultad ganada y detalle por campeón.
- Lectura del guardado de ExpandedWinTracker al abrir cada página; contador de aliados con victoria y opción `[SoulSavior] Enabled`.
- Corrección del estiramiento de cintas con placas flexibles y de la restauración de banderas entre clanes.
- La integración no modifica los guardados del juego ni del tracker. Las victorias por alma todavía no tienen una vista propia.

### 0.3.0

- **Registro de progreso del logbook:** un clan por fila y cinco clanes por página, con las flechas del juego. Los clanes posteriores al décimo dejan de quedar fuera de la hoja. El contador «Page N of M» incluye estas páginas.
- Las banderas de aliados conservan su tamaño y la cinta de color llega hasta el medidor de maestría de cartas.
- El medidor crece en columnas cuando un clan tiene más de 42 cartas. Todos los clanes usan la misma rejilla de 12 × 5 para mantener la alineación.
- La página de los clanes del DLC, Railforged y Wurmkin, recibe la misma disposición.
- **Filtros de cartas:** el adorno inferior del panel se oculta cuando se solapa con la caja de búsqueda, por ejemplo al aparecer una tercera fila de botones de clan.
- Nuevas secciones `[ProgressGrid]` y `[CardFilter]` en la configuración. `Enabled = false` conserva la interfaz original.
- El registro detallado queda desactivado por defecto (`Verbose = false`). Las configuraciones anteriores conservan su valor: puede desactivarse manualmente en `[LogbookFit]` y `[ArtifactsPaging]`.

### 0.2.1

- Actualización de la página del mod: capturas de las dos páginas de artefactos, nueva captura de mejoras de campeón con 21 clanes y README actualizado.
- **Sin cambios en el código:** el comportamiento es el mismo que en la 0.2.0.

### 0.2.0

- **Artefactos del logbook:** las columnas de clan se reparten en páginas que caben en la hoja, usando las flechas nativas del juego.
- Si todas las columnas caben, se mantiene una sola página sin flechas.
- Las páginas se equilibran por ancho: 24 columnas se reparten en 12 + 12, en lugar de 17 + 7.
- Nueva sección `[ArtifactsPaging]`; `Enabled = false` conserva la interfaz original.
- **Light Forge Upgrades:** `HeaderReserve` reserva espacio para el título y evita que la primera fila de rombos lo tape cuando hay muchos clanes. `ScaleMultiplier` permite reducir más los iconos para aumentar la separación.

### 0.1.0

- Primera versión.
- **Light Forge Upgrades:** los rombos de clan forman una rejilla que aprovecha el ancho de la página, para que todos los clanes instalados sean visibles y seleccionables. Con 18 clanes, se muestran tres columnas de seis al 91 % de su tamaño original.
- Ajustes en `BepInEx\config\mt2_custom_clan_ui_fixes.Plugin.cfg`. `Enabled = false` conserva la interfaz original.

## English

[Volver al español ↑](#español)

### 0.4.4 — installed locally; not published on Thunderstore

- Soul Savior tab adjustment: measure the visible button graphics and wait until the header has valid positions. Version 0.4.3 had logged endpoints of 0/0 and zero spacing.
- Keep the distribution after selection animations and width changes, without accepting a zero measurement.
- Component tests cover delayed layout, graphics offset from their controllers, repeated adjustments, animations and width changes. All pass. The local build has no errors or warnings.
- Installed and verified by version and SHA256. **In-game visual confirmation that all eight icons are separated is still pending.**

### 0.4.3 — local test; not published on Thunderstore

- First attempt to fix the Soul Savior tab overlapping the trophy: distribute tabs by their centres and maintain their positions before rendering.
- Preserve anchors and pivots, and protect the header width from automatic size fitting.
- In-game testing confirmed that the overlap remained. The log showed zero calculated spacing; version 0.4.4 addresses that cause.

### 0.4.2 — not published on Thunderstore

- New top Soul Savior tab with direct access, its own page counter and pagination separate from normal progress.
- Five clans per Soul Savior page. Remove the heading that took space away from the fifth row.
- Normal and DLC clans share pagination: 23 clans fit into five pages, with Yokai, Railforged and Wurmkin together on the last page.
- Preserve the DLC data pages for progress updates and victory animations.
- Controller navigation selects the first visible clan.
- Build, geometry, save-reading and pagination checks pass. Later screenshots revealed that the new tab overlapped the trophy.

### 0.4.1 — not published on Thunderstore

- Balance ally banners while preparing hidden pages too: 22 allies form two rows of 11.
- Position banners after the portrait in both normal progress and Soul Savior. Ignore legacy negative `FlagOffsetX` values to prevent portrait overlap.
- Keep a 20-pixel gap between the ribbon tip and the card mastery meter.
- The Soul Savior heading no longer inherits the clan-name localizer, which could replace it with “Banished”.
- Normal progress and Soul Savior keep separate views: both sets of 22 allies do not fit together at their original size.
- Layout visually confirmed by David.

### 0.4.0 — not published on Thunderstore

- Initial Soul Savior integration in the progress record: main/ally clan combinations, highest winning difficulty and details per champion.
- Read ExpandedWinTracker's save when each page opens; show an ally-win counter and add the `[SoulSavior] Enabled` option.
- Fix ribbon stretching with flexible plaques and banner restoration between clans.
- The integration does not modify game or tracker saves. Wins per soul do not yet have a separate view.

### 0.3.0

- **Logbook progress record:** one clan per row and five clans per page, using the game's arrows. Clans past the tenth are no longer drawn outside the sheet. The “Page N of M” counter includes these pages.
- Ally banners keep their full size, and the clan's colour ribbon reaches the card mastery meter.
- The meter grows in columns when a clan has more than 42 cards. All clans use the same 12 × 5 grid to keep meters aligned.
- The DLC clan page, Railforged and Wurmkin, receives the same layout.
- **Card filters:** hide the panel's bottom ornament when it overlaps the search box, for example when a third row of clan buttons appears.
- New `[ProgressGrid]` and `[CardFilter]` configuration sections. `Enabled = false` preserves the original interface.
- Detailed logging is off by default (`Verbose = false`). Older configurations retain their values: logging can be disabled manually in `[LogbookFit]` and `[ArtifactsPaging]`.

### 0.2.1

- Update the mod page with screenshots of both artifact pages, a new champion-upgrade screenshot with 21 clans and an updated README.
- **No code changes:** behaviour is identical to version 0.2.0.

### 0.2.0

- **Logbook artifacts:** distribute clan columns across pages that fit the sheet, using the game's native arrows.
- When all columns fit, keep a single page without arrows.
- Balance pages by width: 24 columns split into 12 + 12 rather than 17 + 7.
- New `[ArtifactsPaging]` section; `Enabled = false` preserves the original interface.
- **Light Forge Upgrades:** `HeaderReserve` leaves room for the heading so the first row of diamonds does not cover it when many clans are installed. `ScaleMultiplier` allows further icon shrinking for extra spacing.

### 0.1.0

- First release.
- **Light Forge Upgrades:** arrange clan diamonds in a grid that uses the page width so all installed clans are visible and selectable. With 18 clans, this is three columns of six at 91% of the original size.
- Configure the mod in `BepInEx\config\mt2_custom_clan_ui_fixes.Plugin.cfg`. `Enabled = false` preserves the original interface.

<!-- 2026-09-23-0040||claude-mt2-CustomClanUIFixes||plugins/frutos-CustomClanUIFixes/CHANGELOG.md||entrada v0.3.0: hoja de progreso, caja de busqueda del filtro, secciones [ProgressGrid] y [CardFilter], traza apagada por defecto -->

<!-- 2026-09-30-1937||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Integracion Soul Savior 0.4.0 completada; consultas validadas y presentacion separada de victorias normales -->

<!-- 2026-09-30-2025||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||0.4.1: FlagOffsetX por defecto cero y negativos antiguos ignorados; documentar reparto y condicion de espacio -->

<!-- 2026-09-30-2121||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Añadir cambios y validaciones de 0.4.2 -->

<!-- 2026-09-30-2128||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Documentar entrega 0.4.2, validaciones y pendientes; distinguir push de código de publicación Thunderstore -->

<!-- 2026-09-30-2132||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Documentar corrección de pestaña 0.4.3 y validación visual pendiente -->

<!-- 2026-09-30-2209||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Documentar causa confirmada por log y corrección 0.4.4 con prueba de componente -->

<!-- 2026-09-30-2221||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Changelog bilingüe completo: español primero, enlace English follows al inicio; actualizar instalación 0.4.4 y fallos confirmados 0.4.3 -->
