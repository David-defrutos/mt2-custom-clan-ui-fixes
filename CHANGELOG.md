# Changelog

## v0.2.0

- **Logbook, Artifacts page**: the clan columns are now paginated, so the ones that used to
  run off the right edge can be reached. It reuses the game's own page-turn arrows — no new
  UI — and a page holds as many columns as the sheet is wide.
- Nothing changes when every column already fits: no arrows, same screen as before.
- New `[ArtifactsPaging]` section in the config file, with `Enabled = false` to leave the
  page exactly as the game draws it.

## v0.1.0

- First release.
- **Logbook, Light Forge Upgrades page**: the clan diamonds are laid out in a grid that uses
  the full width of the page, so every installed clan is visible and clickable. With 18 clans
  that is 3 columns of 6 at 91% of the original size.
- Everything is configurable in `BepInEx\config\mt2_custom_clan_ui_fixes.Plugin.cfg`, and
  `Enabled = false` leaves the screen exactly as the game draws it.
