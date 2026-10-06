using System;
using System.Collections.Generic;
using I2.Loc;
namespace mt2_custom_clan_ui_fixes.Plugin
{
    internal static class CardUsageText
    {
        static readonly Dictionary<string, string[]> ExtraTerms = new()
        {
            ["en"] = new[] { "Pending allies", "All allies completed", "All combinations", "Without a win", "Next allies", "Card plays", "Direct", "Automatic", "Counts start when tracking is installed. Replays are excluded; actual plays before undo or restart remain counted.", "Normal mode", "Clans" },
            ["es"] = new[] { "Aliados pendientes", "Todos los aliados completados", "Todas las combinaciones", "Sin victoria", "Siguientes aliados", "Cartas jugadas", "Directas", "Automáticas", "El recuento empieza al instalar el seguimiento. Excluye replays; las jugadas reales antes de deshacer o reiniciar siguen contadas.", "Modo normal", "Clanes" },
            ["fr"] = new[] { "Alliés restants", "Tous les alliés terminés", "Toutes les combinaisons", "Sans victoire", "Alliés suivants", "Cartes jouées", "Directes", "Automatiques", "Le comptage commence à l’installation. Les replays sont exclus ; les actions jouées avant annulation ou redémarrage restent comptées.", "Mode normal", "Clans" },
            ["de"] = new[] { "Fehlende Verbündete", "Alle Verbündeten abgeschlossen", "Alle Kombinationen", "Ohne Sieg", "Nächste Verbündete", "Gespielte Karten", "Direkt", "Automatisch", "Zählung ab Installation. Replays sind ausgeschlossen; tatsächliche Spiele vor Rücknahme oder Neustart bleiben gezählt.", "Normaler Modus", "Clans" },
            ["it"] = new[] { "Alleati mancanti", "Tutti gli alleati completati", "Tutte le combinazioni", "Senza vittoria", "Alleati successivi", "Carte giocate", "Dirette", "Automatiche", "Il conteggio inizia all’installazione. Esclude i replay; le giocate prima di annullare o riavviare restano contate.", "Modalità normale", "Clan" },
            ["pt"] = new[] { "Aliados pendentes", "Todos os aliados completos", "Todas as combinações", "Sem vitória", "Próximos aliados", "Cartas jogadas", "Diretas", "Automáticas", "Contagem a partir da instalação. Replays excluídos; jogadas antes de desfazer ou reiniciar continuam contadas.", "Modo normal", "Clãs" },
            ["pl"] = new[] { "Pozostali sojusznicy", "Wszyscy sojusznicy ukończeni", "Wszystkie kombinacje", "Bez zwycięstwa", "Następni sojusznicy", "Zagrane karty", "Bezpośrednie", "Automatyczne", "Liczenie od instalacji. Powtórki są wykluczone; zagrania przed cofnięciem lub restartem pozostają policzone.", "Tryb normalny", "Klany" },
            ["ru"] = new[] { "Оставшиеся союзники", "Все союзники пройдены", "Все сочетания", "Без победы", "Следующие союзники", "Сыгранные карты", "Прямые", "Автоматические", "Подсчёт с момента установки. Повторы исключены; реальные действия до отмены или перезапуска сохраняются.", "Обычный режим", "Кланы" },
            ["ja"] = new[] { "未達成の同盟", "すべての同盟を達成", "すべての組み合わせ", "勝利なし", "次の同盟", "カード使用回数", "直接", "自動", "導入後から集計します。リプレイを除外し、取り消しや再起動前の実際の使用は残ります。", "通常モード", "クラン" },
            ["ko"] = new[] { "남은 동맹", "모든 동맹 완료", "모든 조합", "승리 없음", "다음 동맹", "카드 사용 횟수", "직접", "자동", "설치 후 집계합니다. 리플레이는 제외하며 취소나 재시작 전 실제 사용은 유지합니다.", "일반 모드", "클랜" },
            ["zh"] = new[] { "待完成盟友", "所有盟友已完成", "所有组合", "无胜利", "下一组盟友", "卡牌使用次数", "直接", "自动", "从安装时开始计数。排除重放；撤销或重启前的实际使用仍计入。", "普通模式", "氏族" },
            ["zh-hant"] = new[] { "待完成盟友", "所有盟友已完成", "所有組合", "無勝利", "下一組盟友", "卡牌使用次數", "直接", "自動", "從安裝時開始計數。排除重播；撤銷或重啟前的實際使用仍計入。", "普通模式", "氏族" }
        };
        internal static string Extra(string key)
        {
            int index = Array.IndexOf(new[] { "Pending", "Complete", "All", "Missing", "Next", "Plays", "Direct", "Automatic", "UsageNote", "Normal", "Clans" }, key);
            string language = (LocalizationManager.CurrentLanguageCode ?? "en").ToLowerInvariant().Replace('_', '-');
            language = language.StartsWith("zh-tw") || language.StartsWith("zh-hk") || language.StartsWith("zh-hant") ? "zh-hant" : language.Split('-')[0];
            return (ExtraTerms.TryGetValue(language, out var values) ? values : ExtraTerms["en"])[index];
        }

    }
}

// 2026-10-04-2148||codex-soulsavior-split||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\CardUsageText.cs||Conservar solo términos utilizados en tooltips; retirar catálogo Soul Savior
