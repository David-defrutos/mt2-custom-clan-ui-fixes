# Changelog

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
