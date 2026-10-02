using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using ShinyShoe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace mt2_custom_clan_ui_fixes.Plugin
{
    [HarmonyPatch]
    public static class LogbookSoulSavior
    {
        public static bool Enabled = true;
        internal static readonly string SavePath = Path.Combine(Paths.ConfigPath,
            "ExpandedWinTracker.Plugin", "extraMetagameSave.json");

        [HarmonyPatch(typeof(CompendiumSectionChecklist), "InitializeImpl")]
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void AddPages(CompendiumSectionChecklist __instance)
        {
            if (!Enabled) return;
            var pages = __instance.ChecklistPages;
            var target = __instance.GetComponentInParent<CompendiumScreen>()?.GetComponentInChildren<SoulSaviorSection>(true);
            if (target == null || target.Pages.Count != 0) return;
            var added = new List<ChecklistPage>();
            try
            {
                var save = AccessTools.Field(typeof(CompendiumSection), "saveManager")
                    .GetValue(__instance) as SaveManager;
                var source = pages.OfType<StandardChecklistPage>().FirstOrDefault();
                if (save == null || source == null) return;
                var layout = AccessTools.Field(typeof(StandardChecklistPage),
                    "clanSectionsLayoutAllClans").GetValue(source) as LayoutGroup;
                if (layout == null || !layout.gameObject.activeSelf)
                    layout = AccessTools.Field(typeof(StandardChecklistPage),
                        "clanSectionsLayoutStartingClans").GetValue(source) as LayoutGroup;
                var template = source.GetComponentsInChildren<ClanChecklistSection>(true)
                    .FirstOrDefault(s => s.transform.parent == layout?.transform);
                if (layout == null || template == null) return;

                var classes = save.GetBalanceData().GetClassDatas()
                    .Where(c => c != null && c.GetChampionData(0) != null
                        && (c.GetRequiredDlc() == DLC.None || save.IsDlcInstalled(c.GetRequiredDlc()))
                        && (!c.IsCrew() || save.GetMetagameSave()
                            .IsFeatureUnlocked(MetagameSaveData.UnlockedFeature.Crew))).ToList();
                var soulCatalog = save.GetAllGameData().GetAllSoulDatas()
                    .Where(s => s != null && !s.IsHidden
                        && (s.GetRequiredDLC() == DLC.None || save.IsDlcInstalled(s.GetRequiredDLC())))
                    .GroupBy(s => SoulSaviorRecords.SoulKey(s.name), StringComparer.Ordinal)
                    .Select(g => g.OrderBy(s => s.GetTierLevel()).First())
                    .OrderBy(s => s.GetName(), StringComparer.CurrentCulture).ToList();
                const int soulsPerPage = 15;
                for (int page = 0; page * soulsPerPage < soulCatalog.Count; page++)
                {
                    var obj = new GameObject("Soul Savior souls " + (page + 1), typeof(RectTransform));
                    obj.SetActive(false);
                    obj.transform.SetParent(target.transform, false);
                    CopyRect((RectTransform)source.transform, (RectTransform)obj.transform);
                    var view = obj.AddComponent<SoulSaviorSoulsPage>();
                    added.Add(view);
                    view.Build((RectTransform)layout.transform,
                        Field<TMP_Text>(template, "clanNameLabel")!,
                        soulCatalog.Skip(page * soulsPerPage).Take(soulsPerPage).ToList());
                }
                target.SoulCatalog.AddRange(soulCatalog);
                const int perPage = 5;
                int count = (classes.Count + perPage - 1) / perPage;
                for (int page = 0; page < count; page++)
                {
                    var obj = new GameObject("Soul Savior checklist " + (page + 1),
                        typeof(RectTransform));
                    obj.SetActive(false);
                    obj.transform.SetParent(target.transform, false);
                    CopyRect((RectTransform)source.transform, (RectTransform)obj.transform);
                    var view = obj.AddComponent<SoulSaviorChecklistPage>();
                    added.Add(view);
                    view.Build(save, template, (RectTransform)layout.transform,
                        classes.Skip(page * perPage).Take(perPage).ToList(), page + 1, count);
                }
                var explorerObject = new GameObject("Progress explorer", typeof(RectTransform));
                explorerObject.SetActive(false);
                explorerObject.transform.SetParent(target.transform, false);
                CopyRect((RectTransform)source.transform, (RectTransform)explorerObject.transform);
                var explorer = explorerObject.AddComponent<ProgressExplorerPage>();
                added.Add(explorer);
                explorer.Build(save, (RectTransform)layout.transform,
                    Field<TMP_Text>(template, "clanNameLabel")!, classes, soulCatalog);
                // Añadir solo cuando todas las paginas estan preparadas.
                foreach (var view in added) target.Pages.Add(view);
                target.BuildSidebar(classes, labelTemplate: LogbookSoulSavior.Field<TMP_Text>(template, "clanNameLabel")!);
                target.SetIcon(save);
                AccessTools.Method(typeof(PaginatedCompendiumSection), "RefreshPagination")
                    ?.Invoke(__instance, null);
                __instance.PageCountChangedSignal.Dispatch();
                Plugin.Logger.LogInfo("[SoulSavior] " + classes.Count + " clanes, "
                    + added.Count + " paginas; lectura de " + SavePath);
            }
            catch (Exception e)
            {
                foreach (var view in added)
                {
                    target.Pages.Remove(view);
                    if (view != null) UnityEngine.Object.Destroy(view.gameObject);
                }
                Plugin.Logger.LogWarning("[SoulSavior] No se pudieron crear las paginas: " + e);
            }
        }

        internal static void CopyRect(RectTransform from, RectTransform to)
        {
            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.sizeDelta = from.sizeDelta;
            to.anchoredPosition = from.anchoredPosition;
            to.localScale = from.localScale;
            to.localRotation = from.localRotation;
        }

        internal static T? Field<T>(object owner, string name) where T : class
            => AccessTools.Field(owner.GetType(), name)?.GetValue(owner) as T;
    }

    public sealed class SoulSaviorChecklistPage : ChecklistPage
    {
        SaveManager? save;
        readonly List<ClanChecklistSection> rows = new();
        readonly List<ClassData> classes = new();
        RectTransform? grid;
        TMP_Text? title;
        TMP_Text? note;
        int number, total;
        static readonly MethodInfo? Widen = AccessTools.Method(typeof(LogbookProgressGrid),
            "EnsancharLista");

        internal void Build(SaveManager manager, ClanChecklistSection template,
            RectTransform sourceLayout, List<ClassData> clans, int page, int count)
        {
            save = manager;
            classes.AddRange(clans);
            number = page;
            total = count;
            var obj = new GameObject("Soul Savior rows", typeof(RectTransform),
                typeof(GridLayoutGroup));
            obj.transform.SetParent(transform, false);
            grid = (RectTransform)obj.transform;
            LogbookSoulSavior.CopyRect(sourceLayout, grid);
            var layout = obj.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(sourceLayout.rect.width > 1f
                ? sourceLayout.rect.width : 1440f, 157f);
            layout.spacing = new Vector2(0f, 14f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 1;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.UpperLeft;

            var label = LogbookSoulSavior.Field<TMP_Text>(template, "clanNameLabel")!;
            title = Label(label, grid, "Soul Savior title", 0f, 10f, 1400f, 28f, 24f);
            note = Label(label, grid, "Soul Savior note", 0f, -860f, 1400f, 28f, 20f);
            title.gameObject.SetActive(false); // La pestaña identifica el modo; se conserva el alto para cinco filas.
            note.text = SoulSaviorText.Get(0);

            foreach (var clan in classes)
            {
                var row = UnityEngine.Object.Instantiate(template, grid, false);
                row.gameObject.name = "Soul Savior " + clan.GetTitle();
                var focus = row.gameObject.AddComponent<SoulSaviorClanFocus>();
                focus.Clan = clan;
                var rowButton = row.gameObject.GetComponent<GameUISelectableButton>()
                    ?? row.gameObject.AddComponent<GameUISelectableButton>();
                rowButton.targetGraphic = LogbookSoulSavior.Field<Image>(row, "clanIcon");
                rowButton.SetNavigation(new Navigation { mode = Navigation.Mode.Automatic });
                // Normalizar SOLO la copia: puede heredar aliados ya rebalanceados.
                var top = LogbookSoulSavior.Field<LayoutGroup>(row, "subclanVictoryLayout")!;
                foreach (var flag in row.GetComponentsInChildren<SubclanVictoryItem>(true))
                    flag.transform.SetParent(top.transform, false);
                rows.Add(row);
                AccessTools.Field(typeof(ClanChecklistSection), "includeDlcContent")
                    .SetValue(row, true);
                row.gameObject.SetActive(true);
            }
        }

        public override IGameUIComponent? GetDefaultGameUISelectable()
            => rows.SelectMany(r => r.GetComponentsInChildren<SubclanVictoryItem>())
                .Where(f => f.gameObject.activeInHierarchy).Select(f => f.GetComponent<IGameUIComponent>()).FirstOrDefault()
                ?? rows.FirstOrDefault()?.GetComponent<GameUISelectableButton>();

        public override void Open()
        {
            base.Open();
            try
            {
                RefreshRecords();
                GetComponentInParent<SoulSaviorSection>()?.EnsureSelectedClan(classes);
                Adjust();
                StartCoroutine(Settle());
            }
            catch (Exception e)
            {
                foreach (var row in rows) row.gameObject.SetActive(false);
                if (note != null) note.text = SoulSaviorText.Get(3);
                Plugin.Logger.LogWarning("[SoulSavior] Error al abrir la pagina: " + e);
            }
        }

        void RefreshRecords()
        {
            if (save == null) return;
            SoulSaviorRecords records;
            try
            {
                using var input = File.Open(LogbookSoulSavior.SavePath,
                    FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                records = SoulSaviorRecords.Read(input);
            }
            catch (Exception e)
            {
                foreach (var row in rows) row.gameObject.SetActive(false);
                if (note != null) note.text = SoulSaviorText.Get(2);
                Plugin.Logger.LogWarning("[SoulSavior] Guardado no disponible: " + e.Message);
                return;
            }
            if (note != null) note.text = records.Count == 0
                ? SoulSaviorText.Get(1)
                : SoulSaviorText.Get(0);

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                row.Set(classes[i], save);
                foreach (var flag in row.GetComponentsInChildren<SubclanVictoryItem>(true))
                {
                    if (!flag.gameObject.activeSelf || flag.data == null) continue;
                    var main = flag.data.mainClassData;
                    var ally = flag.data.subClassData;
                    var data = new SubclanVictoryItem.Data
                    {
                        mainClassData = main,
                        subClassData = ally,
                        covenantLevels = new List<MetagameSaveData.ClassCombinationWinData>()
                    };
                    for (int champion = 0; champion < 2; champion++)
                        data.covenantLevels.Add(new MetagameSaveData.ClassCombinationWinData
                        {
                            highestAscensionLevel = records.Get(main.GetID(), ally.GetID(), champion)
                        });
                    flag.Set(data, 0);
                    ShowFlag(flag, records);
                }
                row.ReparentCrewVictoryItems();
                // La maestria y nivel normales no representan victorias de Soul Savior.
                var level = LogbookSoulSavior.Field<LevelIndicator>(row, "levelIndicator");
                if (level != null) level.gameObject.SetActive(false);
                var portraits = LogbookSoulSavior.Field<List<ClanChecklistChampionUI>>(row,
                    "championPortraits");
                if (portraits != null)
                    foreach (var portrait in portraits) portrait.SetMastered(false);
                ShowSummary(row, classes[i], records);
                if (GetComponentInParent<SoulSaviorSection>()?.ShowPendingOnly == true)
                    foreach (var flag in row.GetComponentsInChildren<SubclanVictoryItem>())
                        if (flag.data != null && records.Best(classes[i].GetID(), flag.data.subClassData.GetID()) >= 0)
                            flag.gameObject.SetActive(false);
            }
        }

        static void ShowFlag(SubclanVictoryItem flag, SoulSaviorRecords records)
        {
            var data = flag.data;
            int best = records.Best(data.mainClassData.GetID(), data.subClassData.GetID());
            var crowns = LogbookSoulSavior.Field<List<GameObjectSwapper>>(flag, "maxVictoryCrowns");
            if (crowns != null)
                foreach (var crown in crowns) crown.gameObject.SetActive(false);
            var sheen = LogbookSoulSavior.Field<Animator>(flag, "bannerSheen");
            if (sheen != null) sheen.gameObject.SetActive(false);
            var icon = LogbookSoulSavior.Field<Image>(flag, "covenantIcon");
            if (icon != null) icon.enabled = false;
            var label = LogbookSoulSavior.Field<TMP_Text>(flag, "covenantLevelLabel");
            if (label != null)
            {
                label.enabled = true;
                label.text = best < 0 ? "—" : "S" + best;
            }
            var body = new System.Text.StringBuilder();
            for (int champion = 0; champion < 2; champion++)
            {
                var card = data.mainClassData.GetChampionData(champion)?.championCardData;
                if (card == null) continue;
                int level = records.Get(data.mainClassData.GetID(), data.subClassData.GetID(), champion);
                body.Append(card.GetName()).Append(": ")
                    .Append(level < 0 ? SoulSaviorText.Get(4) : SoulSaviorText.Get(5, level))
                    .AppendLine();
            }
            var tooltip = LogbookSoulSavior.Field<TooltipProviderComponent>(flag, "tooltipProvider");
            if (tooltip != null) tooltip.SetTooltipLocalized(
                "Soul Savior · " + data.mainClassData.GetTitle() + " / " + data.subClassData.GetTitle(),
                body.ToString(), TooltipDesigner.TooltipDesignType.DefaultWide);
        }

        static void ShowSummary(ClanChecklistSection row, ClassData main, SoulSaviorRecords records)
        {
            var meter = LogbookSoulSavior.Field<CardMasteryMeter>(row, "cardMasteryMeter");
            if (meter == null) return;
            foreach (Transform child in meter.transform) child.gameObject.SetActive(false);
            var layout = meter.GetComponent<GridLayoutGroup>();
            if (layout != null) layout.enabled = false;
            var old = meter.transform.Find("Soul Savior summary");
            TMP_Text label;
            if (old != null) { old.gameObject.SetActive(true); label = old.GetComponent<TMP_Text>(); }
            else label = Label(LogbookSoulSavior.Field<TMP_Text>(row, "clanNameLabel")!,
                meter.transform, "Soul Savior summary", 0f, -10f, 190f, 105f, 22f);
            var flags = row.GetComponentsInChildren<SubclanVictoryItem>(true)
                .Where(f => f.gameObject.activeSelf && f.data != null).ToList();
            int won = flags.Count(f => records.Best(main.GetID(), f.data.subClassData.GetID()) >= 0);
            label.text = SoulSaviorText.Get(6, won, flags.Count);
        }

        internal static TMP_Text Label(TMP_Text template, Transform parent, string name,
            float x, float y, float width, float height, float size)
        {
            // Un objeto nuevo conserva la fuente y el material, sin heredar el
            // localizador I2 del nombre del clan que reescribia el titulo a "Banished".
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            TMP_Text label = obj.GetComponent<TextMeshProUGUI>();
            label.font = template.font;
            label.fontSharedMaterial = template.fontSharedMaterial;
            label.color = template.color;
            label.name = name;
            label.gameObject.SetActive(true);
            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(x, y);
            rt.localScale = Vector3.one;
            var element = label.GetComponent<LayoutElement>()
                ?? label.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;
            label.enableAutoSizing = false;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.raycastTarget = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        void Adjust()
        {
            if (grid == null) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(grid);
            Widen?.Invoke(null, new object[]
            {
                rows.Cast<Component>().ToList(), grid.rect.width
            });
            LayoutRebuilder.ForceRebuildLayoutImmediate(grid);
        }

        IEnumerator Settle()
        {
            for (int frame = 0; frame < 5; frame++)
            {
                yield return null;
                if (!isActiveAndEnabled) yield break;
                Adjust();
            }
        }
    }
    public sealed class SoulSaviorFocusRelay : MonoBehaviour
    {
        void OnEnable() => UISignals.GameUITriggered.AddListener(Focus);
        void OnDisable() => UISignals.GameUITriggered.RemoveListener(Focus);
        void Focus(CoreInputControlMapping mapping, IGameUIComponent target)
        {
            var row = target?.component?.GetComponentInParent<SoulSaviorClanFocus>();
            if (row != null && row.isActiveAndEnabled && row.GetComponentInParent<SoulSaviorSection>() == GetComponent<SoulSaviorSection>())
                GetComponent<SoulSaviorSection>().SelectClan(row.Clan);
        }
    }

    public sealed class SoulSaviorClanFocus : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        internal ClassData? Clan;
        void Focus() => GetComponentInParent<SoulSaviorSection>()?.SelectClan(Clan);
        public void OnPointerEnter(PointerEventData data) => Focus();
        public void OnSelect(BaseEventData data) => Focus();
    }

    public sealed class SoulSaviorSoulsPage : ChecklistPage
    {
        readonly List<(SoulData Soul, Image Icon, TMP_Text Name, TMP_Text Record, GameUISelectableButton Button, TooltipProviderComponent Tooltip)> items = new();
        RectTransform? grid;
        GridLayoutGroup? layout;
        TMP_Text? heading;
        TMP_Text? note;
        float measuredSoulWidth = -1f;

        internal void Build(RectTransform sourceLayout, TMP_Text template, List<SoulData> souls)
        {
            var obj = new GameObject("Soul Savior rows", typeof(RectTransform), typeof(GridLayoutGroup));
            obj.transform.SetParent(transform, false);
            grid = (RectTransform)obj.transform;
            LogbookSoulSavior.CopyRect(sourceLayout, grid);
            grid.anchoredPosition += new Vector2(0f, -48f);
            layout = obj.GetComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.spacing = new Vector2(16f, 14f);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            heading = SoulSaviorChecklistPage.Label(template, grid, "Soul catalog heading",
                0f, 48f, 1400f, 42f, 30f);
            note = SoulSaviorChecklistPage.Label(template, grid, "Soul catalog note",
                0f, -755f, 1400f, 62f, 20f);
            LogbookProgressTheme.Ink(heading, true);
            LogbookProgressTheme.Ink(note);
            foreach (var soul in souls)
            {
                var row = new GameObject("Soul " + soul.name, typeof(RectTransform), typeof(Image));
                row.transform.SetParent(grid, false);
                var background = row.GetComponent<Image>();
                background.color = new Color(0.10f, 0.08f, 0.04f, 0.16f);
                background.raycastTarget = true;
                var button = row.AddComponent<GameUISelectableButton>();
                button.targetGraphic = background;
                button.SetNavigation(new Navigation { mode = Navigation.Mode.Automatic });
                var tooltip = row.AddComponent<TooltipProviderComponent>();
                var iconObject = new GameObject("Soul icon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(row.transform, false);
                var icon = iconObject.GetComponent<Image>();
                icon.sprite = soul.GetIcon();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var rect = icon.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(12f, -16f);
                rect.sizeDelta = new Vector2(60f, 60f);
                var name = SoulSaviorChecklistPage.Label(template, row.transform, "Soul name",
                    86f, -10f, 330f, 64f, 26f);
                name.enableAutoSizing = true;
                name.fontSizeMin = 18f;
                name.fontSizeMax = 26f;
                var record = SoulSaviorChecklistPage.Label(template, row.transform, "Soul record",
                    86f, -77f, 330f, 52f, 20f);
                record.enableAutoSizing = true;
                record.fontSizeMin = 14f;
                record.fontSizeMax = 20f;
                LogbookProgressTheme.Button(button, name, true);
                name.alignment = TextAlignmentOptions.MidlineLeft;
                LogbookProgressTheme.Ink(name, true);
                LogbookProgressTheme.Ink(record);
                LogbookProgressTheme.Status(button.transform, false);
                items.Add((soul, icon, name, record, button, tooltip));
            }
            Resize();
        }

        void Resize()
        {
            if (grid == null || layout == null) return;
            if (Math.Abs(grid.rect.width - measuredSoulWidth) < 0.5f) return;
            measuredSoulWidth = grid.rect.width;
            float width = Mathf.Max(120f, (grid.rect.width - 32f) / 3f);
            layout.cellSize = new Vector2(width, 136f);
            foreach (var item in items)
            {
                item.Name.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - 98f);
                item.Record.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - 98f);
            }
            if (heading != null) heading.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, grid.rect.width);
            if (note != null) note.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, grid.rect.width);
        }
        public override IGameUIComponent? GetDefaultGameUISelectable() => items.FirstOrDefault().Button;
        void LateUpdate() => Resize();

        public override void Open()
        {
            base.Open();
            if (heading != null) heading.text = "Soul Savior · " + SoulSaviorText.Get(13);
            if (note != null) note.text = SoulSaviorText.Get(15);
            try
            {
                using var input = File.Open(LogbookSoulSavior.SavePath, FileMode.Open,
                    FileAccess.Read, FileShare.ReadWrite);
                var records = SoulSaviorRecords.Read(input);
                foreach (var item in items)
                {
                    item.Name.text = item.Soul.GetName();
                    var win = records.Soul(SoulSaviorRecords.SoulKey(item.Soul.name));
                    item.Record.text = win.Difficulty < 0 ? SoulSaviorText.Get(4)
                        : SoulSaviorText.Get(14, win.Tier, "S" + win.Difficulty);
                    item.Tooltip.SetTooltipLocalized(item.Soul.GetName(), item.Soul.GetDescription()
                        + "\n\n" + item.Record.text, TooltipDesigner.TooltipDesignType.DefaultWide);
                    LogbookProgressTheme.Status(item.Button.transform, win.Difficulty >= 0);
                    item.Icon.color = win.Difficulty < 0 ? new Color(0.55f, 0.55f, 0.55f, 0.75f) : Color.white;
                }
            }
            catch (Exception e)
            {
                // Never leave results from the last successful read visible after a read error.
                foreach (var item in items)
                {
                    item.Name.text = item.Soul.GetName();
                    item.Record.text = "—";
                    item.Icon.color = Color.gray;
                    LogbookProgressTheme.Status(item.Button.transform, false);
                    item.Tooltip.SetTooltipLocalized(item.Soul.GetName(), SoulSaviorText.Get(2),
                        TooltipDesigner.TooltipDesignType.DefaultWide);
                }
                if (note != null) note.text = SoulSaviorText.Get(2);
                Plugin.Logger.LogWarning("[SoulSavior] Almas no disponibles: " + e.Message);
            }
            Resize();
        }
    }

    public sealed class SoulSaviorSection : PaginatedCompendiumSection
    {
        internal readonly List<ChecklistPage> Pages = new();
        internal readonly List<SoulData> SoulCatalog = new();
        internal CompendiumTab? Tab;
        readonly List<ClassData> sidebarClasses = new();
        internal ClassData? SelectedClan;
        internal int PendingPage;
        internal int PendingPageCount = 1;
        RectTransform? sidebar;
        TMP_Text? sidebarBody;
        RectTransform? sidebarDetail;
        float measuredSidebarWidth = -1f, measuredSidebarHeight = -1f;
        TMP_Text? sidebarLegend;
        TMP_Text? sidebarCredit;
        GameUISelectableButton? nextPending;
        GameUISelectableButton? pendingFilter;
        TMP_Text? pendingFilterLabel;
        internal bool ShowPendingOnly;
        TMP_Text? nextPendingLabel;
        TMP_Text? fontReference;
        string sidebarLanguage = "";
        readonly ProgressNavigator navigator = new();
        SoulSaviorViewState viewState => navigator.Legacy;
        bool compactNavigation => navigator.Compact;
        bool explorerView => navigator.ExplorerView;
        GameUISelectableButton? soulsViewButton, clansViewButton, explorerViewButton;
        TMP_Text? soulsViewLabel, clansViewLabel, explorerViewLabel;
        List<ChecklistPage> CurrentPages => compactNavigation ? Pages : explorerView
            ? Pages.Where(p => p is ProgressExplorerPage).ToList()
            : Pages.Where(p => !(p is ProgressExplorerPage) && (p is SoulSaviorSoulsPage) == viewState.ViewingSouls).ToList();
        ProgressExplorerPage? Explorer => Pages.OfType<ProgressExplorerPage>().FirstOrDefault();
        protected override int PageCount => navigator.NativePageCount(Explorer?.ResultPageCount ?? 1);
        internal void RefreshExplorerPagination()
        {
            if (!explorerView || compactNavigation) return;
            currentPageIndex = navigator.NativePageIndex(Explorer?.ResultPage ?? 0);
            base.RefreshPage();
            PageCountChangedSignal.Dispatch();
        }
        public override void TurnPage(int direction)
        {
            if (explorerView && !compactNavigation) { Explorer?.AdvancePage(direction); return; }
            base.TurnPage(direction);
        }
        void UpdateViewCounts() => navigator.UpdateCounts(Pages.Count(p => p is SoulSaviorSoulsPage),
            Pages.Count(p => p is SoulSaviorChecklistPage), Pages.Any(p => p is ProgressExplorerPage));
        protected override int GetInitialPage()
        {
            UpdateViewCounts();
            return navigator.InitialPage;
        }
        void SwitchView(bool souls)
        {
            UpdateViewCounts();
            currentPageIndex = navigator.SwitchLegacy(souls, currentPageIndex);
            RefreshPage();
            PageCountChangedSignal.Dispatch();
        }
        void SwitchExplorer()
        {
            currentPageIndex = navigator.SwitchExplorer(currentPageIndex);
            RefreshPage();
            PageCountChangedSignal.Dispatch();
        }
        public override bool ApplyScreenInput(CoreInputControlMapping mapping, IGameUIComponent? target, InputManager.Controls control)
        {
            var explorer = GetComponentInChildren<ProgressExplorerPage>();
            if (explorer != null && explorer.ApplyInput(mapping, target, control)) return true;
            foreach (var dispatch in GetComponentsInChildren<ProgressButtonAction>())
            {
                var button = dispatch.GetComponent<GameUISelectableButton>();
                if (button != null && button.TryTrigger(mapping, target!, control))
                { dispatch.Activate(); return true; }
            }
            return base.ApplyScreenInput(mapping, target, control);
        }
        protected override void InitializeImpl() { }
        internal void Configure(CompendiumSectionChecklist source)
        {
            _section = (CompendiumScreen.Section)8;
            mainNavLayer = gameObject.AddComponent<BaseNavigationLayer>();
            gameObject.AddComponent<SoulSaviorFocusRelay>();
            pageCountLabel = (TextMeshProUGUI)AccessTools.Field(typeof(PaginatedCompendiumSection),
                "pageCountLabel").GetValue(source);
            pageCountKey = (string)AccessTools.Field(typeof(PaginatedCompendiumSection),
                "pageCountKey").GetValue(source);
        }
        protected override void RefreshPage()
        {
            UpdateViewCounts();
            var visible = CurrentPages;
            currentPageIndex = Mathf.Clamp(currentPageIndex, 0, Math.Max(0, visible.Count - 1));
            var selectedPage = visible.Count == 0 ? null : visible[currentPageIndex];
            navigator.Remember(currentPageIndex);
            base.RefreshPage();
            foreach (var page in Pages) page.Toggle(page == selectedPage);
            mainNavLayer.SetDefaultGameSelectable(GetDefaultGameUISelectable());
            if (fontReference != null)
                foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                {
                    var ink = text.GetComponent<LogbookInkLabel>();
                    bool changed = text.font != fontReference.font;
                    if (changed) text.font = fontReference.font;
                    if (ink == null || changed) text.fontSharedMaterial = fontReference.fontSharedMaterial;
                    if (ink != null && changed) LogbookProgressTheme.Ink(text, ink.Heading);
                }
            RefreshSidebar();
            RefreshTabTooltip();
        }
        public override IGameUIComponent? GetDefaultGameUISelectable()
        {
            var visible = CurrentPages;
            return visible.Count == 0 ? null : visible[Mathf.Clamp(currentPageIndex, 0, visible.Count - 1)]
                .GetDefaultGameUISelectable();
        }
        internal void SetIcon(SaveManager manager)
        {
            if (Tab == null) return;
            var soul = Resources.FindObjectsOfTypeAll<SoulData>()
                .Where(s => s.GetSoulTypeIcon() != null)
                .OrderBy(s => s.GetSoulType()).ThenBy(s => s.GetID()).FirstOrDefault();
            var icons = Tab.GetComponentsInChildren<Image>(true)
                .Where(i => i.name.Equals("Icon", StringComparison.OrdinalIgnoreCase)
                    || i.name.Equals("IconSelected", StringComparison.OrdinalIgnoreCase)).ToList();
            if (icons.Count == 0)
            {
                var fallback = Tab.GetComponentsInChildren<Image>(true)
                    .FirstOrDefault(i => i.name.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0);
                if (fallback != null) icons.Add(fallback);
            }
            foreach (var icon in icons)
            {
                if (soul != null) icon.sprite = soul.GetSoulTypeIcon();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                bool selected = icon.name.IndexOf("selected", StringComparison.OrdinalIgnoreCase) >= 0;
                float size = selected ? 48f : 42f;
                icon.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                icon.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
                icon.rectTransform.localScale = Vector3.one;
                icon.color = selected ? Color.white : new Color(0.72f, 0.82f, 0.86f, 1f);
            }
            RefreshTabTooltip();
        }

        void RefreshTabTooltip()
        {
            var tooltip = Tab?.GetComponentInChildren<TooltipProviderComponent>(true);
            if (tooltip != null) tooltip.SetTooltipLocalized("Soul Savior",
                SoulSaviorText.Get(7), TooltipDesigner.TooltipDesignType.DefaultWide);
        }

        internal void BuildSidebar(List<ClassData> clans, TMP_Text labelTemplate)
        {
            UpdateViewCounts();
            sidebarClasses.AddRange(clans);
            fontReference = labelTemplate;
            var obj = new GameObject("Soul Savior overview", typeof(RectTransform));
            obj.transform.SetParent(transform, false);
            sidebar = (RectTransform)obj.transform;
            sidebar.anchorMin = sidebar.anchorMax = new Vector2(0.5f, 0.5f);
            sidebar.pivot = new Vector2(0f, 1f);
            sidebar.sizeDelta = new Vector2(340f, 720f);
            var title = SoulSaviorChecklistPage.Label(labelTemplate, sidebar,
                "Soul Savior overview title", 0f, 0f, 340f, 44f, 32f);
            title.text = "Soul Savior"; title.alignment = TextAlignmentOptions.Center;
            LogbookProgressTheme.Ink(title, true);
            LogbookProgressTheme.Divider(sidebar, -43f);
            var detail = new GameObject("Soul Savior detail frame", typeof(RectTransform), typeof(Image));
            detail.transform.SetParent(sidebar, false);
            sidebarDetail = (RectTransform)detail.transform;
            sidebarDetail.anchorMin = new Vector2(0f,1f); sidebarDetail.anchorMax = Vector2.one;
            sidebarDetail.pivot = new Vector2(0.5f,1f); sidebarDetail.anchoredPosition = new Vector2(0f,-98f);
            sidebarDetail.sizeDelta = new Vector2(0f,440f);
            detail.GetComponent<Image>().color = new Color(0.66f,0.48f,0.25f,0.10f);
            detail.GetComponent<Image>().raycastTarget = false;
            LogbookProgressTheme.Frame(detail.transform);
            soulsViewButton = SidebarButton(labelTemplate, "Souls view", -50f,
                () => SwitchView(true), out soulsViewLabel);
            clansViewButton = SidebarButton(labelTemplate, "Clans view", -50f,
                () => SwitchView(false), out clansViewLabel);
            explorerViewButton = SidebarButton(labelTemplate, "Progress view", -50f, SwitchExplorer, out explorerViewLabel);
            LayoutViewButton(soulsViewButton, soulsViewLabel, 0f, 108f);
            LayoutViewButton(clansViewButton, clansViewLabel, 116f, 108f);
            LayoutViewButton(explorerViewButton, explorerViewLabel, 232f, 108f);
            sidebarBody = SoulSaviorChecklistPage.Label(labelTemplate, sidebar,
                "Soul Savior overview totals", 14f, -110f, 312f, 416f, 22f);
            LogbookProgressTheme.Ink(sidebarBody);
            sidebarBody.enableAutoSizing = true;
            sidebarBody.fontSizeMin = 14f;
            sidebarBody.fontSizeMax = 22f;
            sidebarBody.gameObject.AddComponent<GameUISelectable>();
            sidebarBody.gameObject.AddComponent<TooltipProviderComponent>();
            sidebarBody.raycastTarget = true;
            nextPending = SidebarButton(labelTemplate, "Next pending allies", -545f, () =>
            {
                PendingPage = (PendingPage + 1) % PendingPageCount;
                RefreshSidebar();
            }, out nextPendingLabel);
            pendingFilter = SidebarButton(labelTemplate, "Pending combination filter", -595f, () =>
            {
                ShowPendingOnly = !ShowPendingOnly;
                RefreshPage();
            }, out pendingFilterLabel);
            sidebarLegend = SoulSaviorChecklistPage.Label(labelTemplate, sidebar,
                "Soul Savior overview legend", 0f, -460f, 340f, 170f, 20f);
            sidebarCredit = SoulSaviorChecklistPage.Label(labelTemplate, sidebar,
                "Soul Savior overview credit", 0f, -665f, 340f, 80f, 16f);
            LogbookProgressTheme.Ink(sidebarLegend);
            LogbookProgressTheme.Ink(sidebarCredit);
            LogbookProgressTheme.Divider(sidebarCredit.transform, 10f);
        }

        GameUISelectableButton SidebarButton(TMP_Text template, string name, float y,
            UnityEngine.Events.UnityAction action, out TMP_Text caption)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(GameUISelectableButton));
            obj.transform.SetParent(sidebar, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(340f, 44f);
            var button = obj.GetComponent<GameUISelectableButton>();
            button.targetGraphic = obj.GetComponent<Image>();
            button.targetGraphic.color = new Color(0.45f, 0.33f, 0.18f, 0.35f);
            button.SetNavigation(new Navigation { mode = Navigation.Mode.Automatic });
            var dispatch = obj.AddComponent<ProgressButtonAction>();
            dispatch.Action = () => action();
            caption = SoulSaviorChecklistPage.Label(template, rect, name + " text", 8f, -5f, 324f, 34f, 22f);
            caption.enableAutoSizing = true;
            caption.fontSizeMin = 16f;
            caption.fontSizeMax = 22f;
            LogbookProgressTheme.Button(button, caption);
            return button;
        }

        internal void EnsureSelectedClan(List<ClassData> visible)
        {
            if (SelectedClan == null || !visible.Contains(SelectedClan))
                SelectClan(visible.FirstOrDefault());
        }
        internal void SelectClan(ClassData? clan)
        {
            if (SelectedClan == clan) return;
            SelectedClan = clan;
            PendingPage = 0;
            RefreshSidebar();
        }

        void RefreshSidebar()
        {
            sidebarLanguage = I2.Loc.LocalizationManager.CurrentLanguageCode;
            measuredSidebarWidth = -1f; // Reflow when buttons become visible or their captions change.
            if (sidebarBody == null) return;
            if (soulsViewLabel != null) soulsViewLabel.text = SoulSaviorText.Get(13);
            if (clansViewLabel != null) clansViewLabel.text = SoulSaviorText.Extra("Clans");
            if (soulsViewButton != null) soulsViewButton.interactable = viewState.SoulPages > 0;
            if (clansViewButton != null) clansViewButton.interactable = viewState.ClanPages > 0;
            if (sidebarLegend != null) sidebarLegend.text = SoulSaviorText.Get(
                viewState.ViewingSouls ? 15 : 11);
            if (sidebarCredit != null) sidebarCredit.text = SoulSaviorText.Get(12);
            if (explorerViewLabel != null) explorerViewLabel.text = ProgressText.Get("Progress");
            LogbookProgressTheme.Select(soulsViewButton, !explorerView && viewState.ViewingSouls);
            LogbookProgressTheme.Select(clansViewButton, !explorerView && !viewState.ViewingSouls);
            LogbookProgressTheme.Select(explorerViewButton, explorerView);
            LogbookProgressTheme.Select(pendingFilter, ShowPendingOnly);
            if (explorerView)
            {
                if (pendingFilter != null) pendingFilter.gameObject.SetActive(false);
                if (nextPending != null) nextPending.gameObject.SetActive(false);
                sidebarBody.text = ProgressText.Get("Progress") + "\n\n" + ProgressText.Get("Cards") + "\n"
                    + (CardUsageTracker.Store?.Snapshot().Select(e => e.CardId).Distinct().Count().ToString() ?? "—")
                    + "\n\n" + SoulSaviorText.Extra("Clans") + "\n" + sidebarClasses.Count
                    + "\n\n" + SoulSaviorText.Get(13) + "\n" + SoulCatalog.Count;
                if (sidebarLegend != null) sidebarLegend.text = SoulSaviorText.Extra("UsageNote");
                sidebarBody.GetComponent<TooltipProviderComponent>()?.SetTooltipLocalized(ProgressText.Get("Progress"),
                    sidebarBody.text, TooltipDesigner.TooltipDesignType.DefaultWide);
                return;
            }

            try
            {
                using var input = File.Open(LogbookSoulSavior.SavePath,
                    FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var records = SoulSaviorRecords.Read(input);
                var (won, possible, clansWon, clanCount, best) = records.Overview(sidebarClasses.Select(c => c.GetID()));
                bool soulView = viewState.ViewingSouls;
                if (pendingFilter != null) pendingFilter.gameObject.SetActive(!soulView);
                if (pendingFilterLabel != null) pendingFilterLabel.text = SoulSaviorText.Extra(ShowPendingOnly ? "Missing" : "All");
                if (soulView && nextPending != null) nextPending.gameObject.SetActive(false);
                int soulWon = SoulCatalog.Count(s => records.Soul(SoulSaviorRecords.SoulKey(s.name)).Difficulty >= 0);
                sidebarBody.text = soulView
                    ? SoulSaviorText.Get(16) + "\n<size=38>" + soulWon + " / " + SoulCatalog.Count + "</size>"
                    : SoulSaviorText.Get(8) + "\n<size=38>" + won + " / " + possible
                    + "</size>\n\n" + SoulSaviorText.Get(9) + "\n<size=34>" + clansWon + " / "
                    + clanCount + "</size>\n\n" + SoulSaviorText.Get(10)
                    + "\n<size=34>" + (best < 0 ? "—" : "S" + best) + "</size>";
                if (!soulView && SelectedClan != null)
                {
                    var allies = sidebarClasses.Where(c => c.GetID() != SelectedClan.GetID()).ToList();
                    var pending = allies.Where(c => records.Best(SelectedClan.GetID(), c.GetID()) < 0).ToList();
                    PendingPageCount = Math.Max(1, (pending.Count + 7) / 8);
                    PendingPage = Mathf.Clamp(PendingPage, 0, PendingPageCount - 1);
                    var body = new System.Text.StringBuilder();
                    body.Append("<color=#611C13><size=28>").Append(SelectedClan.GetTitle()).Append("</size></color>\n")
                        .Append(allies.Count - pending.Count).Append(" / ").Append(allies.Count).Append("\n\n");
                    for (int champion = 0; champion < 2; champion++)
                    {
                        var card = SelectedClan.GetChampionData(champion)?.championCardData;
                        if (card == null) continue;
                        int level = records.ChampionBest(SelectedClan.GetID(), champion, allies.Select(c => c.GetID()));
                        body.Append(card.GetName()).Append(": ").Append(level < 0 ? "—" : "S" + level).Append("\n");
                    }
                    body.Append("\n").Append(SoulSaviorText.Extra("Pending")).Append(": ").Append(pending.Count)
                        .Append("\n");
                    foreach (var ally in pending.Skip(PendingPage * 8).Take(8))
                        body.Append("• ").Append(ally.GetTitle()).Append("\n");
                    if (pending.Count == 0) body.Append(SoulSaviorText.Extra("Complete"));
                    sidebarBody.text = body.ToString();
                    if (nextPending != null) nextPending.gameObject.SetActive(PendingPageCount > 1);
                    if (nextPendingLabel != null) nextPendingLabel.text = SoulSaviorText.Extra("Next")
                        + " (" + (PendingPage + 1) + "/" + PendingPageCount + ")";
                }

                var panelTooltip = sidebarBody.GetComponent<TooltipProviderComponent>();
                if (panelTooltip != null) panelTooltip.SetTooltipLocalized("Soul Savior",
                    sidebarBody.text, TooltipDesigner.TooltipDesignType.DefaultWide);
            }
            catch (Exception e)
            {
                sidebarBody.text = SoulSaviorText.Get(2);
                if (nextPending != null) nextPending.gameObject.SetActive(false);
                sidebarBody.GetComponent<TooltipProviderComponent>()?.SetTooltipLocalized("Soul Savior",
                    sidebarBody.text, TooltipDesigner.TooltipDesignType.DefaultWide);
                Plugin.Logger.LogWarning("[SoulSavior] Resumen no disponible: " + e.Message);
            }
        }

        void LateUpdate()
        {
            if (sidebar == null || compendiumScreen == null) return;
            if (sidebarLanguage != I2.Loc.LocalizationManager.CurrentLanguageCode)
            {
                RefreshPage(); // También reconstruye filas, etiquetas y tooltips en el idioma nuevo.
            }
            var root = compendiumScreen.transform as RectTransform;
            var activeGrid = GetComponentsInChildren<GridLayoutGroup>()
                .FirstOrDefault(g => g.name == "Soul Savior rows");
            if (root == null || activeGrid == null) return;
            var corners = new Vector3[4];
            ((RectTransform)activeGrid.transform).GetWorldCorners(corners);
            float listLeft = root.InverseTransformPoint(corners[0]).x;
            float available = listLeft - root.rect.xMin - 48f;
            bool compact = available < 220f || root.rect.height < 680f;
            if (compactNavigation != compact)
            {
                currentPageIndex = navigator.Resize(compact, currentPageIndex);
                RefreshPage();
                PageCountChangedSignal.Dispatch();
            }
            sidebar.gameObject.SetActive(!compact);
            if (compact) return;
            float width = Mathf.Min(360f, available);
            sidebar.position = root.TransformPoint(new Vector3(root.rect.xMin + 24f,
                root.rect.yMax - 150f, 0f));
            float bodyHeight = Mathf.Clamp(root.rect.height - 560f, 140f, 500f);
            if (Math.Abs(width - measuredSidebarWidth) < 0.5f && Math.Abs(bodyHeight - measuredSidebarHeight) < 0.5f) return;
            measuredSidebarWidth = width; measuredSidebarHeight = bodyHeight;
            sidebar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            float actionY = -110f - bodyHeight;
            if (sidebarDetail != null) sidebarDetail.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bodyHeight);
            if (sidebarBody != null) sidebarBody.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bodyHeight - 24f);
            if (nextPending != null)
            {
                var rect = (RectTransform)nextPending.transform;
                rect.anchoredPosition = new Vector2(0f, actionY);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                if (nextPending.gameObject.activeSelf) actionY -= 50f;
            }
            if (pendingFilter != null)
            {
                var rect = (RectTransform)pendingFilter.transform;
                rect.anchoredPosition = new Vector2(0f, actionY);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                if (pendingFilter.gameObject.activeSelf) actionY -= 50f;
            }
            if (pendingFilterLabel != null) pendingFilterLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - 16f);
            if (nextPendingLabel != null) nextPendingLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - 16f);
            if (sidebarLegend != null)
            {
                sidebarLegend.rectTransform.anchoredPosition = new Vector2(0f, actionY);
                sidebarLegend.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100f);
                sidebarLegend.enableAutoSizing = true;
                sidebarLegend.fontSizeMin = 14f;
                sidebarLegend.fontSizeMax = 20f;
            }
            if (sidebarCredit != null)
            {
                sidebarCredit.rectTransform.anchoredPosition = new Vector2(0f, actionY - 114f);
                sidebarCredit.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
            }
            foreach (var text in sidebar.GetComponentsInChildren<TMP_Text>())
                text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    text.GetComponentInParent<GameUISelectableButton>() != null ? width - 16f : text == sidebarBody ? width - 28f : width);
            float viewWidth = (width - 16f) / 3f;
            LayoutViewButton(soulsViewButton, soulsViewLabel, 0f, viewWidth);
            LayoutViewButton(clansViewButton, clansViewLabel, viewWidth + 8f, viewWidth);
            LayoutViewButton(explorerViewButton, explorerViewLabel, 2f * (viewWidth + 8f), viewWidth);
        }
        static void LayoutViewButton(GameUISelectableButton? button, TMP_Text? label, float x, float width)
        {
            if (button == null || label == null) return;
            var rect = (RectTransform)button.transform;
            rect.anchoredPosition = new Vector2(x, -56f);
            rect.sizeDelta = new Vector2(width, 36f);
            label.rectTransform.sizeDelta = new Vector2(width - 16f, 26f);
            label.fontSizeMax = 20f;
            label.fontSizeMin = 12f;
        }
    }

    [HarmonyPatch]
    public static class SoulSaviorTabRegistration
    {
        [HarmonyPatch(typeof(CompendiumScreen), "Awake")]
        [HarmonyPostfix]
        static void Register(CompendiumScreen __instance)
        {
            if (!LogbookSoulSavior.Enabled) return;
            var sections = LogbookSoulSavior.Field<Dictionary<CompendiumScreen.Section, CompendiumSection>>(
                __instance, "pagesBySection");
            var tabs = LogbookSoulSavior.Field<Dictionary<CompendiumScreen.Section, CompendiumTab>>(
                __instance, "tabsBySection");
            var id = (CompendiumScreen.Section)8;
            if (sections == null || tabs == null || sections.ContainsKey(id)) return;
            var source = sections[CompendiumScreen.Section.Checklist] as CompendiumSectionChecklist;
            if (source == null || !tabs.TryGetValue(CompendiumScreen.Section.Stats, out var template)) return;
            var obj = new GameObject("Soul Savior section", typeof(RectTransform));
            obj.SetActive(false);
            obj.transform.SetParent(source.transform.parent, false);
            LogbookSoulSavior.CopyRect((RectTransform)source.transform, (RectTransform)obj.transform);
            var section = obj.AddComponent<SoulSaviorSection>();
            section.Configure(source);
            var originals = tabs.Values.OrderBy(t => t.transform.GetSiblingIndex()).ToList();
            var tab = UnityEngine.Object.Instantiate(template, template.transform.parent, false);
            tab.name = "Soul Savior tab";
            AccessTools.Field(typeof(CompendiumTab), "section").SetValue(tab, id);
            tab.transform.SetAsLastSibling();
            tab.gameObject.SetActive(true);
            section.Tab = tab;
            sections.Add(id, section);
            tabs.Add(id, tab);
            LogbookSoulSavior.Field<List<CompendiumScreen.Section>>(__instance, "sectionOrder")?.Add(id);
            var all = LogbookSoulSavior.Field<PaginatedCompendiumSection[]>(__instance, "paginatedSections")!;
            var registered = new List<PaginatedCompendiumSection>(all);
            registered.Add(section);
            AccessTools.Field(typeof(CompendiumScreen), "paginatedSections")
                .SetValue(__instance, registered.ToArray());
            var refresh = (Action)Delegate.CreateDelegate(typeof(Action), __instance,
                AccessTools.Method(typeof(CompendiumScreen), "RefreshPageTurnZones"));
            section.PageCountChangedSignal.AddListener(refresh);
            var placement = tab.gameObject.AddComponent<SoulSaviorTabPlacement>();
            placement.Originals = originals;
            placement.Added = tab;
            Plugin.Logger.LogInfo("[SoulSavior] Pestaña independiente registrada.");
        }
    }

    public sealed class SoulSaviorTabPlacement : MonoBehaviour
    {
        internal List<CompendiumTab> Originals = new();
        internal CompendiumTab? Added;
        readonly List<RectTransform> tabs = new();
        readonly List<RectTransform> graphics = new();
        RectTransform? parent;
        float left, right, baseline, measuredWidth;
        bool ready, warned;

        void OnEnable() { Canvas.willRenderCanvases += Apply; }
        void OnDisable() { Canvas.willRenderCanvases -= Apply; }

        static RectTransform VisibleAnchor(CompendiumTab tab)
        {
            // El controlador CompendiumTab puede tener centro cero aunque el rombo
            // esté desplazado dentro de un hijo. Medir el gráfico del botón.
            var target = tab.Button.targetGraphic;
            var image = tab.Button.GetComponentsInChildren<Image>(true)
                .Where(i => i.sprite != null && i.enabled && i.gameObject.activeInHierarchy
                    && i.rectTransform.rect.width > 1f && i.rectTransform.rect.height > 1f
                    && i.rectTransform.rect.width < 300f && i.rectTransform.rect.height < 300f)
                .OrderByDescending(i => i.rectTransform.rect.width * i.rectTransform.rect.height)
                .FirstOrDefault();
            return image != null ? image.rectTransform
                : target != null ? target.rectTransform : (RectTransform)tab.Button.transform;
        }

        static Vector3 Center(RectTransform graphic)
            => graphic.TransformPoint(graphic.rect.center);

        internal static bool ValidSpan(float start, float end, int count)
            => count > 1 && end - start > (count - 1) * 1f;

        bool Capture()
        {
            if (Added == null || Originals.Count < 2 || !Added.gameObject.activeInHierarchy) return false;
            parent = Added.transform.parent as RectTransform;
            if (parent == null || parent.rect.width <= 1f) return false;
            var ordered = Originals.Where(t => t != null)
                .Select(t => new { Tab = t, Graphic = VisibleAnchor(t) })
                .OrderBy(t => parent.InverseTransformPoint(Center(t.Graphic)).x).ToList();
            if (ordered.Count < 2) return false;
            var first = parent.InverseTransformPoint(Center(ordered[0].Graphic));
            var last = parent.InverseTransformPoint(Center(ordered[ordered.Count - 1].Graphic));
            if (!ValidSpan(first.x, last.x, ordered.Count))
            {
                if (!warned)
                {
                    warned = true;
                    Plugin.Logger.LogWarning("[SoulSavior] Esperando geometría válida del encabezado: "
                        + string.Join("; ", ordered.Select(t => t.Tab.name + " / " + t.Graphic.name
                            + " x=" + parent.InverseTransformPoint(Center(t.Graphic)).x
                            + " ancho=" + t.Graphic.rect.width)));
                }
                return false; // Nunca aceptar extremos 0/0; reintentar al mostrarse la pantalla.
            }
            left = first.x;
            right = last.x;
            baseline = first.y;
            measuredWidth = parent.rect.width;
            var fitter = parent.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            foreach (var item in ordered)
            {
                tabs.Add((RectTransform)item.Tab.transform);
                graphics.Add(item.Graphic);
            }
            tabs.Add((RectTransform)Added.transform);
            graphics.Add(VisibleAnchor(Added));
            foreach (var tab in tabs)
            {
                var element = tab.GetComponent<LayoutElement>()
                    ?? tab.gameObject.AddComponent<LayoutElement>();
                element.ignoreLayout = true;
            }
            ready = true;
            Plugin.Logger.LogInfo("[SoulSavior] Ocho gráficos visibles entre " + left + " y " + right
                + "; separación " + (right - left) / (tabs.Count - 1));
            return true;
        }

        internal static float CenterX(float start, float end, int index, int count)
            => start + (end - start) * index / Mathf.Max(1, count - 1);

        void LateUpdate() { Apply(); }

        void Apply()
        {
            if (!ready && !Capture()) return;
            if (parent == null || Added == null || parent.rect.width <= 1f) return;
            float scale = measuredWidth > 1f ? parent.rect.width / measuredWidth : 1f;
            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                var graphic = graphics[i];
                if (tab == null || graphic == null) continue;
                var target = parent.TransformPoint(new Vector3(
                    CenterX(left, right, i, tabs.Count) * scale, baseline, 0f));
                tab.position += target - Center(graphic);
            }
        }
    }

    // Decorative graphics never receive input; the native dispatcher owns every button.
    public sealed class LogbookInkLabel : MonoBehaviour
    { internal bool Heading; }

    internal static class LogbookProgressTheme
    {
        static readonly Color InkColor = new Color(0.23f, 0.13f, 0.07f, 1f);
        internal static void Ink(TMP_Text text, bool heading = false)
        {
            var marker = text.GetComponent<LogbookInkLabel>() ?? text.gameObject.AddComponent<LogbookInkLabel>();
            marker.Heading = heading;
            text.color = heading ? new Color(0.38f, 0.09f, 0.07f, 1f) : InkColor;
            text.outlineWidth = 0f;
            text.lineSpacing = 3f;
        }
        static Image Graphic(Transform parent, string name, Color color, Vector2 min, Vector2 max,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            obj.transform.SetParent(parent, false);
            obj.GetComponent<LayoutElement>().ignoreLayout = true;
            var image = obj.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            var rect = image.rectTransform; rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            return image;
        }
        internal static void Frame(Transform parent)
        {
            var edge = new Color(0.48f, 0.34f, 0.18f, 0.9f);
            Graphic(parent, "Book frame top", edge, new Vector2(0, 1), Vector2.one, new Vector2(2,-2), new Vector2(-2,0));
            Graphic(parent, "Book frame bottom", edge, Vector2.zero, new Vector2(1,0), new Vector2(2,0), new Vector2(-2,2));
            Graphic(parent, "Book frame left", edge, Vector2.zero, new Vector2(0,1), Vector2.zero, new Vector2(2,0));
            Graphic(parent, "Book frame right", edge, new Vector2(1,0), Vector2.one, new Vector2(-2,0), Vector2.zero);
            Graphic(parent, "Book frame glint", new Color(0.94f,0.82f,0.55f,0.5f), new Vector2(0,1), Vector2.one, new Vector2(3,-3), new Vector2(-3,-2));
        }
        internal static void Button(GameUISelectableButton button, TMP_Text caption, bool row = false)
        {
            Frame(button.transform);
            caption.alignment = row ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
            if (row) Ink(caption);
            else { caption.color = new Color(1f,0.94f,0.79f,1f); caption.fontSizeMax = 22f; }
            Select(button, false, row);
        }
        internal static void Select(GameUISelectableButton? button, bool selected, bool row = false)
        {
            if (button == null) return;
            var normal = row ? new Color(0.76f,0.63f,0.40f,0.22f)
                : selected ? new Color(0.57f,0.32f,0.09f,0.96f) : new Color(0.34f,0.17f,0.11f,0.94f);
            button.targetGraphic.color = Color.white;
            var colors = button.colors;
            colors.normalColor = normal; colors.highlightedColor = new Color(0.67f,0.47f,0.20f,1f);
            colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(0.40f,0.23f,0.10f,1f);
            colors.disabledColor = new Color(0.48f,0.41f,0.31f,0.4f); colors.fadeDuration = 0.12f;
            button.transition = Selectable.Transition.ColorTint; button.colors = colors;
        }
        internal static void Status(Transform parent, bool complete)
        {
            var existing = parent.Find("Progress status");
            var image = existing == null ? Graphic(parent, "Progress status", Color.white,
                Vector2.zero, new Vector2(0,1), new Vector2(3,4), new Vector2(7,-4)) : existing.GetComponent<Image>();
            image.color = complete ? new Color(0.68f,0.43f,0.08f,0.95f) : new Color(0.48f,0.34f,0.18f,0.28f);
        }
        internal static RectTransform Divider(Transform parent, float y)
        {
            // Copy only the native book decoration, never its layout or localisation components.
            var source = parent.root.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.sprite != null
                && i.name.IndexOf("divider", StringComparison.OrdinalIgnoreCase) >= 0);
            var image = Graphic(parent, "Progress book divider", new Color(0.44f,0.26f,0.12f,0.65f),
                new Vector2(0,1), Vector2.one, Vector2.zero, Vector2.zero);
            var rect = image.rectTransform; rect.pivot = new Vector2(0.5f,0.5f);
            rect.sizeDelta = new Vector2(0,source == null ? 2f : 18f); rect.anchoredPosition = new Vector2(0,y);
            if (source != null) { image.sprite = source.sprite; image.type = source.type; image.color = source.color; }
            return rect;
        }
    }
}
// 2026-09-30-1931||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Nueva integracion de lectura de ExpandedWinTracker y paginas Soul Savior por clan

// 2026-09-30-1937||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Integracion Soul Savior 0.4.0 completada; consultas validadas y presentacion separada de victorias normales

// 2026-09-30-2021||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Reparto incluso en paginas apagadas; banderas despues del retrato; margen real de punta; etiquetas sin localizador heredado

// 2026-09-30-2116||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||0.4.2: pestaña independiente Soul Savior y cinco clanes por página sin cabecera que reste altura

// 2026-09-30-2119||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Ajustar ocho pestañas al activarse el botón; soportar fila manual, HorizontalLayoutGroup y GridLayoutGroup

// 2026-09-30-2122||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Evitar resolución de extensión Concat del juego y su referencia incidental a Steamworks

// 2026-09-30-2123||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Comprobar disponibilidad de pestaña plantilla antes de crear la sección

// 2026-09-30-2131||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||0.4.3: medir centros en espacio común, repartir ocho pestañas y mantener posición tras animaciones/layout

// 2026-09-30-2133||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Evitar que ContentSizeFitter colapse el encabezado al excluir botones del layout

// 2026-09-30-2206||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||0.4.4: medir gráfico targetGraphic del botón; rechazar extremos 0/0 y reintentar antes del render hasta layout válido

// 2026-09-30-2206||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Preferir imagen visible del rombo frente al controlador de tamaño cero; corregir traza de diagnóstico

// 2026-09-30-2235||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Sustituir textos fijos por catálogo del idioma del jugador y preparar resumen lateral

// 2026-09-30-2236||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Resumen lateral con combinaciones, clanes, dificultad, leyenda y crédito; iconos normal/seleccionado ajustados; cambio de idioma en vivo

// 2026-09-30-2241||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Usar resumen probado del lector y actualizar fuente/material nativos al cambiar idioma

// 2026-10-01-2324||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Añadir páginas de almas antes de clanes en la misma pestaña; catálogo del juego por familia y resumen contextual

// 2026-10-01-2325||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Vista de quince familias de almas por página: iconos, nombres localizados, máximos independientes, vacío y error sin datos obsoletos

// 2026-10-02-0031||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Mejora 3: panel del clan enfocado por cursor o mando; aliados pendientes por bloques y máximo de cada campeón

// 2026-10-02-0033||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Mejora 5: selección nativa de almas y tooltips, foco visible, botón de pendientes y tamaños adaptativos sin desbordar texto

// 2026-10-02-0035||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Mejora 2: filtro Sin victoria, contadores completos y foco de clanes completos; reparar lectura cp1252 accidental de UTF-8 del paso anterior

// 2026-10-02-0044||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Conectar panel al foco nativo de mouse y mando con señales y retirada de listeners al ocultarse, sin depender de foco almacenado

// 2026-10-02-0047||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Ajuste compacto: reservar pie según alto, evitar desbordes y ofrecer texto completo del panel en tooltip; conservar margen interior de botones

// 2026-10-02-0047||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Corregir posición de llave del tooltip dentro de try; borrar también contenido de tooltip ante fallo de lectura

// 2026-10-02-0735||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Borrador: accesos Almas/Clanes localizados, paginación independiente, última página por vista y actualización de flechas nativas

// 2026-10-02-0738||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Mantener acceso a almas y clanes por flechas si el panel no cabe; redimensionar sin perder página y ajustar botones desde creación

// 2026-10-02-0903||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Integrar acceso Progreso sin otro icono superior, mantener paginación compacta y despachar botones/buscador con mando

// 2026-10-02-0919||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Usar estado puro de tres vistas para que Progreso no altere última página de almas/clanes

// 2026-10-02-0923||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Usar contador y flechas del juego en tablas de Progreso; conservar modo compacto entre vistas

// 2026-10-02-0926||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Usar fórmulas probadas de contador e índice nativos del explorador

// 2026-10-02-0934||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Activar botones solo por el despacho nativo del juego para evitar duplicación entre onClick Unity e input de ratón/mando

// 2026-10-02-0949||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Pulir panel lateral: tinta sobre pergamino, ficha enmarcada, separadores del libro y botones con selección dorada

// 2026-10-02-0951||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Conservar estilo de tinta al renovar materiales de fuente y cambiar idioma

// 2026-10-02-1045||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Unificar tarjetas de almas con marcos y tinta, señalar victorias, corregir tooltips obsoletos y evitar recrear materiales al navegar

// 2026-10-02-1104||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Compactar acciones ocultas y ayudas de la ficha; actualizar medidas solo cuando cambia geometría o contenido

// 2026-10-02-1106||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\LogbookSoulSavior.cs||Usar navegación compacta también con altura insuficiente para evitar cortar el panel lateral
