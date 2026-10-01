using System;
using System.Collections;
using System.IO;
using BepInEx;
using HarmonyLib;

namespace mt2_custom_clan_ui_fixes.Plugin
{
    [HarmonyPatch]
    public static class CardUsageTracker
    {
        public static bool Enabled = true;
        internal static readonly string SavePath = Path.Combine(Paths.ConfigPath, "CustomClanUIFixes", "card-usage.json");
        static CardUsageStore? store;
        static bool unavailable;
        internal static CardUsageStore? Store
        {
            get
            {
                if (!Enabled || unavailable) return null;
                try { return store ??= CardUsageStore.Open(SavePath); }
                catch (Exception e) { unavailable = true; Plugin.Logger.LogWarning("[CardUsage] File preserved; tracking unavailable: " + e); return null; }
            }
        }
        [HarmonyPatch(typeof(CardManager), "OnCardPlayed")]
        [HarmonyPostfix]
        static void WrapPlay(ref IEnumerator __result, CardState playCard, bool fromDirectPlay,
            SaveManager ___saveManager, AllGameManagers ___allGameManagers)
        {
            if (Enabled) __result = Track(__result, playCard, fromDirectPlay, ___saveManager, ___allGameManagers);
        }
        static IEnumerator Track(IEnumerator original, CardState card, bool direct, SaveManager save, AllGameManagers managers)
        {
            bool started = false;
            try
            {
                while (original.MoveNext())
                {
                    if (!started) { started = true; Count(card, direct, save, managers); }
                    yield return original.Current;
                }
                if (!started) Count(card, direct, save, managers);
            }
            finally { (original as IDisposable)?.Dispose(); }
        }
        static void Count(CardState card, bool direct, SaveManager save, AllGameManagers managers)
        {
            try
            {
                var replay = managers?.GetReplayManager();
                if (card == null || card.IsAnyAbility() || save == null || save.PreviewMode
                    || save.GetRunType() == RunType.None || save.GetRunType() == RunType.EditorSaveGame
                    || save.UndoMode != UndoMode.None || replay == null || replay.IsPlayingBackAReplay()) return;
                Store?.Record(card.GetCardDataID(), save.GetRunType().ToString(), direct);
            }
            catch (Exception e)
            {
                // Stop persistence on errors rather than silently replacing or resetting a user's data.
                unavailable = true;
                Plugin.Logger.LogWarning("[CardUsage] Tracking stopped; existing file preserved: " + e);
            }
        }
        [HarmonyPatch(typeof(CardTooltipContainer), "AddTooltipsForCardState")]
        [HarmonyPostfix]
        static void AddUsage(CardTooltipContainer __instance, CardState cardState)
        {
            if (!Enabled || cardState == null || cardState.IsAnyAbility()) return;
            try
            {
                var current = Store;
                if (current == null) return;
                var all = current.Get(cardState.GetCardDataID());
                var normal = current.Get(cardState.GetCardDataID(), RunType.Class.ToString());
                var souls = current.Get(cardState.GetCardDataID(), RunType.RegionRun.ToString());
                string body = SoulSaviorText.Extra("Direct") + ": " + all.Direct
                    + "\n" + SoulSaviorText.Extra("Automatic") + ": " + all.Automatic
                    + "\n" + SoulSaviorText.Extra("Normal") + ": " + checked(normal.Direct + normal.Automatic)
                    + "\nSoul Savior: " + checked(souls.Direct + souls.Automatic)
                    + "\n\n" + SoulSaviorText.Extra("UsageNote");
                __instance.ShowTooltip(new TooltipContent(SoulSaviorText.Extra("Plays") + ": "
                    + checked(all.Direct + all.Automatic), body, TooltipDesigner.TooltipDesignType.DefaultWide,
                    "CustomClanUIFixes.CardUsage"), suppressRelayout: true);
            }
            catch (Exception e) { Plugin.Logger.LogWarning("[CardUsage] Tooltip unavailable: " + e.Message); }
        }
    }
}

// 2026-10-02-0039||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\CardUsageTracker.cs||Mejora 4: contar una vez al iniciar la resolución de cartas, excluir habilidades/preview/replay/undo reproducido y mostrar tooltip histórico

// 2026-10-02-0044||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\CardUsageTracker.cs||Guardar referencia al replay manager para exclusión segura y compilación sin aviso nullable

// 2026-10-02-0052||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\CardUsageTracker.cs||Subtotal normal usando RunType.Class real; excluir partidas sin modo y guardados de editor
