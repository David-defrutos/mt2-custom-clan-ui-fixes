# Custom Clan UI Fixes — Monster Train 2

Keep the logbook usable when you have many clans installed. CustomClanUIFixes adjusts the game's interface so clan icons, ally flags and artifact columns stay accessible.

## Features

- **Champion upgrades:** automatically fits clan diamonds into the available space while keeping them clickable.
- **Artifacts:** splits clan columns across pages using the game's page-turn arrows.
- **Progress Record:** wider clan rows, balanced ally flags, pagination and a mastery grid that fits larger custom clans.
- **Card search:** clears the decoration that overlaps the search field.
- **Card usage tooltips:** records actual card plays from installation onwards, with separate direct and automatic counts. Historical plays before tracking was installed are unavailable; replay actions are excluded.

![Champion upgrades with custom clans](https://raw.githubusercontent.com/David-defrutos/mt2-custom-clan-ui-fixes/main/screenshots/logbook-champion-upgrades.png)

![Paginated artifact columns](https://raw.githubusercontent.com/David-defrutos/mt2-custom-clan-ui-fixes/main/screenshots/logbook-artifacts-page1.png)

## Version 0.5.0: Soul Savior has its own logbook

Install [SoulSaviorLogbook](https://thunderstore.io/c/monster-train-2/p/frutos/SoulSaviorLogbook/) for the **Souls and Clans** views, champion records and pending allies. The two mods can be used together.

The experimental **Progress · Cards explorer** remains in development and is not included in 0.5.0. Existing card usage counts are preserved and remain available in card tooltips.

## Installation and configuration

Install through your Monster Train 2 mod manager. BepInEx is the package dependency.

Settings are in `BepInEx/config/mt2_custom_clan_ui_fixes.Plugin.cfg`. Each UI fix has its own Enabled setting. Defaults work automatically; layout and debugging options are available for custom setups.

[Source code and issue tracker](https://github.com/David-defrutos/mt2-custom-clan-ui-fixes)

---

## Castellano

Arreglos de interfaz para jugar con muchos clanes instalados:

- Encaja los rombos de las mejoras de campeón y mantiene su selección.
- Pagina las columnas de artefactos con las flechas del juego.
- Ensancha las filas de progreso, reparte las banderas de aliados y ajusta el medidor de cartas dominadas.
- Despeja la decoración que cruza el buscador de cartas.
- Muestra usos reales de cartas en los tooltips, separando jugadas directas y automáticas. El seguimiento empieza al instalarlo y excluye las acciones de replays.

**En 0.5.0, Almas y Clanes pasan a [SoulSaviorLogbook](https://thunderstore.io/c/monster-train-2/p/frutos/SoulSaviorLogbook/).** Se pueden usar ambos mods juntos.

**Progress · Cards sigue en desarrollo** y su explorador no se incluye en esta versión. Se conservan los usos ya registrados y sus tooltips.

Instálalo mediante el gestor de mods. Los ajustes están en `BepInEx/config/mt2_custom_clan_ui_fixes.Plugin.cfg`; cada arreglo puede activarse por separado.
