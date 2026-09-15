using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace mt2_custom_clan_ui_fixes.Plugin
{
    /// <summary>
    /// Arreglos de interfaz para jugar con muchos clanes modeados instalados.
    /// No anade contenido y no depende de Trainworks ni de Conductor: solo parchea
    /// pantallas del juego base con Harmony.
    ///
    /// De momento uno solo: la pagina de mejoras de campeon del logbook
    /// (ver code/LogbookClanFit.cs).
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

            new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}
