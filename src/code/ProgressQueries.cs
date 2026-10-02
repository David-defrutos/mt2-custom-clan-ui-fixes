using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace mt2_custom_clan_ui_fixes.Plugin
{
    internal sealed class ProgressNavigator
    {
        internal readonly SoulSaviorViewState Legacy = new();
        internal bool Compact { get; private set; }
        internal bool ExplorerView { get; private set; }
        bool hasExplorer;
        int LegacyCount => Legacy.SoulPages + Legacy.ClanPages;
        internal int PageCount => Compact ? LegacyCount + (hasExplorer ? 1 : 0) : ExplorerView ? 1 : Legacy.PageCount;
        internal int InitialPage => ExplorerView ? (Compact ? LegacyCount : 0) : Legacy.DisplayIndex(Compact);
        internal int NativePageCount(int resultPages) => !Compact && ExplorerView ? Math.Max(1, resultPages) : PageCount;
        internal int NativePageIndex(int resultPage) => !Compact && ExplorerView ? Math.Max(0, resultPage) : InitialPage;
        internal void UpdateCounts(int souls, int clans, bool explorer)
        {
            Legacy.UpdateCounts(souls, clans); hasExplorer = explorer;
            if (!hasExplorer) ExplorerView = false;
            else if (LegacyCount == 0) ExplorerView = true;
        }
        internal void Remember(int index)
        {
            index = Math.Max(0, Math.Min(index, PageCount - 1));
            if (Compact)
            {
                ExplorerView = hasExplorer && index >= LegacyCount;
                if (!ExplorerView) Legacy.RememberDisplayed(index, true);
            }
            else if (!ExplorerView) Legacy.Remember(index);
        }
        internal int SwitchLegacy(bool souls, int outgoing)
        {
            Remember(outgoing);
            Legacy.SwitchView(souls, Legacy.InitialPage);
            ExplorerView = LegacyCount == 0 && hasExplorer;
            return InitialPage;
        }
        internal int SwitchExplorer(int outgoing)
        {
            Remember(outgoing);
            ExplorerView = hasExplorer;
            return InitialPage;
        }
        internal int Resize(bool compact, int outgoing)
        {
            Remember(outgoing); Compact = compact; return InitialPage;
        }
    }

    internal enum ProgressFilter { All, Missing, BelowGoal }
    internal enum ProgressSort { Name, Progress, Pending }
    internal enum UsageMode { All, Normal, SoulSavior, Other }
    internal sealed class ProgressIdentity
    {
        internal string Id = "", Name = "", Clan = "";
        internal bool HasSecondChampion = true;
    }
    internal sealed class ProgressResult
    {
        internal string Id = "", Name = "";
        internal long Direct, Automatic;
        internal int Done, Total, Best = -1, Tier = -1;
        internal string[] Pending = Array.Empty<string>();
        internal long Plays => checked(Direct + Automatic);
        internal double Ratio => Total == 0 ? 0 : (double)Done / Total;
    }
    // Queries only: filtering never changes a tracker save or invents historical usage.
    internal static class ProgressQueries
    {
        internal static bool Matches(string name, string query) => string.IsNullOrWhiteSpace(query)
            || CultureInfo.CurrentCulture.CompareInfo.IndexOf(name, query.Trim(),
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
        internal static bool Included(int difficulty, ProgressFilter filter, int goal) => filter == ProgressFilter.All
            || (filter == ProgressFilter.Missing ? difficulty < 0 : difficulty < goal);
        internal static bool ModeMatches(string mode, UsageMode filter) => filter == UsageMode.All
            || (filter == UsageMode.Normal ? mode == "Class" : filter == UsageMode.SoulSavior ? mode == "RegionRun"
                : mode != "Class" && mode != "RegionRun");
        internal static List<ProgressResult> Cards(IEnumerable<CardUsageEntry> entries,
            IEnumerable<ProgressIdentity> catalog, UsageMode mode, string? clan, string search)
        {
            var identities = catalog.GroupBy(c => c.Id, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            return entries.Where(e => ModeMatches(e.Mode!, mode)).GroupBy(e => e.CardId!, StringComparer.Ordinal)
                .Select(g => new ProgressResult { Id = g.Key,
                    Name = identities.TryGetValue(g.Key, out var info) ? info.Name : g.Key,
                    Direct = g.Aggregate(0L, (sum, e) => checked(sum + e.Direct)),
                    Automatic = g.Aggregate(0L, (sum, e) => checked(sum + e.Automatic)) })
                .Where(r => r.Plays > 0 && Matches(r.Name, search)
                    && (clan == null || (identities.TryGetValue(r.Id, out var info) && info.Clan == clan)))
                .OrderByDescending(r => r.Plays).ThenBy(r => r.Name, StringComparer.CurrentCulture)
                .ThenBy(r => r.Id, StringComparer.Ordinal).ToList();
        }
        internal static List<ProgressResult> Clans(SoulSaviorRecords records,
            IEnumerable<ProgressIdentity> catalog, int champion, int goal, string search, ProgressSort sort)
        {
            var clans = catalog.GroupBy(c => c.Id, StringComparer.Ordinal).Select(g => g.First()).ToList();
            var result = new List<ProgressResult>();
            foreach (var clan in clans)
            {
                if (champion == 1 && !clan.HasSecondChampion) continue;
                var levels = clans.Where(c => c.Id != clan.Id).Select(c => new {
                    Clan = c, Level = champion < 0 ? records.Best(clan.Id, c.Id) : records.Get(clan.Id, c.Id, champion)
                }).ToList();
                result.Add(new ProgressResult { Id = clan.Id, Name = clan.Name, Total = levels.Count,
                    Done = levels.Count(x => x.Level >= goal),
                    Best = levels.Count == 0 ? -1 : levels.Max(x => x.Level),
                    Pending = levels.Where(x => x.Level < goal).Select(x => x.Clan.Name).ToArray() });
            }
            // Sort progress as a ratio, so clans with different denominators stay comparable.
            var filtered = result.Where(c => Matches(c.Name, search));
            return sort == ProgressSort.Progress ? filtered.OrderByDescending(c => c.Ratio).ThenBy(c => c.Name, StringComparer.CurrentCulture).ToList()
                : sort == ProgressSort.Pending ? filtered.OrderByDescending(c => c.Pending.Length).ThenBy(c => c.Name, StringComparer.CurrentCulture).ToList()
                : filtered.OrderBy(c => c.Name, StringComparer.CurrentCulture).ToList();
        }
        internal static List<ProgressResult> Allies(SoulSaviorRecords records,
            IEnumerable<ProgressIdentity> catalog, string main, int champion, int goal, string search)
        {
            var clans = catalog.GroupBy(c => c.Id, StringComparer.Ordinal).Select(g => g.First()).ToList();
            var selected = clans.FirstOrDefault(c => c.Id == main);
            if (selected == null || (champion == 1 && !selected.HasSecondChampion)) return new List<ProgressResult>();
            return clans.Where(c => c.Id != main).Select(c => new ProgressResult {
                Id = c.Id, Name = c.Name,
                Best = champion < 0 ? records.Best(main, c.Id) : records.Get(main, c.Id, champion)
            }).Where(c => c.Best < goal && Matches(c.Name, search))
                .OrderBy(c => c.Name, StringComparer.CurrentCulture).ToList();
        }
        internal static List<ProgressResult> Souls(SoulSaviorRecords records,
            IEnumerable<ProgressIdentity> catalog, ProgressFilter filter, int goal, string search)
            => catalog.GroupBy(c => c.Id, StringComparer.Ordinal).Select(g => g.First()).Select(c => {
                var win = records.Soul(c.Id);
                return new ProgressResult { Id = c.Id, Name = c.Name, Best = win.Difficulty, Tier = win.Tier,
                    Done = win.Difficulty >= goal ? 1 : 0, Total = 1 };
            }).Where(c => Included(c.Best, filter, goal) && Matches(c.Name, search))
                .OrderBy(c => c.Name, StringComparer.CurrentCulture).ToList();
        internal static (int Done, int Total, double Ratio) Goal(IEnumerable<ProgressResult> rows)
        {
            int done = 0, total = 0;
            foreach (var row in rows) { done = checked(done + row.Done); total = checked(total + row.Total); }
            return (done, total, total == 0 ? 0 : (double)done / total);
        }
        internal static int PageCount(int count, int size) => Math.Max(1, (Math.Max(0, count) + size - 1) / size);
    }
}

// 2026-10-02-0854||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressQueries.cs||Consultas puras para ranking de cartas, objetivos, filtros de almas y pendientes por campeón; búsqueda y orden de clanes

// 2026-10-02-0917||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressQueries.cs||Consultar aliados pendientes del detalle con IDs únicos y sin inventar un segundo campeón

// 2026-10-02-0919||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressQueries.cs||Estado probado de navegación con tercera vista y conservación de memorias al cambiar ancho

// 2026-10-02-0926||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressQueries.cs||Exponer cómputo real de contador nativo de tablas para probar coherencia con vistas compactas
