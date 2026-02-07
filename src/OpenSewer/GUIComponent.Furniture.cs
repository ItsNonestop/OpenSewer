using OpenSewer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenSewer
{
    internal partial class GUIComponent
    {
        private const string FurnitureAllKey = "All";
        string _furnitureFilterText = string.Empty;
        int _furnitureShowMode = 0; // 0=All 1=Have 2=Missing
        Vector2 _furnitureCategoryScroll;
        Vector2 _furnitureItemsScroll;
        readonly HashSet<string> _furnitureCollapsedCategories = [];
        string _furnitureActiveCategoryKey = string.Empty;
        Furniture _selectedFurniture;
        Furniture.Skin _selectedFurnitureSkin;
        GUIStyle _furnitureSelectionGridStyle;

        private void InitFurnitureStyles()
        {
            _furnitureSelectionGridStyle ??= new GUIStyle(GUI.skin.button)
            {
                onNormal = { textColor = Color.cyan },
                onHover = { textColor = Color.cyan }
            };
        }

        private void DrawFurnitureHeaderRow()
        {
            InitFurnitureStyles();

            bool hasList = TryBuildFurnitureGroups(out var categoryKeys, out _, out int totalItems);

            using (new GUILayout.HorizontalScope("box"))
            {
                using (new GUILayout.VerticalScope(GUILayout.Width(220)))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUI.SetNextControlName("FurnitureFilter");
                        string nextText = GUILayout.TextField(_furnitureFilterText, GUILayout.Width(160));
                        if (!string.IsNullOrEmpty(nextText) && GUILayout.Button("X", GUILayout.Width(23)))
                        {
                            nextText = string.Empty;
                            ReleaseFurnitureFilter();
                        }

                        if (!string.Equals(nextText, _furnitureFilterText, StringComparison.Ordinal))
                            _furnitureFilterText = nextText;
                    }
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label("Show:");
                _furnitureShowMode = GUILayout.SelectionGrid(
                    selected: _furnitureShowMode,
                    texts: ["All", "Have", "Missing"],
                    xCount: 3,
                    style: _furnitureSelectionGridStyle,
                    options: GUILayout.Width(230)
                );
                GUILayout.FlexibleSpace();

                int categories = hasList ? categoryKeys.Count : 0;
                GUILayout.Label($"Furniture: {totalItems} | Categories: {categories}");
            }

            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.KeyDown)
                ReleaseFurnitureFilter();
        }

        private void DrawFurnitureBody3Column()
        {
            bool hasList = TryBuildFurnitureGroups(out var categoryKeys, out var grouped, out _);
            bool ready = FurnitureOperations.IsReady();

            using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                using (new GUILayout.VerticalScope("box", GUILayout.Width(LeftW), GUILayout.ExpandHeight(true)))
                {
                    GUILayout.Label("Categories", sectionHeaderStyle);

                    if (hasList && categoryKeys.Count > 0)
                        DrawFurnitureCategories(categoryKeys);
                    else
                        GUILayout.Label("No categories");
                }

                using (new GUILayout.VerticalScope("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    GUILayout.Label("Furniture", sectionHeaderStyle);

                    if (!ready)
                    {
                        GUILayout.Label("Furniture system not ready yet (load a save first).");
                    }
                    else
                    {
                        DrawFurnitureTiles(categoryKeys, grouped);
                    }
                }

                using (new GUILayout.VerticalScope("box", GUILayout.Width(RightW), GUILayout.ExpandHeight(true)))
                {
                    DrawSelectedFurniturePanel(ready);
                }
            }
        }

        private void DrawFurnitureCategories(List<string> categoryKeys)
        {
            using (var scope = new GUILayout.ScrollViewScope(_furnitureCategoryScroll, GUILayout.ExpandHeight(true)))
            {
                _furnitureCategoryScroll = scope.scrollPosition;

                foreach (var categoryKey in categoryKeys)
                {
                    bool selected = string.Equals(_furnitureActiveCategoryKey, categoryKey, StringComparison.Ordinal);
                    bool collapsed = _furnitureCollapsedCategories.Contains(categoryKey);

                    using (new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(collapsed ? "+" : "-", GUILayout.Width(24)))
                        {
                            if (collapsed)
                                _furnitureCollapsedCategories.Remove(categoryKey);
                            else
                                _furnitureCollapsedCategories.Add(categoryKey);
                        }

                        GUI.color = selected ? Color.green : Color.white;
                        if (GUILayout.Button(categoryKey, buttonStyle))
                            _furnitureActiveCategoryKey = categoryKey;
                        GUI.color = Color.white;
                    }
                }
            }
        }

        private void DrawFurnitureTiles(
            List<string> categoryKeys,
            Dictionary<string, List<(Furniture furniture, Furniture.Skin skin, string title, Sprite image, int ownedCount)>> grouped)
        {
            if (categoryKeys.Count == 0)
            {
                GUILayout.Label("No furniture entries");
                return;
            }

            if (string.IsNullOrEmpty(_furnitureActiveCategoryKey))
                _furnitureActiveCategoryKey = FurnitureAllKey;

            if (!string.Equals(_furnitureActiveCategoryKey, FurnitureAllKey, StringComparison.Ordinal)
                && !grouped.ContainsKey(_furnitureActiveCategoryKey))
            {
                _furnitureActiveCategoryKey = FurnitureAllKey;
            }

            bool isCollapsed = _furnitureCollapsedCategories.Contains(_furnitureActiveCategoryKey);
            if (isCollapsed)
            {
                GUILayout.Label("Selected category is collapsed. Expand it on the left.");
                return;
            }

            List<(Furniture furniture, Furniture.Skin skin, string title, Sprite image, int ownedCount)> cells;
            if (string.Equals(_furnitureActiveCategoryKey, FurnitureAllKey, StringComparison.Ordinal))
            {
                cells = [];
                foreach (var categoryKey in categoryKeys)
                {
                    if (string.Equals(categoryKey, FurnitureAllKey, StringComparison.Ordinal))
                        continue;

                    if (grouped.TryGetValue(categoryKey, out var list))
                        cells.AddRange(list);
                }
            }
            else
            {
                cells = grouped.TryGetValue(_furnitureActiveCategoryKey, out var activeCells)
                    ? activeCells
                    : [];
            }

            float computedCenterWidth = Mathf.Max(1f, windowRect.width - LeftW - RightW - OuterPad);
            int columns = Mathf.Clamp(Mathf.FloorToInt((computedCenterWidth + Pad) / (Tile + Pad)), 1, 4);
            float availableWidth = Mathf.Max(1f, columns * Tile + (columns - 1) * Pad);

            using (var sv = new GUILayout.ScrollViewScope(_furnitureItemsScroll, false, true, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                _furnitureItemsScroll = sv.scrollPosition;

                using (new GUILayout.VerticalScope(GUILayout.Width(availableWidth)))
                {
                    if (cells.Count == 0)
                    {
                        GUILayout.Label("No furniture in this category");
                    }
                    else
                    {
                        int rows = Mathf.CeilToInt(cells.Count / (float)columns);
                        for (int row = 0; row < rows; row++)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                for (int col = 0; col < columns; col++)
                                {
                                    int idx = row * columns + col;
                                    if (idx < cells.Count)
                                    {
                                        var cell = cells[idx];
                                        var rect = GUILayoutUtility.GetRect(Tile, Tile, GUI.skin.button);
                                        GUI.Box(rect, GUIContent.none);
                                        if (cell.image != null)
                                            Components.DrawSpriteInRect(cell.image, rect);
                                        else
                                            GUI.DrawTexture(rect, Texture2D.redTexture, ScaleMode.ScaleToFit, true);

                                        bool clickedThisTile = Event.current.type == EventType.MouseDown
                                            && rect.Contains(Event.current.mousePosition);
                                        if (clickedThisTile)
                                        {
                                            _selectedFurniture = cell.furniture;
                                            _selectedFurnitureSkin = cell.skin;

                                            Event.current.Use();
                                        }

                                        if (cell.ownedCount > 0)
                                            GUI.Label(rect, cell.ownedCount.ToString());

                                        if (col < columns - 1)
                                            GUILayout.Space(Pad);
                                    }
                                    else
                                    {
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

        private void DrawSelectedFurniturePanel(bool ready)
        {
            GUILayout.Label("Selected Furniture", sectionHeaderStyle);

            string title = _selectedFurniture == null
                ? "None"
                : (!string.IsNullOrWhiteSpace(_selectedFurniture.title) ? _selectedFurniture.title : _selectedFurniture.name);
            GUILayout.Label(title, sectionHeaderStyle);

            string category = _selectedFurniture == null ? "N/A" : _selectedFurniture.category.ToString();
            GUILayout.Label($"Category: {category}");

            string skinText = _selectedFurnitureSkin == null
                ? "Base"
                : (string.IsNullOrWhiteSpace(_selectedFurnitureSkin.id) ? "Skin" : _selectedFurnitureSkin.id);
            GUILayout.Label($"Skin: {skinText}");

            int ownedCount = FurnitureOperations.GetOwnedCount(_selectedFurniture, _selectedFurnitureSkin);
            GUILayout.Label($"Owned: {ownedCount}");

            GUILayout.FlexibleSpace();

            bool canSpawn = ready && _selectedFurniture != null;
            GUI.enabled = canSpawn;
            if (GUILayout.Button("Spawn"))
                FurnitureOperations.Spawn(_selectedFurniture, _selectedFurnitureSkin);
            GUI.enabled = true;
        }

        private bool TryBuildFurnitureGroups(
            out List<string> categoryKeys,
            out Dictionary<string, List<(Furniture furniture, Furniture.Skin skin, string title, Sprite image, int ownedCount)>> grouped,
            out int totalItems)
        {
            categoryKeys = [];
            grouped = new(StringComparer.Ordinal);
            totalItems = 0;

            if (!FurnitureOperations.TryGetFurnitureList(out var furnitures) || furnitures == null)
            {
                _furnitureActiveCategoryKey = string.Empty;
                return false;
            }

            string filter = (_furnitureFilterText ?? string.Empty).Trim();
            bool hasFilter = filter.Length > 0;

            IEnumerable<(Furniture furniture, Furniture.Skin skin, string title, Sprite image, int ownedCount)> cells =
                FurnitureOperations.ExpandToCells(furnitures)
                    .Where(x => x.furniture != null)
                    .Where(x => x.furniture.category != Furniture.Category.None)
                    .Where(x => !hasFilter
                                || (!string.IsNullOrWhiteSpace(x.title) && x.title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                                || (!string.IsNullOrWhiteSpace(x.furniture.name) && x.furniture.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
                    .Where(x =>
                    {
                        return _furnitureShowMode switch
                        {
                            1 => x.ownedCount > 0,
                            2 => x.ownedCount == 0,
                            _ => true
                        };
                    });

            foreach (var cell in cells)
            {
                string key = cell.furniture.category.ToString();
                if (!grouped.TryGetValue(key, out var list))
                {
                    list = [];
                    grouped[key] = list;
                }

                list.Add(cell);
                totalItems++;
            }

            categoryKeys = grouped.Keys
                .Where(x => !string.Equals(x, FurnitureAllKey, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            categoryKeys.Insert(0, FurnitureAllKey);

            foreach (var key in categoryKeys)
            {
                if (grouped.TryGetValue(key, out var list))
                    grouped[key] = list.OrderBy(x => x.title, StringComparer.OrdinalIgnoreCase).ToList();
            }

            if (string.IsNullOrEmpty(_furnitureActiveCategoryKey))
            {
                _furnitureActiveCategoryKey = FurnitureAllKey;
            }
            else if (!string.Equals(_furnitureActiveCategoryKey, FurnitureAllKey, StringComparison.Ordinal)
                     && !grouped.ContainsKey(_furnitureActiveCategoryKey))
            {
                _furnitureActiveCategoryKey = FurnitureAllKey;
            }

            return true;
        }

        private void ReleaseFurnitureFilter()
        {
            if (GUI.GetNameOfFocusedControl() == "FurnitureFilter")
            {
                GUI.FocusControl(null);
                GUI.FocusWindow(windowId);
            }
        }
    }
}
