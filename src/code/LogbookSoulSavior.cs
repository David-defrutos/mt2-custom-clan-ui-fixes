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
            if (!Enabled || !File.Exists(SavePath)) return;
            var pages = __instance.ChecklistPages;
            var target = __instance.GetComponentInParent<CompendiumScreen>()?.GetComponentInChildren<SoulSaviorSection>(true);
            if (target == null || target.Pages.Count != 0) return;
            var added = new List<SoulSaviorChecklistPage>();
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
                // Añadir solo cuando todas las paginas estan preparadas.
                foreach (var view in added) target.Pages.Add(view);
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
            note.text = "Dificultad máxima ganada · Detalle por campeón en cada bandera";

            foreach (var clan in classes)
            {
                var row = UnityEngine.Object.Instantiate(template, grid, false);
                row.gameObject.name = "Soul Savior " + clan.GetTitle();
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
            => rows.FirstOrDefault()?.GetDefaultGameUISelectable();

        public override void Open()
        {
            base.Open();
            try
            {
                RefreshRecords();
                Adjust();
                StartCoroutine(Settle());
            }
            catch (Exception e)
            {
                foreach (var row in rows) row.gameObject.SetActive(false);
                if (note != null) note.text = "No se pudieron mostrar los registros de Soul Savior.";
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
                if (note != null) note.text = "No se pudieron leer los registros de Soul Savior.";
                Plugin.Logger.LogWarning("[SoulSavior] Guardado no disponible: " + e.Message);
                return;
            }
            if (note != null) note.text = records.Count == 0
                ? "Todavía no hay victorias de Soul Savior registradas."
                : "Dificultad máxima ganada · Detalle por campeón en cada bandera";

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
                    .Append(level < 0 ? "sin victoria registrada" : "dificultad " + level)
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
            label.text = won + " / " + flags.Count + "\naliados\nganados";
        }

        static TMP_Text Label(TMP_Text template, Transform parent, string name,
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
    public sealed class SoulSaviorSection : PaginatedCompendiumSection
    {
        internal readonly List<SoulSaviorChecklistPage> Pages = new();
        internal CompendiumTab? Tab;
        protected override int PageCount => Pages.Count;
        protected override void InitializeImpl() { }
        internal void Configure(CompendiumSectionChecklist source)
        {
            _section = (CompendiumScreen.Section)8;
            mainNavLayer = gameObject.AddComponent<BaseNavigationLayer>();
            pageCountLabel = (TextMeshProUGUI)AccessTools.Field(typeof(PaginatedCompendiumSection),
                "pageCountLabel").GetValue(source);
            pageCountKey = (string)AccessTools.Field(typeof(PaginatedCompendiumSection),
                "pageCountKey").GetValue(source);
        }
        protected override void RefreshPage()
        {
            base.RefreshPage();
            for (int i = 0; i < Pages.Count; i++) Pages[i].Toggle(i == currentPageIndex);
            mainNavLayer.SetDefaultGameSelectable(GetDefaultGameUISelectable());
        }
        public override IGameUIComponent? GetDefaultGameUISelectable()
            => Pages.Count == 0 ? null : Pages[Mathf.Clamp(currentPageIndex, 0, Pages.Count - 1)]
                .GetDefaultGameUISelectable();
        internal void SetIcon(SaveManager manager)
        {
            if (Tab == null) return;
            var icon = Tab.GetComponentsInChildren<Image>(true)
                .FirstOrDefault(i => i.name.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? Tab.GetComponentsInChildren<Image>(true).LastOrDefault(i => i.sprite != null);
            var soul = Resources.FindObjectsOfTypeAll<SoulData>()
                .FirstOrDefault(s => s.GetSoulTypeIcon() != null);
            if (icon != null && soul != null)
            {
                icon.sprite = soul.GetSoulTypeIcon();
                icon.color = Color.white;
            }
            var tooltip = Tab.GetComponentInChildren<TooltipProviderComponent>(true);
            if (tooltip != null) tooltip.SetTooltipLocalized("Soul Savior",
                "Seguimiento de victorias por clan y aliado.", TooltipDesigner.TooltipDesignType.DefaultWide);
        }
    }

    [HarmonyPatch]
    public static class SoulSaviorTabRegistration
    {
        [HarmonyPatch(typeof(CompendiumScreen), "Awake")]
        [HarmonyPostfix]
        static void Register(CompendiumScreen __instance)
        {
            if (!LogbookSoulSavior.Enabled || !File.Exists(LogbookSoulSavior.SavePath)) return;
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
