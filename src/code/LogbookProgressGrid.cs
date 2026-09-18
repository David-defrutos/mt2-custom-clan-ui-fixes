using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace mt2_custom_clan_ui_fixes.Plugin
{
    /// <summary>
    /// TRAZA, DE MOMENTO. Vuelca como esta montada la hoja de progreso del logbook
    /// (`CompendiumSectionChecklist`, la de "Progress Record") para poder reorganizarla
    /// despues con datos y no a ojo. **No cambia nada de la pantalla.**
    ///
    /// Lo que ya se sabe, leido en IL el 18-sep-2026:
    ///   - `CompendiumSectionChecklist.PageCount` es `checklistPages.Count` a secas, asi que
    ///     el numero de hojas sale de esa lista. Con CustomClanHelper instalado son 3: las
    ///     que crea el juego mas la suya.
    ///   - `StandardChecklistPage.Initialize` **solo coge los clanes de lanzamiento**
    ///     (`IsLaunchClan`), separando los de tripulacion, que ademas piden la feature 8
    ///     desbloqueada. Los clanes modeados NO entran ahi: por eso CustomClanHelper monta su
    ///     propia hoja.
    ///   - Las secciones se crean con `GameObjectUtil.PopulateViewList` sobre
    ///     `clanSectionsLayoutAllClans` (o `...StartingClans` si falta la feature 8), y al
    ///     final llama a `ReparentCrewVictoryItems()` en cada una.
    ///   - Cada `ClanChecklistSection` tiene DOS layouts de banderitas:
    ///     `subclanVictoryLayout` (aliados normales) y `subclanVictoryLayoutCrew`
    ///     (tripulacion), con `victoryItems` como lista completa.
    ///
    /// Lo que falta por saber, y es lo que vuelca esta traza: que rejilla usa la hoja
    /// (tamano de celda, columnas, separacion), cuantas secciones entran, que ancho real
    /// tiene el panel de cada seccion y cuantos items hay en cada una de las dos filas.
    /// </summary>
    [HarmonyPatch]
    public static class LogbookProgressGrid
    {
        public static bool Verbose = true;
        public static int DetailSections = 2;   // cuantas secciones se vuelcan al detalle

        static readonly FieldInfo? FPaginas =
            AccessTools.Field(typeof(CompendiumSectionChecklist), "checklistPages");
        static readonly FieldInfo? FLayoutTodos =
            AccessTools.Field(typeof(StandardChecklistPage), "clanSectionsLayoutAllClans");
        static readonly FieldInfo? FLayoutIniciales =
            AccessTools.Field(typeof(StandardChecklistPage), "clanSectionsLayoutStartingClans");
        static readonly FieldInfo? FSecciones =
            AccessTools.Field(typeof(StandardChecklistPage), "clanChecklistSections");
        static readonly FieldInfo? FClase =
            AccessTools.Field(typeof(ClanChecklistSection), "classData");
        static readonly FieldInfo? FFila =
            AccessTools.Field(typeof(ClanChecklistSection), "subclanVictoryLayout");
        static readonly FieldInfo? FFilaCrew =
            AccessTools.Field(typeof(ClanChecklistSection), "subclanVictoryLayoutCrew");
        static readonly FieldInfo? FItems =
            AccessTools.Field(typeof(ClanChecklistSection), "victoryItems");

        static bool yaVolcado;

        [HarmonyPatch(typeof(CompendiumSectionChecklist), "InitializeImpl")]
        [HarmonyPostfix]
        static void TrasInicializar(CompendiumSectionChecklist __instance)
        {
            if (!Verbose || yaVolcado || __instance == null) return;
            // El layout no ha corrido todavia: se deja para el frame siguiente, y solo si el
            // objeto esta activo (misma pega que en las otras dos pantallas).
            if (!__instance.isActiveAndEnabled) return;
            try { __instance.StartCoroutine(VolcarUnFrameDespues(__instance)); }
            catch (Exception e) { Log("no se pudo encolar el volcado: " + e.Message, true); }
        }

        static IEnumerator VolcarUnFrameDespues(CompendiumSectionChecklist seccion)
        {
            yield return null;
            yield return null;
            Volcar(seccion);
        }

        static void Volcar(CompendiumSectionChecklist seccion)
        {
            if (yaVolcado) return;
            try
            {
                if (FPaginas?.GetValue(seccion) is not IEnumerable paginas) return;

                int n = 0;
                foreach (var p in paginas)
                {
                    n++;
                    if (p is not Component comp) { Log($"hoja {n}: {p?.GetType().Name ?? "null"}"); continue; }
                    Log($"hoja {n}: {comp.GetType().Name} \"{comp.name}\"{Medidas(comp.transform)}");
                    if (p is StandardChecklistPage estandar) VolcarEstandar(estandar);
                }
                if (n > 0) yaVolcado = true;
                Log($"total {n} hojas (PageCount = checklistPages.Count)");
            }
            catch (Exception e)
            {
                Log("fallo volcando la hoja: " + e, true);
            }
        }

        static void VolcarEstandar(StandardChecklistPage hoja)
        {
            foreach (var (nombre, campo) in new[] { ("allClans", FLayoutTodos), ("startingClans", FLayoutIniciales) })
            {
                if (campo?.GetValue(hoja) is not Component layout) continue;
                Log($"  layout {nombre}: \"{layout.name}\"{Medidas(layout.transform)}" +
                    $" activo={layout.gameObject.activeInHierarchy}{Ajustes(layout.transform)}");
            }

            if (FSecciones?.GetValue(hoja) is not IEnumerable secciones) return;
            int i = 0;
            foreach (var s in secciones)
            {
                if (s is not ClanChecklistSection seccion) continue;
                i++;
                string clan = "?";
                try
                {
                    var datos = FClase?.GetValue(seccion);
                    var titulo = datos?.GetType().GetMethod("GetTitle", Type.EmptyTypes);
                    clan = titulo?.Invoke(datos, null) as string ?? "?";
                }
                catch { }

                int items = Cuenta(FItems?.GetValue(seccion));
                if (i > DetailSections)
                {
                    Log($"  seccion {i}: {clan}, {items} banderitas");
                    continue;
                }

                Log($"  seccion {i}: {clan}, {items} banderitas, seccion{Medidas(seccion.transform)}");
                foreach (var (nombre, campo) in new[] { ("normales", FFila), ("tripulacion", FFilaCrew) })
                {
                    if (campo?.GetValue(seccion) is not Component fila) continue;
                    int hijos = 0, visibles = 0;
                    foreach (Transform h in fila.transform) { hijos++; if (h.gameObject.activeSelf) visibles++; }
                    Log($"    fila {nombre}: \"{fila.name}\"{Medidas(fila.transform)}" +
                        $" {visibles}/{hijos} hijos{Ajustes(fila.transform)}");
                    foreach (Transform h in fila.transform)
                    {
                        Log($"      hijo \"{h.name}\"{Medidas(h)}");
                        break;   // con el primero basta para saber el tamano de una banderita
                    }
                }
                // Los hermanos de la seccion: el fondo que habria que estirar sale de aqui.
                var hermanos = new StringBuilder();
                foreach (Transform h in seccion.transform)
                    hermanos.Append($" [{h.name}{Medidas(h)}]");
                Log($"    hijos de la seccion:{hermanos}");
            }
        }

        static int Cuenta(object? lista)
        {
            if (lista is ICollection coleccion) return coleccion.Count;
            if (lista is IEnumerable enumerable)
            {
                int n = 0;
                foreach (var _ in enumerable) n++;
                return n;
            }
            return -1;
        }

        static string Medidas(Transform? t) =>
            t is RectTransform rt ? $" {rt.rect.width:0}x{rt.rect.height:0}" : "";

        /// <summary>
        /// Los ajustes del LayoutGroup, por reflexion para no referenciar UnityEngine.UI.
        /// </summary>
        static string Ajustes(Transform t)
        {
            var sb = new StringBuilder();
            foreach (var c in t.GetComponents<Component>())
            {
                if (c == null) continue;
                var tipo = c.GetType();
                if (!tipo.Name.Contains("LayoutGroup") && !tipo.Name.Contains("ContentSizeFitter")) continue;
                sb.Append(' ').Append(tipo.Name).Append('(');
                foreach (var nombre in new[] { "cellSize", "spacing", "constraint", "constraintCount",
                                               "childForceExpandWidth", "childControlWidth",
                                               "childAlignment", "horizontalFit", "verticalFit" })
                {
                    object? v = null;
                    try { v = tipo.GetProperty(nombre)?.GetValue(c, null); } catch { }
                    if (v != null) sb.Append(nombre).Append('=').Append(v).Append(' ');
                }
                sb.Append(')');
            }
            return sb.ToString();
        }

        static void Log(string mensaje, bool aviso = false)
        {
            if (aviso) Plugin.Logger.LogWarning("[ProgressGrid] " + mensaje);
            else if (Verbose) Plugin.Logger.LogInfo("[ProgressGrid] " + mensaje);
        }
    }
}
