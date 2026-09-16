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
        static bool zonaTrazada;
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

                float presupuesto = AnchoUtil(raiz);
                if (presupuesto <= 1f) return;

                // --- repartir columnas en paginas
                paginas.Clear();
                var enCurso = new List<int>();
                float usado = 0f;
                for (int i = 0; i < columnas.Count; i++)
                {
                    float coste = anchos[i];
                    bool cabe = ColumnsPerPage > 0
                        ? enCurso.Count < ColumnsPerPage
                        : enCurso.Count == 0 || usado + coste <= presupuesto + 0.5f;

                    if (!cabe)
                    {
                        paginas.Add(enCurso);
                        enCurso = new List<int>();
                        usado = 0f;
                    }
                    enCurso.Add(i);
                    usado += coste;
                }
                if (enCurso.Count > 0) paginas.Add(enCurso);

                pagina = Mathf.Clamp(pagina, 0, paginas.Count - 1);
                Pintar(seccion);
            }
            catch (Exception e)
            {
                Log("fallo repartiendo la pagina: " + e, true);
            }
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
            for (int i = 0; i < columnas.Count; i++)
            {
                float paso = i + 1 < columnas.Count
                    ? Mathf.Abs(columnas[i + 1].localPosition.x - columnas[i].localPosition.x)
                    : 0f;
                float ocupado = paso > 1f ? paso : Mathf.Max(0f, columnas[i].rect.width);
                // La ultima no tiene paso: se le da el de su vecina si su rect no dice nada.
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
            for (int i = 0; i < ahora.Count; i++) detalle.Append(i == 0 ? "" : " ").Append($"{ahora[i]:0}");
            Log($"medidas confirmadas: {ahora.Count} columnas, {suma:0} px en total, " +
                $"ocupacion [{detalle}]");
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
                Log($"{columnas.Count} columnas en {paginas.Count} pagina(s), " +
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
