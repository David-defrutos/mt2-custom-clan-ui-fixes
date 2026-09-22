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
    /// Despeja la caja de busqueda del panel de filtros de cartas (`SearchFilterUI`, el
    /// "Search" que hay debajo de Clan / Type / Rarity / Cost / Mastery).
    ///
    /// El problema, visto en partida el 22-sep-2026: **por encima de la caja pasa un adorno**
    /// -la misma greca con el rombo en medio que separa las secciones del panel- y el texto
    /// que escribes queda cruzado por ella. No estorba solo a la vista: con el cursor y la
    /// linea a la misma altura no hay forma de leer lo que llevas escrito.
    ///
    /// El adorno es **decoracion pura**: no es el marco de la caja ni su fondo, asi que
    /// apagarlo no rompe nada. Lo unico delicado es no confundirlo con el marco de verdad, y
    /// de ahi las tres cautelas:
    ///
    ///   1. **Solo se mira fuera del propio `SearchFilterUI`.** El fondo y el marco de la caja
    ///      cuelgan del mismo objeto que el campo de texto; el adorno viene del panel que hay
    ///      por encima. Mirando solo fuera, el marco no corre peligro. Si algun dia resulta
    ///      que el adorno esta dentro, `AlsoInside = true` amplia la busqueda **sin
    ///      recompilar**.
    ///   2. **Nunca se toca un padre del campo.** Un ascendiente siempre solapa al campo, asi
    ///      que entraria en el reparto por pura geometria.
    ///   3. **Nada mas alto que el doble de la caja.** Eso deja fuera fondos de panel y
    ///      paneles enteros, que tambien solapan. El adorno es una linea fina.
    ///
    /// Se apaga el componente que pinta (`enabled = false`), no el objeto: asi el layout no se
    /// entera y no se mueve nada de sitio.
    ///
    /// Para desactivarlo: [CardFilter] Enabled = false en el config de BepInEx.
    /// </summary>
    [HarmonyPatch]
    public static class CardFilterSearch
    {
        // --- ajustes, los rellena Plugin.Awake desde el config de BepInEx ---
        public static bool Enabled = true;
        public static bool Verbose = true;
        public static bool DumpTree = true;      // volcar el arbol del panel una vez
        public static bool AlsoInside = false;   // mirar tambien dentro del SearchFilterUI
        public static float MinOverlap = 0.25f;  // cuanto del alto de la caja ha de tapar
        public static int Levels = 1;            // niveles que se sube para buscar el adorno

        static readonly FieldInfo? FCampo =
            AccessTools.Field(typeof(SearchFilterUI), "inputField");
        static readonly FieldInfo? FBoton =
            AccessTools.Field(typeof(SearchFilterUI), "clearButton");

        /// <summary>Componentes que pintan una imagen. Los textos no se tocan.</summary>
        static readonly HashSet<string> Dibujan = new() { "Image", "RawImage" };

        static bool volcado;

        // ------------------------------------------------------------------ montaje

        [HarmonyPatch(typeof(SearchFilterUI), "SetUp")]
        [HarmonyPostfix]
        static void TrasMontar(SearchFilterUI __instance)
        {
            if (!Enabled || __instance == null) return;

            Ajustar(__instance);

            // Como en las otras pantallas: el layout del panel se rehace unos frames despues
            // de montarlo, y hasta entonces las medidas son las de antes.
            if (__instance.isActiveAndEnabled)
            {
                try { __instance.StartCoroutine(AjustarUnosFrames(__instance)); }
                catch (Exception e) { Log("no se pudo encolar el reintento: " + e.Message, true); }
            }
        }

        static IEnumerator AjustarUnosFrames(SearchFilterUI ui)
        {
            for (int i = 1; i <= 20; i++)
            {
                yield return null;
                if (ui == null) yield break;
                if (i <= 3 || i == 10 || i == 20) Ajustar(ui);
            }
        }

        // ------------------------------------------------------------------ el arreglo

        static void Ajustar(SearchFilterUI ui)
        {
            try
            {
                if (FCampo?.GetValue(ui) is not Component campo || campo == null) return;
                if (campo.transform is not RectTransform rtCampo) return;

                Transform raiz = ui.transform;
                for (int i = 0; i < Levels && raiz.parent != null; i++) raiz = raiz.parent;

                if (DumpTree && !volcado)
                {
                    volcado = true;
                    Volcar(raiz, rtCampo);
                }

                Rect caja = Caja(raiz, rtCampo);
                if (caja.height <= 1f) return;   // el layout aun no ha corrido

                Transform? boton = (FBoton?.GetValue(ui) as Component)?.transform;

                int tocados = 0;
                foreach (var t in Descendientes(raiz))
                {
                    if (t == null || t == rtCampo) continue;
                    if (t is not RectTransform rt) continue;

                    // Cautela 1: el marco y el fondo de la caja cuelgan del propio
                    // SearchFilterUI. Fuera de el, lo que solape es del panel.
                    if (!AlsoInside && EsDescendiente(t, ui.transform)) continue;
                    if (boton != null && EsDescendiente(t, boton)) continue;

                    // Cautela 2: un padre del campo solapa siempre, por geometria.
                    if (EsAscendiente(t, rtCampo)) continue;

                    Rect otra = Caja(raiz, rt);
                    if (!Cruza(caja, otra)) continue;

                    foreach (var c in t.GetComponents<Component>())
                    {
                        if (c == null) continue;
                        if (!Dibujan.Contains(c.GetType().Name)) continue;
                        if (c is not Behaviour b || !b.enabled) continue;

                        b.enabled = false;
                        tocados++;
                        Log($"adorno sobre la caja de busqueda: \"{Ruta(t, raiz)}\"" +
                            $" {otra.width:0}x{otra.height:0} ({c.GetType().Name}) -> apagado" +
                            $" [caja {caja.width:0}x{caja.height:0}]");
                    }
                }

                if (tocados == 0)
                    Log("no se ha encontrado nada que tape la caja de busqueda" +
                        (AlsoInside ? "." : ": solo se ha mirado FUERA del SearchFilterUI." +
                                            " Prueba [CardFilter] AlsoInside = true, o sube Levels."));
            }
            catch (Exception e) { Log("fallo despejando la caja de busqueda: " + e.Message, true); }
        }

        /// <summary>
        /// Si `otra` tapa la caja lo bastante como para ser el adorno. La tercera cautela -que
        /// no sea mas alta que el doble de la caja- es la que deja fuera fondos y paneles.
        /// </summary>
        static bool Cruza(Rect caja, Rect otra)
        {
            if (otra.height > caja.height * 2f) return false;

            float alto = Mathf.Min(caja.yMax, otra.yMax) - Mathf.Max(caja.yMin, otra.yMin);
            float ancho = Mathf.Min(caja.xMax, otra.xMax) - Mathf.Max(caja.xMin, otra.xMin);
            if (alto <= 0f || ancho <= 0f) return false;

            return alto >= caja.height * MinOverlap && ancho >= caja.width * 0.3f;
        }

        // ------------------------------------------------------------------ utilidades

        /// <summary>
        /// El rectangulo de `t` en el espacio de `referencia`. Se mide con las esquinas del
        /// mundo porque sumar posiciones y anclajes no acierta cuando hay escalas o pivotes
        /// distintos por medio.
        /// </summary>
        static Rect Caja(Transform referencia, RectTransform t)
        {
            var esquinas = new Vector3[4];
            t.GetWorldCorners(esquinas);
            Vector3 a = referencia.InverseTransformPoint(esquinas[0]);
            Vector3 b = referencia.InverseTransformPoint(esquinas[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
                                   Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        static IEnumerable<Transform> Descendientes(Transform raiz)
        {
            foreach (Transform h in raiz)
            {
                yield return h;
                foreach (var n in Descendientes(h)) yield return n;
            }
        }

        static bool EsDescendiente(Transform t, Transform raiz)
        {
            for (var p = t; p != null; p = p.parent) if (p == raiz) return true;
            return false;
        }

        static bool EsAscendiente(Transform t, Transform hijo)
        {
            for (var p = hijo; p != null; p = p.parent) if (p == t) return true;
            return false;
        }

        static string Ruta(Transform t, Transform raiz)
        {
            var partes = new List<string>();
            for (var p = t; p != null && p != raiz; p = p.parent) partes.Add(p.name);
            partes.Reverse();
            return string.Join("/", partes);
        }

        /// <summary>
        /// Vuelca el arbol del panel una sola vez, con nombre, medidas y que pinta cada
        /// objeto. Es lo que permitio arreglar las otras pantallas sin adivinar: si el
        /// heuristico de arriba no acierta, aqui se ve exactamente que objeto es el adorno.
        /// </summary>
        static void Volcar(Transform raiz, RectTransform campo)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append("arbol del panel de filtros desde \"").Append(raiz.name).Append("\":\n");
                Rama(sb, raiz, raiz, campo, 0);
                Plugin.Logger.LogInfo("[CardFilter] " + sb);
            }
            catch (Exception e) { Log("no se pudo volcar el arbol: " + e.Message, true); }
        }

        static void Rama(StringBuilder sb, Transform t, Transform raiz, RectTransform campo, int nivel)
        {
            if (nivel > 8) return;

            sb.Append(' ', nivel * 2).Append(t == campo ? "* " : "- ").Append(t.name);
            if (t is RectTransform rt) sb.Append($" {rt.rect.width:0}x{rt.rect.height:0}");
            if (!t.gameObject.activeSelf) sb.Append(" (apagado)");

            var pinta = new List<string>();
            foreach (var c in t.GetComponents<Component>())
                if (c != null) pinta.Add(c.GetType().Name);
            if (pinta.Count > 0) sb.Append("  [").Append(string.Join(", ", pinta)).Append(']');
            sb.Append('\n');

            foreach (Transform h in t) Rama(sb, h, raiz, campo, nivel + 1);
        }

        static void Log(string mensaje, bool aviso = false)
        {
            if (aviso) Plugin.Logger.LogWarning("[CardFilter] " + mensaje);
            else if (Verbose) Plugin.Logger.LogInfo("[CardFilter] " + mensaje);
        }
    }
}

// 2026-09-22-2245||claude-mt2-CustomClanUIFixes||plugins/frutos-CustomClanUIFixes/src/code/CardFilterSearch.cs||fichero nuevo: apaga el adorno que cruza la caja de busqueda del panel de filtros de cartas (postfix de SearchFilterUI.SetUp), con volcado del arbol del panel
