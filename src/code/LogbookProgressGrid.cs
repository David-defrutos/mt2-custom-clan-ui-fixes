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
        public static bool WidenSections = true; // estirar la seccion a la celda
        public static bool FreeFlagWidth = true; // soltar el ancho de las banderitas
        public static float FlagSpacing = 6f;    // separacion entre banderitas
        /// <summary>
        /// Como se reparte el ancho dentro de la seccion:
        ///   "overlay" (por defecto) — como lo dibuja el juego pero ancho: la placa de color
        ///                             se estira y las banderitas siguen encima de ella;
        ///   "inflow"                — el contenedor de aliados entra en la fila y la placa
        ///                             se estrecha a `PlaqueWidth`, porque se queda vacia.
        /// </summary>
        public static string Layout = "overlay";
        public static float PlaqueWidth = 370f;  // ancho de la placa en modo "inflow"

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
        /// Que el ancho nuevo de la celda llegue de verdad al contenido. Aqui hay DOS
        /// problemas encadenados, y se descubrieron uno detras de otro, cada uno tapando al
        /// siguiente. Los dos estan resueltos midiendo, no adivinando: la traza de esta misma
        /// clase es la que los canto.
        ///
        /// **1. Las banderitas aplastadas: `childControlWidth`.**
        /// La cuenta de los 642 px lleva a pensar que faltaba sitio, y no era eso.
        /// `subclanVictoryLayout` es un `HorizontalLayoutGroup` con `childControlWidth=True`,
        /// y con esa bandera puesta el layout **decide** el ancho de cada hijo y lo reparte
        /// entre los doce: por ancha que se ponga la seccion, las banderitas se quedan a ~24
        /// px. Quitandola, cada una recupera sus 48 y la fila pide sus 642 honestos.
        ///
        /// **2. La media hoja en blanco: `ignoreLayout` en el contenedor.**
        /// Con la celda ya a 1440 y la seccion tambien, el dibujo seguia ocupando 711 px
        /// pegados a la izquierda. El motivo, en la traza: el `LayoutElement` de
        /// `Subclan victory container` viene del juego con **`ignoreLayout = True`**, asi que
        /// el `HorizontalLayoutGroup` de la seccion **no lo cuenta**: solo coloca
        /// `Main class section` (582) y `Card mastery meter` (105), que con los 24 de
        /// separacion suman exactamente 711. El contenedor flotaba aparte, anclado, y por eso
        /// ensancharlo a mano no movia nada.
        ///
        /// Esto es de diseno del juego: con la celda original de 710 el banner de aliados se
        /// solapa encima del retrato a proposito. Con una sola columna sobra sitio, asi que se
        /// mete en la fila (`ignoreLayout = false`) y las tres partes se reparten de verdad:
        /// 582 + 24 + 658 + 24 + 105 = **1393** de los 1440. Lo que sobra se lo queda el
        /// contenedor con `flexibleWidth = 1`.
        ///
        /// Ninguna de las dos toca la jerarquia.
        ///
        /// **Y una vez hay sitio, hay que decidir que hacer con el.** Probado en partida el
        /// 18-sep: metiendo el contenedor en la fila la hoja se llena, pero la placa de color
        /// **se queda vacia**, porque las banderitas vivian encima de ella —para eso estaba el
        /// `ignoreLayout`— y se van a la derecha, sobre el pergamino. De ahi los dos modos de
        /// `Layout`:
        ///
        ///   - **`overlay`** (por defecto): se respeta el diseno del juego y se ensancha. La
        ///     placa se estira hasta el medidor de cartas —quitandole su `ContentSizeFitter`,
        ///     que es quien la clava en 582— y el contenedor sigue flotando encima, ya con
        ///     sitio para las doce banderitas.
        ///   - **`inflow`**: el contenedor entra en la fila y la placa se estrecha a
        ///     `PlaqueWidth`, lo que ocupa el retrato, ya que se queda sin banderitas.
        /// </summary>
        static void Ensanchar(float anchoCelda)
        {
            if (anchoCelda <= 1f) return;
            if (!WidenSections && !FreeFlagWidth) return;

            bool superponer = !Layout.Equals("inflow", StringComparison.OrdinalIgnoreCase);

            foreach (var s in secciones)
            {
                if (s == null || s.transform is not RectTransform seccion) continue;

                if (WidenSections)
                {
                    SoltarAjustador(seccion);
                    FijarAncho(seccion, anchoCelda);
                }

                // Las tres partes, por nombre.
                Transform? placa = null, contenedor = null, medidor = null;
                foreach (Transform h in seccion)
                {
                    if (h.name.Contains("Main class section")) placa = h;
                    else if (h.name.Contains("victory container")) contenedor = h;
                    else if (h.name.Contains("Card mastery")) medidor = h;
                }
                if (contenedor == null) continue;

                // Las filas primero: de ellas sale lo que pide el contenedor.
                float pideFila = 0f;
                foreach (Transform f in contenedor) pideFila = Mathf.Max(pideFila, Fila(f));
                if (pideFila <= 1f) continue;
                float pideContenedor = pideFila + 16f;   // relleno del VerticalLayoutGroup

                if (!WidenSections) continue;

                float hueco = LeerFloat(Componente(seccion, "HorizontalLayoutGroup"), "spacing", 24f);
                float anchoMedidor = medidor is RectTransform rm && rm.rect.width > 1f ? rm.rect.width : 105f;

                if (superponer)
                {
                    // --- Modo "overlay": el diseno del juego, pero ancho.
                    // La placa se estira hasta donde empieza el medidor de cartas, y el
                    // contenedor de aliados sigue flotando encima de ella.
                    float anchoPlaca = Mathf.Max(200f, anchoCelda - anchoMedidor - hueco * 2f);
                    if (placa != null)
                    {
                        SoltarAjustador(placa);          // su ContentSizeFitter la clava en 582
                        PreferirAncho(placa, anchoPlaca);
                        FijarAncho(placa, anchoPlaca);
                    }

                    PonerIgnoreLayout(contenedor, true);
                    float desde = contenedor is RectTransform rc ? Mathf.Abs(rc.anchoredPosition.x) : 0f;
                    float cabe = Mathf.Max(pideContenedor, anchoPlaca - desde - hueco);
                    FijarAncho(contenedor, cabe);
                }
                else
                {
                    // --- Modo "inflow": el contenedor entra en la fila y la placa se
                    // estrecha a lo que ocupa el retrato, porque se queda sin banderitas.
                    if (placa != null)
                    {
                        SoltarAjustador(placa);
                        PreferirAncho(placa, Mathf.Max(100f, PlaqueWidth));
                        FijarAncho(placa, Mathf.Max(100f, PlaqueWidth));
                    }
                    EnFila(contenedor, Mathf.Min(pideContenedor, anchoCelda - hueco));
                }
            }
        }

        /// <summary>
        /// Mete el contenedor de aliados en la fila de la seccion y le da el ancho que pide,
        /// dejandole ademas el sobrante (`flexibleWidth = 1`) para que no quede hueco.
        /// </summary>
        static void EnFila(Transform contenedor, float ancho)
        {
            var le = LayoutElementDe(contenedor);
            if (le == null) return;
            try
            {
                var t = le.GetType();
                t.GetProperty("ignoreLayout")?.SetValue(le, false, null);
                t.GetProperty("preferredWidth")?.SetValue(le, ancho, null);
                t.GetProperty("minWidth")?.SetValue(le, ancho, null);
                t.GetProperty("flexibleWidth")?.SetValue(le, 1f, null);
            }
            catch (Exception e) { Log($"no se pudo meter en fila {contenedor.name}: {e.Message}", true); }
        }

        static void PonerIgnoreLayout(Transform t, bool valor)
        {
            var le = LayoutElementDe(t);
            if (le == null) return;
            try { le.GetType().GetProperty("ignoreLayout")?.SetValue(le, valor, null); }
            catch (Exception e) { Log($"no se pudo poner ignoreLayout en {t.name}: {e.Message}", true); }
        }

        /// <summary>
        /// Una de las dos filas de banderitas. Devuelve lo que pide de ancho, que es lo que
        /// necesita saber el contenedor.
        /// </summary>
        static float Fila(Transform fila)
        {
            int n = 0; float lado = 0f;
            foreach (Transform b in fila)
            {
                if (!b.gameObject.activeSelf) continue;
                n++;
                if (b is RectTransform rb && rb.rect.width > lado) lado = rb.rect.width;
            }
            if (n == 0) return 0f;
            if (lado <= 1f) lado = 48f;

            if (FreeFlagWidth && Componente(fila, "HorizontalLayoutGroup") is Component grupo)
            {
                // LA palanca de las banderitas. Sin esto, ensanchar no sirve de nada.
                PonerBool(grupo, "childControlWidth", false);
                PonerBool(grupo, "childForceExpandWidth", false);
            }

            float pide = n * lado + (n - 1) * FlagSpacing;
            if (WidenSections) PreferirAncho(fila, pide);
            return pide;
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

        static float LeerFloat(Component? c, string propiedad, float porDefecto)
        {
            if (c == null) return porDefecto;
            try
            {
                var v = c.GetType().GetProperty(propiedad)?.GetValue(c, null);
                if (v != null) return Convert.ToSingle(v);
            }
            catch { }
            return porDefecto;
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
        static void FijarAncho(Transform? t, float ancho)
        {
            if (t is not RectTransform rt) return;
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

        /// <summary>El `LayoutElement` del objeto, creandolo si no lo lleva.</summary>
        static Component? LayoutElementDe(Transform t)
        {
            var le = Componente(t, "LayoutElement");
            if (le != null) return le;
            try
            {
                var tipo = AccessTools.TypeByName("UnityEngine.UI.LayoutElement");
                return tipo == null ? null : t.gameObject.AddComponent(tipo);
            }
            catch (Exception e)
            {
                Log($"no se pudo anadir LayoutElement a {t.name}: {e.Message}", true);
                return null;
            }
        }

        /// <summary>
        /// `LayoutElement.preferredWidth`: es lo que mira el layout del padre cuando es el
        /// quien reparte el ancho.
        /// </summary>
        static void PreferirAncho(Transform t, float ancho)
        {
            var le = LayoutElementDe(t);
            if (le == null) return;
            try
            {
                var tipo = le.GetType();
                tipo.GetProperty("preferredWidth")?.SetValue(le, ancho, null);
                tipo.GetProperty("minWidth")?.SetValue(le, ancho, null);
                tipo.GetProperty("flexibleWidth")?.SetValue(le, 0f, null);
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
