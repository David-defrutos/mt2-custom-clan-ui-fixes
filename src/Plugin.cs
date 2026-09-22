using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace mt2_custom_clan_ui_fixes.Plugin
{
    /// <summary>
    /// Arreglos de interfaz para jugar con muchos clanes modeados instalados.
    /// No anade contenido: solo parchea pantallas del juego base con Harmony.
    ///
    /// Dos pantallas del logbook, las dos por el mismo motivo (sus secciones no paginan):
    ///   - mejoras de campeon, que se sale por abajo  (ver code/LogbookClanFit.cs)
    ///   - artefactos, que se sale por la derecha     (ver code/LogbookArtifactsPaging.cs)
    /// </summary>
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger = new(MyPluginInfo.PLUGIN_GUID);

        public void Awake()
        {
            Logger = base.Logger;

            var cfgActivo = Config.Bind(
                "LogbookFit", "Enabled", true,
                "Encaja los rombos de clan de la pagina de mejoras del logbook cuando no caben.");
            var cfgEscalaMinima = Config.Bind(
                "LogbookFit", "MinScale", 0.45f,
                "Hasta donde se deja encoger un rombo. 1 = tamano original.");
            var cfgColumnas = Config.Bind(
                "LogbookFit", "MaxAutoColumns", 3,
                "Columnas como mucho. 2 = se deja la rejilla del juego y solo se escala; 3 aprovecha el ancho de la hoja.");
            var cfgSeparacion = Config.Bind(
                "LogbookFit", "ColumnSpacing", 16f,
                "Separacion entre columnas cuando se pasa de dos. La del juego son 96 px.");
            var cfgCabecera = Config.Bind(
                "LogbookFit", "HeaderReserve", 130f,
                "Pixeles que se reservan arriba para el titulo de la hoja. La zona medida lo incluye, asi que sin esto los rombos se le suben encima cuando hay muchos clanes.");
            var cfgMultiplicador = Config.Bind(
                "LogbookFit", "ScaleMultiplier", 1f,
                "Multiplica el factor calculado, para dejar los rombos algo mas pequenos de lo que haria falta. 1 = lo justo para que quepan; 0,9 = un 10% mas pequenos.");
            var cfgAlto = Config.Bind(
                "LogbookFit", "HeightBudget", 0f,
                "Alto util de la hoja en pixeles. 0 = detectarlo solo (medido: 1000).");
            var cfgAncho = Config.Bind(
                "LogbookFit", "WidthBudget", 0f,
                "Ancho util de la hoja en pixeles. 0 = detectarlo solo (medido: 400). Subelo a 440 para rombos a tamano original.");
            var cfgReintentos = Config.Bind(
                "LogbookFit", "RetryFrames", 5,
                "Frames que se reintenta la colocacion tras abrir la pantalla. El juego crea los botones de la columna de tripulacion uno o mas frames despues.");
            var cfgTraza = Config.Bind(
                "LogbookFit", "Verbose", true,
                "Escribe en LogOutput.log lo que mide y lo que ajusta.");

            LogbookClanFit.Enabled = cfgActivo.Value;
            LogbookClanFit.MinScale = cfgEscalaMinima.Value;
            LogbookClanFit.MaxAutoColumns = cfgColumnas.Value;
            LogbookClanFit.ColumnSpacing = cfgSeparacion.Value;
            LogbookClanFit.HeaderReserve = cfgCabecera.Value;
            LogbookClanFit.ScaleMultiplier = cfgMultiplicador.Value;
            LogbookClanFit.HeightBudget = cfgAlto.Value;
            LogbookClanFit.WidthBudget = cfgAncho.Value;
            LogbookClanFit.RetryFrames = cfgReintentos.Value;
            LogbookClanFit.Verbose = cfgTraza.Value;

            var cfgArtActivo = Config.Bind(
                "ArtifactsPaging", "Enabled", true,
                "Pagina la pagina de artefactos del logbook cuando las columnas de clan no caben a lo ancho. Usa las flechas de paso de pagina del propio juego.");
            var cfgArtColumnas = Config.Bind(
                "ArtifactsPaging", "ColumnsPerPage", 0,
                "Columnas por pagina. 0 = las que quepan segun el ancho medido.");
            var cfgArtEquilibrio = Config.Bind(
                "ArtifactsPaging", "Balance", true,
                "Reparte las columnas en paginas igual de llenas (24 columnas -> 12 y 12) en vez de llenar la primera y dejar la ultima a medias (17 y 7).");
            var cfgArtAncho = Config.Bind(
                "ArtifactsPaging", "WidthBudget", 0f,
                "Ancho util de la hoja en pixeles. 0 = detectarlo solo. Si el reparto se queda corto o largo, mira la linea 'zona:' del log y fija aqui el ancho bueno.");
            var cfgArtReintentos = Config.Bind(
                "ArtifactsPaging", "RetryFrames", 10,
                "Frames que se reintenta el reparto tras abrir la pantalla, mientras el juego termina de crear y colocar las columnas. Las medidas no se dan por buenas hasta que dos pasadas seguidas coinciden, asi que conviene que sobren.");
            var cfgArtTraza = Config.Bind(
                "ArtifactsPaging", "Verbose", true,
                "Escribe en LogOutput.log lo que mide y como reparte.");

            LogbookArtifactsPaging.Enabled = cfgArtActivo.Value;
            LogbookArtifactsPaging.ColumnsPerPage = cfgArtColumnas.Value;
            LogbookArtifactsPaging.Balance = cfgArtEquilibrio.Value;
            LogbookArtifactsPaging.WidthBudget = cfgArtAncho.Value;
            LogbookArtifactsPaging.RetryFrames = cfgArtReintentos.Value;
            LogbookArtifactsPaging.Verbose = cfgArtTraza.Value;

            var cfgProgActivo = Config.Bind(
                "ProgressGrid", "Enabled", true,
                "Pone la hoja de progreso del logbook a una columna y la pagina, para que se vean todos los clanes y las banderas de aliados no se aplasten.");
            var cfgProgColumnas = Config.Bind(
                "ProgressGrid", "Columns", 1,
                "Columnas de la rejilla de clanes. 1 = una seccion por fila, que es lo que deja sitio a las banderas; 2 = la rejilla del juego.");
            var cfgProgFilas = Config.Bind(
                "ProgressGrid", "RowsPerPage", 0,
                "Filas por hoja. 0 = las que quepan de alto (con la celda del juego, 5).");
            var cfgProgTraza = Config.Bind(
                "ProgressGrid", "Verbose", true,
                "Escribe en LogOutput.log la rejilla, el reparto en sub-paginas y el detalle de las primeras secciones.");
            var cfgProgDetalle = Config.Bind(
                "ProgressGrid", "DetailSections", 0,
                "Cuantas secciones de clan se vuelcan con todo el detalle. 0 = ninguna. Solo para depurar.");
            var cfgProgEnsanchar = Config.Bind(
                "ProgressGrid", "WidenSections", true,
                "Estira la seccion de clan y el contenedor de aliados hasta el ancho nuevo de la celda. A false, la celda se ensancha pero dentro todo sigue pegado a la izquierda.");
            var cfgProgSoltar = Config.Bind(
                "ProgressGrid", "FreeFlagWidth", true,
                "Quita childControlWidth a las dos filas de banderas: es lo que hace que cada una recupere sus 48 px en vez de repartirse el ancho de la fila.");
            var cfgProgReparto = Config.Bind(
                "ProgressGrid", "Layout", "inflow",
                "Como se reparte el ancho dentro de la seccion. \"inflow\" = el contenedor de aliados entra en la fila y la placa se queda lo que sobra, con su franja de color llegando hasta las banderas. \"overlay\" = el intento de ensanchar la placa dejando las banderas encima, que se descarto porque el fondo de color no crece y las banderas tapan el nombre del clan.");
            var cfgProgPlaca = Config.Bind(
                "ProgressGrid", "PlaqueWidth", 280f,
                "Ancho de la placa del retrato en el modo \"inflow\". 280 es el valor ajustado en partida: deja el retrato y el nombre, y las banderas empiezan justo despues. 0 = todo lo que sobre.");
            var cfgProgReparto2 = Config.Bind(
                "ProgressGrid", "BalanceFlagRows", true,
                "Reparte las banderas de aliados a mitades entre las dos filas (con 18, 9 y 9 en vez de 12 y 6), para que la fila larga quepa dentro de la cinta de color. Mueve objetos de padre, igual que hace el juego: si aparecen banderas duplicadas o que no responden, ponlo a false.");
            var cfgProgIzquierda = Config.Bind(
                "ProgressGrid", "FlagAlignLeft", true,
                "Pega las banderas al principio de su fila en vez de centrarlas, para que caigan dentro de la cinta.");
            var cfgProgExtra = Config.Bind(
                "ProgressGrid", "RibbonExtra", 0f,
                "Pixeles de mas para la cinta de color, POR ENCIMA del borde del medidor de cartas. Dejalo en 0: la cinta ya se mide sola hasta ahi. Solo sirve para alargarla a proposito, y entonces se solapa con el medidor.");
            var cfgProgMedidor = Config.Bind(
                "ProgressGrid", "FixMasteryMeter", true,
                "El medidor de cartas dominadas viene con 7 columnas fijas, justo para las 42 cartas de un clan del juego base. Un clan con mas cartas necesita otra fila y de alto no cabe: se sale y los rectangulos se pisan. Con esto se fijan las filas y la rejilla crece a lo ancho, que es donde hay sitio.");
            var cfgProgColsMedidor = Config.Bind(
                "ProgressGrid", "MeterColumns", 12,
                "Columnas del medidor de cartas, iguales para todos los clanes. A 5 filas dan sitio para 60 cartas; al clan que tenga menos le quedan las celdas del final vacias, que es lo que hace que todos los medidores queden a plomo.");
            var cfgProgFilasMedidor = Config.Bind(
                "ProgressGrid", "MeterRows", 5,
                "Filas que caben de alto en la seccion. Solo se usa como tope: si algun clan no cabe en MeterColumns x MeterRows, se anaden columnas para todos antes que dejar que se salga.");
            var cfgProgCorrer = Config.Bind(
                "ProgressGrid", "FlagOffsetX", -280f,
                "Pixeles que se mueven las banderas de aliados dentro de la seccion. Negativo = hacia la izquierda. Se aplica como relleno del layout, que es lo unico que el propio layout no deshace.");
            var cfgProgVolcado = Config.Bind(
                "ProgressGrid", "DumpTree", false,
                "Vuelca una vez en LogOutput.log el arbol entero de la primera seccion de clan, con anchos y con que componente pinta cada objeto. Para saber a que hay que apuntar sin adivinar nombres.");
            var cfgProgFranja = Config.Bind(
                "ProgressGrid", "StretchPlaqueFill", true,
                "Estira la franja de color de la placa hasta el final de esta. Sin esto la placa se ensancha pero el color se queda en su ancho preferido y deja pergamino a la vista.");
            var cfgProgSeparacion = Config.Bind(
                "ProgressGrid", "FlagSpacing", 6f,
                "Separacion entre banderas al calcular lo que pide la fila. La del juego son 6 px.");

            LogbookProgressGrid.Enabled = cfgProgActivo.Value;
            LogbookProgressGrid.Columns = cfgProgColumnas.Value;
            LogbookProgressGrid.RowsPerPage = cfgProgFilas.Value;
            LogbookProgressGrid.Verbose = cfgProgTraza.Value;
            LogbookProgressGrid.DetailSections = cfgProgDetalle.Value;
            LogbookProgressGrid.WidenSections = cfgProgEnsanchar.Value;
            LogbookProgressGrid.FreeFlagWidth = cfgProgSoltar.Value;
            LogbookProgressGrid.Layout = cfgProgReparto.Value;
            LogbookProgressGrid.PlaqueWidth = cfgProgPlaca.Value;
            LogbookProgressGrid.StretchPlaqueFill = cfgProgFranja.Value;
            LogbookProgressGrid.DumpTree = cfgProgVolcado.Value;
            LogbookProgressGrid.BalanceFlagRows = cfgProgReparto2.Value;
            LogbookProgressGrid.FlagAlignLeft = cfgProgIzquierda.Value;
            LogbookProgressGrid.RibbonExtra = cfgProgExtra.Value;
            LogbookProgressGrid.FlagOffsetX = cfgProgCorrer.Value;
            LogbookProgressGrid.FixMasteryMeter = cfgProgMedidor.Value;
            LogbookProgressGrid.MeterColumns = cfgProgColsMedidor.Value;
            LogbookProgressGrid.MeterRows = cfgProgFilasMedidor.Value;
            LogbookProgressGrid.FlagSpacing = cfgProgSeparacion.Value;

            var cfgFiltroActivo = Config.Bind(
                "CardFilter", "Enabled", true,
                "Despeja la caja de busqueda del panel de filtros de cartas: por encima le pasa un adorno -la greca con el rombo que separa las secciones- y el texto que escribes queda cruzado por ella.");
            var cfgFiltroTraza = Config.Bind(
                "CardFilter", "Verbose", true,
                "Escribe en LogOutput.log que adorno ha apagado y de que tamano era.");
            var cfgFiltroArbol = Config.Bind(
                "CardFilter", "DumpTree", true,
                "Vuelca una vez el arbol del panel de filtros, con nombres, medidas y componentes. Es lo que permite saber que objeto es el adorno si el automatismo no acierta. Ponlo a false cuando ya este afinado.");
            var cfgFiltroDentro = Config.Bind(
                "CardFilter", "AlsoInside", false,
                "Buscar el adorno tambien DENTRO del propio SearchFilterUI. Por defecto no, porque ahi cuelgan el fondo y el marco de la caja y apagarlos la dejaria invisible. Ponlo a true solo si el log dice que no ha encontrado nada.");
            var cfgFiltroSolape = Config.Bind(
                "CardFilter", "MinOverlap", 0.25f,
                "Cuanto del alto de la caja tiene que tapar algo para darlo por adorno. 0,25 = una cuarta parte. Subelo si apaga algo que no debia.");
            var cfgFiltroNiveles = Config.Bind(
                "CardFilter", "Levels", 1,
                "Niveles que se sube desde el SearchFilterUI para buscar el adorno. Con 1 se mira la seccion Search entera; subelo a 2 si el adorno cuelga del panel de filtros completo.");

            CardFilterSearch.Enabled = cfgFiltroActivo.Value;
            CardFilterSearch.Verbose = cfgFiltroTraza.Value;
            CardFilterSearch.DumpTree = cfgFiltroArbol.Value;
            CardFilterSearch.AlsoInside = cfgFiltroDentro.Value;
            CardFilterSearch.MinOverlap = cfgFiltroSolape.Value;
            CardFilterSearch.Levels = cfgFiltroNiveles.Value;

            new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}

// 2026-09-22-2233||claude-mt2-CustomClanUIFixes||plugins/frutos-CustomClanUIFixes/src/Plugin.cs||MeterColumns 9->12 y MeterRows 6->5 en Config.Bind, y su descripcion (54 -> 60 cartas)
// 2026-09-22-2245||claude-mt2-CustomClanUIFixes||plugins/frutos-CustomClanUIFixes/src/Plugin.cs||seccion [CardFilter] nueva en el config (Enabled, Verbose, DumpTree, AlsoInside, MinOverlap, Levels) y volcado a CardFilterSearch
