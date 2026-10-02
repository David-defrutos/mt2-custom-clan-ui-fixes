using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace mt2_custom_clan_ui_fixes.Plugin
{
    [DataContract]
    public sealed class CardUsageEntry
    {
        [DataMember(Name = "cardId")] public string? CardId;
        [DataMember(Name = "mode")] public string? Mode;
        [DataMember(Name = "direct")] public long Direct;
        [DataMember(Name = "automatic")] public long Automatic;
    }
    [DataContract]
    public sealed class CardUsageFile
    {
        [DataMember(Name = "version")] public int Version;
        [DataMember(Name = "startedUtc")] public string? StartedUtc;
        [DataMember(Name = "entries")] public List<CardUsageEntry>? Entries;
    }
    // Own data only. Never changes game saves or ExpandedWinTracker records.
    public sealed class CardUsageStore
    {
        readonly Dictionary<(string Card, string Mode), (long Direct, long Automatic)> counts = new();
        readonly string path;
        DateTime? lastWrite;
        public string StartedUtc { get; private set; }
        CardUsageStore(string file) { path = file; StartedUtc = DateTime.UtcNow.ToString("O"); }
        public static CardUsageStore Open(string file)
        {
            var result = new CardUsageStore(file);
            if (!File.Exists(file)) return result;
            result.lastWrite = File.GetLastWriteTimeUtc(file);
            string json;
            using (var reader = new StreamReader(file, Encoding.UTF8, true)) json = reader.ReadToEnd();
            using var input = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var data = new DataContractJsonSerializer(typeof(CardUsageFile)).ReadObject(input) as CardUsageFile;
            if (data == null || data.Version != 1 || data.Entries == null
                || !DateTimeOffset.TryParse(data.StartedUtc, out _))
                throw new InvalidDataException("Unsupported or incomplete card usage file.");
            result.StartedUtc = data.StartedUtc!;
            foreach (var row in data.Entries)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.CardId) || string.IsNullOrWhiteSpace(row.Mode)
                    || row.Direct < 0 || row.Automatic < 0 || result.counts.ContainsKey((row.CardId!, row.Mode!)))
                    throw new InvalidDataException("Invalid or duplicate card usage entry.");
                result.counts[(row.CardId!, row.Mode!)] = (row.Direct, row.Automatic);
            }
            result.CheckUnchanged();
            return result;
        }
        void CheckUnchanged()
        {
            DateTime? current = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : null;
            if (current != lastWrite) throw new IOException("Card usage file changed externally; refusing overwrite.");
        }
        public (long Direct, long Automatic) Get(string card, string? mode = null)
        {
            long direct = 0, automatic = 0;
            foreach (var item in counts)
                if (item.Key.Card == card && (mode == null || item.Key.Mode == mode))
                { direct = checked(direct + item.Value.Direct); automatic = checked(automatic + item.Value.Automatic); }
            return (direct, automatic);
        }
        public List<CardUsageEntry> Snapshot() => counts.Select(c => new CardUsageEntry {
            CardId = c.Key.Card, Mode = c.Key.Mode, Direct = c.Value.Direct, Automatic = c.Value.Automatic
        }).ToList();
        public void Record(string card, string mode, bool direct)
        {
            if (string.IsNullOrWhiteSpace(card) || string.IsNullOrWhiteSpace(mode))
                throw new ArgumentException("Missing card ID or mode.");
            var key = (card, mode);
            bool existed = counts.TryGetValue(key, out var old);
            counts[key] = direct ? (checked(old.Direct + 1), old.Automatic) : (old.Direct, checked(old.Automatic + 1));
            try { Save(); }
            catch { if (existed) counts[key] = old; else counts.Remove(key); throw; }
        }
        void Save()
        {
            CheckUnchanged();
            var data = new CardUsageFile
            {
                Version = 1, StartedUtc = StartedUtc,
                Entries = counts.OrderBy(c => c.Key.Card, StringComparer.Ordinal).ThenBy(c => c.Key.Mode, StringComparer.Ordinal)
                    .Select(c => new CardUsageEntry { CardId = c.Key.Card, Mode = c.Key.Mode,
                        Direct = c.Value.Direct, Automatic = c.Value.Automatic }).ToList()
            };
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var bom = Encoding.UTF8.GetPreamble(); file.Write(bom, 0, bom.Length);
                    new DataContractJsonSerializer(typeof(CardUsageFile)).WriteObject(file, data);
                    file.Flush(true);
                }
                CheckUnchanged();
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                lastWrite = File.GetLastWriteTimeUtc(path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}

// 2026-10-02-0039||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\CardUsageStore.cs||Mejora 4: recuentos por carta y modo, persistencia propia atómica, copia anterior y protección frente a corrupción/cambios externos

// 2026-10-02-0854||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\CardUsageStore.cs||Exponer copia de recuentos para ranking sin permitir mutaciones de datos internos
