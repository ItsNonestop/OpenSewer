using OpenSewer.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using UnityEngine;

namespace OpenSewer
{
    internal partial class GUIComponent : WindowBase
    {
        enum ModTab
        {
            Main,
            ItemSpawner,
            FurnitureSpawner,
            Stats
        }
        const bool DEBUG_LAYOUT = false;

        ModTab _currentTab = ModTab.Main;

        GUIStyle buttonStyle;
        GUIStyle selectedTabStyle;
        GUIStyle unselectedTabStyle;
        GUIStyle sectionHeaderStyle;
        GUIStyle richLabelStyle;
        Vector2 categoryScrollPos;
        Vector2 itemScrollPos;
        Vector2 mainTabScrollPos;
        Vector2 _statsScrollPos;
        bool windowHover;
        bool showGodModeTodo;
        bool showNoClipTodo;
        Item selectedItem;
        bool _showAmountPicker;
        Item _amountPickerItem;
        int _amountPickerValue = 1;
        float _amountPickerSliderValue = 1f;
        int _amountPickerMaxStack = 1;
        string _amountPickerText = "1";
        const float LeftW = 220f;
        const float RightW = 260f;
        const float OuterPad = 24f;
        const float Tile = 64f;
        const float Pad = 6f;
        int _gridColsCached = 4;
        float _centerWidthCached;
        float _nextItemsGridLogTime;
        bool _layoutNumbersLogged;

        float minHeight = 520f;
        float minWidth = 860f;

        const int delayForFrames = 3; // 1 to avoid null per cycle. 3 to avoid null per click.
        int framesSinceHover;

        IEnumerable<Item> Filtereditems;
        readonly HashSet<string> excludeCategory = ["Liquid", "ItemGroup"];
        List<string> validCategories = [];

        IDisposable selectedCategoriesSub;
        IDisposable hoverItemSub;
        IDisposable textFilterSub;
        readonly Dictionary<string, string> _statsInputBuffers = [];
        bool _statsShowVitals = true;
        bool _statsShowNeeds = true;
        bool _statsShowMental = true;
        bool _statsShowAddictions = true;
        bool _statsShowAdvanced;

        private readonly struct EditableStatRow
        {
            public readonly string Key;
            public readonly string Label;
            public readonly Func<float> GetValue;
            public readonly Action<float> SetValue;
            public readonly float Min;
            public readonly float Max;
            public readonly bool IsPercentLike;

            public EditableStatRow(
                string key,
                string label,
                Func<float> getValue,
                Action<float> setValue,
                float min,
                float max,
                bool isPercentLike)
            {
                Key = key;
                Label = label;
                GetValue = getValue;
                SetValue = setValue;
                Min = min;
                Max = max;
                IsPercentLike = isPercentLike;
            }
        }
        void CategorySelectionChanged<T>(T _)
        {
            var filterText = TextFilter.Value.Trim();
            var hasText = filterText.Length > 0;

            // Parse ID if possible
            var validId = int.TryParse(filterText, out int parsedId)
                          && parsedId > 0 && parsedId < 1_000_000;

            // Text predicate
            bool TextMatch(Item item)
            {
                if (validId && parsedId == item.ID)
                    return true;

                if (!hasText)
                    return true;

                return item.Title.ToLower().Contains(filterText.ToLower());
            }

            // Compute valid categories (based on text filter)
            validCategories = ItemHandler.Items
                .Where(item => !item.Categories.Any(excludeCategory.Contains))
                .Where(item => !SelectedCategories.Any() || SelectedCategories.All(item.Categories.Contains))
                .Where(TextMatch)
                .SelectMany(item => item.Categories)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Selected categories that actually exist in validCategories
            var effectiveSelected = SelectedCategories
                .Where(validCategories.Contains)
                .ToList();

            // Selected-categories predicate
            bool SelectedMatch(Item item) =>
                effectiveSelected.All(item.Categories.Contains);

            // Choose predicate based on whether text is present
            Func<Item, bool> predicateToUse =
                hasText ? TextMatch : SelectedMatch;

            // Final filtered items
            Filtereditems = ItemHandler.Items
                .Where(item => item.Categories.All(validCategories.Contains))
                .Where(predicateToUse)
                .Where(SelectedMatch)   // always enforce selected categories
                .ToList();
        }

        void Init()
        {
            if (windowRect.Equals(default))
            {
                var x = Screen.width / 2 - minWidth / 2;
                var y = Screen.height / 2f - minHeight / 2;
                windowRect = new(x, y, minWidth, minHeight);
            }

            textFilterSub ??= TextFilter
                .Throttle(TimeSpan.FromMilliseconds(250))
                .DistinctUntilChanged()
                .Subscribe(CategorySelectionChanged);

            //hoverItemSub ??= HoverItem
            //    .DistinctUntilChanged()
            //    .Subscribe(HoverItemChanged);

            selectedCategoriesSub ??= SelectedCategories.Changes
                .Subscribe(CategorySelectionChanged);

            buttonStyle ??= new(GUI.skin.button)
            {
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft
            };

            selectedTabStyle ??= new(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold
            };

            unselectedTabStyle ??= new(GUI.skin.button);

            sectionHeaderStyle ??= new(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold
            };

            richLabelStyle ??= new(GUI.skin.label)
            {
                richText = true
            };
        }

        private void AddItem(Item item)
        {
            RequestSpawnOne(item);
        }

        private void SpawnExact(Item item, int amount)
        {
            if (item == null) return;
            if (BackpackStorage.instance == null) return;
            if (amount < 1) return;

            ItemOperations.AddItemsAndDropRemaining(item: item,
                                                    owner: -1,
                                                    slot: null,
                                                    meta: item.Meta,
                                                    amount: amount,
                                                    stackAmount: 0);
        }

        private void RequestSpawnOne(Item item)
        {
            SpawnExact(item, 1);
        }

        private void RequestSpawnStack(Item item)
        {
            SpawnExact(item, GetMaxStack(item));
        }

        private void RequestSpawnAmount(Item item, int requestedAmount)
        {
            if (item == null) return;
            if (BackpackStorage.instance == null) return;

            int clampedRequested = Mathf.Max(1, requestedAmount);
            int maxStack = GetMaxStack(item);
            if (clampedRequested <= maxStack)
            {
                SpawnExact(item, clampedRequested);
                return;
            }

            int fullStacks = clampedRequested / maxStack;
            int remainder = clampedRequested % maxStack;

            for (int i = 0; i < fullStacks; i++)
                SpawnExact(item, maxStack);

            if (remainder > 0)
                SpawnExact(item, remainder);
        }

        private static int GetMaxStack(Item item)
        {
            return Mathf.Max(1, item?.Stackable ?? 1);
        }

        private void OpenAmountPicker(Item item)
        {
            if (item == null) return;

            selectedItem = item;
            _amountPickerItem = item;
            _amountPickerMaxStack = GetMaxStack(item);
            _amountPickerValue = _amountPickerMaxStack;
            _amountPickerSliderValue = _amountPickerValue;
            _amountPickerText = _amountPickerValue.ToString();
            _showAmountPicker = true;
            Plugin.DLog($"Picker opened for {item.ID}/{item.Title} max={_amountPickerMaxStack}");
        }

        private void CloseAmountPicker()
        {
            _showAmountPicker = false;
        }

        private void SpawnAmountFromPicker()
        {
            var item = _amountPickerItem;
            if (item == null) return;
            if (BackpackStorage.instance == null) return;

            int requestedAmount = _amountPickerValue;
            if (!string.IsNullOrWhiteSpace(_amountPickerText) && int.TryParse(_amountPickerText, out int typedAmount))
                requestedAmount = typedAmount;

            requestedAmount = Mathf.Max(1, requestedAmount);

            Plugin.DLog($"Picker confirmed amount={requestedAmount}");
            RequestSpawnAmount(item, requestedAmount);

            CloseAmountPicker();
        }
        void Release()
        {
            if (GUI.GetNameOfFocusedControl() == "Filter")
            {
                GUI.FocusControl(null);
                GUI.FocusWindow(windowId);
            }
        }

        protected void DrawGui()
        {
            Init();
            windowHover = windowRect.Contains(Event.current.mousePosition);
            GUI.backgroundColor = Color.black;
            windowRect = GUILayout.Window(windowId, windowRect, WindowFunction, "OpenSewer");
            GUI.backgroundColor = Color.white;

            if (HoverItem.Value != null && framesSinceHover++ > delayForFrames)
                HoverItem.OnNext(null);
        }

        private void WindowFunction(int id)
        {
            DrawTabBar();

            switch (_currentTab)
            {
                case ModTab.Main:
                    DrawMainTab();
                    break;
                case ModTab.ItemSpawner:
                    DrawFilterHeaderRow();
                    DrawSpawnerBody3Column();
                    using (new GUILayout.HorizontalScope()) { Footer(); }
                    break;
                case ModTab.FurnitureSpawner:
                    DrawFurnitureHeaderRow();
                    DrawFurnitureBody3Column();
                    break;
                case ModTab.Stats:
                    DrawStatsTab();
                    break;
            }

            if (!GUI.changed) GUI.DragWindow();
        }

        private void DrawTabBar()
        {
            using (new GUILayout.HorizontalScope("box"))
            {
                if (GUILayout.Button("MAIN", _currentTab == ModTab.Main ? selectedTabStyle : unselectedTabStyle, GUILayout.Height(28)))
                    _currentTab = ModTab.Main;

                if (GUILayout.Button("ITEM SPAWNER", _currentTab == ModTab.ItemSpawner ? selectedTabStyle : unselectedTabStyle, GUILayout.Height(28)))
                    _currentTab = ModTab.ItemSpawner;

                if (GUILayout.Button("FURNITURE SPAWNER", _currentTab == ModTab.FurnitureSpawner ? selectedTabStyle : unselectedTabStyle, GUILayout.Height(28)))
                    _currentTab = ModTab.FurnitureSpawner;

                if (GUILayout.Button("STATS", _currentTab == ModTab.Stats ? selectedTabStyle : unselectedTabStyle, GUILayout.Height(28)))
                    _currentTab = ModTab.Stats;
            }
        }

        private void DrawMainTab()
        {
            var hasStats = PlayerStatsReader.TryGetSnapshot(out var stats);

            using (var scroll = new GUILayout.ScrollViewScope(mainTabScrollPos, GUILayout.MinHeight(minHeight)))
            {
                mainTabScrollPos = scroll.scrollPosition;

                using (new GUILayout.VerticalScope("box"))
                {
                    GUILayout.Label("Player Stats", sectionHeaderStyle);
                    if (!hasStats)
                    {
                        GUILayout.Label("Stats not ready (load into a save)");
                    }
                    else
                    {
                        GUILayout.Label("Stats Overview", sectionHeaderStyle);
                        DrawStatValueRow("Health", stats.Health);
                        DrawStatValueRow("Hunger", stats.Hunger);
                        DrawStatValueRow("Thirst", stats.Thirst);
                    }
                }

                using (new GUILayout.VerticalScope("box"))
                {
                    GUILayout.Label("General Info", sectionHeaderStyle);
                    if (hasStats)
                    {
                        DrawInfoRow("Depression", FormatStatValue(stats.Depression));
                        DrawInfoRow("Sleep Need", FormatStatValue(stats.SleepNeed));
                        DrawInfoRow("WC Need", FormatStatValue(stats.WCNeed));
                        DrawInfoRow("Hygiene", FormatStatValue(stats.Hygiene));
                    }
                    else
                    {
                        DrawInfoRow("Depression", "N/A");
                        DrawInfoRow("Sleep Need", "N/A");
                        DrawInfoRow("WC Need", "N/A");
                        DrawInfoRow("Hygiene", "N/A");
                    }
                }

                using (new GUILayout.VerticalScope("box"))
                {
                    if (GUILayout.Button("Enable God Mode"))
                        showGodModeTodo = true;
                    if (showGodModeTodo)
                        GUILayout.Label("TODO: hook console command");

                    if (GUILayout.Button("Enable NoClip"))
                        showNoClipTodo = true;
                    if (showNoClipTodo)
                        GUILayout.Label("TODO: hook console command");
                }
            }
        }

        private void DrawStatValueRow(string label, StatValue value)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(110));
                if (!value.HasValue)
                {
                    GUILayout.Label("N/A");
                }
                else if (value.HasMax && value.Max > 0f)
                {
                    float ratio = Mathf.Clamp01(value.Value / value.Max);
                    GUILayout.HorizontalSlider(ratio, 0f, 1f, GUILayout.MinWidth(220));
                    GUILayout.Label($"{value.Value:0.##}/{value.Max:0.##} ({Mathf.RoundToInt(ratio * 100f)}%)", GUILayout.Width(150));
                }
                else
                {
                    GUILayout.Label($"{value.Value:0.##}");
                }
            }
        }

        private static string FormatStatValue(StatValue value)
        {
            if (!value.HasValue)
                return "N/A";

            if (value.HasMax && value.Max > 0f)
            {
                float ratio = Mathf.Clamp01(value.Value / value.Max);
                return $"{value.Value:0.##}/{value.Max:0.##} ({Mathf.RoundToInt(ratio * 100f)}%)";
            }

            return $"{value.Value:0.##}";
        }

        private void DrawStatsTab()
        {
            var vitalsRows = BuildVitalsRows();
            var needsRows = BuildNeedsRows();
            var mentalRows = BuildMentalRows();
            var addictionsRows = BuildAddictionsRows();
            var advancedRows = BuildAdvancedRows();

            bool isReady = PlayerStatsAccess.IsReady();

            using (var scroll = new GUILayout.ScrollViewScope(_statsScrollPos, false, true, GUILayout.MinHeight(minHeight)))
            {
                _statsScrollPos = scroll.scrollPosition;

                using (new GUILayout.VerticalScope("box"))
                {
                    GUILayout.Label("Stats Editor", sectionHeaderStyle);
                    GUILayout.Label(isReady
                        ? "Live values. Slider and numeric input apply immediately."
                        : "Stats not ready - load into a save");

                    bool freezeAll = StatFreezer.FreezeAllEnabled;
                    bool freezeAllNext = GUILayout.Toggle(freezeAll, "Freeze All");
                    if (freezeAllNext != freezeAll)
                    {
                        if (freezeAllNext)
                        {
                            var visibleKeys = BuildVisibleStatsRowKeys(vitalsRows, needsRows, mentalRows, addictionsRows, advancedRows);
                            StatFreezer.FreezeAllEnabled = true;
                            StatFreezer.EnableFreezeForKeys(visibleKeys);
                        }
                        else
                        {
                            StatFreezer.DisableAllFrozen();
                        }
                    }
                }

                bool prevEnabled = GUI.enabled;
                GUI.enabled = prevEnabled && isReady;

                DrawStatsSection("Vitals", ref _statsShowVitals, vitalsRows);
                DrawStatsSection("Needs", ref _statsShowNeeds, needsRows);
                DrawStatsSection("Mental", ref _statsShowMental, mentalRows);
                DrawStatsSection("Addictions", ref _statsShowAddictions, addictionsRows);
                DrawStatsSection("Advanced", ref _statsShowAdvanced, advancedRows);

                GUI.enabled = prevEnabled;
            }
        }

        private List<EditableStatRow> BuildVitalsRows()
        {
            return
            [
                new EditableStatRow("vitals.health", "Health", PlayerStatsAccess.GetHealth, PlayerStatsAccess.SetHealth, 0f, 100f, true)
            ];
        }

        private List<EditableStatRow> BuildNeedsRows()
        {
            return
            [
                new EditableStatRow("needs.hunger", "Hunger", PlayerStatsAccess.GetHunger, PlayerStatsAccess.SetHunger, 0f, 100f, true),
                new EditableStatRow("needs.thirst", "Thirst", PlayerStatsAccess.GetThirst, PlayerStatsAccess.SetThirst, 0f, 100f, true),
                new EditableStatRow("needs.sleep", "Sleep Need", PlayerStatsAccess.GetSleepNeed, PlayerStatsAccess.SetSleepNeed, 0f, 100f, true),
                new EditableStatRow("needs.wc", "WC Need", PlayerStatsAccess.GetWcNeed, PlayerStatsAccess.SetWcNeed, 0f, 100f, true),
                new EditableStatRow("needs.hygiene", "Hygiene", PlayerStatsAccess.GetHygiene, PlayerStatsAccess.SetHygiene, 0f, 100f, true)
            ];
        }

        private List<EditableStatRow> BuildMentalRows()
        {
            return
            [
                new EditableStatRow("mental.depression", "Depression", PlayerStatsAccess.GetDepression, PlayerStatsAccess.SetDepression, 0f, 100f, true)
            ];
        }

        private List<EditableStatRow> BuildAddictionsRows()
        {
            return
            [
                new EditableStatRow("addictions.need.mushroom", "Mushroom Need", PlayerStatsAccess.GetMushroomNeed, PlayerStatsAccess.SetMushroomNeed, 0f, 100f, true),
                new EditableStatRow("addictions.need.alcohol", "Alcohol Need", PlayerStatsAccess.GetAlcoholNeed, PlayerStatsAccess.SetAlcoholNeed, 0f, 100f, true),
                new EditableStatRow("addictions.need.smoking", "Smoking Need", PlayerStatsAccess.GetSmokingNeed, PlayerStatsAccess.SetSmokingNeed, 0f, 100f, true),
                new EditableStatRow("addictions.need.gambling", "Gambling Need", PlayerStatsAccess.GetGamblingNeed, PlayerStatsAccess.SetGamblingNeed, 0f, 100f, true),
                new EditableStatRow("addictions.level.mushroom", "Mushroom Addiction", PlayerStatsAccess.GetMushroomAddiction, PlayerStatsAccess.SetMushroomAddiction, 0f, 100f, true),
                new EditableStatRow("addictions.level.alcohol", "Alcohol Addiction", PlayerStatsAccess.GetAlcoholAddiction, PlayerStatsAccess.SetAlcoholAddiction, 0f, 100f, true),
                new EditableStatRow("addictions.level.smoking", "Smoking Addiction", PlayerStatsAccess.GetSmokingAddiction, PlayerStatsAccess.SetSmokingAddiction, 0f, 100f, true),
                new EditableStatRow("addictions.level.gambling", "Gambling Addiction", PlayerStatsAccess.GetGamblingAddiction, PlayerStatsAccess.SetGamblingAddiction, 0f, 100f, true)
            ];
        }

        private List<EditableStatRow> BuildAdvancedRows()
        {
            return
            [
                new EditableStatRow("advanced.rate.need.mushroom", "Mushroom Need Baserate", PlayerStatsAccess.GetMushroomNeedBaserate, PlayerStatsAccess.SetMushroomNeedBaserate, -1000f, 1000f, false),
                new EditableStatRow("advanced.rate.need.alcohol", "Alcohol Need Baserate", PlayerStatsAccess.GetAlcoholNeedBaserate, PlayerStatsAccess.SetAlcoholNeedBaserate, -1000f, 1000f, false),
                new EditableStatRow("advanced.rate.need.smoking", "Smoking Need Baserate", PlayerStatsAccess.GetSmokingNeedBaserate, PlayerStatsAccess.SetSmokingNeedBaserate, -1000f, 1000f, false),
                new EditableStatRow("advanced.rate.need.gambling", "Gambling Need Baserate", PlayerStatsAccess.GetGamblingNeedBaserate, PlayerStatsAccess.SetGamblingNeedBaserate, -1000f, 1000f, false),
                new EditableStatRow("advanced.rate.change.mushroom", "Mushroom Addiction Change", PlayerStatsAccess.GetMushroomAddictionChange, PlayerStatsAccess.SetMushroomAddictionChange, -1000f, 1000f, false),
                new EditableStatRow("advanced.rate.change.alcohol", "Alcohol Addiction Change", PlayerStatsAccess.GetAlcoholAddictionChange, PlayerStatsAccess.SetAlcoholAddictionChange, -1000f, 1000f, false),
                new EditableStatRow("advanced.rate.change.smoking", "Smoking Addiction Change", PlayerStatsAccess.GetSmokingAddictionChange, PlayerStatsAccess.SetSmokingAddictionChange, -1000f, 1000f, false),
                new EditableStatRow("advanced.rate.change.gambling", "Gambling Addiction Change", PlayerStatsAccess.GetGamblingAddictionChange, PlayerStatsAccess.SetGamblingAddictionChange, -1000f, 1000f, false),
                new EditableStatRow("advanced.withdrawal.mental", "Withdrawal MentalHealth Loss", PlayerStatsAccess.GetWithdrawalMentalHealthLoss, PlayerStatsAccess.SetWithdrawalMentalHealthLoss, -1000f, 1000f, false)
            ];
        }

        private void DrawStatsSection(string title, ref bool expanded, List<EditableStatRow> rows)
        {
            using (new GUILayout.VerticalScope("box"))
            {
                if (GUILayout.Button($"{(expanded ? "[-]" : "[+]")} {title}", GUILayout.Height(24)))
                    expanded = !expanded;

                if (expanded)
                {
                    foreach (var row in rows)
                        DrawEditableStatRow(row);
                }
            }
        }

        private void DrawEditableStatRow(EditableStatRow row)
        {
            float current = PlayerStatsAccess.ClampRange(row.GetValue.Invoke(), row.Min, row.Max);
            string controlName = $"stats.input.{row.Key}";
            bool rowFrozen = StatFreezer.GetFrozenEnabled(row.Key);

            if (!_statsInputBuffers.TryGetValue(row.Key, out var buffer))
                buffer = FormatStatInput(current);

            if (GUI.GetNameOfFocusedControl() != controlName)
                buffer = FormatStatInput(current);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(row.Label, GUILayout.Width(166f));

                bool rowFrozenNext = GUILayout.Toggle(rowFrozen, "Freeze", GUILayout.Width(62f));
                if (rowFrozenNext != rowFrozen)
                {
                    StatFreezer.SetFrozenEnabled(row.Key, rowFrozenNext);
                    rowFrozen = rowFrozenNext;
                    if (!rowFrozenNext)
                        StatFreezer.FreezeAllEnabled = false;
                }

                float sliderValue = GUILayout.HorizontalSlider(current, row.Min, row.Max, GUILayout.MinWidth(220f));
                if (!Mathf.Approximately(sliderValue, current))
                {
                    float clampedSlider = PlayerStatsAccess.ClampRange(sliderValue, row.Min, row.Max);
                    row.SetValue.Invoke(clampedSlider);
                    current = clampedSlider;
                    buffer = FormatStatInput(clampedSlider);
                    if (rowFrozen)
                        StatFreezer.SetFrozenTarget(row.Key, clampedSlider);
                }

                GUI.SetNextControlName(controlName);
                string nextBuffer = GUILayout.TextField(buffer, GUILayout.Width(78f));
                if (!string.Equals(nextBuffer, buffer, StringComparison.Ordinal))
                {
                    buffer = nextBuffer;
                    if (TryParseFloat(nextBuffer, out float typedValue))
                    {
                        float clampedTyped = PlayerStatsAccess.ClampRange(typedValue, row.Min, row.Max);
                        row.SetValue.Invoke(clampedTyped);
                        current = clampedTyped;
                        buffer = FormatStatInput(clampedTyped);
                        if (rowFrozen)
                            StatFreezer.SetFrozenTarget(row.Key, clampedTyped);
                    }
                }

                GUILayout.Label(FormatStatDisplay(current, row.Min, row.Max, row.IsPercentLike), GUILayout.Width(110f));
                GUILayout.Label(rowFrozen ? "FROZEN" : string.Empty, GUILayout.Width(56f));
            }

            _statsInputBuffers[row.Key] = buffer;
        }

        private List<string> BuildVisibleStatsRowKeys(
            List<EditableStatRow> vitalsRows,
            List<EditableStatRow> needsRows,
            List<EditableStatRow> mentalRows,
            List<EditableStatRow> addictionsRows,
            List<EditableStatRow> advancedRows)
        {
            List<string> keys = new(32);

            AddSectionKeys(keys, _statsShowVitals, vitalsRows);
            AddSectionKeys(keys, _statsShowNeeds, needsRows);
            AddSectionKeys(keys, _statsShowMental, mentalRows);
            AddSectionKeys(keys, _statsShowAddictions, addictionsRows);
            AddSectionKeys(keys, _statsShowAdvanced, advancedRows);

            return keys;
        }

        private static void AddSectionKeys(List<string> keys, bool sectionVisible, List<EditableStatRow> rows)
        {
            if (!sectionVisible || rows == null)
                return;

            for (int i = 0; i < rows.Count; i++)
                keys.Add(rows[i].Key);
        }

        private static string FormatStatInput(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string FormatStatDisplay(float value, float min, float max, bool isPercentLike)
        {
            if (!isPercentLike || min != 0f || max != 100f)
                return value.ToString("0.##", CultureInfo.InvariantCulture);

            float clamped = Mathf.Clamp(value, 0f, 100f);
            return $"{clamped:0.##}/100 ({Mathf.RoundToInt(clamped)}%)";
        }

        private static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                || float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private void DrawStatBar(string label, float value)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(110));
                GUILayout.HorizontalSlider(value, 0f, 1f, GUILayout.MinWidth(220));
                GUILayout.Label($"{Mathf.RoundToInt(value * 100f)}%", GUILayout.Width(45));
            }
        }

        private static void DrawInfoRow(string label, string value)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(110));
                GUILayout.Label(value);
            }
        }

        private void DrawFilterHeaderRow()
        {
            using (new GUILayout.HorizontalScope("box"))
            {
                using (new GUILayout.VerticalScope(GUILayout.Width(180)))
                {
                    FilterBox();
                }

                GUILayout.FlexibleSpace();

                var totalItems = Filtereditems?.Count() ?? 0;
                var totalPages = Mathf.Max(1, Mathf.CeilToInt(totalItems / 10f));
                var selectedCategoryCount = SelectedCategories.Count();

                GUILayout.Label($"Page 1/{totalPages} | Items: {totalItems} | Categories: {selectedCategoryCount}", richLabelStyle);
            }
        }

        private void DrawSpawnerBody3Column()
        {
            using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                using (new GUILayout.VerticalScope("box", GUILayout.Width(LeftW), GUILayout.ExpandHeight(true)))
                {
                    GUILayout.Label("Categories", sectionHeaderStyle);
                    Categories();
                }

                using (new GUILayout.VerticalScope("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    GUILayout.Label("Items", sectionHeaderStyle);
                    Items();
                }

                using (new GUILayout.VerticalScope("box", GUILayout.Width(RightW), GUILayout.ExpandHeight(true)))
                {
                    DrawSelectedItemPanel();
                }
            }
        }

        private void DrawSelectedItemPanel()
        {
            GUILayout.Label("Selected Item", sectionHeaderStyle);

            var displayItem = _showAmountPicker ? _amountPickerItem : (HoverItem.Value ?? selectedItem);
            var title = displayItem?.Title ?? "None";
            GUILayout.Label(title, sectionHeaderStyle);

            var categories = displayItem?.Categories?.Any() == true
                ? string.Join(", ", displayItem.Categories)
                : "None";
            GUILayout.Label($"Categories: {categories}");

            var idText = displayItem != null ? displayItem.ID.ToString() : "N/A";
            GUILayout.Label($"ID: {idText}");

            using (new GUILayout.VerticalScope("box"))
            {
                GUILayout.Label("Description", sectionHeaderStyle);
                GUILayout.Label("No description available.");
            }

            GUILayout.FlexibleSpace();

            if (_showAmountPicker)
            {
                using (new GUILayout.VerticalScope("box"))
                {
                    GUILayout.Label("Spawn Amount", sectionHeaderStyle);
                    GUILayout.Label("Up to MaxStack with slider");
                    GUILayout.Label("Type more to spawn multiple stacks");

                    int sliderValue = Mathf.RoundToInt(
                        GUILayout.HorizontalSlider(_amountPickerSliderValue, 1f, _amountPickerMaxStack)
                    );
                    if (sliderValue != Mathf.RoundToInt(_amountPickerSliderValue))
                    {
                        _amountPickerSliderValue = sliderValue;
                        _amountPickerValue = sliderValue;
                        _amountPickerText = sliderValue.ToString();
                    }
                    else
                    {
                        _amountPickerSliderValue = sliderValue;
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Amount", GUILayout.Width(56));
                        var nextText = GUILayout.TextField(_amountPickerText, GUILayout.Width(72));
                        if (!string.Equals(nextText, _amountPickerText, StringComparison.Ordinal))
                        {
                            _amountPickerText = nextText;
                            if (int.TryParse(nextText, out int typed))
                            {
                                _amountPickerValue = Mathf.Max(1, typed);
                                _amountPickerSliderValue = Mathf.Clamp(_amountPickerValue, 1, _amountPickerMaxStack);
                            }
                        }
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Spawn Amount"))
                            SpawnAmountFromPicker();
                        if (GUILayout.Button("Cancel"))
                            CloseAmountPicker();
                    }
                }
            }
        }

        void Footer()
        {
            string text = string.Empty;
            if (HoverItem.Value != null)
            {
                text += "LMB: Add one";
                text += ", RMB: Open amount picker";
            }

            //GUILayout.FlexibleSpace();
            GUILayout.Space(170);
            GUILayout.Label(text);
            //GUILayout.FlexibleSpace();
        }

        void FilterBox()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUI.SetNextControlName("Filter");
                string text = GUILayout.TextField(TextFilter.Value, GUILayout.Width(124));
                if (!string.IsNullOrEmpty(text))
                {
                    if (GUILayout.Button("X", GUILayout.Width(23)))
                    {
                        text = string.Empty;
                        Release();
                    }
                }
                if (TextFilter.Value != text)
                    TextFilter.OnNext(text);
            }

            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.KeyDown)
                Release();
        }

        void Categories()
        {
            using (var scope = new GUILayout.ScrollViewScope(categoryScrollPos, GUILayout.ExpandHeight(true)))
            {
                categoryScrollPos = scope.scrollPosition;

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Categories");
                    if (SelectedCategories.Any() && GUILayout.Button("X", GUILayout.Width(23)))
                        SelectedCategories.Clear();
                    GUILayout.FlexibleSpace();
                }

                foreach (var category in validCategories)
                {
                    bool selected = SelectedCategories.Contains(category);

                    GUI.color = selected ? Color.green : (SelectedCategories.Any() ? Color.gray : Color.white);
                    if (GUILayout.Button(category, buttonStyle))
                    {
                        if (selected) SelectedCategories.Remove(category);
                        else SelectedCategories.Add(category);
                    }
                    GUI.color = Color.white;
                }
            }
        }

        void Items()
        {
            using (new GUILayout.HorizontalScope())
            {
                if (HoverItem.Value != null)
                {
                    GUI.color = Color.white;

                    string formattedId = HoverItem?.Value.ID.ToString();
                    if (int.TryParse(TextFilter.Value, out int id) && id == HoverItem?.Value.ID)
                        formattedId = $"<color=cyan>{id}</color>";
                    GUILayout.Label($"{formattedId}", GUILayout.MinWidth(50));

                    var formattedTitle = Helpers.ColorizedMatch(TextFilter.Value, HoverItem.Value.Title, RichtextColor.cyan);
                    GUILayout.Label($"{formattedTitle}", richLabelStyle);

                    GUILayout.FlexibleSpace();
                    GUI.color = Color.gray;

                    var categories = HoverItem?.Value?.Categories
                        .Select(cat => SelectedCategories.Contains(cat) ? $"<color=cyan>{cat}</color>" : cat);

                    GUILayout.Label(string.Join(", ", categories ?? Enumerable.Empty<string>()));

                    GUI.color = Color.white;
                }
                else
                {
                    GUILayout.Label(string.Empty);
                }
            }

            float computedCenterWidth = Mathf.Max(1f, windowRect.width - LeftW - RightW - OuterPad);
            int computedCols = Mathf.Clamp(
                Mathf.FloorToInt((computedCenterWidth + Pad) / (Tile + Pad)),
                1,
                4
            );

            if (Event.current.type == EventType.Repaint)
            {
                _centerWidthCached = computedCenterWidth;
                _gridColsCached = computedCols;
            }

            float centerWidth = _centerWidthCached > 0f ? _centerWidthCached : computedCenterWidth;
            int columns = Mathf.Clamp(_gridColsCached, 1, 4);
            float availableWidth = Mathf.Max(1f, columns * Tile + (columns - 1) * Pad);

            if (DEBUG_LAYOUT && !_layoutNumbersLogged && Event.current.type == EventType.Repaint)
            {
                float requiredCenterFor4 = 4f * Tile + 3f * Pad;
                float requiredWindowW = LeftW + RightW + OuterPad + requiredCenterFor4;
                Plugin.DLog($"Items grid required width: RequiredCenterFor4={requiredCenterFor4:F1}, RequiredWindowW={requiredWindowW:F1}");
                _layoutNumbersLogged = true;
            }

            if (DEBUG_LAYOUT && Time.time >= _nextItemsGridLogTime)
            {
                const bool horizontalEnabled = false;
                Plugin.DLog(
                    $"Items grid metrics: event={Event.current.type} windowRect.w={windowRect.width:F1} " +
                    $"leftW={LeftW:F1} rightW={RightW:F1} centerW={centerWidth:F1} tile={Tile:F1} pad={Pad:F1} " +
                    $"cachedCols={columns} computedCols={computedCols} " +
                    $"scrollH={horizontalEnabled}");
                _nextItemsGridLogTime = Time.time + 1f;
            }

            using (var sv = new GUILayout.ScrollViewScope(itemScrollPos, false, true, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                itemScrollPos = sv.scrollPosition;
                var items = Filtereditems == null
                    ? new List<Item>()
                    : (Filtereditems as IList<Item> ?? Filtereditems.ToList());

                using (new GUILayout.VerticalScope(GUILayout.Width(availableWidth)))
                {
                    if (items.Count == 0)
                    {
                        GUILayout.Label("No items");
                    }
                    else
                    {
                        int rows = Mathf.CeilToInt(items.Count / (float)columns);
                        for (int row = 0; row < rows; row++)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                for (int col = 0; col < columns; col++)
                                {
                                    int idx = row * columns + col;
                                    if (idx < items.Count)
                                    {
                                        var item = items[idx];
                                        var rect = GUILayoutUtility.GetRect(Tile, Tile, GUI.skin.button);
                                        GUI.Box(rect, GUIContent.none);
                                        if (item.Appearance?.Sprite != null)
                                        {
                                            Components.DrawSpriteInRect(item.Appearance.Sprite, rect);
                                        }
                                        else
                                        {
                                            GUI.DrawTexture(rect, Texture2D.redTexture, ScaleMode.ScaleToFit, true);
                                        }

                                        bool clickedThisTile = Event.current.type == EventType.MouseDown
                                            && rect.Contains(Event.current.mousePosition);
                                        if (clickedThisTile)
                                        {
                                            selectedItem = item;
                                            HoverItem.OnNext(item);
                                            framesSinceHover = 0;

                                            if (Event.current.button == 1)
                                            {
                                                OpenAmountPicker(item);
                                                Event.current.Use();
                                            }
                                            else if (Event.current.button == 0)
                                            {
                                                RequestSpawnOne(item);
                                                Event.current.Use();
                                            }
                                        }

                                        if (windowHover && Event.current.type == EventType.Repaint)
                                        {
                                            if (rect.Contains(Event.current.mousePosition))
                                            {
                                                HoverItem.OnNext(item);
                                                framesSinceHover = 0;
                                            }
                                        }

                                        if (col < columns - 1)
                                            GUILayout.Space(Pad);
                                    }
                                    else
                                    {
                                        // Keep layout control count stable for incomplete final rows.
                                        GUILayout.Space(Tile + Pad);
                                        if (col < columns - 1)
                                            GUILayout.Space(Pad);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}




