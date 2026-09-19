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
        ///   "inflow" (por defecto) — el contenedor de aliados entra en la fila, y la placa
        ///                            se queda todo lo que sobra, asi que su franja de color
        ///                            llega hasta las banderitas;
        ///   "overlay"              — se intento respetar el diseno del juego ensanchando la
        ///                            placa con las banderitas encima. **Descartado**: ver
        ///                            `Ensanchar`.
        /// </summary>
        public static string Layout = "inflow";
        public static float PlaqueWidth = 0f;        // 0 = lo que sobre; >0 = fijo
        public static bool StretchPlaqueFill = true; // estirar la franja de color de la placa
        public static bool DumpTree = true;          // volcar el arbol de la primera seccion
        public static bool BalanceFlagRows = true;   // repartir las banderitas 9 y 9
        public static bool FlagAlignLeft = true;     // pegarlas a la izquierda de la cinta
        public static float RibbonExtra = 0f;        // px de mas para la cinta de color

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
            // Cinco frames seguidos y luego tres tardias. Las tardias hacen falta porque el
            // layout de la seccion se rehace DESPUES de la quinta -se vio en la traza: la
            // cinta volvia a medir 370 al empezar cada pasada- y con ella se movia el medidor
            // de cartas, que es contra lo que se mide.
            for (int i = 1; i <= 40; i++)
            {
                yield return null;
                if (pagina == null) yield break;
                if (i <= 5 || i == 10 || i == 20 || i == 40) Ajustar();
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
                Volcar();
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

        // ------------------------------------------------------------------ volcado

        static bool volcado;

        /// <summary>
        /// El arbol entero de la primera seccion, una vez por sesion: nombre, ancho, y si el
        /// objeto **pinta** algo (`Image`, `RawImage`, texto). Tres rondas seguidas se ha
        /// intentado ensanchar "la barra de color" apuntando al objeto equivocado —primero la
        /// seccion, luego el contenedor, luego `Level section`, que resulto medir 212 px y ser
        /// solo el texto—, asi que aqui se mira la jerarquia entera de una vez en vez de ir
        /// adivinando nombres de uno en uno.
        /// </summary>
        static void Volcar()
        {
            if (volcado || !DumpTree || !Verbose) return;
            if (secciones.Count == 0 || secciones[0] == null) return;
            volcado = true;

            Log("---- arbol de la primera seccion ----");
            VolcarRama(secciones[0].transform, 0);
            Log("---- fin del arbol ----");
        }

        static void VolcarRama(Transform t, int nivel)
        {
            if (nivel > 5) return;

            string pinta = "";
            foreach (var c in t.GetComponents<Component>())
            {
                if (c == null) continue;
                var n = c.GetType().Name;
                if (n.Contains("Image") || n.Contains("Text") || n.Contains("Sprite")
                    || n.Contains("Mask") || n.Contains("Canvas"))
                    pinta += " " + n;
            }

            Log($"{new string(' ', nivel * 2)}[{nivel}] \"{t.name}\"" +
                $"{(t.gameObject.activeSelf ? "" : " (apagado)")}{Medidas(t)}{pinta}");

            foreach (Transform h in t) VolcarRama(h, nivel + 1);
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
                if (FlagAlignLeft && Componente(contenedor, "VerticalLayoutGroup") is Component vg)
                    PonerAlineacion(vg, 3);

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
                    // --- Modo "inflow": el contenedor de aliados entra en la fila, pega las
                    // banderitas a su sitio sin holgura, y **la placa se queda todo lo que
                    // sobra**, asi que su franja de color llega hasta las banderitas.
                    EnFila(contenedor, pideContenedor);

                    if (placa != null)
                    {
                        float anchoPlaca = PlaqueWidth > 0f
                            ? PlaqueWidth
                            : Mathf.Max(200f, anchoCelda - pideContenedor - anchoMedidor - hueco * 2f);

                        SoltarAjustador(placa);
                        PreferirAncho(placa, anchoPlaca);
                        PonerFlexible(placa, 1f);   // y lo que quede suelto, tambien para ella
                        FijarAncho(placa, anchoPlaca);

                        // Y la franja de color, que es el ultimo hijo de la placa, se estira
                        // **mas alla de la placa** para que pase por debajo de las banderitas
                        // y llegue al final, como el banner del juego.
                        if (StretchPlaqueFill)
                        {
                            // Antes de medir, que el layout termine: si no, el medidor de
                            // cartas todavia no esta en su sitio y la distancia sale corta.
                            ReconstruirLayout(seccion);
                            EstirarFranja(seccion, placa, medidor, anchoPlaca,
                                          pideContenedor + hueco * 2f);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Mete el contenedor de aliados en la fila de la seccion con el ancho justo que pide
        /// y **sin holgura** (`flexibleWidth = 0`): el sobrante es para la placa. Con holgura
        /// aqui, el contenedor se quedaba los ~900 px de la fila y su `VerticalLayoutGroup`
        /// centraba las banderitas, dejando hueco a los dos lados.
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
                t.GetProperty("flexibleWidth")?.SetValue(le, 0f, null);
            }
            catch (Exception e) { Log($"no se pudo meter en fila {contenedor.name}: {e.Message}", true); }
        }

        static void PonerFlexible(Transform t, float valor)
        {
            var le = LayoutElementDe(t);
            if (le == null) return;
            try { le.GetType().GetProperty("flexibleWidth")?.SetValue(le, valor, null); }
            catch (Exception e) { Log($"no se pudo dar holgura a {t.name}: {e.Message}", true); }
        }

        /// <summary>
        /// La franja de color: el ultimo hijo de la placa. Se le da un ancho que **se sale de
        /// la placa** por la derecha, tanto como ocupa el contenedor de aliados, para que la
        /// barra pase por debajo de las banderitas y llegue al final, que es como lo dibuja el
        /// juego con pocos clanes.
        ///
        /// Puede salirse porque en Unity un hijo no esta recortado por el rect del padre
        /// mientras no haya mascara; y queda por DEBAJO de las banderitas porque la placa va
        /// antes que el contenedor en la jerarquia, y ese es el orden en que se dibuja.
        ///
        /// El ancho se calcula desde lo que ocupan los hermanos anteriores —el retrato y el
        /// nombre, que no cambian— y no desde el ancho actual de la franja, para que repetir
        /// la pasada cinco frames seguidos de el mismo resultado y no la vaya alargando.
        ///
        /// **Y hay que sacarla del layout de la placa.** Primer intento: darle un
        /// `preferredWidth` grande. No basta —crecio un poco y se planto—, porque el
        /// `HorizontalLayoutGroup` de la placa reparte el ancho DE LA PLACA entre sus hijos:
        /// por mucho que la franja pida 1100, si en la placa quedan 424 libres, 424 le da. Con
        /// `ignoreLayout = true` el grupo deja de tocarla y el ancho que se le ponga se queda;
        /// a cambio tampoco la coloca, asi que hay que sostenerle el borde izquierdo a mano
        /// (si no, al crecer con el pivote centrado se iria media franja hacia la izquierda).
        /// </summary>
        static void EstirarFranja(RectTransform seccion, Transform placa, Transform? medidor,
                                  float anchoPlaca, float sobresale)
        {
            // Solo cuando la placa ya tiene su ancho definitivo: antes de eso el layout aun
            // no ha colocado la franja y se congelaria en un sitio que no es.
            if (placa is not RectTransform rp || Mathf.Abs(rp.rect.width - anchoPlaca) > 1f) return;

            float huecoPlaca = LeerFloat(Componente(placa, "HorizontalLayoutGroup"), "spacing", 0f);

            // La barra es "Clan Ribbon container", hermana del texto y del retrato. Costo tres
            // rondas dar con ella: se probo con la seccion, con el contenedor de aliados y con
            // "Level section", que resulto medir 212 px y llevar solo el nombre y el nivel.
            // Dentro de la cinta van la imagen de color, la mascara con el icono de clan al
            // fondo, dos filetes y la punta de flecha del final.
            Transform? cinta = null;
            float antes = 0f;
            foreach (Transform h in placa)
            {
                if (!h.gameObject.activeSelf) continue;
                if (h.name.Contains("Ribbon container")) { cinta = h; break; }
                antes += (h is RectTransform r ? r.rect.width : 0f) + huecoPlaca;
            }
            if (cinta is not RectTransform rc) return;

            float anchoViejo = rc.rect.width;

            // El destino **se mide, no se calcula**. La cuenta (placa + hueco + contenedor +
            // hueco) daba 1076 y el medidor de cartas esta de verdad en ~1465: la seccion no
            // coloca sus tres partes en una fila limpia -se solapan, y alguna trae su propio
            // `ignoreLayout`-, asi que sumar anchos no vale. Midiendo la distancia real entre
            // el borde izquierdo de la cinta y el del medidor se acierta pase lo que pase con
            // el layout, y ademas se corrige solo en cada pasada.
            // Se calcula por las dos vias y se coge la mayor. La medida es la buena cuando
            // el layout ya ha colocado el medidor; cuando no, sale corta -288 px en vez de
            // ~1250- y encogeria la cinta por debajo de lo que ya funcionaba. Quedandose con
            // la mayor, el peor caso es el resultado de la version anterior, nunca peor.
            float calculado = Mathf.Max(0f, anchoPlaca - antes) + Mathf.Max(0f, sobresale);
            float medido = DistanciaHasta(seccion, rc, medidor);
            float objetivo = Mathf.Max(calculado, medido) + RibbonExtra;
            if (anchoViejo <= 1f || Mathf.Abs(anchoViejo - objetivo) < 0.5f) return;
            float crece = objetivo - anchoViejo;

            PonerIgnoreLayout(cinta, true);
            EstirarManteniendoIzquierda(rc, objetivo);
            int tocados = EstirarDentro(cinta, anchoViejo, crece, 0);

            Log($"cinta \"{cinta.name}\" {anchoViejo:0} -> {objetivo:0} px " +
                $"(calculado {calculado:0}, medido {medido:0} hasta " +
                $"\"{(medidor != null ? medidor.name : "?")}\", placa {anchoPlaca:0}, " +
                $"antes {antes:0}, extra {RibbonExtra:0}, {tocados} pieza(s) dentro)");
        }

        static readonly MethodInfo? MReconstruir = AccessTools.Method(
            AccessTools.TypeByName("UnityEngine.UI.LayoutRebuilder"), "ForceRebuildLayoutImmediate");

        /// <summary>
        /// Fuerza al layout a resolverse YA. Sin esto se mide sobre posiciones a medio hacer.
        /// </summary>
        static void ReconstruirLayout(RectTransform rt)
        {
            try { MReconstruir?.Invoke(null, new object[] { rt }); }
            catch (Exception e) { Log("no se pudo reconstruir el layout: " + e.Message, true); }
        }

        /// <summary>
        /// Cuanto hay, en unidades de la seccion, desde el borde izquierdo de `desde` hasta el
        /// borde izquierdo de `hasta`. Devuelve 0 si no se puede medir.
        /// </summary>
        static float DistanciaHasta(RectTransform seccion, RectTransform desde, Transform? hasta)
        {
            if (hasta is not RectTransform rh) return 0f;
            try
            {
                var esquinas = new Vector3[4];
                desde.GetWorldCorners(esquinas);
                float x0 = seccion.InverseTransformPoint(esquinas[0]).x;
                rh.GetWorldCorners(esquinas);
                float x1 = seccion.InverseTransformPoint(esquinas[0]).x;
                return Mathf.Max(0f, x1 - x0);
            }
            catch (Exception e)
            {
                Log("no se pudo medir la distancia al medidor: " + e.Message, true);
                return 0f;
            }
        }

        /// <summary>
        /// Las piezas de dentro de la cinta. Crecen **lo mismo** que la cinta, no hasta su
        /// ancho: asi los filetes, que van 16 px mas estrechos que el fondo, conservan su
        /// margen en vez de igualarse. Se estira lo que ya abarcaba la cinta entera (el color,
        /// la mascara, los filetes) y se deja en paz lo que mide otra cosa, como el icono de
        /// clan del fondo. La punta de flecha no se estira: **se lleva al extremo**, que es lo
        /// que le corresponde a una punta.
        /// </summary>
        static int EstirarDentro(Transform padre, float anchoViejo, float crece, int nivel)
        {
            if (nivel > 3 || anchoViejo <= 1f) return 0;
            int n = 0;
            foreach (Transform h in padre)
            {
                if (!h.gameObject.activeSelf || h is not RectTransform rt) continue;

                if (h.name.Contains("Edge"))
                {
                    var q = rt.localPosition;
                    q.x += crece;
                    rt.localPosition = q;
                    n++;
                    continue;
                }

                // Margen de 24 px para que entren los filetes (354 de una cinta de 370).
                float falta = anchoViejo - rt.rect.width;
                if (falta >= -1f && falta <= 24f)
                {
                    EstirarManteniendoIzquierda(rt, rt.rect.width + crece);
                    n++;
                }
                n += EstirarDentro(h, anchoViejo, crece, nivel + 1);
            }
            return n;
        }

        /// <summary>
        /// Cambia el ancho dejando el borde izquierdo donde estaba. Hace falta porque, fuera
        /// del layout, crecer con el pivote centrado se lleva media franja hacia la izquierda.
        /// </summary>
        static void EstirarManteniendoIzquierda(RectTransform rt, float ancho)
        {
            float izquierda = rt.localPosition.x - rt.rect.width * rt.pivot.x;
            FijarAncho(rt, ancho);
            float ahora = rt.localPosition.x - rt.rect.width * rt.pivot.x;

            var p = rt.localPosition;
            p.x += izquierda - ahora;
            rt.localPosition = p;
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
                // LA palanca de las banderitas: con childControlWidth el layout DECIDE el
                // ancho de cada hija y lo reparte entre las doce. Sin quitarla, ensanchar la
                // seccion no sirve de nada.
                PonerBool(grupo, "childControlWidth", false);
                PonerBool(grupo, "childForceExpandWidth", false);
                // MiddleLeft (3): las banderitas pegadas al principio de la fila en vez de
                // centradas, para que caigan dentro de la cinta de color.
                if (FlagAlignLeft) PonerAlineacion(grupo, 3);
            }

            float pide = n * lado + (n - 1) * FlagSpacing;
            if (WidenSections) PreferirAncho(fila, pide);
            return pide;
        }

        // --------------------------------------------------------- reparto de banderitas

        // Lo que hemos movido nosotros, para poder devolverlo antes de que el juego toque.
        static readonly Dictionary<Transform, Transform> devolverA = new();

        /// <summary>
        /// Reparte las banderitas entre las dos filas a mitades: con 18 aliados, **9 y 9** en
        /// vez de 12 y 6. Asi la fila larga pide 480 px en vez de 642 y cabe dentro de la
        /// cinta de color.
        ///
        /// Esto mueve objetos de padre, que es justo lo que el mod tiene prohibido desde el
        /// dia que dejo 26 rombos muertos en la pagina de mejoras. La diferencia es **donde**
        /// se hace: el propio juego reparte estos iconos moviendolos de fila
        /// (`ReparentCrewVictoryItems`), asi que aqui se engancha un postfijo a ESE metodo y
        /// se rebalancea justo despues, cada vez que el juego rehace la pantalla. No se
        /// queda nada colgado entre reconstrucciones, que era el problema de aquella vez.
        ///
        /// Y para que el juego nunca se encuentre con su fila cambiada, lo que movemos se
        /// devuelve a su sitio ANTES de que el corra cualquiera de los dos metodos suyos.
        ///
        /// Si algun dia aparecen banderitas duplicadas o que no responden:
        /// `BalanceFlagRows = false` en el config y vuelve el reparto del juego.
        /// </summary>
        [HarmonyPatch(typeof(ClanChecklistSection), "ReparentCrewVictoryItems")]
        [HarmonyPostfix]
        static void TrasRepartirAliados(ClanChecklistSection __instance)
        {
            if (!Enabled || !BalanceFlagRows) return;
            try
            {
                if (FFila?.GetValue(__instance) is not Component c1) return;
                if (FFilaCrew?.GetValue(__instance) is not Component c2) return;
                Transform f1 = c1.transform, f2 = c2.transform;
                if (!f1.gameObject.activeInHierarchy && !f2.gameObject.activeInHierarchy) return;

                var arriba = Activos(f1);
                var abajo = Activos(f2);
                int total = arriba.Count + abajo.Count;
                if (total < 4) return;

                int quiero = Mathf.CeilToInt(total / 2f);   // 18 -> 9
                if (arriba.Count <= quiero) return;

                // Se bajan los ultimos de la fila de arriba, por el orden en que estan, y se
                // ponen delante de los de tripulacion para no descolocar a estos.
                int mover = arriba.Count - quiero;
                for (int i = 0; i < mover; i++)
                {
                    var t = arriba[arriba.Count - 1 - i];
                    if (!devolverA.ContainsKey(t)) devolverA[t] = f1;
                    t.SetParent(f2, false);
                    t.SetSiblingIndex(0);
                }
                Log($"aliados repartidos {arriba.Count}+{abajo.Count} -> {quiero}+{total - quiero}");
            }
            catch (Exception e) { Log("fallo repartiendo los aliados: " + e, true); }
        }

        /// <summary>Antes de que el juego toque sus filas, se le devuelve lo que movimos.</summary>
        [HarmonyPatch(typeof(ClanChecklistSection), "ReparentCrewVictoryItems")]
        [HarmonyPrefix]
        static void AntesDeRepartir() => Devolver();

        [HarmonyPatch(typeof(ClanChecklistSection), "ResetParentOfCrewVictoryItems")]
        [HarmonyPrefix]
        static void AntesDeDeshacer() => Devolver();

        static void Devolver()
        {
            if (devolverA.Count == 0) return;
            foreach (var par in devolverA)
            {
                var t = par.Key;
                if (t == null || par.Value == null) continue;
                try { t.SetParent(par.Value, false); } catch { }
            }
            devolverA.Clear();
        }

        static List<Transform> Activos(Transform padre)
        {
            var l = new List<Transform>();
            foreach (Transform h in padre) if (h.gameObject.activeSelf) l.Add(h);
            return l;
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

        /// <summary>`childAlignment`, que es un TextAnchor: 3 = MiddleLeft.</summary>
        static void PonerAlineacion(Component c, int valor)
        {
            try
            {
                var prop = c.GetType().GetProperty("childAlignment");
                if (prop == null) return;
                var actual = prop.GetValue(c, null);
                if (actual != null && Convert.ToInt32(actual) == valor) return;
                prop.SetValue(c, Enum.ToObject(prop.PropertyType, valor), null);
            }
            catch (Exception ex) { Log($"no se pudo alinear {c.GetType().Name}: {ex.Message}", true); }
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
                    // Un nivel mas dentro de las dos partes que se tocan: en el contenedor
                    // viven las filas de banderitas, y en la placa, la franja de color.
                    if (!h.name.Contains("victory container") && !h.name.Contains("Main class section"))
                        continue;
                    foreach (Transform f in h)
                    {
                        Log($"      hijo \"{f.name}\"{(f.gameObject.activeSelf ? "" : " (apagado)")}" +
                            $"{Medidas(f)}{Componentes(f)}");
                        foreach (Transform n2 in f)
                            Log($"        nieto \"{n2.name}\"{(n2.gameObject.activeSelf ? "" : " (apagado)")}" +
                                $"{Medidas(n2)}{Componentes(n2)}");
                    }
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
