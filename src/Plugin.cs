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
                "Pone la hoja de progreso del logbook a una columna y la pagina, para que se vean todos los clanes y las banderitas de aliados no se aplasten.");
            var cfgProgColumnas = Config.Bind(
                "ProgressGrid", "Columns", 1,
                "Columnas de la rejilla de clanes. 1 = una seccion por fila, que es lo que deja sitio a las banderitas; 2 = la rejilla del juego.");
            var cfgProgFilas = Config.Bind(
                "ProgressGrid", "RowsPerPage", 0,
                "Filas por hoja. 0 = las que quepan de alto (con la celda del juego, 5).");
            var cfgProgTraza = Config.Bind(
                "ProgressGrid", "Verbose", true,
                "Escribe en LogOutput.log la rejilla, el reparto en sub-paginas y el detalle de las primeras secciones.");
            var cfgProgDetalle = Config.Bind(
                "ProgressGrid", "DetailSections", 1,
                "Cuantas secciones de clan se vuelcan con todo el detalle.");

            LogbookProgressGrid.Enabled = cfgProgActivo.Value;
            LogbookProgressGrid.Columns = cfgProgColumnas.Value;
            LogbookProgressGrid.RowsPerPage = cfgProgFilas.Value;
            LogbookProgressGrid.Verbose = cfgProgTraza.Value;
            LogbookProgressGrid.DetailSections = cfgProgDetalle.Value;

            new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}
