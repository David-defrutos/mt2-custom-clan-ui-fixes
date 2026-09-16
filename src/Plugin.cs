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
            LogbookArtifactsPaging.WidthBudget = cfgArtAncho.Value;
            LogbookArtifactsPaging.RetryFrames = cfgArtReintentos.Value;
            LogbookArtifactsPaging.Verbose = cfgArtTraza.Value;

            new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}
