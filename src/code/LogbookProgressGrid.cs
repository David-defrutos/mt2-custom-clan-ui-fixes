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
        public static bool WidenSections = true; // estirar seccion y contenedor de aliados
        public static bool FreeFlagWidth = true; // soltar el ancho de las banderitas
        public static float FlagSpacing = 6f;    // separacion entre banderitas al recolocarlas

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
            // La traza se toma AQUI, no en la primera pasada: en la primera, el layout
            // todavia no ha rehecho nada y la seccion sigue diciendo el ancho de antes.
            Trazar();
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
                Ensanchar(anchoCelda);
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

        // ------------------------------------------------------------- ancho de la seccion

        /// <summary>
        /// Lo que faltaba: que el ancho nuevo de la celda llegue de verdad a la fila de
        /// banderitas. Poner la celda a 1440 no basta —lo probado el 18-sep—, porque la
        /// seccion y sus hijos siguen midiendo lo de antes, y sobre todo porque **quien
        /// aplasta las banderitas no es la falta de sitio, es el propio layout de la fila**:
        /// `subclanVictoryLayout` es un `HorizontalLayoutGroup` con `childControlWidth=True`,
        /// y con esa bandera el layout DECIDE el ancho de cada hijo y lo reparte entre los
        /// doce. Por ancha que se ponga la seccion, mientras esa bandera siga puesta el
        /// reparto manda.
        ///
        /// Asi que se tiran tres palancas a la vez, de fuera hacia dentro, porque cual de las
        /// tres gobierna depende de componentes que solo se ven en la traza:
        ///
        ///   1. la seccion se estira a la celda, y se le quita el `ContentSizeFitter`
        ///      horizontal si lo lleva (es lo que volveria a dejarla en 710);
        ///   2. al contenedor de aliados y a sus dos filas se les pone un `LayoutElement`
        ///      con el `preferredWidth` que necesitan, por si es el layout de la seccion
        ///      quien reparte;
        ///   3. y a las dos filas se les quita `childControlWidth` y `childForceExpandWidth`,
        ///      que es la palanca que de verdad devuelve a cada banderita sus 48 px.
        ///
        /// Ninguna toca la jerarquia. Todas se apagan con `WidenSections` y `FreeFlagWidth`.
        /// </summary>
        static void Ensanchar(float anchoCelda)
        {
            if (!WidenSections && !FreeFlagWidth) return;
            if (anchoCelda <= 1f) return;

            foreach (var s in secciones)
            {
                if (s == null || s.transform is not RectTransform seccion) continue;

                if (WidenSections)
                {
                    SoltarAjustador(seccion);
                    FijarAncho(seccion, anchoCelda);
                }

                // El contenedor de aliados: se estira hasta el borde derecho de la celda,
                // descontando lo que ocupa a su izquierda y un margen.
                foreach (Transform h in seccion)
                {
                    if (!h.name.Contains("victory container")) continue;
                    float margenIzq = h is RectTransform c ? c.anchoredPosition.x : 0f;
                    float disponible = Mathf.Max(100f, anchoCelda - Mathf.Abs(margenIzq) - 24f);

                    if (WidenSections && h is RectTransform cont)
                    {
                        SoltarAjustador(cont);
                        PreferirAncho(cont, disponible);
                        FijarAncho(cont, disponible);
                    }

                    foreach (Transform f in h) Fila(f, disponible);
                    break;
                }
            }
        }

        /// <summary>Una de las dos filas de banderitas.</summary>
        static void Fila(Transform fila, float disponible)
        {
            var grupo = Componente(fila, "HorizontalLayoutGroup");

            // Cuantas banderitas hay y cuanto miden de verdad.
            int n = 0; float lado = 0f;
            foreach (Transform b in fila)
            {
                if (!b.gameObject.activeSelf) continue;
                n++;
                if (b is RectTransform rb && rb.rect.width > lado) lado = rb.rect.width;
            }
            if (n == 0) return;
            if (lado <= 1f) lado = 48f;

            if (FreeFlagWidth && grupo != null)
            {
                // LA palanca. Sin esto, lo demas no sirve de nada.
                PonerBool(grupo, "childControlWidth", false);
                PonerBool(grupo, "childForceExpandWidth", false);
            }

            float pide = n * lado + (n - 1) * FlagSpacing;
            float ancho = Mathf.Min(pide, disponible);

            if (WidenSections && fila is RectTransform rf)
            {
                SoltarAjustador(rf);
                PreferirAncho(rf, ancho);
                FijarAncho(rf, ancho);
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

        /// <summary>Un componente del objeto, buscado por nombre de tipo.</summary>
        static Component? Componente(Transform t, string nombreTipo)
        {
            foreach (var c in t.GetComponents<Component>())
                if (c != null && c.GetType().Name.Contains(nombreTipo)) return c;
            return null;
        }

        static void PonerBool(Component c, string propiedad, bool valor)
        {
            try { c.GetType().GetProperty(propiedad)?.SetValue(c, valor, null); }
            catch (Exception e) { Log($"no se pudo poner {propiedad}: {e.Message}", true); }
        }

        /// <summary>
        /// Ancho, respetando las anclas. `SetSizeWithCurrentAnchors` es lo unico que se porta
        /// igual con anclas fijas y con anclas estiradas.
        /// </summary>
        static void FijarAncho(RectTransform rt, float ancho)
        {
            try
            {
                if (Mathf.Abs(rt.rect.width - ancho) < 0.5f) return;
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ancho);
            }
            catch (Exception e) { Log($"no se pudo estirar {rt.name}: {e.Message}", true); }
        }

        /// <summary>
        /// Un `ContentSizeFitter` horizontal vuelve a imponer el ancho de antes en cuanto el
        /// layout corre otra vez, asi que se deja en Unconstrained (0).
        /// </summary>
        static void SoltarAjustador(Transform t)
        {
            var f = Componente(t, "ContentSizeFitter");
            if (f == null) return;
            try
            {
                var prop = f.GetType().GetProperty("horizontalFit");
                if (prop == null) return;
                var actual = prop.GetValue(f, null);
                if (actual != null && Convert.ToInt32(actual) == 0) return;
                prop.SetValue(f, Enum.ToObject(prop.PropertyType, 0), null);
                Log($"ajustador horizontal de \"{t.name}\" a Unconstrained");
            }
            catch (Exception e) { Log($"no se pudo soltar el ajustador de {t.name}: {e.Message}", true); }
        }

        /// <summary>
        /// `LayoutElement.preferredWidth`, creando el componente si no lo lleva: es lo que
        /// mira el layout del padre cuando es el quien reparte el ancho.
        /// </summary>
        static void PreferirAncho(Transform t, float ancho)
        {
            try
            {
                var le = Componente(t, "LayoutElement");
                if (le == null)
                {
                    var tipo = AccessTools.TypeByName("UnityEngine.UI.LayoutElement");
                    if (tipo == null) return;
                    le = t.gameObject.AddComponent(tipo);
                    if (le == null) return;
                }
                le.GetType().GetProperty("preferredWidth")?.SetValue(le, ancho, null);
                le.GetType().GetProperty("minWidth")?.SetValue(le, ancho, null);
                le.GetType().GetProperty("flexibleWidth")?.SetValue(le, 0f, null);
            }
            catch (Exception e) { Log($"no se pudo fijar el ancho de {t.name}: {e.Message}", true); }
        }

        /// <summary>
        /// Una vez por sesion y con el layout ya resuelto: quien decide los anchos dentro de
        /// una seccion. Es lo que falta para poder estirar el contenedor de aliados.
        /// </summary>
        static void Trazar()
        {
            if (trazado || !Verbose || hoja == null) return;
            trazado = true;

            int n = 0;
            foreach (var s in secciones)
            {
                n++;
                if (n > DetailSections) continue;

                string clan = "?";
                try
                {
                    var datos = FClase?.GetValue(s);
                    clan = datos?.GetType().GetMethod("GetTitle", Type.EmptyTypes)?.Invoke(datos, null) as string ?? "?";
                }
                catch { }

                Log($"  seccion {n}: {clan}{Medidas(s.transform)}{Componentes(s.transform)}");
                foreach (Transform h in s.transform)
                {
                    Log($"    parte \"{h.name}\"{Medidas(h)}{Componentes(h)}");
                    // Un nivel mas dentro del contenedor de aliados, que es lo que hay que
                    // ensanchar: ahi viven las dos filas de banderitas.
                    if (!h.name.Contains("victory container")) continue;
                    foreach (Transform f in h)
                        Log($"      fila \"{f.name}\"{Medidas(f)}{Componentes(f)}");
                }
            }
            Log($"total {secciones.Count} secciones, {porHoja} por hoja, {subpaginas} sub-paginas");
        }

        /// <summary>
        /// Los componentes que mandan en el tamano: layouts, ajustadores y LayoutElement.
        /// Por reflexion, para no referenciar UnityEngine.UI.
        /// </summary>
        static string Componentes(Transform t)
        {
            var sb = new StringBuilder();
            foreach (var c in t.GetComponents<Component>())
            {
                if (c == null) continue;
                var tipo = c.GetType();
                var nombre = tipo.Name;
                if (!nombre.Contains("LayoutGroup") && !nombre.Contains("LayoutElement")
                    && !nombre.Contains("ContentSizeFitter")) continue;

                sb.Append(' ').Append(nombre).Append('(');
                foreach (var prop in new[] { "spacing", "cellSize", "childForceExpandWidth",
                                             "childControlWidth", "childAlignment",
                                             "preferredWidth", "minWidth", "flexibleWidth",
                                             "ignoreLayout", "horizontalFit" })
                {
                    object? v = null;
                    try { v = tipo.GetProperty(prop)?.GetValue(c, null); } catch { }
                    if (v != null) sb.Append(prop).Append('=').Append(v).Append(' ');
                }
                sb.Append(')');
            }
            return sb.ToString();
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
