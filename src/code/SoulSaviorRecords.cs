using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace mt2_custom_clan_ui_fixes.Plugin
{
    // Session-only navigation: each view retains its own page, without save writes.
    internal sealed class SoulSaviorViewState
    {
        internal bool ViewingSouls { get; private set; } = true;
        internal int SoulPages { get; private set; }
        internal int ClanPages { get; private set; }
        int soulPage, clanPage;
        internal int PageCount => ViewingSouls ? SoulPages : ClanPages;
        internal int InitialPage => ViewingSouls ? soulPage : clanPage;
        internal int DisplayIndex(bool compact) => InitialPage + (compact && !ViewingSouls ? SoulPages : 0);
        internal void RememberDisplayed(int index, bool compact)
        {
            if (!compact) { Remember(index); return; }
            index = Clamp(index, SoulPages + ClanPages);
            ViewingSouls = index < SoulPages || ClanPages == 0;
            Remember(ViewingSouls ? index : index - SoulPages);
        }
        static int Clamp(int page, int count) => Math.Max(0, Math.Min(page, count - 1));
        internal void UpdateCounts(int souls, int clans)
        {
            SoulPages = Math.Max(0, souls);
            ClanPages = Math.Max(0, clans);
            soulPage = Clamp(soulPage, SoulPages);
            clanPage = Clamp(clanPage, ClanPages);
            if (PageCount == 0 && SoulPages + ClanPages > 0) ViewingSouls = !ViewingSouls;
        }
        internal void Remember(int page)
        {
            if (ViewingSouls) soulPage = Clamp(page, SoulPages);
            else clanPage = Clamp(page, ClanPages);
        }
        internal int SwitchView(bool souls, int outgoingPage)
        {
            Remember(outgoingPage);
            if ((souls ? SoulPages : ClanPages) > 0) ViewingSouls = souls;
            return InitialPage;
        }
    }

    // Contrato de lectura compatible con extraMetagameSave.json de ExpandedWinTracker.
    // No escribe ni modifica el guardado del tracker o del juego.
    [DataContract]
    public sealed class SoulSaviorRecord
    {
        [DataMember(Name = "classID")] public string? ClassId;
        [DataMember(Name = "subclassID")] public string? SubclassId;
        [DataMember(Name = "championIndex")] public int? ChampionIndex;
        [DataMember(Name = "highestGameLevel")] public int? HighestGameLevel;
    }

    [DataContract]
    public sealed class SoulSaviorSoulRecord
    {
        [DataMember(Name = "name")] public string? Name;
        [DataMember(Name = "tier")] public int? Tier;
        [DataMember(Name = "highestGameLevel")] public int? HighestGameLevel;
    }

    [DataContract]
    public sealed class SoulSaviorSave
    {
        [DataMember(Name = "soulSaviorClassWins")] public List<SoulSaviorRecord>? Wins;
        [DataMember(Name = "soulWins")] public List<SoulSaviorSoulRecord>? Souls;
    }

    public sealed class SoulSaviorRecords
    {
        readonly Dictionary<(string, string, int), int> wins = new();
        readonly Dictionary<(string Main, string Ally), int> pairBest = new();
        public int Count => wins.Count;
        readonly Dictionary<string, (int Tier, int Difficulty)> souls = new(StringComparer.Ordinal);
        public int SoulCount => souls.Count;
        // ExpandedWinTracker removes the final two characters of SoulData.name.
        public static string SoulKey(string assetName)
            => assetName.Length > 2 ? assetName.Substring(0, assetName.Length - 2) : assetName;
        public (int Tier, int Difficulty) Soul(string name)
            => souls.TryGetValue(name, out var record) ? record : (-1, -1);

        public static SoulSaviorRecords Read(Stream input)
        {
            var save = new DataContractJsonSerializer(typeof(SoulSaviorSave)).ReadObject(input)
                       as SoulSaviorSave;
            if (save == null) throw new InvalidDataException("Guardado vacio o no valido.");
            var result = new SoulSaviorRecords();
            if (save.Wins == null) throw new InvalidDataException("Falta soulSaviorClassWins.");
            foreach (var win in save.Wins)
            {
                if (win == null || string.IsNullOrWhiteSpace(win.ClassId)
                    || string.IsNullOrWhiteSpace(win.SubclassId)
                    || !win.ChampionIndex.HasValue || !win.HighestGameLevel.HasValue
                    || win.ChampionIndex.Value < 0 || win.HighestGameLevel.Value < 0) continue;
                var key = (win.ClassId!, win.SubclassId!, win.ChampionIndex.Value);
                if (!result.wins.TryGetValue(key, out int old) || old < win.HighestGameLevel.Value)
                    result.wins[key] = win.HighestGameLevel.Value;
            }
            // Older saves without soulWins still have valid clan records.
            if (save.Souls != null)
                foreach (var soul in save.Souls)
                {
                    if (soul == null || string.IsNullOrWhiteSpace(soul.Name)
                        || !soul.Tier.HasValue || !soul.HighestGameLevel.HasValue
                        || soul.Tier.Value < 0 || soul.HighestGameLevel.Value < 0) continue;
                    var old = result.Soul(soul.Name!);
                    // These maxima are independent in the tracker, not a paired achievement.
                    result.souls[soul.Name!] = (Math.Max(old.Tier, soul.Tier.Value),
                        Math.Max(old.Difficulty, soul.HighestGameLevel.Value));
                }
            foreach (var win in result.wins)
            {
                var pair = (win.Key.Item1, win.Key.Item2);
                if (!result.pairBest.TryGetValue(pair, out int old) || win.Value > old)
                    result.pairBest[pair] = win.Value;
            }
            return result;
        }

        public (int Won, int Possible, int ClansWon, int ClanCount, int Best) Overview(IEnumerable<string> clanIds)
        {
            var ids = new HashSet<string>(clanIds);
            ids.RemoveWhere(string.IsNullOrWhiteSpace);
            int won = 0, possible = 0, clansWon = 0, best = -1;
            foreach (var main in ids)
            {
                bool any = false;
                foreach (var ally in ids)
                {
                    if (main == ally) continue;
                    possible++;
                    int level = Best(main, ally);
                    if (level < 0) continue;
                    won++;
                    any = true;
                    best = Math.Max(best, level);
                }
                if (any) clansWon++;
            }
            return (won, possible, clansWon, ids.Count, best);
        }
        // -1 = sin victoria registrada; 0 tambien es una victoria valida.
        public int Get(string main, string ally, int champion)
            => wins.TryGetValue((main, ally, champion), out int level) ? level : -1;

        public int ChampionBest(string main, int champion, IEnumerable<string> allies)
        {
            int best = -1;
            foreach (var ally in new HashSet<string>(allies))
                if (ally != main) best = Math.Max(best, Get(main, ally, champion));
            return best;
        }

        public int Best(string main, string ally)
            => pairBest.TryGetValue((main, ally), out int best) ? best : -1;
    }
}
// 2026-09-30-1931||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorRecords.cs||Nueva integracion de lectura de ExpandedWinTracker y paginas Soul Savior por clan

// 2026-09-30-1937||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorRecords.cs||Integracion Soul Savior 0.4.0 completada; consultas validadas y presentacion separada de victorias normales

// 2026-09-30-2241||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorRecords.cs||Resumen por IDs distintos: parejas direccionales, sin duplicar campeones ni clanes y con ausencia distinta de victoria S0

// 2026-10-01-2324||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorRecords.cs||Leer soulWins con máximos independientes, duplicados, ausencia y claves compatibles con ExpandedWinTracker

// 2026-10-02-0031||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorRecords.cs||Mejora 3: máximo por campeón para los aliados instalados sin autoalianzas ni duplicados

// 2026-10-02-0735||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorRecords.cs||Borrador: estado de navegación con páginas independientes, memoria por vista y límites de catálogos vacíos

// 2026-10-02-0738||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorRecords.cs||Navegación compacta: convertir índices globales y conservar memoria de cada vista al redimensionar

// 2026-10-02-0914||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorRecords.cs||Cachear máximo por pareja al leer para evitar recorrer todos los registros al buscar y ordenar progreso
