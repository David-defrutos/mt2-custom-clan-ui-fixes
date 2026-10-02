using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using HarmonyLib;
using ShinyShoe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace mt2_custom_clan_ui_fixes.Plugin
{
    // Actions use the game's input dispatcher for both pointer and controller input.
    public sealed class ProgressButtonAction : MonoBehaviour
    {
        internal Action? Action;
        int lastFrame = -1;
        internal void Activate()
        {
            if (lastFrame == Time.frameCount || !isActiveAndEnabled) return;
            lastFrame = Time.frameCount;
            Action?.Invoke();
        }
    }

    public sealed class ProgressExplorerPage : ChecklistPage
    {
        const int RowsPerPage = 10;
        SaveManager? save;
        RectTransform? panel;
        TMP_Text? template, title, summary, footer, header, valueHeader, searchPlaceholder, emptyState;
        GameUISelectableInputField? search;
        InputFieldContainer? searchContainer;
        Image? bar;
        readonly List<ClassData> clans = new();
        readonly List<SoulData> souls = new();
        readonly List<(GameUISelectableButton Button, TMP_Text Name, TMP_Text Value, TooltipProviderComponent Tooltip)> rows = new();
        readonly List<(GameUISelectableButton Button, TMP_Text Caption, int Row, int Column)> controls = new();
        GameUISelectableButton? firstTab, previous, next, goalButton;
        TMP_Text? cardsCaption, clansCaption, soulsCaption, optionA, optionB, optionC, goalCaption, previousCaption, nextCaption, refreshCaption, clearCaption;
        SoulSaviorRecords? records;
        CardUsageStore? usage;
        List<ProgressIdentity>? cardCatalog;
        int view, page, clanListPage, clanFilter, champion = -1, goal, maxGoal = 10;
        UsageMode mode;
        ProgressFilter soulFilter;
        ProgressSort sort;
        string selectedClan = "";
        string query = "", clanListQuery = "";
        float measuredWidth;
        double goalRatio;
        internal void Build(SaveManager manager, RectTransform source, TMP_Text label,
            List<ClassData> classes, List<SoulData> catalog)
        {
            save = manager; template = label; clans.AddRange(classes); souls.AddRange(catalog);
            var obj = new GameObject("Soul Savior rows", typeof(RectTransform), typeof(GridLayoutGroup));
            obj.transform.SetParent(transform, false);
            panel = (RectTransform)obj.transform;
            LogbookSoulSavior.CopyRect(source, panel);
            obj.GetComponent<GridLayoutGroup>().enabled = false;
            title = Label("Progress title", 0f, 0f, 1400f, 40f, 30f);
            Fit(title, 18f, 30f); LogbookProgressTheme.Ink(title, true);
            firstTab = Button("Cards", 0, 0, () => SetView(0), out cardsCaption);
            Button("Clans", 0, 1, () => SetView(1), out clansCaption);
            Button("Souls", 0, 2, () => SetView(2), out soulsCaption);
            Button("Option A", 1, 0, ChangeA, out optionA);
            Button("Option B", 1, 1, ChangeB, out optionB);
            Button("Option C", 1, 2, () => { if (view == 1 && selectedClan.Length > 0) { selectedClan = ""; query = clanListQuery; search?.SetTextWithoutNotify(query); page = clanListPage; Render(); } else ResetFilters(); }, out optionC);
            goalButton = Button("Goal", 2, 0, () => { goal = (goal + 1) % (maxGoal + 1); ResetPage(); }, out goalCaption);
            BuildSearch();
            summary = Label("Progress summary", 0f, -192f, 1400f, 36f, 24f);
            Fit(summary, 14f, 24f); LogbookProgressTheme.Ink(summary);
            var background = new GameObject("Goal bar background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(panel, false);
            SetRect((RectTransform)background.transform, 0f, -230f, 1400f, 6f);
            background.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.3f);
            background.GetComponent<Image>().raycastTarget = false;
            var fill = new GameObject("Goal bar", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(background.transform, false);
            bar = fill.GetComponent<Image>(); bar.color = new Color(0.72f, 0.54f, 0.13f, 0.9f); bar.raycastTarget = false;
            SetRect(bar.rectTransform, 0f, 0f, 0f, 6f);
            header = Label("Progress columns", 0f, -240f, 630f, 28f, 20f);
            valueHeader = Label("Progress value columns", 630f, -240f, 770f, 28f, 20f);
            Fit(header, 14f, 20f); Fit(valueHeader, 14f, 20f);
            LogbookProgressTheme.Ink(header, true); LogbookProgressTheme.Ink(valueHeader, true);
            valueHeader.alignment = TextAlignmentOptions.MidlineRight;
            for (int i = 0; i < RowsPerPage; i++)
            {
                int index = i;
                var row = MakeButton("Progress row " + i, () => OpenClan(index), out var name);
                var value = SoulSaviorChecklistPage.Label(label, row.transform, "Progress values", 640f, -5f, 740f, 32f, 22f);
                Fit(value, 16f, 22f); LogbookProgressTheme.Ink(value);
                value.alignment = TextAlignmentOptions.MidlineRight;
                LogbookProgressTheme.Status(row.transform, false);
                value.richText = false; name.richText = false;
                var tooltip = row.gameObject.AddComponent<TooltipProviderComponent>();
                rows.Add((row, name, value, tooltip));
                SetRect((RectTransform)row.transform, 0f, -270f - i * 42f, 1400f, 38f);
            }
            emptyState = Label("Progress empty state", 16f, -320f, 1368f, 120f, 26f);
            emptyState.alignment = TextAlignmentOptions.Center;
            Fit(emptyState, 18f, 26f); LogbookProgressTheme.Ink(emptyState);
            emptyState.gameObject.SetActive(false);
            previous = Button("Previous", 3, 0, () => AdvancePage(-1), out previousCaption);
            next = Button("Next", 3, 1, () => AdvancePage(1), out nextCaption);
            Button("Refresh", 3, 2, RefreshData, out refreshCaption);
            footer = Label("Progress footer", 0f, -752f, 1400f, 80f, 20f);
            Fit(footer, 14f, 20f); LogbookProgressTheme.Ink(footer);
            Resize();
        }
        TMP_Text Label(string name, float x, float y, float width, float height, float size)
            => SoulSaviorChecklistPage.Label(template!, panel!, name, x, y, width, height, size);
        static void Fit(TMP_Text text, float min, float max)
        { text.enableAutoSizing = true; text.fontSizeMin = min; text.fontSizeMax = max; }
        static void SetRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
        }
        GameUISelectableButton MakeButton(string name, Action action, out TMP_Text caption)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(GameUISelectableButton), typeof(LayoutElement));
            obj.transform.SetParent(panel, false);
            obj.GetComponent<LayoutElement>().ignoreLayout = true;
            var button = obj.GetComponent<GameUISelectableButton>();
            button.targetGraphic = obj.GetComponent<Image>();
            button.targetGraphic.color = new Color(0.35f, 0.25f, 0.10f, 0.18f);
            button.SetNavigation(new Navigation { mode = Navigation.Mode.Automatic });
            var dispatch = obj.AddComponent<ProgressButtonAction>(); dispatch.Action = action;
            caption = SoulSaviorChecklistPage.Label(template!, obj.transform, name + " label", 8f, -5f, 400f, 32f, 22f);
            Fit(caption, 14f, 22f);
            LogbookProgressTheme.Button(button, caption, name.StartsWith("Progress row", StringComparison.Ordinal));
            return button;
        }
        GameUISelectableButton Button(string name, int row, int column, Action action, out TMP_Text caption)
        {
            var button = MakeButton(name, action, out caption);
            controls.Add((button, caption, row, column)); return button;
        }
        void BuildSearch()
        {
            var obj = new GameObject("Progress search", typeof(RectTransform), typeof(Image));
            obj.SetActive(false);
            obj.transform.SetParent(panel, false);
            search = obj.AddComponent<GameUISelectableInputField>();
            search.targetGraphic = obj.GetComponent<Image>(); search.targetGraphic.color = new Color(0.1f, 0.1f, 0.1f, 0.18f);
            var viewport = new GameObject("Search viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(obj.transform, false);
            var text = SoulSaviorChecklistPage.Label(template!, viewport.transform, "Search text", 0f, 0f, 460f, 32f, 22f);
            text.richText = false; text.overflowMode = TextOverflowModes.Overflow;
            LogbookProgressTheme.Ink(text);
            search.caretColor = new Color(0.23f,0.13f,0.07f,1f);
            search.customCaretColor = true;
            search.selectionColor = new Color(0.68f,0.43f,0.08f,0.35f);
            LogbookProgressTheme.Frame(obj.transform);
            searchPlaceholder = SoulSaviorChecklistPage.Label(template!, viewport.transform, "Search placeholder", 0f, 0f, 460f, 32f, 22f);
            LogbookProgressTheme.Ink(searchPlaceholder);
            searchPlaceholder.color = new Color(0.35f, 0.3f, 0.25f, 0.8f);
            search.textViewport = (RectTransform)viewport.transform; search.textComponent = (TextMeshProUGUI)text;
            search.placeholder = searchPlaceholder; search.lineType = TMP_InputField.LineType.SingleLine;
            search.characterLimit = 80; search.richText = false;
            search.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            search.onValueChanged.AddListener(value => { query = value; page = 0; Render(); });
            var activate = Button("Search activate", 4, 0, () => search.ActivateInputField(), out var label);
            label.text = ProgressText.Get("Search");
            Button("Search clear", 4, 1, () => search.text = "", out clearCaption);
            searchContainer = obj.AddComponent<InputFieldContainer>();
            AccessTools.Field(typeof(InputFieldContainer), "inputField").SetValue(searchContainer, search);
            AccessTools.Field(typeof(InputFieldContainer), "triggerButton").SetValue(searchContainer, activate);
            obj.SetActive(true);
        }
        internal bool ApplyInput(CoreInputControlMapping mapping, IGameUIComponent? target, InputManager.Controls control)
            => isActiveAndEnabled && searchContainer != null && searchContainer.ApplyScreenInput(mapping, target!, control);
        public override IGameUIComponent? GetDefaultGameUISelectable() => firstTab;
        public override void Open() { base.Open(); RefreshData(); }
        void SetView(int value) { view = value; selectedClan = ""; query = ""; search?.SetTextWithoutNotify(""); page = 0; Render(); }
        internal int ResultPage => page;
        internal int ResultPageCount => ProgressQueries.PageCount(visible.Count, RowsPerPage);
        internal void AdvancePage(int direction) { page = Mathf.Clamp(page + direction, 0, ResultPageCount - 1); Render(); }
        void ResetPage() { page = 0; Render(); }
        void ChangeA()
        {
            if (view == 0) mode = (UsageMode)(((int)mode + 1) % 4);
            else if (view == 1)
            {
                champion = champion == 1 ? -1 : champion + 1;
                var selected = clans.FirstOrDefault(c => c.GetID() == selectedClan);
                if (champion == 1 && selected != null && selected.GetChampionData(1)?.championCardData == null)
                    champion = -1;
            }
            else soulFilter = (ProgressFilter)(((int)soulFilter + 1) % 3);
            ResetPage();
        }
        void ChangeB()
        {
            if (view == 0) clanFilter = (clanFilter + 1) % (clans.Count + 2);
            else if (view == 1) sort = (ProgressSort)(((int)sort + 1) % 3);
            else RefreshData();
            ResetPage();
        }
        void ResetFilters()
        {
            selectedClan = ""; mode = UsageMode.All; clanFilter = 0; soulFilter = ProgressFilter.All;
            champion = -1; sort = ProgressSort.Name; query = "";
            if (search != null) search.SetTextWithoutNotify(""); ResetPage();
        }
        bool HasCurrentFilters => query.Trim().Length > 0 || (view == 0 ? mode != UsageMode.All || clanFilter != 0
            : view == 1 ? champion >= 0 || sort != ProgressSort.Name : soulFilter != ProgressFilter.All);
        List<ProgressResult> visible = new();
        void OpenClan(int row)
        {
            if (view != 1 || selectedClan.Length > 0) return;
            int index = page * RowsPerPage + row;
            if (index >= visible.Count) return;
            selectedClan = visible[index].Id;
            clanListPage = page; clanListQuery = query; query = ""; search?.SetTextWithoutNotify(""); page = 0; Render();
        }
        void RefreshData()
        {
            usage = CardUsageTracker.Store;
            cardCatalog = null;
            records = null;
            try
            {
                using var stream = File.Open(LogbookSoulSavior.SavePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                records = SoulSaviorRecords.Read(stream);
                maxGoal = Math.Max(10, records.Overview(clans.Select(c => c.GetID())).Best);
                foreach (var soul in souls) maxGoal = Math.Max(maxGoal, records.Soul(SoulSaviorRecords.SoulKey(soul.name)).Difficulty);
            }
            catch (Exception error) { Plugin.Logger.LogWarning("[Progress] Tracker read failed: " + error.Message); }
            Render();
        }
        static string Difficulty(int value) => value < 0 ? "—" : "S" + value;
        string ModeName() => mode == UsageMode.All ? ProgressText.Get("All") : mode == UsageMode.Normal
            ? SoulSaviorText.Extra("Normal") : mode == UsageMode.SoulSavior ? "Soul Savior" : ProgressText.Get("Other");
        List<ProgressIdentity> ClanCatalog() => clans.Select(c => new ProgressIdentity {
            Id = c.GetID(), Name = c.GetTitle(), HasSecondChampion = c.GetChampionData(1)?.championCardData != null
        }).ToList();
        List<ProgressIdentity> SoulCatalog() => souls.Select(s => new ProgressIdentity { Id = SoulSaviorRecords.SoulKey(s.name), Name = s.GetName() }).ToList();
        void Render()
        {
            if (save == null || title == null || summary == null || footer == null || header == null) return;
            foreach (var control in controls.Where(c => c.Row == 0))
                LogbookProgressTheme.Select(control.Button, control.Column == view);
            cardsCaption!.text = ProgressText.Get("Cards");
            clansCaption!.text = SoulSaviorText.Extra("Clans");
            soulsCaption!.text = SoulSaviorText.Get(13);
            goalCaption!.text = ProgressText.Get("Goal") + ": ≥ S" + goal;
            goalButton!.interactable = view != 0;
            searchPlaceholder!.text = ProgressText.Get("Search"); clearCaption!.text = ProgressText.Get("Clear");
            controls.First(c => c.Row == 4 && c.Column == 0).Caption.text = ProgressText.Get("Search");
            refreshCaption!.text = ProgressText.Get("Refresh");
            optionA!.text = view == 0 ? ProgressText.Get("Mode") + ": " + ModeName()
                : view == 1 ? (champion < 0 ? ProgressText.Get("Both") : ProgressText.Get("Champion") + " " + (champion + 1))
                : soulFilter == ProgressFilter.All ? ProgressText.Get("All") : soulFilter == ProgressFilter.Missing ? SoulSaviorText.Extra("Missing") : ProgressText.Get("BelowGoal");
            string? clan = clanFilter == 0 ? null : clanFilter == 1 ? "" : clans[clanFilter - 2].GetID();
            optionB!.text = view == 0 ? (clanFilter == 0 ? ProgressText.Get("AllClans") : clanFilter == 1 ? ProgressText.Get("Neutral") : clans[clanFilter - 2].GetTitle())
                : view == 1 ? ProgressText.Get(sort == ProgressSort.Name ? "Name" : sort == ProgressSort.Progress ? "ProgressOrder" : "PendingOrder") : ProgressText.Get("Refresh");
            optionC!.text = view == 1 && selectedClan.Length > 0 ? ProgressText.Get("AllClans") : ProgressText.Get("Clear");
            foreach (var control in controls.Where(c => c.Row == 1))
            {
                bool active = control.Column == 0 ? view == 0 ? mode != UsageMode.All : view == 1 ? champion >= 0 : soulFilter != ProgressFilter.All
                    : control.Column == 1 && (view == 0 ? clanFilter != 0 : view == 1 && sort != ProgressSort.Name);
                LogbookProgressTheme.Select(control.Button, active);
                if (control.Column == 2) control.Button.interactable = selectedClan.Length > 0 || HasCurrentFilters;
            }
            LogbookProgressTheme.Select(goalButton, goal > 0);
            foreach (var control in controls.Where(c => c.Row == 4 && c.Column == 1))
                control.Button.interactable = query.Length > 0;
            foreach (var control in controls)
            {
                var tooltip = control.Button.GetComponent<TooltipProviderComponent>()
                    ?? control.Button.gameObject.AddComponent<TooltipProviderComponent>();
                bool cyclic = control.Row == 2 || control.Row == 1 && (control.Column == 0 || control.Column == 1 && view != 2);
                tooltip.SetTooltipLocalized(control.Caption.text, cyclic ? ProgressText.Get("CycleHint") : control.Caption.text,
                    TooltipDesigner.TooltipDesignType.DefaultWide);
            }
            title.text = ProgressText.Get("Progress") + " · " + (view == 0 ? ProgressText.Get("Cards") : view == 1 ? SoulSaviorText.Extra("Clans") : SoulSaviorText.Get(13));
            footer.text = view == 0 ? SoulSaviorText.Extra("UsageNote") : view == 2 ? SoulSaviorText.Get(15) + "\n" + ProgressText.Get("GoalHint")
                : (selectedClan.Length == 0 ? ProgressText.Get("SelectClan") + "\n" : "") + ProgressText.Get("GoalHint");
            visible.Clear();
            double ratio = 0;
            string emptyMessage = ProgressText.Get("NoResults");
            bool succeeded = false;
            try
            {
                if (view == 0)
                {
                    if (usage == null) throw new InvalidOperationException(ProgressText.Get("Unavailable"));
                    cardCatalog ??= save.GetAllGameData().GetAllCardData().Where(c => c != null).Select(c => new ProgressIdentity {
                        Id = c.GetID(), Name = c.GetName(), Clan = c.GetLinkedClassID() ?? ""
                    }).ToList();
                    visible = ProgressQueries.Cards(usage.Snapshot(), cardCatalog, mode, clan, query);
                    long direct = visible.Aggregate(0L, (sum, row) => checked(sum + row.Direct));
                    long automatic = visible.Aggregate(0L, (sum, row) => checked(sum + row.Automatic));
                    summary.text = SoulSaviorText.Extra("Direct") + ": " + direct + "   ·   " + SoulSaviorText.Extra("Automatic") + ": " + automatic;
                    if (DateTimeOffset.TryParse(usage.StartedUtc, out var started)) footer.text = ProgressText.Get("Since") + " "
                        + started.ToLocalTime().ToString("d", CultureInfo.CurrentCulture) + " · " + footer.text;
                    header.text = ProgressText.Get("Name");
                    valueHeader!.text = SoulSaviorText.Extra("Plays") + "  ·  " + SoulSaviorText.Extra("Direct") + "  ·  " + SoulSaviorText.Extra("Automatic");
                }
                else
                {
                    if (records == null) throw new InvalidOperationException(SoulSaviorText.Get(2));
                    var all = view == 1 ? ProgressQueries.Clans(records, ClanCatalog(), champion, goal, "", sort)
                        : ProgressQueries.Souls(records, SoulCatalog(), ProgressFilter.All, goal, "");
                    var progress = ProgressQueries.Goal(all); ratio = progress.Ratio;
                    var clanGoal = ProgressQueries.Goal(ProgressQueries.Clans(records, ClanCatalog(), champion, goal, "", sort));
                    var soulGoal = ProgressQueries.Goal(ProgressQueries.Souls(records, SoulCatalog(), ProgressFilter.All, goal, ""));
                    summary.text = "≥ S" + goal + "   ·   " + SoulSaviorText.Get(8) + ": " + clanGoal.Done + "/" + clanGoal.Total
                        + "   ·   " + SoulSaviorText.Get(13) + ": " + soulGoal.Done + "/" + soulGoal.Total
                        + "   ·   " + (progress.Ratio * 100).ToString("0.#", CultureInfo.CurrentCulture) + "%";
                    visible = view == 1 ? all.Where(c => ProgressQueries.Matches(c.Name, query)).ToList()
                        : ProgressQueries.Souls(records, SoulCatalog(), soulFilter, goal, query);
                    if (view == 1 && selectedClan.Length > 0)
                    {
                        var selected = clans.FirstOrDefault(c => c.GetID() == selectedClan);
                        if (selected != null)
                        {
                            title.text += " · " + selected.GetTitle();
                            if (champion >= 0) title.text += " · " + (selected.GetChampionData(champion)?.championCardData?.GetName() ?? "—");
                            visible = ProgressQueries.Allies(records, ClanCatalog(), selectedClan, champion, goal, query);
                        }
                    }
                    header.text = ProgressText.Get("Name");
                    valueHeader!.text = view == 2 || selectedClan.Length > 0 ? ProgressText.Get("Best")
                        : ProgressText.Get("Goal") + " · " + ProgressText.Get("Best") + " · " + ProgressText.Get("PendingGoal");
                }
                succeeded = true;
            }
            catch (Exception error)
            {
                visible.Clear(); summary.text = view == 0 ? ProgressText.Get("Unavailable") : SoulSaviorText.Get(2);
                emptyMessage = summary.text;
                header.text = ""; valueHeader!.text = ""; ratio = 0;
                Plugin.Logger.LogWarning("[Progress] Query unavailable: " + error.Message);
            }
            if (emptyState != null)
            {
                emptyState.text = emptyMessage;
                emptyState.gameObject.SetActive(visible.Count == 0);
            }
            if (succeeded) summary.text += "   ·   " + ProgressText.Get("Results") + ": " + visible.Count;
            goalRatio = ratio;
            if (bar != null) bar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Mathf.Max(1f, measuredWidth) * (float)Math.Max(0, Math.Min(1, ratio)));
            int pages = ProgressQueries.PageCount(visible.Count, RowsPerPage);
            page = Mathf.Clamp(page, 0, pages - 1);
            previous!.interactable = page > 0; next!.interactable = page + 1 < pages;
            previousCaption!.text = ProgressText.Get("Previous"); nextCaption!.text = ProgressText.Get("Next") + " (" + (page + 1) + "/" + pages + ")";
            for (int i = 0; i < rows.Count; i++)
            {
                int index = page * RowsPerPage + i;
                rows[i].Button.gameObject.SetActive(index < visible.Count);
                if (index >= visible.Count) continue;
                var row = visible[index];
                LogbookProgressTheme.Select(rows[i].Button, false, true);
                var palette = rows[i].Button.colors;
                palette.normalColor = i % 2 == 0 ? new Color(0.76f,0.63f,0.40f,0.22f) : new Color(0.76f,0.63f,0.40f,0.09f);
                rows[i].Button.colors = palette;
                LogbookProgressTheme.Status(rows[i].Button.transform,
                    view != 0 && (view == 2 || selectedClan.Length > 0 ? row.Best >= goal : row.Total > 0 && row.Done == row.Total));
                rows[i].Name.text = (view == 0 ? (index + 1) + ". " : "") + row.Name;
                rows[i].Value.text = view == 0 ? row.Plays + "   ·   " + row.Direct + "   ·   " + row.Automatic
                    : view == 2 ? (row.Best < 0 ? "—" : SoulSaviorText.Get(14, row.Tier, Difficulty(row.Best)))
                    : selectedClan.Length > 0 ? Difficulty(row.Best)
                    : row.Done + "/" + row.Total + "   ·   " + Difficulty(row.Best) + "   ·   " + row.Pending.Length;
                string tooltip = rows[i].Value.text;
                if (view == 1 && selectedClan.Length == 0)
                {
                    tooltip = ProgressText.Get("PendingGoal") + " ≥ S" + goal + "\n" + string.Join("\n", row.Pending);
                    var main = clans.First(c => c.GetID() == row.Id);
                    if (champion >= 0) tooltip = (main.GetChampionData(champion)?.championCardData?.GetName() ?? "—") + "\n" + tooltip;
                }
                rows[i].Tooltip.SetTooltipLocalized(row.Name, tooltip, TooltipDesigner.TooltipDesignType.DefaultWide);
            }
            Resize();
            GetComponentInParent<SoulSaviorSection>()?.RefreshExplorerPagination();
        }
        void LateUpdate() { if (panel != null && Math.Abs(panel.rect.width - measuredWidth) > 1f) Resize(); }
        void Resize()
        {
            if (panel == null) return;
            measuredWidth = panel.rect.width > 1f ? panel.rect.width : 1400f;
            float cell = (measuredWidth - 24f) / 3f;
            // Single-record views leave more room for localized names than multi-value tables.
            float nameShare = view == 2 || selectedClan.Length > 0 ? 0.65f : view == 0 ? 0.56f : 0.48f;
            foreach (var control in controls)
            {
                float y = control.Row == 0 ? -48f : control.Row == 1 ? -96f : control.Row == 2 ? -144f : control.Row == 3 ? -704f : -144f;
                float x = control.Row == 4 ? (control.Column == 0 ? cell + 12f : measuredWidth - 110f) : control.Column * (cell + 12f);
                float width = control.Row == 4 ? (control.Column == 0 ? Math.Min(105f, measuredWidth * 0.15f) : Math.Min(110f, measuredWidth * 0.14f)) : cell;
                if (control.Row == 4 && control.Column == 1) x = measuredWidth - width;
                SetRect((RectTransform)control.Button.transform, x, y, width, 38f);
                control.Caption.rectTransform.sizeDelta = new Vector2(width - 16f, 28f);
            }
            if (search != null)
            {
                float left = cell + 24f + Math.Min(105f, measuredWidth * 0.15f);
                float width = Math.Max(40f, measuredWidth - left - Math.Min(110f, measuredWidth * 0.14f) - 12f);
                SetRect((RectTransform)search.transform, left, -144f, width, 38f);
                SetRect(search.textViewport, 8f, -5f, width - 16f, 28f);
                search.textComponent.rectTransform.sizeDelta = new Vector2(width - 16f, 28f);
                searchPlaceholder!.rectTransform.sizeDelta = new Vector2(width - 16f, 28f);
            }
            foreach (var row in rows)
            {
                ((RectTransform)row.Button.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, measuredWidth);
                row.Name.rectTransform.sizeDelta = new Vector2(measuredWidth * nameShare - 16f, 28f);
                row.Value.rectTransform.anchoredPosition = new Vector2(measuredWidth * nameShare, -5f);
                row.Value.rectTransform.sizeDelta = new Vector2(measuredWidth * (1f - nameShare) - 16f, 28f);
            }
            if (header != null) header.rectTransform.sizeDelta = new Vector2(measuredWidth * nameShare - 12f, 28f);
            if (valueHeader != null)
            {
                valueHeader.rectTransform.anchoredPosition = new Vector2(measuredWidth * nameShare, -240f);
                valueHeader.rectTransform.sizeDelta = new Vector2(measuredWidth * (1f - nameShare) - 16f, 28f);
            }
            if (emptyState != null) emptyState.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, measuredWidth - 32f);
            foreach (var label in new[] { title, summary, footer })
                if (label != null)
                {
                    label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, measuredWidth);
                    var tooltip = label.GetComponent<TooltipProviderComponent>() ?? label.gameObject.AddComponent<TooltipProviderComponent>();
                    label.raycastTarget = true;
                    if (label.GetComponent<GameUISelectable>() == null) label.gameObject.AddComponent<GameUISelectable>();
                    tooltip.SetTooltipLocalized(title?.text ?? ProgressText.Get("Progress"), label.text,
                        TooltipDesigner.TooltipDesignType.DefaultWide);
                }
            if (bar != null) bar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                measuredWidth * (float)Math.Max(0, Math.Min(1, goalRatio)));
            var background = panel.Find("Goal bar background") as RectTransform;
            if (background != null) background.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, measuredWidth);
        }
    }
}

// 2026-10-02-0900||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressExplorerPage.cs||Explorador de progreso: ranking y filtros, objetivos con barra, almas pendientes y detalle de alianzas por campeón

// 2026-10-02-0903||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressExplorerPage.cs||Conservar campeón al volver del detalle y proporción de barra al redimensionar

// 2026-10-02-0903||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressExplorerPage.cs||Corregir escapes de saltos de línea de textos en generación inicial de explorador

// 2026-10-02-0911||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressExplorerPage.cs||Pulir encabezados alineados y responsive, mostrar ambos objetivos globales, errores localizados y montar buscador apagado

// 2026-10-02-0914||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressExplorerPage.cs||Instrucciones propias del explorador y caché de nombres localizados de cartas durante búsquedas

// 2026-10-02-0917||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressExplorerPage.cs||No heredar búsqueda del clan al abrir aliados, restaurarla al volver y evitar seleccionar campeón inexistente

// 2026-10-02-0923||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressExplorerPage.cs||Sincronizar páginas del ranking y tablas con flechas nativas y aclarar contador de combinaciones

// 2026-10-02-0934||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressExplorerPage.cs||Activar botones solo por el despacho nativo del juego para evitar duplicación entre onClick Unity e input de ratón/mando

// 2026-10-02-0949||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressExplorerPage.cs||Aplicar tema del libro a controles y tablas: marcos, pestañas doradas y encabezados en tinta

// 2026-10-02-1045||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressExplorerPage.cs||Pulir tablas con filas alternas, valores alineados a la derecha y marca de objetivo; buscador con marco y tinta

// 2026-10-02-1051||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressExplorerPage.cs||Resaltar filtros activos, ayudas localizadas, estados vacíos centrados, contador de resultados y limpieza contextual

// 2026-10-02-1052||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressExplorerPage.cs||Restaurar página de la lista además de búsqueda y campeón al volver del detalle

// 2026-10-02-1104||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressExplorerPage.cs||Adaptar ancho de nombres a cada tabla y permitir consultar encabezados y ayudas completos con tooltip
