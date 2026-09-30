using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace mt2_custom_clan_ui_fixes.Plugin
{
    // Contrato de lectura compatible con extraMetagameSave.json de ExpandedWinTracker.
    // No escribe ni modifica el guardado del tracker o del juego.
    [DataContract]
    public sealed class SoulSaviorRecord
    {
        [DataMember(Name = "classID")] public string? ClassId;
        [DataMember(Name = "subclassID")] public string? SubclassId;
        [DataMember(Name = "championIndex")] public int? ChampionIndex;
        [DataMember(Name = "highestGameLevel")] public int? HighestGameLevel;
    }

    [DataContract]
    public sealed class SoulSaviorSave
    {
        [DataMember(Name = "soulSaviorClassWins")] public List<SoulSaviorRecord>? Wins;
    }

    public sealed class SoulSaviorRecords
    {
        readonly Dictionary<(string, string, int), int> wins = new();
        public int Count => wins.Count;

        public static SoulSaviorRecords Read(Stream input)
        {
            var save = new DataContractJsonSerializer(typeof(SoulSaviorSave)).ReadObject(input)
                       as SoulSaviorSave;
            if (save == null) throw new InvalidDataException("Guardado vacio o no valido.");
            var result = new SoulSaviorRecords();
            if (save.Wins == null) throw new InvalidDataException("Falta soulSaviorClassWins.");
            foreach (var win in save.Wins)
            {
                if (win == null || string.IsNullOrWhiteSpace(win.ClassId)
                    || string.IsNullOrWhiteSpace(win.SubclassId)
                    || !win.ChampionIndex.HasValue || !win.HighestGameLevel.HasValue
                    || win.ChampionIndex.Value < 0 || win.HighestGameLevel.Value < 0) continue;
                var key = (win.ClassId!, win.SubclassId!, win.ChampionIndex.Value);
                if (!result.wins.TryGetValue(key, out int old) || old < win.HighestGameLevel.Value)
                    result.wins[key] = win.HighestGameLevel.Value;
            }
            return result;
        }

        // -1 = sin victoria registrada; 0 tambien es una victoria valida.
        public int Get(string main, string ally, int champion)
            => wins.TryGetValue((main, ally, champion), out int level) ? level : -1;

        public int Best(string main, string ally)
        {
            int best = -1;
            foreach (var win in wins)
                if (win.Key.Item1 == main && win.Key.Item2 == ally)
                    best = Math.Max(best, win.Value);
            return best;
        }
    }
}
// 2026-09-30-1931||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorRecords.cs||Nueva integracion de lectura de ExpandedWinTracker y paginas Soul Savior por clan

// 2026-09-30-1937||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorRecords.cs||Integracion Soul Savior 0.4.0 completada; consultas validadas y presentacion separada de victorias normales
