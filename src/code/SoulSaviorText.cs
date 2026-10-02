using System;
using System.Collections.Generic;
using I2.Loc;

namespace mt2_custom_clan_ui_fixes.Plugin
{
    internal static class SoulSaviorText
    {
        // Note, empty, read error, display error, no win, difficulty, row summary,
        // tab description, combinations, clans, highest difficulty, legend, credit.
        internal static readonly Dictionary<string, string[]> Languages = new()
        {
            ["en"] = new[] { "Highest winning difficulty · Hover a banner for champion details", "No Soul Savior wins recorded yet.", "Could not read Soul Savior records.", "Could not display Soul Savior records.", "no recorded win", "difficulty {0}", "{0} / {1}\nallies\nwon", "Track wins by main clan and ally.", "Clan combinations won", "Clans with a win", "Highest winning difficulty", "S0, S1…: highest winning difficulty\n—: no recorded win\n\nHover an ally banner for champion details.", "Records: ExpandedWinTracker\nBrandon / Conductor" },
            ["es"] = new[] { "Dificultad máxima ganada · Detalle por campeón en cada bandera", "Todavía no hay victorias de Soul Savior registradas.", "No se pudieron leer los registros de Soul Savior.", "No se pudieron mostrar los registros de Soul Savior.", "sin victoria registrada", "dificultad {0}", "{0} / {1}\naliados\nganados", "Seguimiento de victorias por clan principal y aliado.", "Combinaciones de clanes ganadas", "Clanes con victoria", "Dificultad máxima ganada", "S0, S1…: dificultad máxima ganada\n—: sin victoria registrada\n\nPasa el cursor sobre una bandera para ver el detalle por campeón.", "Registros: ExpandedWinTracker\nBrandon / Conductor" },
            ["fr"] = new[] { "Difficulté maximale remportée · Détails des champions sur les bannières", "Aucune victoire Soul Savior enregistrée.", "Impossible de lire les résultats Soul Savior.", "Impossible d’afficher les résultats Soul Savior.", "aucune victoire enregistrée", "difficulté {0}", "{0} / {1}\nalliances\ngagnées", "Suivi des victoires par clan principal et allié.", "Combinaisons de clans remportées", "Clans avec une victoire", "Difficulté maximale remportée", "S0, S1… : difficulté maximale remportée\n— : aucune victoire enregistrée\n\nSurvolez une bannière pour les détails des champions.", "Résultats : ExpandedWinTracker\nBrandon / Conductor" },
            ["de"] = new[] { "Höchste gewonnene Schwierigkeit · Championdetails an den Bannern", "Noch keine Soul-Savior-Siege gespeichert.", "Soul-Savior-Daten konnten nicht gelesen werden.", "Soul-Savior-Daten konnten nicht angezeigt werden.", "kein gespeicherter Sieg", "Schwierigkeit {0}", "{0} / {1}\nAllianz-\nsiege", "Siege nach Hauptclan und verbündetem Clan verfolgen.", "Gewonnene Clan-Kombinationen", "Clans mit einem Sieg", "Höchste gewonnene Schwierigkeit", "S0, S1…: höchste gewonnene Schwierigkeit\n—: kein gespeicherter Sieg\n\nFür Championdetails über ein Banner fahren.", "Daten: ExpandedWinTracker\nBrandon / Conductor" },
            ["it"] = new[] { "Difficoltà massima vinta · Dettagli dei campioni sugli stendardi", "Nessuna vittoria Soul Savior registrata.", "Impossibile leggere i risultati Soul Savior.", "Impossibile mostrare i risultati Soul Savior.", "nessuna vittoria registrata", "difficoltà {0}", "{0} / {1}\nalleanze\nvincenti", "Vittorie per clan principale e alleato.", "Combinazioni di clan vinte", "Clan con una vittoria", "Difficoltà massima vinta", "S0, S1…: difficoltà massima vinta\n—: nessuna vittoria registrata\n\nPassa su uno stendardo per i dettagli dei campioni.", "Risultati: ExpandedWinTracker\nBrandon / Conductor" },
            ["pt"] = new[] { "Maior dificuldade vencida · Detalhes dos campeões nas bandeiras", "Nenhuma vitória Soul Savior registrada.", "Não foi possível ler os registros Soul Savior.", "Não foi possível mostrar os registros Soul Savior.", "nenhuma vitória registrada", "dificuldade {0}", "{0} / {1}\nalianças\nvencedoras", "Vitórias por clã principal e aliado.", "Combinações de clãs vencidas", "Clãs com uma vitória", "Maior dificuldade vencida", "S0, S1…: maior dificuldade vencida\n—: nenhuma vitória registrada\n\nPasse sobre uma bandeira para ver os campeões.", "Registros: ExpandedWinTracker\nBrandon / Conductor" },
            ["pl"] = new[] { "Najwyższy wygrany poziom trudności · Szczegóły czempionów na sztandarach", "Brak zapisanych zwycięstw Soul Savior.", "Nie można odczytać wyników Soul Savior.", "Nie można wyświetlić wyników Soul Savior.", "brak zapisanego zwycięstwa", "poziom trudności {0}", "{0} / {1}\nwygranych\nsojuszy", "Zwycięstwa według głównego i sojuszniczego klanu.", "Wygrane kombinacje klanów", "Klany ze zwycięstwem", "Najwyższy wygrany poziom trudności", "S0, S1…: najwyższy wygrany poziom trudności\n—: brak zapisanego zwycięstwa\n\nNajedź na sztandar, aby zobaczyć czempionów.", "Wyniki: ExpandedWinTracker\nBrandon / Conductor" },
            ["ru"] = new[] { "Максимальная сложность победы · Подробности чемпионов на знамёнах", "Победы Soul Savior ещё не записаны.", "Не удалось прочитать записи Soul Savior.", "Не удалось показать записи Soul Savior.", "нет записанной победы", "сложность {0}", "{0} / {1}\nпобедных\nсоюзов", "Победы по основному и союзному клану.", "Победные сочетания кланов", "Кланы с победой", "Максимальная сложность победы", "S0, S1…: максимальная сложность победы\n—: нет записанной победы\n\nНаведите курсор на знамя для подробностей чемпионов.", "Записи: ExpandedWinTracker\nBrandon / Conductor" },
            ["ja"] = new[] { "勝利した最高難易度 · 旗にカーソルを合わせてチャンピオンの詳細を表示", "Soul Saviorの勝利記録はまだありません。", "Soul Saviorの記録を読み込めませんでした。", "Soul Saviorの記録を表示できませんでした。", "勝利記録なし", "難易度 {0}", "{0} / {1}\n勝利した\n同盟", "メインクランと同盟クラン別の勝利記録。", "勝利したクランの組み合わせ", "勝利記録のあるクラン", "勝利した最高難易度", "S0、S1…：勝利した最高難易度\n—：勝利記録なし\n\n旗にカーソルを合わせるとチャンピオンの詳細を表示します。", "記録：ExpandedWinTracker\nBrandon / Conductor" },
            ["ko"] = new[] { "승리한 최고 난이도 · 깃발에서 챔피언 세부 정보 확인", "아직 Soul Savior 승리 기록이 없습니다.", "Soul Savior 기록을 읽을 수 없습니다.", "Soul Savior 기록을 표시할 수 없습니다.", "승리 기록 없음", "난이도 {0}", "{0} / {1}\n승리한\n동맹", "주 클랜과 동맹 클랜별 승리 기록.", "승리한 클랜 조합", "승리 기록이 있는 클랜", "승리한 최고 난이도", "S0, S1…: 승리한 최고 난이도\n—: 승리 기록 없음\n\n깃발에 커서를 올리면 챔피언 세부 정보를 볼 수 있습니다.", "기록: ExpandedWinTracker\nBrandon / Conductor" },
            ["zh"] = new[] { "最高获胜难度 · 悬停旗帜查看勇者详情", "尚无Soul Savior胜利记录。", "无法读取Soul Savior记录。", "无法显示Soul Savior记录。", "无胜利记录", "难度 {0}", "{0} / {1}\n获胜\n盟友", "按主氏族与盟友氏族查看胜利记录。", "获胜氏族组合", "有胜利记录的氏族", "最高获胜难度", "S0、S1…：最高获胜难度\n—：无胜利记录\n\n悬停盟友旗帜可查看勇者详情。", "记录：ExpandedWinTracker\nBrandon / Conductor" },
            ["zh-hant"] = new[] { "最高獲勝難度 · 懸停旗幟查看勇者詳情", "尚無Soul Savior勝利紀錄。", "無法讀取Soul Savior紀錄。", "無法顯示Soul Savior紀錄。", "無勝利紀錄", "難度 {0}", "{0} / {1}\n獲勝\n盟友", "按主氏族與盟友氏族查看勝利紀錄。", "獲勝氏族組合", "有勝利紀錄的氏族", "最高獲勝難度", "S0、S1…：最高獲勝難度\n—：無勝利紀錄\n\n懸停盟友旗幟可查看勇者詳情。", "紀錄：ExpandedWinTracker\nBrandon / Conductor" }
        };

        static readonly Dictionary<string, string[]> SoulTerms = new()
        {
            ["en"] = new[] { "Souls", "Highest soul tier: {0} · Highest difficulty: {1}", "Soul tier and difficulty are independent records. They may come from different wins.", "Souls with a win" },
            ["es"] = new[] { "Almas", "Nivel máximo del alma: {0} · Dificultad máxima: {1}", "El nivel del alma y la dificultad son récords independientes. Pueden proceder de victorias distintas.", "Almas con victoria" },
            ["fr"] = new[] { "Âmes", "Niveau d’âme maximal : {0} · Difficulté maximale : {1}", "Le niveau d’âme et la difficulté sont des records indépendants, issus éventuellement de victoires différentes.", "Âmes avec une victoire" },
            ["de"] = new[] { "Seelen", "Höchste Seelenstufe: {0} · Höchste Schwierigkeit: {1}", "Seelenstufe und Schwierigkeit sind unabhängige Rekorde und können aus verschiedenen Siegen stammen.", "Seelen mit einem Sieg" },
            ["it"] = new[] { "Anime", "Livello massimo dell’anima: {0} · Difficoltà massima: {1}", "Livello dell’anima e difficoltà sono record indipendenti, ottenuti anche in vittorie diverse.", "Anime con una vittoria" },
            ["pt"] = new[] { "Almas", "Nível máximo da alma: {0} · Dificuldade máxima: {1}", "Nível da alma e dificuldade são recordes independentes e podem vir de vitórias diferentes.", "Almas com vitória" },
            ["pl"] = new[] { "Dusze", "Najwyższy poziom duszy: {0} · Najwyższa trudność: {1}", "Poziom duszy i trudność to niezależne rekordy z potencjalnie różnych zwycięstw.", "Dusze ze zwycięstwem" },
            ["ru"] = new[] { "Души", "Максимальный уровень души: {0} · Максимальная сложность: {1}", "Уровень души и сложность — независимые рекорды из возможных разных побед.", "Души с победой" },
            ["ja"] = new[] { "ソウル", "ソウルの最高レベル：{0} · 最高難易度：{1}", "ソウルレベルと難易度は別々の記録です。異なる勝利で達成した場合があります。", "勝利記録のあるソウル" },
            ["ko"] = new[] { "영혼", "영혼 최고 단계: {0} · 최고 난이도: {1}", "영혼 단계와 난이도는 별개의 기록이며 서로 다른 승리에서 달성했을 수 있습니다.", "승리 기록이 있는 영혼" },
            ["zh"] = new[] { "灵魂", "灵魂最高等级：{0} · 最高难度：{1}", "灵魂等级和难度是独立记录，可能来自不同的胜利。", "有胜利记录的灵魂" },
            ["zh-hant"] = new[] { "靈魂", "靈魂最高等級：{0} · 最高難度：{1}", "靈魂等級和難度是獨立紀錄，可能來自不同的勝利。", "有勝利紀錄的靈魂" }
        };

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

        internal static string ForLanguage(string? code, int key)
        {
            string language = (code ?? "en").ToLowerInvariant().Replace('_', '-');
            if (language.StartsWith("zh-tw") || language.StartsWith("zh-hk")
                || language.StartsWith("zh-hant")) language = "zh-hant";
            else language = language.Split('-')[0];
            var catalog = key >= 13 ? SoulTerms : Languages;
            return (catalog.TryGetValue(language, out var values) ? values : catalog["en"])[key >= 13 ? key - 13 : key];
        }
        internal static string Get(int key, params object[] args)
            => string.Format(ForLanguage(LocalizationManager.CurrentLanguageCode, key), args);
    }
}
// 2026-09-30-2235||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorText.cs||Textos de Soul Savior según idioma I2 activo, variantes regionales, 12 tablas y fallback inglés
// 2026-09-30-2245||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorText.cs||Corregir traducciones del contador de alianzas: victorias con aliados, no aliados derrotados

// 2026-10-01-2324||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorText.cs||Añadir textos de catálogo de almas, niveles y máximos independientes en los doce idiomas

// 2026-10-02-0031||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorText.cs||Textos localizados del panel, controles, filtro y futuros recuentos de cartas en doce idiomas

// 2026-10-02-0052||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorText.cs||Añadir etiqueta localizada de modo normal para subtotal de cartas

// 2026-10-02-0052||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorText.cs||Completar término de modo normal en la última tabla (chino tradicional), detectado por prueba de catálogo

// 2026-10-02-0735||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\SoulSaviorText.cs||Borrador: etiqueta Clanes en doce idiomas y retirar instrucción obsoleta de paginación conjunta
