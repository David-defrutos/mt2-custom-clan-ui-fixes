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
    /// Pone la hoja de progreso del logbook (`CompendiumSectionChecklist`, la de
    /// "Progress Record") a una columna y la pagina, para que se vean TODOS los clanes y las
    /// banderitas de aliados dejen de aplastarse.
    ///
    /// Como esta montada, medido en partida el 18-sep-2026:
    ///   - `StandardChecklistPage` tiene la rejilla "All launch clans layout", **1440x846**,
    ///     `GridLayoutGroup` con celda **710x157** y separacion **20x14**. Ahi entran 2
    ///     columnas x 5 filas = **10 secciones**... y con clanes modeados hay **17**: las
    ///     otras siete se dibujan por debajo del borde de la hoja y **no hay forma de
    ///     verlas**. No es que esten apretadas: es que no estan.
    ///   - Cada seccion mide 710x157 y dentro lleva `Main class section` (582),
    ///     `Subclan victory container` (370) y `Card mastery meter` (105).
    ///   - Las banderitas de aliados viven en dos `HorizontalLayoutGroup` de **354 px** con
    ///     `childControlWidth=True`: `Subclan victory layout` (12 aliados normales) y
    ///     `Subclan victory layout crew` (6 de tripulacion). Doce banderitas de 48 px con 6
    ///     de separacion piden 642 px, y solo hay 354, asi que el layout las aplasta a ~24 y
    ///     el dibujo se solapa. La fila de tripulacion, con seis, cabe de sobra.
    ///
    /// La cuenta que resuelve las dos cosas a la vez: **una sola columna**. La celda pasa de
    /// 710 a 1440, el contenedor de aliados se ensancha con ella y las doce banderitas caben
    /// sin encoger. El precio es que por hoja entran 5 secciones en vez de 10, y de ahi la
    /// paginacion.
    ///
    /// Paginar aqui es el mismo truco que en la pagina de artefactos y por los mismos
    /// motivos: **encender y apagar secciones enteras**, sin clonar hojas ni tocar la
    /// jerarquia, y dejando que las flechas del juego hagan el trabajo.
    /// `CompendiumSectionChecklist.TurnPage(int)` cambia de hoja; se intercepta para que
    /// primero recorra las sub-paginas de la hoja estandar y solo cambie de hoja cuando se
    /// acaban. `PageCount` es `checklistPages.Count` a secas, asi que sumarle las
    /// sub-paginas deja bien el "Page N of M".
    ///
    /// Para desactivarlo: [ProgressGrid] Enabled = false en el config de BepInEx.
    /// </summary>
    [HarmonyPatch]
    public static class LogbookProgressGrid
    {
        // --- ajustes, los rellena Plugin.Awake desde el config de BepInEx ---
        public static bool Enabled = true;
        public static int Columns = 1;        // columnas de la rejilla; 2 = como el juego
        public static int RowsPerPage = 0;    // filas por hoja; 0 = las que quepan de alto
        public static bool Verbose = true;
        public static int DetailSections = 1; // secciones que se vuelcan con todo el detalle

        static readonly FieldInfo? FPaginas =
            AccessTools.Field(typeof(CompendiumSectionChecklist), "checklistPages");
        static readonly FieldInfo? FPaginaActual =
            AccessTools.Field(typeof(CompendiumSectionChecklist), "currentPage");
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

        // La hoja estandar viva y sus secciones, en el orden en que las creo el juego.
        static StandardChecklistPage? hoja;
        static readonly List<Component> secciones = new();
        static int porHoja = 10, subpagina, subpaginas = 1;
        static bool trazado;
        static Vector2 celdaOriginal;
        static bool celdaGuardada;

        // ------------------------------------------------------------------ montaje

        [HarmonyPatch(typeof(StandardChecklistPage), "Initialize")]
        [HarmonyPostfix]
        static void TrasInicializarHoja(StandardChecklistPage __instance)
        {
            hoja = __instance;
            secciones.Clear();
            subpagina = 0;
            if (FSecciones?.GetValue(__instance) is IEnumerable lista)
                foreach (var s in lista)
                    if (s is Component c && c != null) secciones.Add(c);

            if (!Enabled || secciones.Count == 0) return;
            Ajustar();
            if (__instance.isActiveAndEnabled)
            {
                try { __instance.StartCoroutine(AjustarUnosFrames(__instance)); }
                catch (Exception e) { Log("no se pudo encolar el reintento: " + e.Message, true); }
            }
        }

        static IEnumerator AjustarUnosFrames(StandardChecklistPage pagina)
        {
            for (int i = 0; i < 5; i++)
            {
                yield return null;
                if (pagina == null) yield break;
                Ajustar();
            }
        }

        /// <summary>
        /// Deja la rejilla a `Columns` columnas, cuenta cuantas secciones entran por hoja y
        /// enciende las de la sub-pagina actual.
        /// </summary>
        static void Ajustar()
        {
            if (!Enabled || hoja == null || secciones.Count == 0) return;
            try
            {
                if (FLayoutTodos?.GetValue(hoja) is not Component layout) return;
                if (layout.transform is not RectTransform rt) return;
                var rejilla = Rejilla(layout.transform);
                if (rejilla == null) return;

                float ancho = rt.rect.width, alto = rt.rect.height;
                if (ancho <= 1f || alto <= 1f) return;   // el layout aun no ha corrido

                var celda = LeerVector(rejilla, "cellSize");
                var hueco = LeerVector(rejilla, "spacing");
                if (celda.y <= 1f) return;
                if (!celdaGuardada) { celdaOriginal = celda; celdaGuardada = true; }

                // --- una sola columna: la celda ocupa la hoja entera
                int columnas = Mathf.Max(1, Columns);
                float anchoCelda = (ancho - (columnas - 1) * hueco.x) / columnas;
                if (Mathf.Abs(anchoCelda - celda.x) > 0.5f)
                {
                    EscribirVector(rejilla, "cellSize", new Vector2(anchoCelda, celdaOriginal.y));
                    Log($"celda {celdaOriginal.x:0}x{celdaOriginal.y:0} -> {anchoCelda:0}x{celdaOriginal.y:0} " +
                        $"({columnas} columna(s) en {ancho:0} px)");
                }

                // --- cuantas filas caben de alto
                int filas = RowsPerPage > 0
                    ? RowsPerPage
                    : Mathf.Max(1, Mathf.FloorToInt((alto + hueco.y) / (celdaOriginal.y + hueco.y)));
                porHoja = Mathf.Max(1, filas * columnas);

                int nuevas = Mathf.CeilToInt((float)secciones.Count / porHoja);
                if (nuevas != subpaginas)
                {
                    subpaginas = nuevas;
                    Log($"{secciones.Count} secciones, {porHoja} por hoja ({columnas}x{filas}) " +
                        $"-> {subpaginas} sub-paginas");
                }
                subpagina = Mathf.Clamp(subpagina, 0, subpaginas - 1);
                Pintar();
                Trazar(layout, rejilla, ancho, alto);
            }
            catch (Exception e)
            {
                Log("fallo ajustando la hoja: " + e, true);
            }
        }

        /// <summary>Enciende las secciones de la sub-pagina actual y apaga el resto.</summary>
        static void Pintar()
        {
            for (int i = 0; i < secciones.Count; i++)
            {
                var c = secciones[i];
                if (c == null) continue;
                bool visible = subpaginas <= 1 || (i / porHoja) == subpagina;
                if (c.gameObject.activeSelf != visible) c.gameObject.SetActive(visible);
            }
        }

        // ------------------------------------------------------------------ paginacion

        /// <summary>
        /// Las flechas. Mientras queden sub-paginas dentro de la hoja estandar se cambia de
        /// sub-pagina y NO se pasa de hoja; cuando se acaban, se deja pasar al original.
        /// </summary>
        [HarmonyPatch(typeof(CompendiumSectionChecklist), "TurnPage")]
        [HarmonyPrefix]
        static bool AntesDePasar(CompendiumSectionChecklist __instance, int dir)
        {
            if (!Enabled || subpaginas <= 1 || hoja == null) return true;
            if (!EstamosEnLaEstandar(__instance))
            {
                // Se viene de otra hoja: al entrar, por el lado que corresponda.
                subpagina = dir < 0 ? subpaginas - 1 : 0;
                return true;
            }

            int destino = subpagina + dir;
            if (destino < 0 || destino >= subpaginas) return true;   // se sale: cambia de hoja

            subpagina = destino;
            Pintar();
            Log($"sub-pagina {subpagina + 1} de {subpaginas}");
            return false;
        }

        /// <summary>
        /// Y lo que enciende esas flechas. El original mira si hay otra hoja a la que ir;
        /// aqui ademas vale si queda sub-pagina dentro de la hoja estandar.
        /// </summary>
        [HarmonyPatch(typeof(PaginatedCompendiumSection), "CanTurnPage")]
        [HarmonyPrefix]
        static bool AntesDePoderPasar(PaginatedCompendiumSection __instance,
                                      PageTurnZone.TurnDir dir, ref bool __result)
        {
            if (!Enabled || subpaginas <= 1) return true;
            if (__instance is not CompendiumSectionChecklist checklist) return true;
            if (!EstamosEnLaEstandar(checklist)) return true;

            int destino = subpagina + (int)dir;
            if (destino >= 0 && destino < subpaginas) { __result = true; return false; }
            return true;   // fuera de las sub-paginas manda el original
        }

        /// <summary>El "Page N of M": PageCount es checklistPages.Count, y la hoja estandar
        /// pasa a valer por todas sus sub-paginas.</summary>
        [HarmonyPatch(typeof(CompendiumSectionChecklist), "get_PageCount")]
        [HarmonyPostfix]
        static void TrasContarPaginas(ref int __result)
        {
            if (Enabled && subpaginas > 1) __result += subpaginas - 1;
        }

        /// <summary>Al volver a la hoja estandar desde otra, hay que repintar la sub-pagina.</summary>
        [HarmonyPatch(typeof(CompendiumSectionChecklist), "RefreshPage")]
        [HarmonyPostfix]
        static void TrasRefrescar(CompendiumSectionChecklist __instance)
        {
            if (Enabled && subpaginas > 1 && EstamosEnLaEstandar(__instance)) Pintar();
        }

        static bool EstamosEnLaEstandar(CompendiumSectionChecklist seccion)
        {
            var actual = FPaginaActual?.GetValue(seccion);
            return actual != null && hoja != null && ReferenceEquals(actual, hoja);
        }

        // ------------------------------------------------------------------ utilidades

        /// <summary>El GridLayoutGroup del objeto, por nombre de tipo (sin UnityEngine.UI).</summary>
        static Component? Rejilla(Transform t)
        {
            foreach (var c in t.GetComponents<Component>())
                if (c != null && c.GetType().Name.Contains("GridLayoutGroup")) return c;
            return null;
        }

        static Vector2 LeerVector(Component c, string propiedad)
        {
            try
            {
                var v = c.GetType().GetProperty(propiedad)?.GetValue(c, null);
                if (v is Vector2 v2) return v2;
            }
            catch { }
            return Vector2.zero;
        }

        static void EscribirVector(Component c, string propiedad, Vector2 valor)
        {
            try { c.GetType().GetProperty(propiedad)?.SetValue(c, valor, null); }
            catch (Exception e) { Log($"no se pudo poner {propiedad}: {e.Message}", true); }
        }

        /// <summary>Una vez por sesion, el detalle de lo que hay montado.</summary>
        static void Trazar(Component layout, Component rejilla, float ancho, float alto)
        {
            if (trazado || !Verbose) return;
            trazado = true;

            Log($"rejilla \"{layout.name}\" {ancho:0}x{alto:0}, celda {LeerVector(rejilla, "cellSize")}, " +
                $"hueco {LeerVector(rejilla, "spacing")}");
            if (FLayoutIniciales?.GetValue(hoja) is Component otro)
                Log($"layout de clanes iniciales (apagado): \"{otro.name}\"");

            int n = 0;
            foreach (var s in secciones)
            {
                n++;
                string clan = "?";
                try
                {
                    var datos = FClase?.GetValue(s);
                    clan = datos?.GetType().GetMethod("GetTitle", Type.EmptyTypes)?.Invoke(datos, null) as string ?? "?";
                }
                catch { }

                if (n > DetailSections) continue;
                Log($"  seccion {n}: {clan}{Medidas(s.transform)}");
                foreach (var (nombre, campo) in new[] { ("normales", FFila), ("tripulacion", FFilaCrew) })
                {
                    if (campo?.GetValue(s) is not Component fila) continue;
                    int hijos = 0;
                    float anchoHijo = 0f;
                    foreach (Transform h in fila.transform)
                    {
                        hijos++;
                        if (anchoHijo <= 0f && h is RectTransform hrt) anchoHijo = hrt.rect.width;
                    }
                    Log($"    fila {nombre}:{Medidas(fila.transform)} {hijos} banderitas de {anchoHijo:0} px");
                }
                var partes = new StringBuilder();
                foreach (Transform h in s.transform) partes.Append($" [{h.name}{Medidas(h)}]");
                Log($"    partes:{partes}");
            }
            Log($"total {n} secciones");
        }

        static string Medidas(Transform? t) =>
            t is RectTransform rt ? $" {rt.rect.width:0}x{rt.rect.height:0}" : "";

        static void Log(string mensaje, bool aviso = false)
        {
            if (aviso) Plugin.Logger.LogWarning("[ProgressGrid] " + mensaje);
            else if (Verbose) Plugin.Logger.LogInfo("[ProgressGrid] " + mensaje);
        }
    }
}
