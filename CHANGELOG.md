# Changelog

## 0.4.2 (local, sin publicar en Thunderstore)

- Octava pestaña superior de Soul Savior, con acceso directo y paginación independiente del registro normal.
- Cinco clanes por página Soul Savior; se elimina la cabecera que restaba altura a la quinta fila.
- Clanes normales y DLC comparten paginación: con 23 clanes quedan cinco hojas; Yokai, Railforged y Wurmkin aparecen juntos en la última.
- Se conservan los objetos y páginas de datos nativos del DLC para aplicar cambios y animaciones de victoria.
- Navegación con mando selecciona el primer clan visible. Los ocho iconos se distribuyen dentro del espacio de los siete anteriores.
- Compilación sin errores ni avisos; pruebas de geometría, lectura y paginación correctas. Validación visual de esta versión pendiente.
## 0.4.1 (local, sin publicar en Thunderstore)

- Banderas normales repartidas también al inicializar páginas ocultas: 22 aliados quedan en 11+11.
- Posición de banderas medida desde el retrato, en modo normal y Soul Savior; el antiguo offset negativo ya no tapa la imagen.
- Punta de la cinta con 20 px de separación real respecto al medidor.
- Título de Soul Savior sin el localizador heredado del nombre del clan.
- Con 22 aliados por modo se mantienen las páginas separadas: dos grupos a tamaño original necesitan 1176 px más separación; el espacio disponible después del retrato y antes del medidor es menor de 1000 px.
## 0.4.0 (local, sin publicar en Thunderstore)

- Páginas Soul Savior en el registro de progreso: combinaciones principal/aliado, máxima dificultad y detalle por campeón, con lectura de ExpandedWinTracker.
- Contador de aliados con victoria y paginación nativa; opción SoulSavior.Enabled.
- Corregido el estiramiento de cintas con placas flexibles y la restauración de banderas entre clanes.
- Sin cambios en los guardados del juego ni del tracker.
- Primera integración: las victorias por alma todavía no tienen página.

## v0.3.0

- **Logbook, Progress Record page**: one clan per row, paginated with the game's own arrows
  (five clans a page), so the clans past the tenth can be seen at all — before, they were
  drawn below the bottom edge of the sheet. The "Page N of M" label counts those pages.
- The ally banners keep their full size instead of being squeezed until they overlap, and
  the clan's colour ribbon runs all the way to the card mastery meter.
- The card mastery meter grows in columns instead of spilling out of its section when a
  clan has more than 42 cards, and every clan gets the same 12 x 5 grid so the meters line up.
- The DLC page (Railforged, Wurmkin) gets the same layout.
- **Logbook, card filters**: once there are enough clans for a third row of clan buttons,
  the ornament that closes the bottom of the filter panel was drawn right across the search
  box, over the text you type. It is now switched off, and only when it actually overlaps.
- New `[ProgressGrid]` and `[CardFilter]` sections in the config file, each with
  `Enabled = false` to leave the screen exactly as the game draws it.
- Logging is now **off by default** in every section (`Verbose = false`). A config file from
  an earlier version keeps the value it already had: set `Verbose = false` by hand in
  `[LogbookFit]` and `[ArtifactsPaging]` if you want a quiet log.

## v0.2.1

- Store page only: screenshots of the artifacts page and of its second page, the champion
  upgrade page reshot with 21 clans, and a README that describes what the mod actually does
  now. **No changes to the mod itself** — 0.2.0 behaves exactly the same.

## v0.2.0

- **Logbook, Artifacts page**: the clan columns are now paginated, so the ones that used to
  run off the right edge can be reached. It reuses the game's own page-turn arrows — no new
  UI — and a page holds as many columns as the sheet is wide.
- Nothing changes when every column already fits: no arrows, same screen as before.
- Pages are balanced by width instead of filling the first one: 24 columns split 12 + 12,
  not 17 + 7.
- New `[ArtifactsPaging]` section in the config file, with `Enabled = false` to leave the
  page exactly as the game draws it.
- **Logbook, Light Forge Upgrades page**: the page heading is no longer overrun. The area
  the mod measures includes it, so past ~20 clans the first row of diamonds climbed over the
  title; `HeaderReserve` now keeps that space free and the grid shrinks to fit below it.
  `ScaleMultiplier` shrinks the diamonds further if you want more air.

## v0.1.0

- First release.
- **Logbook, Light Forge Upgrades page**: the clan diamonds are laid out in a grid that uses
  the full width of the page, so every installed clan is visible and clickable. With 18 clans
  that is 3 columns of 6 at 91% of the original size.
- Everything is configurable in `BepInEx\config\mt2_custom_clan_ui_fixes.Plugin.cfg`, and
  `Enabled = false` leaves the screen exactly as the game draws it.
<!-- 2026-09-23-0040||claude-mt2-CustomClanUIFixes||plugins/frutos-CustomClanUIFixes/CHANGELOG.md||entrada v0.3.0: hoja de progreso, caja de busqueda del filtro, secciones [ProgressGrid] y [CardFilter], traza apagada por defecto -->

<!-- 2026-09-30-1937||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Integracion Soul Savior 0.4.0 completada; consultas validadas y presentacion separada de victorias normales -->

<!-- 2026-09-30-2025||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||0.4.1: FlagOffsetX por defecto cero y negativos antiguos ignorados; documentar reparto y condicion de espacio -->

<!-- 2026-09-30-2121||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Añadir cambios y validaciones de 0.4.2 -->

<!-- 2026-09-30-2128||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\CHANGELOG.md||Documentar entrega 0.4.2, validaciones y pendientes; distinguir push de código de publicación Thunderstore -->
