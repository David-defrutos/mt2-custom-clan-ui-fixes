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
        public static int RetryFrames = 5;         // frames que se reintenta tras abrir
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

        // Medidas naturales, tomadas UNA vez con todas las columnas visibles.
        static readonly List<float> anchos = new();
        static float separacion;
        static bool zonaTrazada;
        static int ultimoTotal = -1, ultimasPaginas = -1, ultimaPagina = -1;

        // --------------------------------------------------------------- parches

        [HarmonyPatch(typeof(CompendiumSectionBlessings), "InitializeImpl")]
        [HarmonyPostfix]
        static void TrasInicializar(CompendiumSectionBlessings __instance)
        {
            // El juego acaba de rehacer las columnas: las medidas de antes ya no valen.
            anchos.Clear();
            separacion = 0f;
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

                // --- medidas naturales. Solo valen con TODAS las columnas encendidas y el
                // layout ya corrido; si alguna esta apagada (porque ya hemos repartido) se
                // encienden y se mide al frame siguiente.
                if (anchos.Count != columnas.Count)
                {
                    bool todasVisibles = true;
                    foreach (var c in columnas)
                        if (!c.gameObject.activeSelf) { c.gameObject.SetActive(true); todasVisibles = false; }
                    if (!todasVisibles) return;

                    // Si TODAS miden cero es que el layout aun no ha corrido: se reintenta al
                    // frame siguiente. Una suelta a cero es una coleccion vacia (un clan sin
                    // artefactos propios), y esa si vale: ocupa cero y no gasta hoja.
                    anchos.Clear();
                    float mayor = 0f;
                    foreach (var c in columnas)
                    {
                        float w = Mathf.Max(0f, c.rect.width);
                        if (w > mayor) mayor = w;
                        anchos.Add(w);
                    }
                    if (mayor <= 1f) { anchos.Clear(); return; }

                    separacion = 0f;
                    if (columnas.Count >= 2)
                    {
                        float paso = Mathf.Abs(columnas[1].localPosition.x - columnas[0].localPosition.x);
                        separacion = Mathf.Clamp(paso - anchos[0], 0f, 400f);
                    }
                    Log($"medidas naturales: {anchos.Count} columnas, la primera {anchos[0]:0} px, " +
                        $"separacion {separacion:0}");
                }

                float presupuesto = AnchoUtil(raiz);
                if (presupuesto <= 1f) return;

                // --- repartir columnas en paginas
                paginas.Clear();
                var enCurso = new List<int>();
                float usado = 0f;
                for (int i = 0; i < columnas.Count; i++)
                {
                    float coste = enCurso.Count == 0 ? anchos[i] : separacion + anchos[i];
                    bool cabe = ColumnsPerPage > 0
                        ? enCurso.Count < ColumnsPerPage
                        : enCurso.Count == 0 || usado + coste <= presupuesto + 0.5f;

                    if (!cabe)
                    {
                        paginas.Add(enCurso);
                        enCurso = new List<int>();
                        usado = 0f;
                        coste = anchos[i];
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
        /// El ancho de hoja que hay para las columnas. La raiz que las contiene no sirve: se
        /// autoexpande con sus hijos, asi que siempre "cabe". Se coge el ancestro mas
        /// estrecho, que es el que de verdad recorta, y se deja trazado para poder fijarlo a
        /// mano con WidthBudget si el heuristico no acierta.
        /// </summary>
        static float AnchoUtil(RectTransform raiz)
        {
            if (WidthBudget > 1f) return WidthBudget;

            var traza = new StringBuilder();
            float menor = 0f;
            var p = raiz.parent as RectTransform;
            int saltos = 0;
            while (p != null && saltos++ < 8)
            {
                float w = p.rect.width;
                traza.Append($" <- {p.name} {w:0}x{p.rect.height:0}");
                if (w > 1f && (menor <= 0f || w < menor)) menor = w;
                p = p.parent as RectTransform;
            }

            if (!zonaTrazada)
            {
                zonaTrazada = true;
                Log($"zona: {raiz.name} {raiz.rect.width:0}x{raiz.rect.height:0}{traza}" +
                    $" -> ancho util {menor:0}");
            }
            return menor;
        }

        static void Log(string mensaje, bool aviso = false)
        {
            if (aviso) Plugin.Logger.LogWarning("[ArtifactsPaging] " + mensaje);
            else if (Verbose) Plugin.Logger.LogInfo("[ArtifactsPaging] " + mensaje);
        }
    }
}
