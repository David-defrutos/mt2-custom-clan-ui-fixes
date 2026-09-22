# Changelog

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
