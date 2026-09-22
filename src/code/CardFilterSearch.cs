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
    /// **Lo que es, medido el 22-sep con el volcado del arbol**: `Content/Bottom divider`
    /// (400x80), el remate de abajo del panel, con tres imagenes: `divider left` 164x28,
    /// `divider center` 44x44 -el rombo- y `divider right` 164x28 -la cruz de la punta-. Es
    /// **hermano** de `Search filter` dentro de `Content`, asi que la cautela 1 no lo protege.
    ///
    /// **Y por que la primera version no hacia nada**: `SetUp` corre con el panel **apagado**.
    /// En ese momento todo mide 0x0 -el volcado lo dejo claro: 0x0 hasta el ultimo boton-, y
    /// como el objeto no esta activo tampoco se podia encolar el reintento. Se media una vez,
    /// a ciegas, y ya. Ahora un vigilante (`VigiaBusqueda`) se cuelga del propio
    /// `SearchFilterUI` y ajusta **cada vez que el panel se enciende**, unos frames despues,
    /// cuando el layout ya ha colocado las cosas. Es la regla 4 de
    /// `docs/referencia/unity-ui-layouts.md` otra vez: un objeto apagado no pasa por el layout.
    ///
    /// Tambien se saltan los objetos apagados al buscar: la lista desplegable de `Mastery`
    /// esta cerrada (apagada) con un rect viejo que cae encima de la caja, y apagarle las
    /// imagenes la dejaria sin fondo al abrirla.
    ///
    /// Para desactivarlo: [CardFilter] Enabled = false en el config de BepInEx.
    /// </summary>
    [HarmonyPatch]
    public static class CardFilterSearch
    {
        // --- ajustes, los rellena Plugin.Awake desde el config de BepInEx ---
        public static bool Enabled = true;
        public static bool Verbose = false;      // en la publicada, apagado; en local, en el .cfg
        public static bool DumpTree = false;     // volcar el arbol del panel una vez
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
        static bool avisado;   // el "no he encontrado nada", una vez por partida
        static int apagados;   // en todas las pasadas: en la ultima ya no queda nada que apagar

        // ------------------------------------------------------------------ montaje

        [HarmonyPatch(typeof(SearchFilterUI), "SetUp")]
        [HarmonyPostfix]
        static void TrasMontar(SearchFilterUI __instance)
        {
            if (!Enabled || __instance == null) return;

            // El panel se monta APAGADO: aqui todo mide 0x0 y no se puede encolar nada. El
            // vigilante ajusta cada vez que se enciende.
            try
            {
                var vigia = __instance.GetComponent<VigiaBusqueda>();
                if (vigia == null) vigia = __instance.gameObject.AddComponent<VigiaBusqueda>();
                vigia.ui = __instance;

                // Si ya estaba encendido, su OnEnable salto al anadirlo, antes de tener `ui`.
                if (__instance.isActiveAndEnabled) vigia.Arrancar();
            }
            catch (Exception e) { Log("no se pudo colgar el vigilante del panel: " + e.Message, true); }
        }

        internal static IEnumerator AjustarUnosFrames(SearchFilterUI ui)
        {
            // Como en las otras pantallas: el layout se rehace unos frames despues de
            // encenderse, y hasta entonces las medidas son las de antes.
            for (int i = 1; i <= 20; i++)
            {
                yield return null;
                if (ui == null) yield break;
                if (i <= 3 || i == 10 || i == 20) Ajustar(ui, i == 20);
            }
        }

        // ------------------------------------------------------------------ el arreglo

        static void Ajustar(SearchFilterUI ui, bool ultima)
        {
            try
            {
                if (FCampo?.GetValue(ui) is not Component campo || campo == null) return;
                if (campo.transform is not RectTransform rtCampo) return;

                Transform raiz = ui.transform;
                for (int i = 0; i < Levels && raiz.parent != null; i++) raiz = raiz.parent;

                Rect caja = Caja(raiz, rtCampo);
                if (caja.height <= 1f) return;   // el layout aun no ha corrido

                // El volcado, DESPUES del layout: el primero salio entero a 0x0.
                if (DumpTree && !volcado)
                {
                    volcado = true;
                    Volcar(raiz, rtCampo);
                }

                Transform? boton = (FBoton?.GetValue(ui) as Component)?.transform;

                int tocados = 0;
                foreach (var t in Descendientes(raiz))
                {
                    if (t == null || t == rtCampo) continue;
                    if (t is not RectTransform rt) continue;

                    // Lo apagado no se ve y su rect es viejo. La lista de Mastery, cerrada,
                    // cae encima de la caja: apagarle el fondo la estropearia al abrirla.
                    if (!t.gameObject.activeInHierarchy) continue;

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
                        apagados++;
                        Log($"adorno sobre la caja de busqueda: \"{Ruta(t, raiz)}\"" +
                            $" {otra.width:0}x{otra.height:0} ({c.GetType().Name}) -> apagado" +
                            $" [caja {caja.width:0}x{caja.height:0}]");
                    }
                }

                if (apagados == 0 && ultima && !avisado)
                {
                    avisado = true;
                    Log("no se ha encontrado nada que tape la caja de busqueda" +
                        (AlsoInside ? "." : ": solo se ha mirado FUERA del SearchFilterUI." +
                                            " Prueba [CardFilter] AlsoInside = true, o sube Levels."));
                }
            }
            catch (Exception e) { Log("fallo despejando la caja de busqueda: " + e.Message, true); }
        }

        /// <summary>
        /// Si `otra` tapa la caja lo bastante como para ser el adorno. La tercera cautela -que
        /// no sea mas alta que el doble de la caja- es la que deja fuera fondos y paneles.
        ///
        /// A lo ancho vale cualquiera de las dos: que cruce un 30% de la caja (las rayas, 164
        /// px) **o** que la mitad de si misma este encima (el rombo, 44 px sobre una caja de
        /// 368: con solo la primera regla se quedaba, medido el 22-sep).
        /// </summary>
        static bool Cruza(Rect caja, Rect otra)
        {
            if (otra.height > caja.height * 2f) return false;

            float alto = Mathf.Min(caja.yMax, otra.yMax) - Mathf.Max(caja.yMin, otra.yMin);
            float ancho = Mathf.Min(caja.xMax, otra.xMax) - Mathf.Max(caja.xMin, otra.xMin);
            if (alto <= 0f || ancho <= 0f) return false;

            return alto >= caja.height * MinOverlap
                && (ancho >= caja.width * 0.3f || ancho >= otra.width * 0.5f);
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

    /// <summary>
    /// Se cuelga del `SearchFilterUI` y lanza el ajuste cada vez que el panel se enciende. Hace
    /// falta porque `SetUp` corre con el panel apagado: ni hay medidas ni se puede arrancar una
    /// corrutina desde ahi.
    /// </summary>
    internal sealed class VigiaBusqueda : MonoBehaviour
    {
        internal SearchFilterUI? ui;

        void OnEnable() => Arrancar();

        internal void Arrancar()
        {
            if (ui == null || !CardFilterSearch.Enabled || !isActiveAndEnabled) return;
            StartCoroutine(CardFilterSearch.AjustarUnosFrames(ui));
        }
    }
}

// 2026-09-22-2245||claude-mt2-CustomClanUIFixes||plugins/frutos-CustomClanUIFixes/src/code/CardFilterSearch.cs||fichero nuevo: apaga el adorno que cruza la caja de busqueda del panel de filtros de cartas (postfix de SearchFilterUI.SetUp), con volcado del arbol del panel
// 2026-09-22-2327||claude-mt2-CustomClanUIFixes||plugins/frutos-CustomClanUIFixes/src/code/CardFilterSearch.cs||valores iniciales Verbose true->false y DumpTree true->false, igual que en Plugin.cs
// 2026-09-22-2341||claude-mt2-CustomClanUIFixes||plugins/frutos-CustomClanUIFixes/src/code/CardFilterSearch.cs||el ajuste pasa de SetUp (panel apagado, todo a 0x0) a un vigilante VigiaBusqueda que ajusta en cada OnEnable; se saltan objetos apagados; volcado despues del layout; aviso de 'nada encontrado' solo una vez
// 2026-09-23-0000||claude-mt2-CustomClanUIFixes||plugins/frutos-CustomClanUIFixes/src/code/CardFilterSearch.cs||Cruza acepta tambien lo que tiene la mitad de su ancho encima de la caja (el rombo de 44 px se quedaba); el aviso de 'nada encontrado' cuenta lo apagado en todas las pasadas, no solo en la ultima
