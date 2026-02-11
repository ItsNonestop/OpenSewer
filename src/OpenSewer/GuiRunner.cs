using OpenSewer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UIButton = UnityEngine.UI.Button;
using UIImage = UnityEngine.UI.Image;

namespace OpenSewer
{
    internal class GuiRunner : MonoBehaviour
    {
        private enum MenuTab
        {
            Main,
            Items,
            Furniture,
            Stats,
            Time,
            Debug
        }

        private sealed class TabRef
        {
            public MenuTab Tab;
            public UIImage Image;
            public RectTransform ButtonRect;
            public TMP_Text Text;
        }

        private sealed class ItemCellRef
        {
            public UIButton Button;
            public UIImage Background;
            public UIImage Icon;
            public TMP_Text Label;
            public Item BoundItem;
        }

        private sealed class CategoryCellRef
        {
            public UIButton Button;
            public TMP_Text Label;
            public string Category;
        }

#pragma warning disable CS0649 // Populated by Unity JsonUtility
        [Serializable]
        private sealed class ItemJsonRoot
        {
            public ItemJsonEntry[] Items;
        }

        [Serializable]
        private sealed class ItemJsonEntry
        {
            public int ID;
            public string Title;
            public string[] Categories;
            public string Description;
            public int BaseValue;
        }
#pragma warning restore CS0649

        private enum ItemDetailsSourceMode
        {
            RuntimeOnly,
            HybridRuntimeJson
        }

        private sealed class ItemDetails
        {
            public string Name;
            public string Category;
            public string Description;
            public int? EstimatedValue;
            public int QuantityBadge;
            public int StackSize;
        }

        private readonly Dictionary<string, Sprite> _spriteCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<TabRef> _tabs = new();
        private readonly Dictionary<MenuTab, GameObject> _pages = new();
        private readonly List<ItemCellRef> _itemCells = new();
        private readonly HashSet<string> _excludedItemCategories = new(StringComparer.OrdinalIgnoreCase) { "Liquid", "ItemGroup" };
        private readonly List<Item> _filteredItems = new();
        private readonly List<string> _itemCategoryOptions = new();
        private readonly List<string> _filteredCategoryOptions = new();
        private readonly List<CategoryCellRef> _categoryCells = new();

        private GameObject _root;
        private MenuTab _activeTab = MenuTab.Main;
        private TMP_FontAsset _menuFont;
        private TMP_Text _statusText;
        private TMP_InputField _itemSearchInput;
        private TMP_InputField _itemAmountInput;
        private TMP_InputField _itemCategoryInput;
        private TMP_Text _itemListInfoText;
        private UIImage _itemDetailsIcon;
        private TMP_Text _itemDetailsQuantityBadgeText;
        private GameObject _itemDetailsQuantityBadgeRoot;
        private TMP_Text _itemDetailsNameText;
        private TMP_Text _itemDetailsCategoryText;
        private TMP_Text _itemDetailsDescriptionText;
        private TMP_Text _itemDetailsStackSizeText;
        private TMP_Text _itemDetailsValueText;
        private GameObject _itemDetailsContentRoot;
        private RectTransform _itemGridHoverRegion;
        private RectTransform _categoryListHoverRegion;
        private GameObject _categoryBrowserPanel;
        private Item _selectedItem;
        private int _itemScrollStartIndex;
        private int _categoryScrollStartIndex;
        private bool _categoryBrowserVisible;
        private bool _visible;
        private bool _itemDetailsSourceInitialized;
        private ItemDetailsSourceMode _itemDetailsSourceMode = ItemDetailsSourceMode.RuntimeOnly;
        private Dictionary<int, ItemJsonEntry> _itemJsonLookup;

        public void Show()
        {
            BuildIfNeeded();
            _visible = true;
            _root.SetActive(true);
            enabled = true;
        }

        public void Hide()
        {
            _visible = false;
            if (_root != null)
                _root.SetActive(false);
            enabled = false;
        }

        private void OnEnable()
        {
            if (_visible && _root != null)
                _root.SetActive(true);
        }

        private void OnDisable()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                bool ok = UiReverseEngineer.TryDumpFullUiHierarchy(out string path);
                SetStatus(ok ? $"F8 dump: {path}" : "F8 dump failed (check BepInEx log).");
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                bool ok = UiReverseEngineer.TryDumpHoveredUi(out string path);
                SetStatus(ok ? $"F9 dump: {path}" : "F9 dump failed (check BepInEx log).");
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                bool ok = UiReverseEngineer.TryDumpHoveredBranchAsCode(8, out string path);
                SetStatus(ok ? $"F10 code dump: {path}" : "F10 code dump failed (check BepInEx log).");
            }

            HandleItemsScrollInput();
            HandleCategoryScrollInput();
        }

        private void BuildIfNeeded()
        {
            if (_root != null)
                return;

            EnsureEventSystem();
            _menuFont = FindTmpFont("Erika Ormig SDF Menu");

            _root = NewUI("OpenSewerCanvas", transform);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6000;
            _root.AddComponent<GraphicRaycaster>();
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;
            Stretch(RT(_root));

            var overlay = Panel(_root.transform, "Overlay", new Color(0f, 0f, 0f, 0.208f));
            Stretch(overlay);

            RectTransform main = NewPanelWithSprite(
                _root.transform,
                "MainPanel",
                "main_UI_background_001",
                new Color(1f, 1f, 1f, 1f),
                Image.Type.Sliced);
            main.anchorMin = new Vector2(0.5f, 0f);
            main.anchorMax = new Vector2(0.5f, 1f);
            main.pivot = new Vector2(0.5f, 0.5f);
            main.anchoredPosition = Vector2.zero;
            main.sizeDelta = new Vector2(1100f, 0f);

            var content = NewUI("MainPanelContent", main);
            var contentRt = RT(content);
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.pivot = new Vector2(0.5f, 0.5f);
            contentRt.sizeDelta = new Vector2(1090f, 900f);

            var topButtons = NewUI("MainTopButtons", content.transform);
            var topButtonsRt = RT(topButtons);
            topButtonsRt.anchorMin = new Vector2(0f, 1f);
            topButtonsRt.anchorMax = new Vector2(1f, 1f);
            topButtonsRt.pivot = new Vector2(0f, 0f);
            topButtonsRt.anchoredPosition = Vector2.zero;
            topButtonsRt.sizeDelta = new Vector2(0f, 45f);

            var topButtonsRow = NewUI("TopButtons", topButtons.transform);
            Stretch(RT(topButtonsRow));
            var h = topButtonsRow.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 0f;
            h.childControlWidth = false;
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.UpperLeft;

            CreateTopTab(topButtonsRow.transform, MenuTab.Main, "Main");
            CreateTopTab(topButtonsRow.transform, MenuTab.Items, "Items");
            CreateTopTab(topButtonsRow.transform, MenuTab.Furniture, "Furniture");
            CreateTopTab(topButtonsRow.transform, MenuTab.Stats, "Stats");
            CreateTopTab(topButtonsRow.transform, MenuTab.Time, "Time");
            CreateTopTab(topButtonsRow.transform, MenuTab.Debug, "Debug");

            var bg = Panel(content.transform, "MainPanelsBackground", new Color(0.16f, 0.16f, 0.16f, 1f));
            Stretch(bg);
            SetSprite(bg.GetComponent<UIImage>(), "main_UI_shadow_001", Image.Type.Sliced, Color.white);

            var shadowTop = NewPanelWithSprite(bg, "ShadowTop", "main_UI_shadow_001", Color.white, Image.Type.Sliced);
            shadowTop.anchorMin = new Vector2(0f, 1f);
            shadowTop.anchorMax = new Vector2(1f, 1f);
            shadowTop.pivot = new Vector2(0.5f, 0f);
            shadowTop.anchoredPosition = Vector2.zero;
            shadowTop.sizeDelta = new Vector2(0f, 50f);

            var shadowBottom = NewPanelWithSprite(bg, "ShadowBottom", "main_UI_shadow_001", Color.white, Image.Type.Sliced);
            shadowBottom.anchorMin = new Vector2(0f, 0f);
            shadowBottom.anchorMax = new Vector2(1f, 0f);
            shadowBottom.pivot = new Vector2(0.5f, 0f);
            shadowBottom.anchoredPosition = Vector2.zero;
            shadowBottom.sizeDelta = new Vector2(0f, 50f);

            var pagesRoot = NewUI("MainPanelsPadding", content.transform);
            var pagesRt = RT(pagesRoot);
            Stretch(pagesRt);
            pagesRt.sizeDelta = new Vector2(-30f, -30f);

            _pages[MenuTab.Main] = BuildMainPage(pagesRoot.transform);
            _pages[MenuTab.Items] = BuildItemsPage(pagesRoot.transform);
            _pages[MenuTab.Furniture] = BuildPlaceholderPage(pagesRoot.transform, "Furniture tools will be wired here.");
            _pages[MenuTab.Stats] = BuildPlaceholderPage(pagesRoot.transform, "Player stats controls will be wired here.");
            _pages[MenuTab.Time] = BuildTimePage(pagesRoot.transform);
            _pages[MenuTab.Debug] = BuildDebugPage(pagesRoot.transform);

            // Keep tabs as top-most sibling so background/page graphics cannot intercept clicks.
            topButtonsRt.SetAsLastSibling();

            SwitchTab(MenuTab.Main);
            _root.SetActive(false);
        }

        private GameObject BuildMainPage(Transform parent)
        {
            RectTransform page = NewPanelWithSprite(parent, "MainPage", "main_UI_background_002", Color.white, Image.Type.Sliced);
            Stretch(page);

            RectTransform body = NewPanelWithSprite(page, "Body", "main_UI_background_empty_001", Color.white, Image.Type.Tiled);
            Stretch(body);
            body.offsetMin = new Vector2(18f, 18f);
            body.offsetMax = new Vector2(-18f, -18f);

            TMP_Text title = NewText(body, "OPENSEWER MOD MENU", 24, TextAlignmentOptions.TopLeft, Color.white);
            title.rectTransform.offsetMin = new Vector2(24f, 0f);
            title.rectTransform.offsetMax = new Vector2(0f, -24f);

            TMP_Text info = NewText(
                body,
                "Native-style shell is ready. Use tabs to access tools.\n\nCurrent build focus:\n- Match vanilla visual style\n- Restore mod functionality by section",
                16,
                TextAlignmentOptions.TopLeft,
                new Color(0.9f, 0.9f, 0.9f, 1f));
            info.rectTransform.offsetMin = new Vector2(24f, 0f);
            info.rectTransform.offsetMax = new Vector2(0f, -78f);

            return page.gameObject;
        }

        private GameObject BuildItemsPage(Transform parent)
        {
            var page = NewUI("ItemsPage", parent);
            Stretch(RT(page));
            _itemCells.Clear();
            _categoryCells.Clear();
            _itemCategoryOptions.Clear();
            _filteredCategoryOptions.Clear();
            _selectedItem = null;
            _categoryScrollStartIndex = 0;
            _categoryBrowserVisible = false;

            const int itemGridColumns = 5;
            const int itemGridVisibleRows = 8;
            const int itemGridVisibleCellCount = itemGridColumns * itemGridVisibleRows;

            RectTransform leftPanel = NewPanelWithSprite(page.transform, "InventoryPanel", "main_UI_background_002", Color.white, Image.Type.Sliced);
            leftPanel.anchorMin = new Vector2(0f, 0f);
            leftPanel.anchorMax = new Vector2(0f, 1f);
            leftPanel.pivot = new Vector2(0f, 0.5f);
            leftPanel.anchoredPosition = Vector2.zero;
            leftPanel.sizeDelta = new Vector2(458f, 0f);

            RectTransform playerSlots = NewPanelWithSprite(leftPanel, "PlayerSlots", "main_UI_background_002", Color.white, Image.Type.Sliced);
            playerSlots.anchorMin = new Vector2(0f, 1f);
            playerSlots.anchorMax = new Vector2(1f, 1f);
            playerSlots.pivot = new Vector2(0.5f, 1f);
            playerSlots.anchoredPosition = Vector2.zero;
            playerSlots.sizeDelta = new Vector2(0f, 590f);

            var redEdges = Panel(playerSlots, "RedEdges", new Color(0.443f, 0.071f, 0.094f, 0.35f));
            redEdges.anchorMin = new Vector2(0f, 1f);
            redEdges.anchorMax = new Vector2(1f, 1f);
            redEdges.pivot = new Vector2(0.5f, 1f);
            redEdges.anchoredPosition = Vector2.zero;
            redEdges.sizeDelta = new Vector2(0f, 590f);

            var content = NewPanelWithSprite(playerSlots, "Content", "main_UI_background_002", Color.white, Image.Type.Sliced);
            Stretch(content);
            content.offsetMin = new Vector2(5f, 5f);
            content.offsetMax = new Vector2(-5f, -5f);

            var inventorySlots = NewUI("InventorySlots", content);
            var invSlotsRt = RT(inventorySlots);
            Stretch(invSlotsRt);
            invSlotsRt.anchoredPosition = Vector2.zero;
            invSlotsRt.offsetMax = Vector2.zero;
            inventorySlots.AddComponent<RectMask2D>();
            _itemGridHoverRegion = invSlotsRt;

            var gridRoot = NewUI("Grid", inventorySlots.transform);
            Stretch(RT(gridRoot));
            var grid = gridRoot.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(55f, 55f);
            grid.spacing = new Vector2(14f, 14f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = itemGridColumns;
            grid.childAlignment = TextAnchor.UpperLeft;
            var pad = grid.padding;
            pad.left = 17;
            pad.top = 17;
            pad.bottom = 17;
            grid.padding = pad;

            for (int i = 0; i < itemGridVisibleCellCount; i++)
            {
                GameObject slot = NewUI($"Slot{i}", gridRoot.transform);
                RT(slot).sizeDelta = new Vector2(55f, 55f);
                var bg = slot.AddComponent<UIImage>();
                SetSprite(bg, "slot_001", Image.Type.Sliced, Color.white);
                bg.raycastTarget = true;
                var btn = slot.AddComponent<UIButton>();
                btn.transition = Selectable.Transition.None;

                var icon = NewUI("Icon", slot.transform).AddComponent<UIImage>();
                icon.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                icon.rectTransform.anchoredPosition = Vector2.zero;
                icon.rectTransform.sizeDelta = new Vector2(38f, 38f);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.enabled = false;

                var label = NewText(slot.transform, string.Empty, 10, TextAlignmentOptions.BottomRight, Color.white);
                label.rectTransform.offsetMin = new Vector2(0f, 0f);
                label.rectTransform.offsetMax = new Vector2(-3f, 0f);
                label.gameObject.SetActive(false);

                var cell = new ItemCellRef
                {
                    Button = btn,
                    Background = bg,
                    Icon = icon,
                    Label = label,
                    BoundItem = null
                };
                int idx = i;
                btn.onClick.AddListener(() => OnItemCellClicked(idx));
                _itemCells.Add(cell);
            }

            RectTransform sortButton = NewPanelWithSprite(content, "SortButton", "button_round_001_red", Color.white, Image.Type.Simple);
            sortButton.anchorMin = new Vector2(0f, 1f);
            sortButton.anchorMax = new Vector2(0f, 1f);
            sortButton.pivot = new Vector2(0f, 0f);
            sortButton.anchoredPosition = new Vector2(-17f, -23f);
            sortButton.sizeDelta = new Vector2(40f, 40f);
            var sortBtn = sortButton.gameObject.AddComponent<UIButton>();
            sortBtn.onClick.AddListener(() =>
            {
                _filteredItems.Reverse();
                ApplyItemCells();
                SetStatus("Items list order reversed.");
            });
            NewText(sortButton, "Sort", 12, TextAlignmentOptions.Center, Color.white);

            RectTransform infoPanel = NewPanelWithSprite(leftPanel, "InfoPanel", "main_UI_background_002", Color.white, Image.Type.Sliced);
            infoPanel.anchorMin = new Vector2(0f, 0f);
            infoPanel.anchorMax = new Vector2(1f, 0f);
            infoPanel.pivot = new Vector2(0.5f, 0f);
            infoPanel.anchoredPosition = Vector2.zero;
            infoPanel.sizeDelta = new Vector2(0f, 260f);

            RectTransform infoEmpty = NewPanelWithSprite(infoPanel, "InfoPanelEmpty", "main_UI_background_empty_001", Color.white, Image.Type.Tiled);
            Stretch(infoEmpty);
            infoEmpty.offsetMin = new Vector2(20f, 20f);
            infoEmpty.offsetMax = new Vector2(-20f, -20f);

            RectTransform detailsRoot = NewUI("SelectedItemDetails", infoEmpty).GetComponent<RectTransform>();
            Stretch(detailsRoot);
            detailsRoot.offsetMin = new Vector2(12f, 10f);
            detailsRoot.offsetMax = new Vector2(-12f, -10f);
            _itemDetailsContentRoot = detailsRoot.gameObject;

            RectTransform iconSlot = NewPanelWithSprite(detailsRoot, "SelectedIconSlot", "slot_001", Color.white, Image.Type.Sliced);
            iconSlot.anchorMin = new Vector2(0f, 1f);
            iconSlot.anchorMax = new Vector2(0f, 1f);
            iconSlot.pivot = new Vector2(0f, 1f);
            iconSlot.anchoredPosition = new Vector2(0f, 0f);
            iconSlot.sizeDelta = new Vector2(76f, 76f);

            _itemDetailsIcon = NewUI("SelectedIcon", iconSlot).AddComponent<UIImage>();
            _itemDetailsIcon.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _itemDetailsIcon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _itemDetailsIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _itemDetailsIcon.rectTransform.anchoredPosition = Vector2.zero;
            _itemDetailsIcon.rectTransform.sizeDelta = new Vector2(56f, 56f);
            _itemDetailsIcon.preserveAspect = true;
            _itemDetailsIcon.enabled = false;
            _itemDetailsIcon.raycastTarget = false;

            RectTransform qtyBadge = NewPanelWithSprite(iconSlot, "QuantityBadge", "main_UI_background_002", Color.white, Image.Type.Sliced);
            qtyBadge.anchorMin = new Vector2(1f, 0f);
            qtyBadge.anchorMax = new Vector2(1f, 0f);
            qtyBadge.pivot = new Vector2(1f, 0f);
            qtyBadge.anchoredPosition = new Vector2(6f, -6f);
            qtyBadge.sizeDelta = new Vector2(42f, 20f);
            _itemDetailsQuantityBadgeRoot = qtyBadge.gameObject;
            _itemDetailsQuantityBadgeText = NewText(qtyBadge, string.Empty, 12, TextAlignmentOptions.Center, Color.white);
            _itemDetailsQuantityBadgeText.enableWordWrapping = false;

            RectTransform textRoot = NewUI("SelectedTextRoot", detailsRoot).GetComponent<RectTransform>();
            textRoot.anchorMin = new Vector2(0f, 0f);
            textRoot.anchorMax = new Vector2(1f, 1f);
            textRoot.pivot = new Vector2(0f, 1f);
            textRoot.offsetMin = new Vector2(90f, 0f);
            textRoot.offsetMax = Vector2.zero;

            _itemDetailsNameText = NewText(textRoot, "Select an item", 16, TextAlignmentOptions.TopLeft, Color.white);
            _itemDetailsNameText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _itemDetailsNameText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _itemDetailsNameText.rectTransform.pivot = new Vector2(0f, 1f);
            _itemDetailsNameText.rectTransform.anchoredPosition = Vector2.zero;
            _itemDetailsNameText.rectTransform.sizeDelta = new Vector2(0f, 24f);
            _itemDetailsNameText.enableWordWrapping = false;

            _itemDetailsCategoryText = NewText(textRoot, "Category: -", 12, TextAlignmentOptions.TopLeft, new Color(0.84f, 0.84f, 0.84f, 1f));
            _itemDetailsCategoryText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _itemDetailsCategoryText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _itemDetailsCategoryText.rectTransform.pivot = new Vector2(0f, 1f);
            _itemDetailsCategoryText.rectTransform.anchoredPosition = new Vector2(0f, -24f);
            _itemDetailsCategoryText.rectTransform.sizeDelta = new Vector2(0f, 20f);
            _itemDetailsCategoryText.enableWordWrapping = false;

            _itemDetailsDescriptionText = NewText(textRoot, "Description: -", 12, TextAlignmentOptions.TopLeft, new Color(0.9f, 0.9f, 0.9f, 1f));
            _itemDetailsDescriptionText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _itemDetailsDescriptionText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _itemDetailsDescriptionText.rectTransform.pivot = new Vector2(0f, 1f);
            _itemDetailsDescriptionText.rectTransform.offsetMin = new Vector2(0f, 36f);
            _itemDetailsDescriptionText.rectTransform.offsetMax = new Vector2(0f, -50f);
            _itemDetailsDescriptionText.enableWordWrapping = true;
            _itemDetailsDescriptionText.overflowMode = TextOverflowModes.Truncate;

            _itemDetailsStackSizeText = NewText(textRoot, "Stack size: -", 12, TextAlignmentOptions.BottomLeft, new Color(0.86f, 0.86f, 0.86f, 1f));
            _itemDetailsStackSizeText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _itemDetailsStackSizeText.rectTransform.anchorMax = new Vector2(1f, 0f);
            _itemDetailsStackSizeText.rectTransform.pivot = new Vector2(0f, 0f);
            _itemDetailsStackSizeText.rectTransform.anchoredPosition = new Vector2(0f, 18f);
            _itemDetailsStackSizeText.rectTransform.sizeDelta = new Vector2(0f, 18f);
            _itemDetailsStackSizeText.enableWordWrapping = false;

            _itemDetailsValueText = NewText(textRoot, "Estimated value: -", 12, TextAlignmentOptions.BottomLeft, new Color(0.86f, 0.86f, 0.86f, 1f));
            _itemDetailsValueText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _itemDetailsValueText.rectTransform.anchorMax = new Vector2(1f, 0f);
            _itemDetailsValueText.rectTransform.pivot = new Vector2(0f, 0f);
            _itemDetailsValueText.rectTransform.anchoredPosition = Vector2.zero;
            _itemDetailsValueText.rectTransform.sizeDelta = new Vector2(0f, 18f);
            _itemDetailsValueText.enableWordWrapping = false;

            RectTransform rightPanel = NewPanelWithSprite(page.transform, "RightPanel", "main_UI_background_002", Color.white, Image.Type.Sliced);
            rightPanel.anchorMin = new Vector2(0f, 0f);
            rightPanel.anchorMax = new Vector2(1f, 1f);
            rightPanel.pivot = new Vector2(0.5f, 0.5f);
            rightPanel.offsetMin = new Vector2(468f, 0f);
            rightPanel.offsetMax = new Vector2(0f, 0f);

            RectTransform topInfo = NewPanelWithSprite(rightPanel, "TopInfo", "main_UI_background_002", Color.white, Image.Type.Sliced);
            topInfo.anchorMin = new Vector2(0f, 1f);
            topInfo.anchorMax = new Vector2(1f, 1f);
            topInfo.pivot = new Vector2(0.5f, 1f);
            topInfo.anchoredPosition = Vector2.zero;
            topInfo.sizeDelta = new Vector2(0f, 420f);

            TMP_Text hdr = NewText(topInfo, "ITEMS", 18, TextAlignmentOptions.TopLeft, Color.white);
            hdr.rectTransform.offsetMin = new Vector2(28f, 0f);
            hdr.rectTransform.offsetMax = new Vector2(0f, -26f);

            RectTransform topInfoBody = NewPanelWithSprite(topInfo, "TopInfoBody", "main_UI_background_empty_001", Color.white, Image.Type.Tiled);
            topInfoBody.anchorMin = new Vector2(0f, 0f);
            topInfoBody.anchorMax = new Vector2(1f, 1f);
            topInfoBody.offsetMin = new Vector2(18f, 18f);
            topInfoBody.offsetMax = new Vector2(-18f, -54f);

            var controls = NewUI("Controls", topInfoBody);
            Stretch(RT(controls));
            var v = controls.AddComponent<VerticalLayoutGroup>();
            v.spacing = 8f;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            v.padding = new RectOffset(14, 14, 10, 10);

            _itemSearchInput = BuildInputRow(controls.transform, "Search by ID or name");
            _itemSearchInput.onValueChanged.AddListener(_ => RefreshItemList());
            _itemSearchInput.characterLimit = 48;

            var categoryRow = NewUI("CategoryRow", controls.transform);
            var categoryLayout = categoryRow.AddComponent<HorizontalLayoutGroup>();
            categoryLayout.spacing = 6f;
            categoryLayout.childControlWidth = true;
            categoryLayout.childControlHeight = true;
            categoryLayout.childForceExpandWidth = false;
            categoryLayout.childForceExpandHeight = false;
            categoryRow.AddComponent<LayoutElement>().preferredHeight = 34f;

            _itemCategoryInput = BuildInputRow(categoryRow.transform, "Category filter (optional)");
            _itemCategoryInput.onValueChanged.AddListener(_ =>
            {
                _categoryScrollStartIndex = 0;
                RefreshItemList();
            });
            _itemCategoryInput.characterLimit = 32;
            var categoryInputLe = _itemCategoryInput.gameObject.GetComponent<LayoutElement>();
            categoryInputLe.flexibleWidth = 1f;
            categoryInputLe.preferredWidth = 0f;

            BuildInlineButton(categoryRow.transform, "Browse", ToggleCategoryBrowser, 92f, 34f);
            BuildInlineButton(categoryRow.transform, "Clear", ClearCategoryFilter, 84f, 34f);

            _itemListInfoText = NewText(controls.transform, "Showing: 0", 12, TextAlignmentOptions.Left, new Color(0.8f, 0.8f, 0.8f, 1f));
            PrepareRectForLayout(_itemListInfoText.rectTransform);
            _itemListInfoText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

            _itemAmountInput = BuildInputRow(controls.transform, "Amount");
            _itemAmountInput.text = "1";
            _itemAmountInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            _itemAmountInput.characterLimit = 5;
            _itemAmountInput.onValueChanged.AddListener(_ => UpdateSelectedItemUi());

            var actionRowA = NewUI("ActionRowA", controls.transform);
            var hA = actionRowA.AddComponent<HorizontalLayoutGroup>();
            hA.spacing = 6f;
            hA.childForceExpandWidth = true;
            hA.childForceExpandHeight = false;
            actionRowA.AddComponent<LayoutElement>().preferredHeight = 56f;
            BuildInlineButton(actionRowA.transform, "Spawn One", SpawnSelectedOne, 0f, 56f);
            BuildInlineButton(actionRowA.transform, "Spawn Stack", SpawnSelectedStack, 0f, 56f);

            var actionRowB = NewUI("ActionRowB", controls.transform);
            var hB = actionRowB.AddComponent<HorizontalLayoutGroup>();
            hB.spacing = 6f;
            hB.childForceExpandWidth = true;
            hB.childForceExpandHeight = false;
            actionRowB.AddComponent<LayoutElement>().preferredHeight = 56f;
            BuildInlineButton(actionRowB.transform, "Spawn Amount", SpawnSelectedAmount, 0f, 56f);
            BuildInlineButton(actionRowB.transform, "Refresh", RefreshItemList, 0f, 56f);

            RectTransform bottomInfo = NewPanelWithSprite(rightPanel, "BottomInfo", "main_UI_background_empty_001", Color.white, Image.Type.Tiled);
            bottomInfo.anchorMin = new Vector2(0f, 0f);
            bottomInfo.anchorMax = new Vector2(1f, 1f);
            bottomInfo.offsetMin = new Vector2(18f, 18f);
            bottomInfo.offsetMax = new Vector2(-18f, -432f);

            RectTransform categoryBrowser = NewPanelWithSprite(bottomInfo, "CategoryBrowser", "main_UI_background_002", Color.white, Image.Type.Sliced);
            Stretch(categoryBrowser);
            categoryBrowser.offsetMin = new Vector2(12f, 12f);
            categoryBrowser.offsetMax = new Vector2(-12f, -12f);
            _categoryBrowserPanel = categoryBrowser.gameObject;
            _categoryBrowserPanel.SetActive(false);

            TMP_Text categoryHeader = NewText(categoryBrowser, "CATEGORIES", 14, TextAlignmentOptions.TopLeft, Color.white);
            categoryHeader.rectTransform.offsetMin = new Vector2(12f, 0f);
            categoryHeader.rectTransform.offsetMax = new Vector2(0f, -12f);

            RectTransform categoryBody = NewPanelWithSprite(categoryBrowser, "CategoryBody", "main_UI_background_empty_001", Color.white, Image.Type.Tiled);
            categoryBody.anchorMin = new Vector2(0f, 0f);
            categoryBody.anchorMax = new Vector2(1f, 1f);
            categoryBody.offsetMin = new Vector2(10f, 10f);
            categoryBody.offsetMax = new Vector2(-10f, -36f);

            var categoryViewport = NewUI("CategoryViewport", categoryBody);
            _categoryListHoverRegion = RT(categoryViewport);
            Stretch(_categoryListHoverRegion);
            categoryViewport.AddComponent<RectMask2D>();

            var categoryList = NewUI("CategoryList", categoryViewport.transform);
            Stretch(RT(categoryList));
            var categoryListLayout = categoryList.AddComponent<VerticalLayoutGroup>();
            categoryListLayout.spacing = 4f;
            categoryListLayout.childControlWidth = true;
            categoryListLayout.childControlHeight = true;
            categoryListLayout.childForceExpandWidth = true;
            categoryListLayout.childForceExpandHeight = false;
            categoryListLayout.padding = new RectOffset(4, 4, 4, 4);

            for (int i = 0; i < 9; i++)
            {
                RectTransform rowRt = NewPanelWithSprite(categoryList.transform, $"CategoryCell{i}", "main_UI_background_002", Color.white, Image.Type.Sliced);
                rowRt.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
                UIButton rowBtn = rowRt.gameObject.AddComponent<UIButton>();
                rowBtn.transition = Selectable.Transition.None;

                TMP_Text rowTxt = NewText(rowRt, string.Empty, 13, TextAlignmentOptions.MidlineLeft, Color.white);
                rowTxt.rectTransform.offsetMin = new Vector2(10f, 0f);
                rowTxt.rectTransform.offsetMax = new Vector2(-10f, 0f);

                var cell = new CategoryCellRef
                {
                    Button = rowBtn,
                    Label = rowTxt,
                    Category = null
                };

                int idx = i;
                rowBtn.onClick.AddListener(() => OnCategoryCellClicked(idx));
                _categoryCells.Add(cell);
            }

            var footer = NewUI("Footer", page.transform);
            var footerRt = RT(footer);
            footerRt.anchorMin = new Vector2(0f, 0f);
            footerRt.anchorMax = new Vector2(1f, 0f);
            footerRt.pivot = new Vector2(0.5f, 0f);
            footerRt.anchoredPosition = new Vector2(0f, -2f);
            footerRt.sizeDelta = new Vector2(0f, 32f);

            NewText(footer.transform, "TAB)EXIT", 14, TextAlignmentOptions.Center, new Color(0.92f, 0.92f, 0.92f, 1f));
            _statusText = NewText(footer.transform, "Mod tab shell ready", 12, TextAlignmentOptions.TopRight, new Color(0.8f, 0.8f, 0.8f, 1f));
            _statusText.rectTransform.offsetMin = new Vector2(0f, -16f);

            RefreshItemList();
            RefreshCategoryBrowserList();
            return page;
        }

        private GameObject BuildTimePage(Transform parent)
        {
            RectTransform page = NewPanelWithSprite(parent, "TimePage", "main_UI_background_002", Color.white, Image.Type.Sliced);
            Stretch(page);

            RectTransform body = NewPanelWithSprite(page, "Body", "main_UI_background_empty_001", Color.white, Image.Type.Tiled);
            Stretch(body);
            body.offsetMin = new Vector2(18f, 18f);
            body.offsetMax = new Vector2(-18f, -18f);

            TMP_Text title = NewText(body, "TIME CONTROLS", 22, TextAlignmentOptions.TopLeft, Color.white);
            title.rectTransform.offsetMin = new Vector2(24f, 0f);
            title.rectTransform.offsetMax = new Vector2(0f, -24f);

            RectTransform btnA = BuildActionButton(body.transform, "Toggle Freeze", new Vector2(24f, -72f), () =>
            {
                TimeFreezer.FreezeEnabled = !TimeFreezer.FreezeEnabled;
                SetStatus($"Time freeze: {(TimeFreezer.FreezeEnabled ? "ON" : "OFF")}");
            });

            BuildActionButton(body.transform, "Capture Current Time", new Vector2(24f, -120f), () =>
            {
                TimeFreezer.CaptureCurrent();
                SetStatus("Captured current time target.");
            });

            BuildActionButton(body.transform, "Set 09:00", new Vector2(24f, -168f), () =>
            {
                if (!TimeAccess.IsReady())
                {
                    SetStatus("Time system not ready.");
                    return;
                }

                int day = TimeAccess.GetDay();
                TimeAccess.SetHourMinute(9, 0);
                if (TimeFreezer.FreezeEnabled)
                    TimeFreezer.SetTarget(day, 9, 0);
                SetStatus("Set time to 09:00.");
            });

            _ = btnA;
            return page.gameObject;
        }

        private GameObject BuildDebugPage(Transform parent)
        {
            RectTransform page = NewPanelWithSprite(parent, "DebugPage", "main_UI_background_002", Color.white, Image.Type.Sliced);
            Stretch(page);

            RectTransform body = NewPanelWithSprite(page, "Body", "main_UI_background_empty_001", Color.white, Image.Type.Tiled);
            Stretch(body);
            body.offsetMin = new Vector2(18f, 18f);
            body.offsetMax = new Vector2(-18f, -18f);

            TMP_Text title = NewText(body, "UI REFERENCE TOOLS", 22, TextAlignmentOptions.TopLeft, Color.white);
            title.rectTransform.offsetMin = new Vector2(24f, 0f);
            title.rectTransform.offsetMax = new Vector2(0f, -24f);

            BuildActionButton(body.transform, "Dump Full UI (F8)", new Vector2(24f, -72f), () =>
            {
                bool ok = UiReverseEngineer.TryDumpFullUiHierarchy(out string path);
                SetStatus(ok ? $"F8 dump: {path}" : "F8 dump failed.");
            });

            BuildActionButton(body.transform, "Dump Hovered UI (F9)", new Vector2(24f, -120f), () =>
            {
                bool ok = UiReverseEngineer.TryDumpHoveredUi(out string path);
                SetStatus(ok ? $"F9 dump: {path}" : "F9 dump failed.");
            });

            BuildActionButton(body.transform, "Dump Hovered As Code (F10)", new Vector2(24f, -168f), () =>
            {
                bool ok = UiReverseEngineer.TryDumpHoveredBranchAsCode(8, out string path);
                SetStatus(ok ? $"F10 dump: {path}" : "F10 dump failed.");
            });

            return page.gameObject;
        }

        private TMP_InputField BuildInputRow(Transform parent, string placeholder)
        {
            RectTransform inputRt = NewPanelWithSprite(parent, $"Input_{placeholder}", "main_UI_background_002", Color.white, Image.Type.Sliced);
            inputRt.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            RectTransform viewport = NewUI("Viewport", inputRt).GetComponent<RectTransform>();
            Stretch(viewport);
            viewport.offsetMin = new Vector2(8f, 4f);
            viewport.offsetMax = new Vector2(-8f, -4f);
            viewport.gameObject.AddComponent<RectMask2D>();

            TMP_Text text = NewText(viewport, string.Empty, 13, TextAlignmentOptions.Left, Color.white);
            text.enableWordWrapping = false;

            TMP_Text hint = NewText(viewport, placeholder, 13, TextAlignmentOptions.Left, new Color(0.74f, 0.74f, 0.74f, 0.9f));
            hint.enableWordWrapping = false;

            TMP_InputField input = inputRt.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = hint;
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        private UIButton BuildInlineButton(Transform parent, string label, Action onClick, float fixedWidth = 0f, float preferredHeight = 32f)
        {
            RectTransform buttonRt = NewPanelWithSprite(parent, $"Btn_{label}", "main_UI_background_002", Color.white, Image.Type.Sliced);
            var le = buttonRt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            if (fixedWidth > 0f)
            {
                le.preferredWidth = fixedWidth;
                le.flexibleWidth = 0f;
            }
            else
            {
                le.flexibleWidth = 1f;
            }

            UIButton button = buttonRt.gameObject.AddComponent<UIButton>();
            button.transition = Selectable.Transition.None;
            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            TMP_Text txt = NewText(buttonRt, label, 15, TextAlignmentOptions.Center, Color.white);
            txt.rectTransform.offsetMin = new Vector2(4f, 0f);
            txt.rectTransform.offsetMax = new Vector2(-4f, 0f);
            return button;
        }

        private void RefreshItemList()
        {
            _filteredItems.Clear();
            _itemScrollStartIndex = 0;

            if (ItemDatabase.instance == null)
            {
                SetStatus("Item database not ready.");
                ApplyItemCells();
                return;
            }

            string q = (_itemSearchInput?.text ?? string.Empty).Trim();
            bool hasId = int.TryParse(q, out int idQuery);
            string categoryFilter = (_itemCategoryInput?.text ?? string.Empty).Trim();

            IEnumerable<Item> items = ItemHandler.Items
                .Where(i => i != null)
                .Where(i => i.Categories == null || !i.Categories.Any(c => _excludedItemCategories.Contains(c)));

            if (!string.IsNullOrEmpty(categoryFilter))
            {
                items = items.Where(i => i.Categories != null && i.Categories.Any(c =>
                    !string.IsNullOrWhiteSpace(c) &&
                    c.IndexOf(categoryFilter, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            if (!string.IsNullOrEmpty(q))
            {
                items = items.Where(i =>
                    (hasId && i.ID == idQuery) ||
                    (!string.IsNullOrEmpty(i.Title) && i.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            _filteredItems.AddRange(items.Take(500));
            UpdateItemListInfo();

            if (_selectedItem != null && !_filteredItems.Contains(_selectedItem))
                _selectedItem = null;

            ApplyItemCells();
            UpdateSelectedItemUi();
            RefreshCategoryBrowserList();
        }

        private void ApplyItemCells()
        {
            int maxStart = Mathf.Max(0, _filteredItems.Count - _itemCells.Count);
            _itemScrollStartIndex = Mathf.Clamp(_itemScrollStartIndex, 0, maxStart);

            for (int i = 0; i < _itemCells.Count; i++)
            {
                int dataIndex = _itemScrollStartIndex + i;
                Item item = dataIndex < _filteredItems.Count ? _filteredItems[dataIndex] : null;
                ItemCellRef cell = _itemCells[i];
                cell.BoundItem = item;

                if (item == null)
                {
                    cell.Icon.enabled = false;
                    cell.Label.text = string.Empty;
                    cell.Background.color = Color.white;
                    continue;
                }

                Sprite sprite = item.Appearance?.Sprite;
                if (sprite != null)
                {
                    cell.Icon.sprite = sprite;
                    cell.Icon.overrideSprite = sprite;
                    cell.Icon.type = Image.Type.Simple;
                    cell.Icon.enabled = true;
                }
                else
                {
                    cell.Icon.enabled = false;
                }

                cell.Label.text = string.Empty;
                bool selected = ReferenceEquals(item, _selectedItem);
                cell.Background.color = selected ? new Color(0.95f, 0.9f, 0.72f, 1f) : Color.white;
            }

            UpdateItemListInfo();
        }

        private void OnItemCellClicked(int index)
        {
            if (index < 0 || index >= _itemCells.Count)
                return;

            Item item = _itemCells[index].BoundItem;
            if (item == null)
                return;

            _selectedItem = item;
            UpdateSelectedItemUi();
            ApplyItemCells();
            SetStatus($"Selected item: {item.ID} {item.Title}");
        }

        private void UpdateSelectedItemUi()
        {
            if (_itemDetailsNameText == null || _itemDetailsDescriptionText == null)
                return;

            if (_selectedItem == null)
            {
                if (_itemDetailsContentRoot != null)
                    _itemDetailsContentRoot.SetActive(false);
                if (_itemDetailsIcon != null)
                    _itemDetailsIcon.enabled = false;
                if (_itemDetailsQuantityBadgeText != null)
                    _itemDetailsQuantityBadgeText.text = string.Empty;
                if (_itemDetailsQuantityBadgeRoot != null)
                    _itemDetailsQuantityBadgeRoot.SetActive(false);
                if (_itemDetailsNameText != null)
                    _itemDetailsNameText.text = "Select an item";
                if (_itemDetailsCategoryText != null)
                    _itemDetailsCategoryText.text = "Category: -";
                if (_itemDetailsDescriptionText != null)
                    _itemDetailsDescriptionText.text = "Description: -";
                if (_itemDetailsStackSizeText != null)
                    _itemDetailsStackSizeText.text = "Stack size: -";
                if (_itemDetailsValueText != null)
                    _itemDetailsValueText.text = "Estimated value: -";
                return;
            }

            if (_itemDetailsContentRoot != null)
                _itemDetailsContentRoot.SetActive(true);
            EnsureItemDetailsSourceInitialized();
            ItemDetails details = ResolveItemDetails(_selectedItem);

            if (_itemDetailsIcon != null)
            {
                Sprite sprite = _selectedItem.Appearance?.Sprite;
                if (sprite != null)
                {
                    _itemDetailsIcon.sprite = sprite;
                    _itemDetailsIcon.overrideSprite = sprite;
                    _itemDetailsIcon.type = Image.Type.Simple;
                    _itemDetailsIcon.enabled = true;
                }
                else
                {
                    _itemDetailsIcon.enabled = false;
                }
            }

            bool hasOwnedAmount = details.QuantityBadge > 0;
            if (_itemDetailsQuantityBadgeRoot != null)
                _itemDetailsQuantityBadgeRoot.SetActive(hasOwnedAmount);
            if (_itemDetailsQuantityBadgeText != null)
                _itemDetailsQuantityBadgeText.text = hasOwnedAmount ? details.QuantityBadge.ToString() : string.Empty;
            if (_itemDetailsNameText != null)
                _itemDetailsNameText.text = details.Name;
            if (_itemDetailsCategoryText != null)
                _itemDetailsCategoryText.text = $"Category: {details.Category}";
            if (_itemDetailsDescriptionText != null)
                _itemDetailsDescriptionText.text = $"Description: {details.Description}";
            if (_itemDetailsStackSizeText != null)
                _itemDetailsStackSizeText.text = $"Stack size: {details.StackSize}";
            if (_itemDetailsValueText != null)
                _itemDetailsValueText.text = details.EstimatedValue.HasValue
                    ? $"Estimated value: {details.EstimatedValue.Value}"
                    : "Estimated value: -";
        }

        private ItemDetails ResolveItemDetails(Item item)
        {
            string runtimeName = NormalizeText(item?.Title);
            string runtimeCategory = NormalizeText(GetRuntimeCategory(item));
            string runtimeDescription = NormalizeText(item?.Description);
            int runtimeValue = item != null ? Mathf.Max(0, item.Value) : 0;
            bool hasRuntimeValue = runtimeValue > 0;

            ItemJsonEntry json = null;
            bool shouldCheckJson = _itemDetailsSourceMode == ItemDetailsSourceMode.HybridRuntimeJson &&
                (string.IsNullOrEmpty(runtimeName) || string.IsNullOrEmpty(runtimeCategory) || string.IsNullOrEmpty(runtimeDescription) || !hasRuntimeValue);
            if (shouldCheckJson)
                json = TryGetItemJson(item?.ID ?? -1);

            string name = runtimeName ?? NormalizeText(json?.Title) ?? $"Item #{item?.ID ?? -1}";
            string category = runtimeCategory ?? NormalizeText(json?.Categories?.FirstOrDefault()) ?? "Uncategorized";
            string description = runtimeDescription ?? NormalizeText(json?.Description) ?? "No description available.";
            int? estimatedValue = hasRuntimeValue
                ? runtimeValue
                : (json != null && json.BaseValue > 0 ? json.BaseValue : (int?)null);

            int quantityBadge = GetQuantityBadgeAmount(item);
            int stackSize = item != null ? Mathf.Max(1, item.Stackable) : 1;
            return new ItemDetails
            {
                Name = name,
                Category = category,
                Description = description,
                EstimatedValue = estimatedValue,
                QuantityBadge = quantityBadge,
                StackSize = stackSize
            };
        }

        private void EnsureItemDetailsSourceInitialized()
        {
            if (_itemDetailsSourceInitialized)
                return;

            _itemDetailsSourceInitialized = true;
            var sampleItems = ItemHandler.Items.Where(i => i != null).Take(5).ToList();
            if (sampleItems.Count == 0)
            {
                _itemDetailsSourceMode = ItemDetailsSourceMode.HybridRuntimeJson;
                EnsureItemJsonLookupLoaded();
                Plugin.DLog("Item details source: no runtime samples; enabling JSON fallback.");
                return;
            }

            bool runtimeComplete = sampleItems.All(i =>
                !string.IsNullOrWhiteSpace(i.Title) &&
                !string.IsNullOrWhiteSpace(GetRuntimeCategory(i)) &&
                !string.IsNullOrWhiteSpace(i.Description));

            for (int i = 0; i < sampleItems.Count; i++)
            {
                Item sample = sampleItems[i];
                string cat = NormalizeText(GetRuntimeCategory(sample)) ?? "-";
                int descLen = string.IsNullOrWhiteSpace(sample.Description) ? 0 : sample.Description.Length;
                Plugin.DLog($"Item details sample {i + 1}: ID={sample.ID}, title='{sample.Title}', category='{cat}', descLen={descLen}, value={sample.Value}");
            }

            if (runtimeComplete)
            {
                _itemDetailsSourceMode = ItemDetailsSourceMode.RuntimeOnly;
                Plugin.DLog("Item details source selected: runtime-only.");
            }
            else
            {
                _itemDetailsSourceMode = ItemDetailsSourceMode.HybridRuntimeJson;
                EnsureItemJsonLookupLoaded();
                Plugin.DLog($"Item details source selected: hybrid runtime+json (json entries={_itemJsonLookup?.Count ?? 0}).");
            }
        }

        private ItemJsonEntry TryGetItemJson(int itemId)
        {
            if (itemId < 0)
                return null;

            EnsureItemJsonLookupLoaded();
            if (_itemJsonLookup == null)
                return null;

            _itemJsonLookup.TryGetValue(itemId, out ItemJsonEntry entry);
            return entry;
        }

        private void EnsureItemJsonLookupLoaded()
        {
            if (_itemJsonLookup != null)
                return;

            _itemJsonLookup = new Dictionary<int, ItemJsonEntry>();
            try
            {
                string itemsPath = Path.Combine(Application.dataPath, "StreamingAssets", "Items.json");
                if (!File.Exists(itemsPath))
                {
                    Plugin.DLog($"Items.json missing at: {itemsPath}");
                    return;
                }

                string rawJson = File.ReadAllText(itemsPath);
                string wrapped = "{\"Items\":" + rawJson + "}";
                ItemJsonRoot parsed = JsonUtility.FromJson<ItemJsonRoot>(wrapped);
                if (parsed?.Items == null || parsed.Items.Length == 0)
                {
                    Plugin.DLog("Items.json parse returned empty item list.");
                    return;
                }

                foreach (ItemJsonEntry entry in parsed.Items)
                {
                    if (entry == null)
                        continue;
                    _itemJsonLookup[entry.ID] = entry;
                }

                Plugin.DLog($"Items.json cache loaded: {_itemJsonLookup.Count} entries.");
            }
            catch (Exception ex)
            {
                Plugin.DLog($"Items.json load failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        private static string GetRuntimeCategory(Item item)
        {
            if (item == null)
                return null;

            if (!string.IsNullOrWhiteSpace(item.Category))
                return item.Category;
            if (item.Categories == null || item.Categories.Length == 0)
                return null;
            return item.Categories.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
        }

        private static int GetQuantityBadgeAmount(Item item)
        {
            if (item == null)
                return 0;

            BackpackStorage backpack = BackpackStorage.instance;
            if (backpack == null)
                return 0;

            try
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                FieldInfo[] storageFields = backpack.GetType().GetFields(flags)
                    .Where(f => typeof(Storage).IsAssignableFrom(f.FieldType))
                    .ToArray();

                int total = 0;
                var seenStorageIds = new HashSet<int>();
                for (int i = 0; i < storageFields.Length; i++)
                {
                    Storage storage = storageFields[i].GetValue(backpack) as Storage;
                    if (storage == null)
                        continue;
                    if (!seenStorageIds.Add(storage.GetInstanceID()))
                        continue;

                    total += CountFromStorageItemData(storage, item.ID);
                    if (total <= 0)
                        total += CountFromStorageSlots(storage, item.ID);
                }

                return total;
            }
            catch (Exception ex)
            {
                Plugin.DLog($"GetQuantityBadgeAmount failed: {ex.GetType().Name}");
                return 0;
            }
        }

        private static int CountFromStorageItemData(Storage storage, int itemId)
        {
            if (storage == null || itemId < 0)
                return 0;

            try
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                MethodInfo getStorageItemData = storage.GetType().GetMethod("GetStorageItemData", flags);
                if (getStorageItemData == null)
                    return 0;

                object rawList = getStorageItemData.Invoke(storage, null);
                if (rawList is not System.Collections.IEnumerable list)
                    return 0;

                int total = 0;
                foreach (object data in list)
                {
                    if (data == null)
                        continue;

                    Type dataType = data.GetType();
                    PropertyInfo itemProp = dataType.GetProperty("item", flags);
                    PropertyInfo amountProp = dataType.GetProperty("amount", flags);
                    if (itemProp == null || amountProp == null)
                        continue;

                    Item dataItem = itemProp.GetValue(data, null) as Item;
                    if (dataItem == null || dataItem.ID != itemId)
                        continue;

                    object rawAmount = amountProp.GetValue(data, null);
                    if (rawAmount is int i)
                        total += Mathf.Max(0, i);
                    else if (rawAmount is float f)
                        total += Mathf.Max(0, Mathf.RoundToInt(f));
                }

                return total;
            }
            catch
            {
                return 0;
            }
        }

        private static int CountFromStorageSlots(Storage storage, int itemId)
        {
            if (storage == null || itemId < 0)
                return 0;

            try
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                FieldInfo storageSlotsField = storage.GetType().GetField("storageSlots", flags);
                if (storageSlotsField == null)
                    return 0;

                if (storageSlotsField.GetValue(storage) is not System.Collections.IEnumerable slots)
                    return 0;

                int total = 0;
                foreach (object slot in slots)
                {
                    if (slot == null)
                        continue;
                    Type slotType = slot.GetType();
                    FieldInfo idField = slotType.GetField("itemId", flags);
                    FieldInfo amountField = slotType.GetField("itemAmount", flags);
                    if (idField == null || amountField == null)
                        continue;

                    object rawId = idField.GetValue(slot);
                    object rawAmount = amountField.GetValue(slot);
                    if (rawId is not int id || id != itemId)
                        continue;

                    if (rawAmount is int amount)
                        total += Mathf.Max(0, amount);
                    else if (rawAmount is float f)
                        total += Mathf.Max(0, Mathf.RoundToInt(f));
                }

                return total;
            }
            catch
            {
                return 0;
            }
        }

        private static string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            return text.Trim();
        }

        private void SpawnSelectedOne()
        {
            if (_selectedItem == null)
            {
                SetStatus("No item selected.");
                return;
            }

            SpawnItemAmount(_selectedItem, 1);
        }

        private void SpawnSelectedStack()
        {
            if (_selectedItem == null)
            {
                SetStatus("No item selected.");
                return;
            }

            int stack = Mathf.Max(1, _selectedItem.Stackable);
            SpawnItemAmount(_selectedItem, stack);
        }

        private void SpawnSelectedAmount()
        {
            if (_selectedItem == null)
            {
                SetStatus("No item selected.");
                return;
            }

            if (!TryParsePositiveInt(_itemAmountInput?.text, out int amount))
            {
                amount = 1;
                if (_itemAmountInput != null)
                    _itemAmountInput.text = "1";
                SetStatus("Invalid amount. Using 1.");
            }

            SpawnItemAmount(_selectedItem, amount);
        }

        private void SpawnItemAmount(Item item, int amount)
        {
            if (item == null)
                return;

            if (BackpackStorage.instance == null)
            {
                SetStatus("Backpack not ready.");
                return;
            }

            try
            {
                int requested = Mathf.Max(1, amount);
                int stack = Mathf.Max(1, item.Stackable);
                int fullStacks = requested / stack;
                int remainder = requested % stack;

                for (int i = 0; i < fullStacks; i++)
                {
                    ItemOperations.AddItemsAndDropRemaining(item, -1, null, item.Meta, stack, 0);
                }

                if (remainder > 0 || requested < stack)
                {
                    int finalAmount = requested < stack ? requested : remainder;
                    if (finalAmount > 0)
                        ItemOperations.AddItemsAndDropRemaining(item, -1, null, item.Meta, finalAmount, 0);
                }

                SetStatus($"Spawned {requested}x {item.Title}");
                if (ReferenceEquals(item, _selectedItem))
                    UpdateSelectedItemUi();
            }
            catch (Exception ex)
            {
                SetStatus($"Spawn failed: {ex.GetType().Name}");
                Plugin.DLog($"SpawnItemAmount error: {ex}");
            }
        }

        private static bool TryParsePositiveInt(string text, out int value)
        {
            if (!int.TryParse(text, out int parsed))
            {
                value = 1;
                return false;
            }

            value = Mathf.Max(1, parsed);
            return true;
        }

        private void HandleItemsScrollInput()
        {
            if (_activeTab != MenuTab.Items || _itemGridHoverRegion == null || _filteredItems.Count <= _itemCells.Count)
                return;

            if (!RectTransformUtility.RectangleContainsScreenPoint(_itemGridHoverRegion, Input.mousePosition, null))
                return;

            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) < 0.01f)
                return;

            int step = 5;
            if (wheel > 0f)
                _itemScrollStartIndex -= step;
            else
                _itemScrollStartIndex += step;

            _itemScrollStartIndex = Mathf.Clamp(_itemScrollStartIndex, 0, Mathf.Max(0, _filteredItems.Count - _itemCells.Count));
            ApplyItemCells();
        }

        private void HandleCategoryScrollInput()
        {
            if (_activeTab != MenuTab.Items || !_categoryBrowserVisible || _categoryListHoverRegion == null || _filteredCategoryOptions.Count <= _categoryCells.Count)
                return;

            if (!RectTransformUtility.RectangleContainsScreenPoint(_categoryListHoverRegion, Input.mousePosition, null))
                return;

            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) < 0.01f)
                return;

            _categoryScrollStartIndex += wheel > 0f ? -1 : 1;
            _categoryScrollStartIndex = Mathf.Clamp(_categoryScrollStartIndex, 0, Mathf.Max(0, _filteredCategoryOptions.Count - _categoryCells.Count));
            ApplyCategoryCells();
        }

        private void UpdateItemListInfo()
        {
            if (_itemListInfoText == null)
                return;

            if (_filteredItems.Count == 0)
            {
                _itemListInfoText.text = "Showing: 0";
                return;
            }

            int start = _itemScrollStartIndex + 1;
            int end = Mathf.Min(_itemScrollStartIndex + _itemCells.Count, _filteredItems.Count);
            _itemListInfoText.text = $"Showing: {start}-{end} of {_filteredItems.Count}";
        }

        private void EnsureItemCategoryOptions()
        {
            if (_itemCategoryOptions.Count > 0)
                return;

            _itemCategoryOptions.Add("All");
            if (ItemDatabase.instance == null || ItemDatabase.database == null)
                return;

            foreach (string category in ItemHandler.Categories)
            {
                if (string.IsNullOrWhiteSpace(category) || _excludedItemCategories.Contains(category))
                    continue;
                _itemCategoryOptions.Add(category);
            }
        }

        private void RefreshCategoryBrowserList()
        {
            EnsureItemCategoryOptions();
            _filteredCategoryOptions.Clear();
            if (_itemCategoryOptions.Count == 0)
            {
                ApplyCategoryCells();
                return;
            }

            string q = (_itemCategoryInput?.text ?? string.Empty).Trim();
            IEnumerable<string> categories = _itemCategoryOptions;
            if (!string.IsNullOrEmpty(q))
            {
                categories = categories.Where(c => c.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            _filteredCategoryOptions.AddRange(categories);
            _categoryScrollStartIndex = Mathf.Clamp(_categoryScrollStartIndex, 0, Mathf.Max(0, _filteredCategoryOptions.Count - _categoryCells.Count));
            ApplyCategoryCells();
        }

        private void ApplyCategoryCells()
        {
            for (int i = 0; i < _categoryCells.Count; i++)
            {
                int dataIndex = _categoryScrollStartIndex + i;
                string category = dataIndex < _filteredCategoryOptions.Count ? _filteredCategoryOptions[dataIndex] : null;
                CategoryCellRef cell = _categoryCells[i];
                cell.Category = category;

                if (string.IsNullOrEmpty(category))
                {
                    cell.Label.text = string.Empty;
                    cell.Button.interactable = false;
                }
                else
                {
                    cell.Label.text = category;
                    cell.Button.interactable = true;
                }
            }
        }

        private void OnCategoryCellClicked(int index)
        {
            if (index < 0 || index >= _categoryCells.Count)
                return;

            string category = _categoryCells[index].Category;
            if (string.IsNullOrEmpty(category))
                return;

            if (_itemCategoryInput != null)
                _itemCategoryInput.text = string.Equals(category, "All", StringComparison.OrdinalIgnoreCase) ? string.Empty : category;

            _categoryBrowserVisible = false;
            if (_categoryBrowserPanel != null)
                _categoryBrowserPanel.SetActive(false);

            SetStatus(string.Equals(category, "All", StringComparison.OrdinalIgnoreCase)
                ? "Category filter cleared."
                : $"Category set: {category}");
        }

        private void ToggleCategoryBrowser()
        {
            _categoryBrowserVisible = !_categoryBrowserVisible;
            if (_categoryBrowserPanel != null)
                _categoryBrowserPanel.SetActive(_categoryBrowserVisible);

            if (_categoryBrowserVisible)
            {
                _categoryScrollStartIndex = 0;
                RefreshCategoryBrowserList();
            }
        }

        private void ClearCategoryFilter()
        {
            if (_itemCategoryInput == null)
                return;

            _itemCategoryInput.text = string.Empty;
            _categoryScrollStartIndex = 0;
            RefreshCategoryBrowserList();
            SetStatus("Category filter cleared.");
        }

        private GameObject BuildPlaceholderPage(Transform parent, string label)
        {
            RectTransform page = NewPanelWithSprite(parent, "Page", "main_UI_background_002", Color.white, Image.Type.Sliced);
            Stretch(page);

            RectTransform empty = NewPanelWithSprite(page, "Empty", "main_UI_background_empty_001", Color.white, Image.Type.Tiled);
            Stretch(empty);
            empty.offsetMin = new Vector2(18f, 18f);
            empty.offsetMax = new Vector2(-18f, -18f);

            NewText(empty, label, 20, TextAlignmentOptions.Center, Color.white);
            return page.gameObject;
        }

        private RectTransform BuildActionButton(Transform parent, string label, Vector2 anchoredTopLeft, Action onClick)
        {
            RectTransform buttonRt = NewPanelWithSprite(parent, $"Btn_{label}", "main_UI_background_002", Color.white, Image.Type.Sliced);
            buttonRt.anchorMin = new Vector2(0f, 1f);
            buttonRt.anchorMax = new Vector2(0f, 1f);
            buttonRt.pivot = new Vector2(0f, 1f);
            buttonRt.anchoredPosition = anchoredTopLeft;
            buttonRt.sizeDelta = new Vector2(320f, 40f);

            UIButton button = buttonRt.gameObject.AddComponent<UIButton>();
            button.transition = Selectable.Transition.None;
            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            TMP_Text txt = NewText(buttonRt, label, 14, TextAlignmentOptions.Center, Color.white);
            txt.rectTransform.sizeDelta = new Vector2(-20f, 0f);
            return buttonRt;
        }

        private void CreateTopTab(Transform parent, MenuTab tab, string label)
        {
            GameObject scale = NewUI($"{label}ButtonScale", parent);
            RT(scale).sizeDelta = new Vector2(177.5f, 45f);
            scale.AddComponent<LayoutElement>().preferredWidth = 177.5f;

            RectTransform buttonRect = NewPanelWithSprite(scale.transform, $"{label}Button", "button_top_001", new Color(0.868f, 0.868f, 0.868f, 1f), Image.Type.Sliced);
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.one;
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0f, -8f);
            buttonRect.sizeDelta = Vector2.zero;

            UIButton button = buttonRect.gameObject.AddComponent<UIButton>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => SwitchTab(tab));

            TMP_Text txt = NewText(buttonRect, label, 15, TextAlignmentOptions.Center, Color.white);
            txt.rectTransform.sizeDelta = new Vector2(-40f, 0f);

            _tabs.Add(new TabRef
            {
                Tab = tab,
                Image = buttonRect.GetComponent<UIImage>(),
                ButtonRect = buttonRect,
                Text = txt
            });
        }

        private void SwitchTab(MenuTab tab)
        {
            _activeTab = tab;
            foreach (var kv in _pages)
                kv.Value.SetActive(kv.Key == _activeTab);

            Sprite activeSprite = GetSprite("button_top_001_selected") ?? GetSprite("button_top_001");
            Sprite idleSprite = GetSprite("button_top_001");

            for (int i = 0; i < _tabs.Count; i++)
            {
                bool selected = _tabs[i].Tab == _activeTab;
                _tabs[i].Image.sprite = selected ? activeSprite : idleSprite;
                _tabs[i].Image.type = Image.Type.Sliced;
                _tabs[i].ButtonRect.anchoredPosition = new Vector2(0f, selected ? -5f : -8f);
                _tabs[i].Text.color = Color.white;
            }

            if (_activeTab == MenuTab.Items)
                RefreshItemList();

            SetStatus($"Active tab: {_activeTab}");
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
            Plugin.DLog(message);
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
                return;

            GameObject es = new("OpenSewerEventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private RectTransform NewPanelWithSprite(Transform parent, string name, string spriteName, Color color, Image.Type type)
        {
            GameObject go = NewUI(name, parent);
            UIImage image = go.AddComponent<UIImage>();
            SetSprite(image, spriteName, type, color);
            return RT(go);
        }

        private void SetSprite(UIImage image, string spriteName, Image.Type type, Color color)
        {
            image.color = color;
            image.type = type;
            image.sprite = GetSprite(spriteName);
            image.overrideSprite = image.sprite;
        }

        private Sprite GetSprite(string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
                return null;

            if (_spriteCache.TryGetValue(spriteName, out Sprite cached))
                return cached;

            Sprite sprite = Resources.FindObjectsOfTypeAll<Sprite>()
                .FirstOrDefault(s => string.Equals(s.name, spriteName, StringComparison.OrdinalIgnoreCase));
            _spriteCache[spriteName] = sprite;
            if (sprite == null)
                Plugin.DLog($"Sprite not found: {spriteName}");
            return sprite;
        }

        private TMP_FontAsset FindTmpFont(string fontName)
        {
            if (string.IsNullOrWhiteSpace(fontName))
                return null;

            TMP_FontAsset resolved = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(f => string.Equals(f.name, fontName, StringComparison.OrdinalIgnoreCase));

            if (resolved == null)
                Plugin.DLog($"TMP font not found: {fontName}");
            else
                Plugin.DLog($"TMP font resolved: {resolved.name}");

            return resolved;
        }

        private TMP_Text NewText(Transform parent, string text, float size, TextAlignmentOptions align, Color color)
        {
            GameObject go = NewUI("TextTMP", parent);
            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.font = _menuFont;
            txt.fontSize = size;
            txt.color = color;
            txt.alignment = align;
            txt.enableWordWrapping = false;
            txt.raycastTarget = false;
            Stretch(txt.rectTransform);
            return txt;
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform RT(GameObject go) => go.GetComponent<RectTransform>();

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void PrepareRectForLayout(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }

        private static RectTransform Panel(Transform parent, string name, Color color)
        {
            GameObject go = NewUI(name, parent);
            UIImage image = go.AddComponent<UIImage>();
            image.color = color;
            return RT(go);
        }
    }
}
