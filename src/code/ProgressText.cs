using System;
using System.Collections.Generic;
using I2.Loc;

namespace mt2_custom_clan_ui_fixes.Plugin
{
    internal static class ProgressText
    {
        internal static readonly string[] Keys = new[] { "Progress", "Cards", "All", "Mode", "Clan", "Neutral", "Other", "Goal", "BelowGoal", "Champion", "Both", "Name", "ProgressOrder", "PendingOrder", "Search", "Clear", "Refresh", "Previous", "Next", "NoResults", "Unavailable", "Since", "PendingGoal", "Best", "Results", "AllClans", "SelectClan", "GoalHint", "CycleHint" };
        internal static readonly Dictionary<string, string[]> Languages = new()
        {
            ["en"] = new[] { "Progress", "Cards", "All", "Mode", "Clan", "Clanless", "Other modes", "Goal", "Below goal", "Champion", "Any champion", "Name", "Most progress", "Most pending", "Search", "Clear", "Refresh", "Previous", "Next", "No matching results.", "Tracking unavailable.", "Since", "Pending at goal", "Best", "Results", "All clans", "Select a clan to see allies below the goal for the chosen champion.", "Select Goal to change the required difficulty.", "Select to cycle through the available options." },
            ["es"] = new[] { "Progreso", "Cartas", "Todos", "Modo", "Clan", "Sin clan", "Otros modos", "Objetivo", "Por debajo del objetivo", "Campeón", "Cualquier campeón", "Nombre", "Más progreso", "Más pendientes", "Buscar", "Limpiar", "Actualizar", "Anterior", "Siguiente", "No hay resultados.", "Seguimiento no disponible.", "Desde", "Pendientes del objetivo", "Máximo", "Resultados", "Todos los clanes", "Selecciona un clan para ver los aliados que faltan al campeón elegido para el objetivo.", "Pulsa Objetivo para cambiar la dificultad requerida.", "Pulsa para recorrer las opciones disponibles." },
            ["fr"] = new[] { "Progression", "Cartes", "Tous", "Mode", "Clan", "Sans clan", "Autres modes", "Objectif", "Sous l’objectif", "Champion", "Tout champion", "Nom", "Plus de progression", "Plus de restants", "Rechercher", "Effacer", "Actualiser", "Précédent", "Suivant", "Aucun résultat.", "Suivi indisponible.", "Depuis", "Restants pour l’objectif", "Maximum", "Résultats", "Tous les clans", "Sélectionnez un clan pour voir les alliés restant au champion choisi pour cet objectif.", "Sélectionnez Objectif pour changer la difficulté requise.", "Sélectionnez pour parcourir les options disponibles." },
            ["de"] = new[] { "Fortschritt", "Karten", "Alle", "Modus", "Clan", "Clanlos", "Andere Modi", "Ziel", "Unter dem Ziel", "Champion", "Beliebiger Champion", "Name", "Größter Fortschritt", "Meiste offene Siege", "Suchen", "Leeren", "Aktualisieren", "Zurück", "Weiter", "Keine Ergebnisse.", "Erfassung nicht verfügbar.", "Seit", "Für das Ziel offen", "Maximum", "Ergebnisse", "Alle Clans", "Wähle einen Clan, um fehlende Verbündete für den gewählten Champion und das Ziel zu sehen.", "Wähle Ziel, um die erforderliche Schwierigkeit zu ändern.", "Wähle dies, um die verfügbaren Optionen durchzugehen." },
            ["it"] = new[] { "Progresso", "Carte", "Tutti", "Modalità", "Clan", "Senza clan", "Altre modalità", "Obiettivo", "Sotto l’obiettivo", "Campione", "Qualsiasi campione", "Nome", "Più progresso", "Più mancanti", "Cerca", "Cancella", "Aggiorna", "Precedente", "Successivo", "Nessun risultato.", "Monitoraggio non disponibile.", "Dal", "Mancanti per l’obiettivo", "Massimo", "Risultati", "Tutti i clan", "Seleziona un clan per vedere gli alleati mancanti al campione scelto per l’obiettivo.", "Seleziona Obiettivo per cambiare la difficoltà richiesta.", "Seleziona per scorrere le opzioni disponibili." },
            ["pt"] = new[] { "Progresso", "Cartas", "Todos", "Modo", "Clã", "Sem clã", "Outros modos", "Objetivo", "Abaixo do objetivo", "Campeão", "Qualquer campeão", "Nome", "Mais progresso", "Mais pendentes", "Pesquisar", "Limpar", "Atualizar", "Anterior", "Seguinte", "Nenhum resultado.", "Acompanhamento indisponível.", "Desde", "Pendentes do objetivo", "Máximo", "Resultados", "Todos os clãs", "Selecione um clã para ver os aliados pendentes do campeão escolhido para o objetivo.", "Selecione Objetivo para mudar a dificuldade exigida.", "Selecione para percorrer as opções disponíveis." },
            ["pl"] = new[] { "Postęp", "Karty", "Wszystkie", "Tryb", "Klan", "Bez klanu", "Inne tryby", "Cel", "Poniżej celu", "Czempion", "Dowolny czempion", "Nazwa", "Największy postęp", "Najwięcej braków", "Szukaj", "Wyczyść", "Odśwież", "Poprzednia", "Następna", "Brak wyników.", "Śledzenie niedostępne.", "Od", "Brakujące do celu", "Maksimum", "Wyniki", "Wszystkie klany", "Wybierz klan, aby zobaczyć brakujących sojuszników dla wybranego czempiona i celu.", "Wybierz Cel, aby zmienić wymaganą trudność.", "Wybierz, aby przełączać dostępne opcje." },
            ["ru"] = new[] { "Прогресс", "Карты", "Все", "Режим", "Клан", "Без клана", "Другие режимы", "Цель", "Ниже цели", "Чемпион", "Любой чемпион", "Название", "Больше прогресса", "Больше недостающих", "Поиск", "Очистить", "Обновить", "Назад", "Далее", "Нет результатов.", "Отслеживание недоступно.", "С", "Осталось до цели", "Максимум", "Результаты", "Все кланы", "Выберите клан, чтобы увидеть союзников, недостающих выбранному чемпиону для цели.", "Выберите Цель, чтобы изменить требуемую сложность.", "Выберите, чтобы переключать доступные варианты." },
            ["ja"] = new[] { "進捗", "カード", "すべて", "モード", "クラン", "クランなし", "その他のモード", "目標", "目標未達成", "チャンピオン", "いずれかのチャンピオン", "名前", "進捗順", "未達成数順", "検索", "クリア", "更新", "前へ", "次へ", "結果がありません。", "追跡を利用できません。", "開始日", "目標までの未達成", "最高", "結果", "すべてのクラン", "クランを選択すると、選んだチャンピオンの目標未達成の同盟を表示します。", "目標を選択すると必要な難易度を変更できます。", "選択すると利用可能な項目を切り替えます。" },
            ["ko"] = new[] { "진행도", "카드", "전체", "모드", "클랜", "클랜 없음", "기타 모드", "목표", "목표 미달", "챔피언", "아무 챔피언", "이름", "진행도 순", "미완료 순", "검색", "지우기", "새로고침", "이전", "다음", "결과가 없습니다.", "추적을 사용할 수 없습니다.", "시작일", "목표까지 남은 항목", "최고", "결과", "모든 클랜", "클랜을 선택하면 선택한 챔피언의 목표에 필요한 남은 동맹을 표시합니다.", "목표를 선택하여 필요한 난이도를 변경합니다.", "선택하면 사용 가능한 옵션을 순서대로 바꿉니다." },
            ["zh"] = new[] { "进度", "卡牌", "全部", "模式", "氏族", "无氏族", "其他模式", "目标", "未达目标", "勇者", "任一勇者", "名称", "进度最多", "待完成最多", "搜索", "清空", "刷新", "上一页", "下一页", "没有结果。", "无法进行跟踪。", "开始日期", "目标待完成", "最高", "结果", "所有氏族", "选择氏族，查看所选勇者尚未达成目标的盟友。", "选择目标可更改所需难度。", "选择以切换可用选项。" },
            ["zh-hant"] = new[] { "進度", "卡牌", "全部", "模式", "氏族", "無氏族", "其他模式", "目標", "未達目標", "勇者", "任一勇者", "名稱", "進度最多", "待完成最多", "搜尋", "清空", "重新整理", "上一頁", "下一頁", "沒有結果。", "無法進行追蹤。", "開始日期", "目標待完成", "最高", "結果", "所有氏族", "選擇氏族，查看所選勇者尚未達成目標的盟友。", "選擇目標可更改所需難度。", "選擇以切換可用選項。" },
        };
        internal static string Get(string key)
        {
            int index = Array.IndexOf(Keys, key);
            if (index < 0) throw new ArgumentException("Unknown progress text key: " + key);
            string language = (LocalizationManager.CurrentLanguageCode ?? "en").ToLowerInvariant().Replace('_', '-');
            language = language.StartsWith("zh-tw") || language.StartsWith("zh-hk") || language.StartsWith("zh-hant")
                ? "zh-hant" : language.Split('-')[0];
            return (Languages.TryGetValue(language, out var values) ? values : Languages["en"])[index];
        }
    }
}

// 2026-10-02-0856||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressText.cs||Textos del explorador de progreso, filtros, objetivos y ranking en doce idiomas

// 2026-10-02-0911||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressText.cs||Precisar que la vista agregada usa cualquier campeón, no exige victoria de ambos; corregir alemán

// 2026-10-02-0914||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressText.cs||Añadir instrucciones de selección de clan y objetivo acordes al explorador en doce idiomas

// 2026-10-02-1051||codex-customclanuifixes-review||C:\Users\david\AppData\Roaming\Thunderstore Mod Manager\DataFolder\MonsterTrain2\profiles\Default\BepInEx\plugins\frutos-CustomClanUIFixes\src\code\ProgressText.cs||Añadir ayuda de controles cíclicos en los doce idiomas del juego
