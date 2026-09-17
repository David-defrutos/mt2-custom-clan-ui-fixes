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
    /// Pagina la pagina de ARTEFACTOS del logbook (CompendiumSectionBlessings) cuando hay
    /// mas clanes de los que caben a lo ancho de la hoja.
    ///
    /// Como esta montada la pantalla, leido en IL de Assembly-CSharp (2026-09-16):
    ///   - `CompendiumSectionBlessings.InitializeImpl` arma `collectionDatas` y de ahi
    ///     `collectionUIs`, una lista de `CompendiumRelicCollection`. CADA ENTRADA ES UNA
    ///     COLUMNA de la hoja: primero los genericos, luego un clan por columna (con el
    ///     tesoro del dragon intercalado) y al final los de evento.
    ///   - Cada coleccion es un `GridLayoutGroup` propio: `Set()` le pone
    ///     `constraint = FixedColumnCount` y `constraintCount = data.columnCount`
    ///     (`maxColumnCountClan` / `maxColumnCountGeneric` / `maxColumnCountStoryEvent`).
    ///     Por eso la de genericos es ancha y las de clan estrechas.
    ///   - La seccion hereda de `CompendiumSection`, **no de PaginatedCompendiumSection**:
    ///     no pagina. Con 19 clanes las ultimas columnas se salen por la derecha y no hay
    ///     forma de verlas. Es el mismo defecto que la pagina de mejoras de campeon, pero
    ///     en horizontal.
    ///
    /// LAS FLECHAS YA ESTAN: `CompendiumScreen.RefreshPageTurnZones` enciende cada
    /// `PageTurnZone` con `CanTurnPageWithinSection`, que llama al virtual
    /// `CompendiumSection.CanTurnPage(dir)` SIN comprobar si la seccion es paginada. O sea
    /// que basta con contestar que si a ese virtual para que el juego pinte sus propias
    /// flechas y llame a `TurnPage(dir)`. No se crea UI nueva.
    ///
    /// Los dos virtuales se parchean **en la clase base**, porque esta seccion no los
    /// sobrescribe: el hueco de la vtable apunta a `CompendiumSection`. Las secciones que si
    /// los sobrescriben (las paginadas) no pasan por aqui, y las otras que tampoco los
    /// sobrescriben se filtran por tipo.
    ///
    /// LA REGLA DEL MOD, otra vez: **no se toca la jerarquia**. Aqui no hace falta mover ni
    /// clonar nada: paginar es encender y apagar columnas enteras (`SetActive`), que es lo
    /// unico que el layout del juego necesita para recolocar las que quedan. Si el juego
    /// reconstruye la pantalla, vuelve a encenderlas todas y nosotros volvemos a repartir.
    ///
    /// Para desactivarlo: [ArtifactsPaging] Enabled = false en el config de BepInEx.
    /// </summary>
    [HarmonyPatch]
    public static class LogbookArtifactsPaging
    {
        // --- ajustes, los rellena Plugin.Awake desde el config de BepInEx ---
        public static bool Enabled = true;
        public static int ColumnsPerPage = 0;      // 0 = las que quepan por ancho
        public static bool Balance = true;         // paginas igual de llenas, no la 1a a tope
        public static float WidthBudget = 0f;      // ancho util en px; 0 = detectarlo
        public static int RetryFrames = 10;        // frames que se reintenta tras abrir
        public static bool Verbose = true;

        static readonly FieldInfo? FColecciones =
            AccessTools.Field(typeof(CompendiumSectionBlessings), "collectionUIs");
        static readonly FieldInfo? FPantalla =
            AccessTools.Field(typeof(CompendiumSection), "compendiumScreen");
        static readonly MethodInfo? MRefrescarFlechas =
            AccessTools.Method(typeof(CompendiumScreen), "RefreshPageTurnZones");

        // Reparto actual. Solo hay una de estas secciones viva a la vez.
        static readonly List<List<int>> paginas = new();
        static int pagina;

        // Medidas naturales. NO valen a la primera: el juego coloca las columnas a lo largo
        // de varios frames, y la pasada temprana da la primera con ancho y las demas a cero
        // (medido el 16-sep: "21 columnas, la primera 288 px" y una sola pagina). Por eso se
        // miden en cada pasada y solo se dan por buenas cuando **dos seguidas coinciden**.
        static readonly List<float> anchos = new();    // ancho OCUPADO por columna, hueco incluido
        static readonly List<float> previas = new();
        static bool medidasEstables;
        static bool zonaTrazada, contenedorTrazado;
        static int ultimoTotal = -1, ultimasPaginas = -1, ultimaPagina = -1;

        // --------------------------------------------------------------- parches

        [HarmonyPatch(typeof(CompendiumSectionBlessings), "InitializeImpl")]
        [HarmonyPostfix]
        static void TrasInicializar(CompendiumSectionBlessings __instance)
        {
            // El juego acaba de rehacer las columnas: las medidas de antes ya no valen.
            anchos.Clear();
            previas.Clear();
            medidasEstables = false;
            paginas.Clear();
            pagina = 0;
            Lanzar(__instance);
        }

        /// <summary>
        /// `Open` es virtual en `CompendiumSection` y esta seccion no lo sobrescribe. Las
        /// paginadas si, pero llaman a base, asi que aqui entra todo: se filtra por tipo.
        /// </summary>
        [HarmonyPatch(typeof(CompendiumSection), "Open")]
        [HarmonyPostfix]
        static void TrasAbrir(CompendiumSection __instance)
        {
            if (__instance is CompendiumSectionBlessings seccion)
            {
                pagina = 0;
                Lanzar(seccion);
            }
        }

        /// <summary>
        /// Lo que enciende las flechas del juego. El original devuelve false siempre.
        /// </summary>
        [HarmonyPatch(typeof(CompendiumSection), "CanTurnPage")]
        [HarmonyPrefix]
        static bool AntesDePoderPasar(CompendiumSection __instance, PageTurnZone.TurnDir dir, ref bool __result)
        {
            if (!Enabled || __instance is not CompendiumSectionBlessings) return true;
            int destino = pagina + (int)dir;
            __result = paginas.Count > 1 && destino >= 0 && destino < paginas.Count;
            return false;   // el original no hace nada mas
        }

        /// <summary>
        /// El original esta vacio. `CompendiumScreen.TurnPage` ya ha comprobado
        /// `CanTurnPage` antes de llegar aqui, y refresca las flechas al volver.
        /// </summary>
        [HarmonyPatch(typeof(CompendiumSection), "TurnPage")]
        [HarmonyPrefix]
        static bool AntesDePasar(CompendiumSection __instance, int dir)
        {
            if (!Enabled || __instance is not CompendiumSectionBlessings seccion) return true;
            if (paginas.Count <= 1) return false;

            int destino = Mathf.Clamp(pagina + dir, 0, paginas.Count - 1);
            if (destino != pagina)
            {
                pagina = destino;
                Pintar(seccion);
            }
            return false;
        }

        // --------------------------------------------------------------- reparto

        static void Lanzar(CompendiumSectionBlessings seccion)
        {
            if (!Enabled || seccion == null) return;
            Repartir(seccion);
            // Al abrir, ni el layout ha corrido ni tienen por que estar creadas todas las
            // columnas. Igual que en la pagina de mejoras: se reintenta unos frames, y solo
            // si el objeto esta activo (en InitializeImpl todavia no lo esta).
            if (seccion.isActiveAndEnabled)
            {
                try { seccion.StartCoroutine(RepartirUnosFrames(seccion)); }
                catch (Exception e) { Log("no se pudo encolar el reintento: " + e.Message, true); }
            }
        }

        static IEnumerator RepartirUnosFrames(CompendiumSectionBlessings seccion)
        {
            for (int i = 0; i < Mathf.Max(1, RetryFrames); i++)
            {
                yield return null;
                if (seccion == null || !seccion.isActiveAndEnabled) yield break;
                Repartir(seccion);
            }
        }

        static void Repartir(CompendiumSectionBlessings seccion)
        {
            try
            {
                var columnas = Columnas(seccion);
                if (columnas.Count == 0) return;
                if (columnas[0].parent is not RectTransform raiz) return;

                // --- medidas naturales. Solo valen con TODAS las columnas encendidas: si
                // alguna esta apagada (porque ya hemos repartido) se encienden y se mide al
                // frame siguiente.
                if (!medidasEstables || anchos.Count != columnas.Count)
                {
                    bool todasVisibles = true;
                    foreach (var c in columnas)
                        if (!c.gameObject.activeSelf) { c.gameObject.SetActive(true); todasVisibles = false; }
                    if (!todasVisibles) return;
                    if (!Medir(columnas)) return;   // aun sin colocar, o sin confirmar
                }

                TrazarContenedor(raiz);
                float presupuesto = AnchoUtil(raiz) - PaddingHorizontal(raiz);
                if (presupuesto <= 1f) return;

                // --- repartir columnas en paginas
                var reparto = ColumnsPerPage > 0
                    ? PorNumeroFijo(columnas.Count, ColumnsPerPage)
                    : Balance ? Equilibrado(presupuesto) : Llenando(presupuesto);

                paginas.Clear();
                paginas.AddRange(reparto);
                pagina = Mathf.Clamp(pagina, 0, paginas.Count - 1);
                Pintar(seccion);
            }
            catch (Exception e)
            {
                Log("fallo repartiendo la pagina: " + e, true);
            }
        }

        /// <summary>
        /// Reparto **equilibrado**: las mismas paginas que harian falta llenando, pero todas
        /// igual de llenas. Con 24 columnas y hoja de 1456 salen 12 y 12 (1080 px cada una)
        /// en vez de 17 y 7; con 21, 11 y 10; y asi.
        ///
        /// Se hace buscando la **capacidad mas pequena** con la que el reparto sigue cabiendo
        /// en ese numero de paginas: reducir la capacidad obliga a adelantar el corte, y la
        /// menor que no anade una pagina es justo la que deja la mas cargada lo mas ligera
        /// posible. Es por ANCHO, no por numero de columnas: con la de genericos ocupando
        /// cuatro veces lo que una de clan, dos paginas con el mismo numero de columnas no
        /// se verian igual de llenas.
        /// </summary>
        static List<List<int>> Equilibrado(float presupuesto)
        {
            int objetivo = Llenando(presupuesto).Count;
            if (objetivo <= 1) return Llenando(presupuesto);

            float min = 0f;
            foreach (var w in anchos) min = Mathf.Max(min, w);   // una columna no se parte
            float bajo = min, alto = presupuesto;
            for (int i = 0; i < 30 && alto - bajo > 0.5f; i++)
            {
                float medio = (bajo + alto) / 2f;
                if (Llenando(medio).Count <= objetivo) alto = medio; else bajo = medio;
            }
            return Llenando(alto);
        }

        /// <summary>Reparto clasico: se van metiendo columnas hasta que no cabe otra.</summary>
        static List<List<int>> Llenando(float capacidad)
        {
            var reparto = new List<List<int>>();
            var enCurso = new List<int>();
            float usado = 0f;
            for (int i = 0; i < anchos.Count; i++)
            {
                if (enCurso.Count > 0 && usado + anchos[i] > capacidad + 0.5f)
                {
                    reparto.Add(enCurso);
                    enCurso = new List<int>();
                    usado = 0f;
                }
                enCurso.Add(i);
                usado += anchos[i];
            }
            if (enCurso.Count > 0) reparto.Add(enCurso);
            return reparto;
        }

        /// <summary>Reparto a ojo, con un numero fijo de columnas por pagina.</summary>
        static List<List<int>> PorNumeroFijo(int total, int porPagina)
        {
            var reparto = new List<List<int>>();
            var enCurso = new List<int>();
            for (int i = 0; i < total; i++)
            {
                if (enCurso.Count >= porPagina) { reparto.Add(enCurso); enCurso = new List<int>(); }
                enCurso.Add(i);
            }
            if (enCurso.Count > 0) reparto.Add(enCurso);
            return reparto;
        }

        /// <summary>
        /// El ancho que OCUPA cada columna, hueco incluido. Se mide por el paso hasta la
        /// siguiente y no por su `rect`: el paso lo pone el layout ya resuelto, mientras que
        /// el rect de una columna puede venir a cero mientras el juego la esta llenando.
        ///
        /// Y no se da por buena una sola pasada: el juego tarda varios frames en colocarlas
        /// todas, asi que se guarda la medida y solo se acepta cuando la siguiente coincide.
        /// </summary>
        static bool Medir(List<RectTransform> columnas)
        {
            var ahora = new List<float>(columnas.Count);
            var rects = new List<float>(columnas.Count);
            var pasos = new List<float>(columnas.Count);
            for (int i = 0; i < columnas.Count; i++)
            {
                float paso = i + 1 < columnas.Count
                    ? Mathf.Abs(columnas[i + 1].localPosition.x - columnas[i].localPosition.x)
                    : 0f;
                float rect = Mathf.Max(0f, columnas[i].rect.width);
                rects.Add(rect);
                pasos.Add(paso);

                // Lo que ocupa de verdad es **el mayor de los dos**, y esto costo la tercera
                // prueba (16-sep): la columna de genericos mide 288 de rect pero solo avanza
                // 180, o sea que **se solapa** con la siguiente. Repartiendo por el paso, la
                // primera pagina sumaba 1404 cuando ocupaba 1512 sobre 1456 de hoja, y la
                // ultima columna se salia. El paso sigue haciendo falta como respaldo: el
                // rect de una columna puede venir a cero mientras el juego la esta llenando.
                float ocupado = Mathf.Max(rect, paso);
                if (ocupado <= 1f && i > 0) ocupado = ahora[i - 1];
                ahora.Add(ocupado);
            }

            float suma = 0f;
            foreach (var w in ahora) suma += w;
            if (suma <= 1f) { previas.Clear(); return false; }   // el layout aun no ha corrido

            bool iguales = previas.Count == ahora.Count;
            if (iguales)
                for (int i = 0; i < ahora.Count; i++)
                    if (Mathf.Abs(previas[i] - ahora[i]) > 0.5f) { iguales = false; break; }

            previas.Clear();
            previas.AddRange(ahora);
            if (!iguales) return false;   // la de la pasada anterior no coincidia: seguimos

            anchos.Clear();
            anchos.AddRange(ahora);
            medidasEstables = true;

            var detalle = new StringBuilder();
            var detalleRect = new StringBuilder();
            for (int i = 0; i < ahora.Count; i++)
            {
                detalle.Append(i == 0 ? "" : " ").Append($"{ahora[i]:0}");
                detalleRect.Append(i == 0 ? "" : " ").Append($"{rects[i]:0}/{pasos[i]:0}");
            }
            Log($"medidas confirmadas: {ahora.Count} columnas, {suma:0} px en total, " +
                $"ocupacion [{detalle}]");
            Log($"rect/paso por columna: [{detalleRect}]");
            return true;
        }

        /// <summary>
        /// Enciende las columnas de la pagina actual y apaga las demas. Con una sola pagina
        /// se encienden todas y la pantalla queda exactamente como la deja el juego.
        /// </summary>
        static void Pintar(CompendiumSectionBlessings seccion)
        {
            var columnas = Columnas(seccion);
            if (columnas.Count == 0 || paginas.Count == 0) return;

            var visibles = paginas[Mathf.Clamp(pagina, 0, paginas.Count - 1)];
            for (int i = 0; i < columnas.Count; i++)
            {
                bool visible = paginas.Count <= 1 || visibles.Contains(i);
                if (columnas[i].gameObject.activeSelf != visible)
                    columnas[i].gameObject.SetActive(visible);
            }

            // Las flechas se refrescan solas al cambiar de seccion y despues de TurnPage,
            // pero no despues de nuestro reparto: la primera pasada puede decir "1 pagina" y
            // la del frame siguiente "3", y sin esto las flechas se quedarian apagadas.
            RefrescarFlechas(seccion);

            if (columnas.Count != ultimoTotal || paginas.Count != ultimasPaginas || pagina != ultimaPagina)
            {
                ultimoTotal = columnas.Count;
                ultimasPaginas = paginas.Count;
                ultimaPagina = pagina;
                var reparto = new StringBuilder();
                foreach (var p in paginas)
                {
                    float ancho = 0f;
                    foreach (var i in p) if (i < anchos.Count) ancho += anchos[i];
                    reparto.Append(reparto.Length == 0 ? "" : " + ").Append($"{p.Count} ({ancho:0} px)");
                }
                Log($"{columnas.Count} columnas en {paginas.Count} pagina(s) [{reparto}], " +
                    $"mostrando la {pagina + 1} con {visibles.Count}");
            }
        }

        static void RefrescarFlechas(CompendiumSectionBlessings seccion)
        {
            try
            {
                var pantalla = FPantalla?.GetValue(seccion);
                if (pantalla != null) MRefrescarFlechas?.Invoke(pantalla, null);
            }
            catch (Exception e)
            {
                Log("no se pudieron refrescar las flechas: " + e.Message, true);
            }
        }

        /// <summary>
        /// Las columnas, tal y como las dejo el juego. No se mueven ni se reordenan.
        /// </summary>
        static List<RectTransform> Columnas(CompendiumSectionBlessings seccion)
        {
            var lista = new List<RectTransform>();
            if (FColecciones?.GetValue(seccion) is not IEnumerable colecciones) return lista;
            foreach (var c in colecciones)
                if (c is Component comp && comp != null && comp.transform is RectTransform rt)
                    lista.Add(rt);
            return lista;
        }

        /// <summary>
        /// El ancho de hoja que hay para las columnas: el rect mas estrecho de la cadena, que
        /// es el que de verdad recorta.
        ///
        /// **La raiz cuenta**, y esto costo dos pruebas (16-sep). En la pagina de mejoras la
        /// raiz de columnas se autoexpandia con sus hijos, asi que alli no valia; aqui la
        /// raiz es `Content`, 1456x936, y es justo la zona buena. Y no basta con mirar si
        /// lleva un `ContentSizeFitter`: el de `Content` **solo ajusta el alto** (936 es el
        /// alto del contenido), mientras que el ancho lo fijan las anclas. Descartarlo por
        /// llevarlo devolvia 1920, con el que los 1836 px de columnas "caben" y no se
        /// paginaba nada. Lo que se mira es `horizontalFit`: si es Unconstrained (0), el
        /// ancho NO lo manda el contenido y ese rect si recorta.
        ///
        /// Todo se devuelve en el espacio local de la raiz, que es donde estan medidas las
        /// columnas; un ancestro con otra escala se convierte con `lossyScale`.
        /// La traza deja la cadena entera para poder fijarlo a mano con `WidthBudget`.
        /// </summary>
        static float AnchoUtil(RectTransform raiz)
        {
            if (WidthBudget > 1f) return WidthBudget;

            var traza = new StringBuilder();
            float escalaRaiz = raiz.lossyScale.x;
            float menor = 0f;

            bool raizCrece = AnchoLoMandaElContenido(raiz);
            if (!raizCrece && raiz.rect.width > 1f) menor = raiz.rect.width;
            traza.Append($"{raiz.name} {raiz.rect.width:0}x{raiz.rect.height:0}")
                 .Append(raizCrece ? " (el ancho lo manda el contenido, no cuenta)" : "");

            var p = raiz.parent as RectTransform;
            int saltos = 0;
            while (p != null && saltos++ < 8)
            {
                float w = p.rect.width;
                // Al espacio de la raiz: si un padre esta escalado, sus px no son los mismos.
                float w2 = escalaRaiz > 0.0001f ? w * (p.lossyScale.x / escalaRaiz) : w;
                bool crece = AnchoLoMandaElContenido(p);
                traza.Append($" <- {p.name} {w:0}x{p.rect.height:0}")
                     .Append(Mathf.Abs(w2 - w) > 1f ? $" (={w2:0} en la raiz)" : "")
                     .Append(crece ? " (crece, no cuenta)" : "");
                if (!crece && w2 > 1f && (menor <= 0f || w2 < menor)) menor = w2;
                p = p.parent as RectTransform;
            }

            if (!zonaTrazada)
            {
                zonaTrazada = true;
                Log($"zona: {traza} -> ancho util {menor:0}");
            }
            return menor;
        }

        /// <summary>
        /// Vuelca una vez como esta montado el contenedor de columnas: sus componentes y, si
        /// lleva un LayoutGroup, sus ajustes. Sirve para saber si las columnas se pueden
        /// separar (subiendo `spacing`) o es el juego el que las pega.
        /// </summary>
        static void TrazarContenedor(RectTransform raiz)
        {
            if (contenedorTrazado || !Verbose) return;
            contenedorTrazado = true;

            var sb = new StringBuilder();
            foreach (var c in raiz.GetComponents<Component>())
            {
                if (c == null) continue;
                var tipo = c.GetType();
                sb.Append(' ').Append(tipo.Name);
                if (!tipo.Name.Contains("LayoutGroup")) continue;

                sb.Append('(');
                foreach (var nombre in new[] { "spacing", "cellSize", "constraint", "constraintCount",
                                               "childForceExpandWidth", "childControlWidth",
                                               "childAlignment", "reverseArrangement" })
                {
                    object? v = null;
                    try { v = tipo.GetProperty(nombre)?.GetValue(c, null); } catch { }
                    if (v != null) sb.Append(nombre).Append('=').Append(v).Append(' ');
                }
                sb.Append($"padding={PaddingHorizontal(raiz):0})");
            }
            Log($"contenedor {raiz.name}:{sb}");
        }

        /// <summary>El padding izquierdo + derecho del LayoutGroup de la raiz, si lo hay.</summary>
        static float PaddingHorizontal(Transform t)
        {
            foreach (var c in t.GetComponents<Component>())
            {
                if (c == null || !c.GetType().Name.Contains("LayoutGroup")) continue;
                try
                {
                    var pad = c.GetType().GetProperty("padding")?.GetValue(c, null);
                    if (pad == null) continue;
                    var izq = pad.GetType().GetProperty("left")?.GetValue(pad, null);
                    var der = pad.GetType().GetProperty("right")?.GetValue(pad, null);
                    return Convert.ToSingle(izq ?? 0) + Convert.ToSingle(der ?? 0);
                }
                catch { return 0f; }
            }
            return 0f;
        }

        /// <summary>
        /// Si un `ContentSizeFitter` activo le esta fijando el ANCHO a este objeto, que es lo
        /// que hace que crezca con lo que tiene dentro y por tanto "siempre quepa". Se mira
        /// por reflexion, comparando el nombre del tipo y leyendo `horizontalFit`, para no
        /// tener que referenciar UnityEngine.UI (misma tactica que en LogbookClanFit).
        /// FitMode: 0 = Unconstrained, 1 = MinSize, 2 = PreferredSize.
        /// </summary>
        static bool AnchoLoMandaElContenido(Transform t)
        {
            foreach (var c in t.GetComponents<Component>())
            {
                if (c == null) continue;
                if (!c.GetType().Name.Contains("ContentSizeFitter")) continue;
                if (c is Behaviour b && !b.enabled) continue;
                try
                {
                    var prop = c.GetType().GetProperty("horizontalFit");
                    if (prop == null) return true;   // no se puede saber: se descarta, como antes
                    var valor = prop.GetValue(c, null);
                    if (valor == null) return true;
                    return Convert.ToInt32(valor) != 0;
                }
                catch { return true; }
            }
            return false;
        }

        static void Log(string mensaje, bool aviso = false)
        {
            if (aviso) Plugin.Logger.LogWarning("[ArtifactsPaging] " + mensaje);
            else if (Verbose) Plugin.Logger.LogInfo("[ArtifactsPaging] " + mensaje);
        }
    }
}
