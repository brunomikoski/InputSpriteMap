using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BrunoMikoski.InputSpriteMap
{
    public sealed class SpritesMapRegistryAtlasImporterWindow : EditorWindow
    {
        private const string SortedColumnKey = "SpritesMapRegistryAtlasImporterWindow_sortedColumn";
        private const float TableRowHeight = 30f;

        private Texture2D _atlasTexture;
        private SpritesMapRegistry _registry;
        private PlatformType _platformType = PlatformType.MouseAndKeyboard;

        private readonly List<ParsedSpriteEntry> _parsedSprites = new List<ParsedSpriteEntry>();
        private SearchField _searchField;
        private string _searchText = string.Empty;
        private string _lastAppliedSearchText = string.Empty;
        private bool _showOnlySelected;
        private bool _lastAppliedShowOnlySelected;
        private ParsedSpritesTreeView _treeView;

        private sealed class ParsedSpriteEntry
        {
            public bool add = true;
            public string inputKeyName = string.Empty;
            public bool isAlreadyAssigned;

            public Sprite sprite;
            public string spriteName = string.Empty;
            public string spriteGuid = string.Empty;
        }

        private sealed class ParsedSpriteTreeViewItem : TreeViewItem
        {
            public int sourceIndex;

            public ParsedSpriteTreeViewItem(int id, int sourceIndex) : base(id)
            {
                this.sourceIndex = sourceIndex;
            }
        }

        private sealed class ParsedSpritesTreeView : TreeView
        {
            private readonly List<ParsedSpriteEntry> _entries;
            private string _searchText = string.Empty;
            private bool _showOnlySelected;

            public ParsedSpritesTreeView(TreeViewState state, MultiColumnHeader header, List<ParsedSpriteEntry> entries)
                : base(state, header)
            {
                _entries = entries;
                rowHeight = TableRowHeight;
                showAlternatingRowBackgrounds = true;
                showBorder = true;
                header.ResizeToFit();
            }

            public void SetFilters(string searchText, bool showOnlySelected)
            {
                _searchText = searchText ?? string.Empty;
                _showOnlySelected = showOnlySelected;
                Reload();
            }

            protected override TreeViewItem BuildRoot()
            {
                TreeViewItem root = new TreeViewItem { id = -1, depth = -1 };
                List<TreeViewItem> rows = new List<TreeViewItem>();
                int id = 0;
                for (int i = 0; i < _entries.Count; i++)
                {
                    ParsedSpriteEntry entry = _entries[i];
                    if (!PassesFilters(entry))
                        continue;

                    rows.Add(new ParsedSpriteTreeViewItem(id++, i));
                }

                root.children = rows;
                return root;
            }

            protected override bool CanMultiSelect(TreeViewItem item)
            {
                return false;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                ParsedSpriteTreeViewItem item = (ParsedSpriteTreeViewItem)args.item;
                ParsedSpriteEntry entry = _entries[item.sourceIndex];

                for (int visibleColumn = 0; visibleColumn < args.GetNumVisibleColumns(); visibleColumn++)
                {
                    Rect cellRect = args.GetCellRect(visibleColumn);
                    int column = args.GetColumn(visibleColumn);

                    switch (column)
                    {
                        case 0:
                            using (new EditorGUI.DisabledScope(entry.isAlreadyAssigned))
                                entry.add = EditorGUI.Toggle(cellRect, entry.add);
                            break;
                        case 1:
                            DrawSpritePreview(cellRect, entry.sprite);
                            break;
                        case 2:
                            using (new EditorGUI.DisabledScope(entry.isAlreadyAssigned))
                                entry.inputKeyName = EditorGUI.TextField(cellRect, entry.inputKeyName);
                            break;
                        case 3:
                            using (new EditorGUI.DisabledScope(true))
                                EditorGUI.TextField(cellRect, entry.spriteName);
                            break;
                    }
                }
            }

            private bool PassesFilters(ParsedSpriteEntry entry)
            {
                if (_showOnlySelected && !entry.add)
                    return false;

                if (string.IsNullOrWhiteSpace(_searchText))
                    return true;

                if (!string.IsNullOrWhiteSpace(entry.inputKeyName)
                    && entry.inputKeyName.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                if (!string.IsNullOrWhiteSpace(entry.spriteName)
                    && entry.spriteName.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                return false;
            }

            private static void DrawSpritePreview(Rect rect, Sprite sprite)
            {
                Rect paddedRect = new Rect(rect.x + 1f, rect.y + 1f, rect.height - 2f, rect.height - 2f);
                EditorGUI.DrawRect(paddedRect, new Color(0f, 0f, 0f, 0.25f));
                if (sprite == null || sprite.texture == null)
                    return;

                Rect spriteRect = sprite.rect;
                Texture2D texture = sprite.texture;
                Rect texCoords = new Rect(
                    spriteRect.x / texture.width,
                    spriteRect.y / texture.height,
                    spriteRect.width / texture.width,
                    spriteRect.height / texture.height);

                GUI.DrawTextureWithTexCoords(paddedRect, texture, texCoords, true);
            }
        }

        [MenuItem("Tools/Input Sprite Map/Import Atlas Sprites -> SpritesMapRegistry")]
        private static void OpenWindow()
        {
            SpritesMapRegistryAtlasImporterWindow window =
                GetWindow<SpritesMapRegistryAtlasImporterWindow>("Atlas -> SpritesMapRegistry");
            window.minSize = new Vector2(920f, 540f);
        }

        private void OnEnable()
        {
            _searchField = new SearchField();
            _treeView = CreateTreeView();

            if (_registry != null)
                return;

            try
            {
                _registry = SpritesMapRegistry.FromAssetDatabase();
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    $"[SpritesMapRegistryAtlasImporterWindow] Could not find registry asset automatically: {e.Message}");
            }
        }

        private void OnGUI()
        {
            if (_treeView == null)
                _treeView = CreateTreeView();
            if (_searchField == null)
                _searchField = new SearchField();

            EditorGUILayout.Space();
            DrawTopControls();
            EditorGUILayout.Space();
            DrawFilters();
            EditorGUILayout.Space();
            DrawParsedSpritesList();
            EditorGUILayout.Space();
            DrawApplyButton();
        }

        private ParsedSpritesTreeView CreateTreeView()
        {
            MultiColumnHeaderState.Column[] columns =
            {
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Add"), width = 44f, minWidth = 40f, autoResize = false },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Sprite"), width = 62f, minWidth = 58f, autoResize = false },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Input Key Name"), width = 260f, minWidth = 200f, autoResize = true },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Sprite Name"), width = 280f, minWidth = 220f, autoResize = true }
            };

            MultiColumnHeader header = new MultiColumnHeader(new MultiColumnHeaderState(columns));
            header.sortedColumnIndex = SessionState.GetInt(SortedColumnKey, 3);
            header.sortingChanged += OnSortingChanged;

            ParsedSpritesTreeView treeView = new ParsedSpritesTreeView(new TreeViewState(), header, _parsedSprites);
            treeView.Reload();
            return treeView;
        }

        private void OnSortingChanged(MultiColumnHeader header)
        {
            SessionState.SetInt(SortedColumnKey, header.sortedColumnIndex);
            SortParsedEntries(header.sortedColumnIndex, header.IsSortedAscending(header.sortedColumnIndex));
            _treeView.Reload();
        }

        private void SortParsedEntries(int sortedColumnIndex, bool ascending)
        {
            Comparison<ParsedSpriteEntry> comparison;
            switch (sortedColumnIndex)
            {
                case 0:
                    comparison = (a, b) => a.add.CompareTo(b.add);
                    break;
                case 1:
                case 3:
                    comparison = (a, b) => string.Compare(a.spriteName, b.spriteName, StringComparison.OrdinalIgnoreCase);
                    break;
                case 2:
                    comparison = (a, b) => string.Compare(a.inputKeyName, b.inputKeyName, StringComparison.OrdinalIgnoreCase);
                    break;
                default:
                    comparison = (a, b) => string.Compare(a.spriteName, b.spriteName, StringComparison.OrdinalIgnoreCase);
                    break;
            }

            _parsedSprites.Sort((a, b) => ascending ? comparison(a, b) : comparison(b, a));
        }

        private void DrawTopControls()
        {
            EditorGUILayout.LabelField("Atlas Import Settings", EditorStyles.boldLabel);

            _registry = (SpritesMapRegistry)EditorGUILayout.ObjectField(
                "Sprites Map Registry",
                _registry,
                typeof(SpritesMapRegistry),
                false);

            _atlasTexture = (Texture2D)EditorGUILayout.ObjectField(
                "Texture Atlas (Sprite Sheet)",
                _atlasTexture,
                typeof(Texture2D),
                false);

            _platformType = (PlatformType)EditorGUILayout.EnumFlagsField("Platform Type", _platformType);

            using (new EditorGUI.DisabledScope(_atlasTexture == null))
            {
                if (GUILayout.Button("Parse Sprites From Atlas"))
                {
                    ParseAtlasSprites();
                    SortParsedEntries(
                        _treeView.multiColumnHeader.sortedColumnIndex,
                        _treeView.multiColumnHeader.IsSortedAscending(_treeView.multiColumnHeader.sortedColumnIndex));
                    _treeView.Reload();
                }
            }
        }

        private void DrawFilters()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Filter", GUILayout.Width(32f));
            _searchText = _searchField.OnToolbarGUI(_searchText);
            bool newOnlySelected = GUILayout.Toggle(_showOnlySelected, "Only Selected", EditorStyles.toolbarButton, GUILayout.Width(100f));
            EditorGUILayout.EndHorizontal();

            _showOnlySelected = newOnlySelected;
            if (_lastAppliedSearchText == _searchText && _lastAppliedShowOnlySelected == _showOnlySelected)
                return;

            _treeView.SetFilters(_searchText, _showOnlySelected);
            _lastAppliedSearchText = _searchText;
            _lastAppliedShowOnlySelected = _showOnlySelected;
        }

        private void ParseAtlasSprites()
        {
            _parsedSprites.Clear();

            if (_atlasTexture == null)
                return;

            string atlasAssetPath = AssetDatabase.GetAssetPath(_atlasTexture);
            if (string.IsNullOrWhiteSpace(atlasAssetPath))
                return;

            UnityEngine.Object[] assetsAtPath = AssetDatabase.LoadAllAssetsAtPath(atlasAssetPath);
            if (assetsAtPath == null || assetsAtPath.Length == 0)
                return;

            Dictionary<string, string> existingByGuidAndSpriteName = BuildExistingInputNameByGuidAndSpriteName();
            Dictionary<string, string> existingByName = BuildExistingInputNameBySpriteName();

            List<Sprite> sprites = new List<Sprite>();
            for (int i = 0; i < assetsAtPath.Length; i++)
            {
                if (assetsAtPath[i] is Sprite sprite)
                {
                    if (!string.IsNullOrWhiteSpace(sprite.name))
                        sprites.Add(sprite);
                }
            }

            sprites.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            for (int i = 0; i < sprites.Count; i++)
            {
                Sprite sprite = sprites[i];
                string spriteAssetPath = AssetDatabase.GetAssetPath(sprite);
                string spriteGuid = AssetDatabase.AssetPathToGUID(spriteAssetPath);
                string matchedInputName = FindExistingInputName(existingByGuidAndSpriteName, existingByName, spriteGuid, sprite.name);
                bool alreadyAssigned = !string.IsNullOrWhiteSpace(matchedInputName);

                _parsedSprites.Add(
                    new ParsedSpriteEntry
                    {
                        add = !alreadyAssigned,
                        inputKeyName = alreadyAssigned ? matchedInputName : string.Empty,
                        isAlreadyAssigned = alreadyAssigned,
                        sprite = sprite,
                        spriteName = sprite.name,
                        spriteGuid = spriteGuid
                    });
            }
        }

        private Dictionary<string, string> BuildExistingInputNameByGuidAndSpriteName()
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (_registry == null || _registry.spriteData == null)
                return result;

            for (int i = 0; i < _registry.spriteData.Length; i++)
            {
                SpriteData data = _registry.spriteData[i];
                if (data == null || string.IsNullOrWhiteSpace(data.Name) || data.PlatformToSprite == null)
                    continue;

                for (int j = 0; j < data.PlatformToSprite.Length; j++)
                {
                    PlatformToSprite mapping = data.PlatformToSprite[j];
                    if (mapping == null || string.IsNullOrWhiteSpace(mapping.SpriteGuid))
                        continue;
                    if (string.IsNullOrWhiteSpace(mapping.SpriteName))
                        continue;

                    string key = BuildGuidAndSpriteNameKey(mapping.SpriteGuid, mapping.SpriteName);
                    if (!result.ContainsKey(key))
                        result.Add(key, data.Name);
                }
            }

            return result;
        }

        private Dictionary<string, string> BuildExistingInputNameBySpriteName()
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> ambiguousNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_registry == null || _registry.spriteData == null)
                return result;

            for (int i = 0; i < _registry.spriteData.Length; i++)
            {
                SpriteData data = _registry.spriteData[i];
                if (data == null || string.IsNullOrWhiteSpace(data.Name) || data.PlatformToSprite == null)
                    continue;

                for (int j = 0; j < data.PlatformToSprite.Length; j++)
                {
                    PlatformToSprite mapping = data.PlatformToSprite[j];
                    if (mapping == null || string.IsNullOrWhiteSpace(mapping.SpriteName))
                        continue;

                    if (!result.TryGetValue(mapping.SpriteName, out string existingInputName))
                    {
                        result.Add(mapping.SpriteName, data.Name);
                        continue;
                    }

                    if (!string.Equals(existingInputName, data.Name, StringComparison.OrdinalIgnoreCase))
                        ambiguousNames.Add(mapping.SpriteName);
                }
            }

            foreach (string ambiguousName in ambiguousNames)
                result.Remove(ambiguousName);

            return result;
        }

        private static string FindExistingInputName(
            Dictionary<string, string> existingByGuidAndSpriteName,
            Dictionary<string, string> existingByName,
            string spriteGuid,
            string spriteName)
        {
            if (!string.IsNullOrWhiteSpace(spriteGuid) && !string.IsNullOrWhiteSpace(spriteName))
            {
                string key = BuildGuidAndSpriteNameKey(spriteGuid, spriteName);
                if (existingByGuidAndSpriteName.TryGetValue(key, out string byGuidAndName))
                    return byGuidAndName;
            }

            if (!string.IsNullOrWhiteSpace(spriteName) && existingByName.TryGetValue(spriteName, out string bySpriteName))
                return bySpriteName;

            return string.Empty;
        }

        private static string BuildGuidAndSpriteNameKey(string spriteGuid, string spriteName)
        {
            return $"{spriteGuid}::{spriteName}";
        }

        private void DrawParsedSpritesList()
        {
            if (_parsedSprites.Count == 0)
            {
                EditorGUILayout.HelpBox("Pick an atlas texture and press 'Parse Sprites From Atlas'.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Parsed Sprites: {_parsedSprites.Count}", EditorStyles.boldLabel);
            Rect tableRect = GUILayoutUtility.GetRect(0f, 100000f, 200f, 100000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _treeView.OnGUI(tableRect);
        }

        private void DrawApplyButton()
        {
            if (_registry == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a SpritesMapRegistry asset first (top of the window).",
                    MessageType.Warning);
                return;
            }

            int enabledCount = 0;
            int selectedCount = 0;
            for (int i = 0; i < _parsedSprites.Count; i++)
            {
                if (_parsedSprites[i].add)
                    enabledCount++;

                if (!_parsedSprites[i].add)
                    continue;

                if (string.IsNullOrWhiteSpace(_parsedSprites[i].inputKeyName))
                    continue;

                selectedCount++;
            }

            if (enabledCount > 0 && selectedCount < enabledCount)
            {
                EditorGUILayout.HelpBox(
                    "Only rows with 'Add' checked AND a non-empty 'Input Key Name' will be applied.",
                    MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(selectedCount == 0))
            {
                if (GUILayout.Button($"Add Selected ({selectedCount}) To SpritesMapRegistry"))
                {
                    int updatedCount = ApplySelectedSpritesToRegistry();
                    Debug.Log($"[SpritesMapRegistryAtlasImporterWindow] Updated/added {updatedCount} input mappings.");
                }
            }
        }

        private int ApplySelectedSpritesToRegistry()
        {
            if (_registry == null)
                return 0;

            Undo.RecordObject(_registry, "Update SpritesMapRegistry from atlas");

            List<SpriteData> spriteDataList = _registry.spriteData != null
                ? new List<SpriteData>(_registry.spriteData)
                : new List<SpriteData>();

            // Normalize existing name -> index map (case-insensitive).
            Dictionary<string, int> nameToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < spriteDataList.Count; i++)
            {
                if (spriteDataList[i] == null)
                    continue;

                if (string.IsNullOrWhiteSpace(spriteDataList[i].Name))
                    continue;

                nameToIndex[spriteDataList[i].Name] = i;
            }

            // Handle duplicates inside the selection: last enabled row wins.
            Dictionary<string, ParsedSpriteEntry> selectionByInputName =
                new Dictionary<string, ParsedSpriteEntry>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _parsedSprites.Count; i++)
            {
                ParsedSpriteEntry entry = _parsedSprites[i];
                if (!entry.add)
                    continue;

                string inputName = entry.inputKeyName?.Trim();
                if (string.IsNullOrWhiteSpace(inputName))
                    continue;

                selectionByInputName[inputName] = entry;
            }

            int processedCount = selectionByInputName.Count;

            foreach (KeyValuePair<string, ParsedSpriteEntry> kvp in selectionByInputName)
            {
                string inputKeyName = kvp.Key;
                ParsedSpriteEntry entry = kvp.Value;

                if (!nameToIndex.TryGetValue(inputKeyName, out int dataIndex))
                {
                    SpriteData newData = new SpriteData
                    {
                        Name = inputKeyName,
                        PlatformToSprite = ArrayEmptyPlatformToSprite()
                    };

                    dataIndex = spriteDataList.Count;
                    spriteDataList.Add(newData);
                    nameToIndex[inputKeyName] = dataIndex;
                }

                SpriteData spriteData = spriteDataList[dataIndex];
                List<PlatformToSprite> platformToSpriteList = spriteData.PlatformToSprite != null
                    ? new List<PlatformToSprite>(spriteData.PlatformToSprite)
                    : new List<PlatformToSprite>();

                // Ensure we don't end up with multiple entries for the same input + PlatformType.
                platformToSpriteList.RemoveAll(p => p != null && p.PlatformType == _platformType);

                platformToSpriteList.Add(
                    new PlatformToSprite
                    {
                        PlatformType = _platformType,
                        SpriteName = entry.spriteName,
                        SpriteGuid = entry.spriteGuid
                    });
                spriteData.PlatformToSprite = platformToSpriteList.ToArray();
            }

            _registry.spriteData = spriteDataList.ToArray();
            EditorUtility.SetDirty(_registry);
            AssetDatabase.SaveAssets();

            return processedCount;
        }

        private static PlatformToSprite[] ArrayEmptyPlatformToSprite()
        {
            return Array.Empty<PlatformToSprite>();
        }

    }
}

