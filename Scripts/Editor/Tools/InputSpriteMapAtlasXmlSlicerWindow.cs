using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.InputSpriteMap
{
    public sealed class InputSpriteMapAtlasXmlSlicerWindow : EditorWindow
    {
        private enum CoordinateMode
        {
            XmlYFromTopLeft = 0,
            XmlYFromBottomLeft = 1
        }

        private TextAsset _xmlAsset;
        private Texture2D _textureAsset;
        private CoordinateMode _coordinateMode = CoordinateMode.XmlYFromTopLeft;
        private bool _trimSpriteNames;
        private string _removeNameTokens = "xbox_,steamdeck_,keyboard_,switch_";
        private bool _removeTokensOnlyAtStart = true;

        [MenuItem("Tools/Input Sprite Map/XML Sprite Slicer")]
        private static void OpenWindow()
        {
            InputSpriteMapAtlasXmlSlicerWindow window = GetWindow<InputSpriteMapAtlasXmlSlicerWindow>("XML Sprite Slicer");
            window.minSize = new Vector2(430f, 150f);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Select atlas XML and target texture", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _xmlAsset = (TextAsset)EditorGUILayout.ObjectField("Atlas XML", _xmlAsset, typeof(TextAsset), false);
            _textureAsset = (Texture2D)EditorGUILayout.ObjectField("Texture", _textureAsset, typeof(Texture2D), false);
            _coordinateMode = (CoordinateMode)EditorGUILayout.EnumPopup("Coordinate Mode", _coordinateMode);
            _trimSpriteNames = EditorGUILayout.Toggle("Trim Sprite Names", _trimSpriteNames);
            _removeNameTokens = EditorGUILayout.TextField("Remove Name Tokens", _removeNameTokens);
            _removeTokensOnlyAtStart = EditorGUILayout.Toggle("Remove Tokens Only At Start", _removeTokensOnlyAtStart);

            EditorGUILayout.HelpBox(GetCoordinateHelpText(), MessageType.Info);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!CanApply()))
            {
                if (GUILayout.Button("Apply XML Slices", GUILayout.Height(28f)))
                    ApplyXmlSlices();
            }
        }

        private bool CanApply()
        {
            return _xmlAsset != null && _textureAsset != null;
        }

        private void ApplyXmlSlices()
        {
            string texturePath = AssetDatabase.GetAssetPath(_textureAsset);
            string xmlPath = AssetDatabase.GetAssetPath(_xmlAsset);
            if (string.IsNullOrWhiteSpace(texturePath) || string.IsNullOrWhiteSpace(xmlPath))
            {
                Debug.LogError("Could not resolve selected XML or texture paths.");
                return;
            }

            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Could not get TextureImporter for '{texturePath}'.");
                return;
            }

            List<SpriteMetaData> sprites;
            try
            {
                sprites = ParseSpriteMetaData(
                    xmlPath,
                    _textureAsset.height,
                    _coordinateMode,
                    _trimSpriteNames,
                    _removeNameTokens,
                    _removeTokensOnlyAtStart);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed parsing sprite XML '{xmlPath}'.\n{exception.Message}");
                return;
            }

            if (sprites.Count == 0)
            {
                Debug.LogWarning($"No <SubTexture> entries found in '{xmlPath}'.");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritesheet = sprites.ToArray();

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            AssetDatabase.Refresh();

            Debug.Log($"Applied {sprites.Count} sprite slices from '{xmlPath}' to '{texturePath}'.");
        }

        private string GetCoordinateHelpText()
        {
            if (_coordinateMode == CoordinateMode.XmlYFromTopLeft)
                return "Use this for most atlas XML files (TexturePacker style): y starts at TOP and goes down.";

            return "Use this when XML already uses Unity-like coordinates: y starts at BOTTOM and goes up.";
        }

        private static List<SpriteMetaData> ParseSpriteMetaData(
            string xmlAssetPath,
            int textureHeight,
            CoordinateMode coordinateMode,
            bool trimSpriteNames,
            string removeNameTokens,
            bool removeTokensOnlyAtStart)
        {
            string fullXmlPath = Path.GetFullPath(xmlAssetPath);
            XDocument document = XDocument.Load(fullXmlPath);
            XElement atlasNode = document.Root;
            if (atlasNode == null)
                return new List<SpriteMetaData>();

            List<SpriteMetaData> spriteMetaDataList = new List<SpriteMetaData>();
            HashSet<string> usedNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (XElement subTextureNode in atlasNode.Elements("SubTexture"))
            {
                string name = RequireAttribute(subTextureNode, "name");
                if (trimSpriteNames)
                    name = name.Trim();
                name = RemoveNameTokens(name, removeNameTokens, removeTokensOnlyAtStart);

                int x = ParseIntAttribute(subTextureNode, "x");
                int y = ParseIntAttribute(subTextureNode, "y");
                int width = ParseIntAttribute(subTextureNode, "width");
                int height = ParseIntAttribute(subTextureNode, "height");

                if (!usedNames.Add(name))
                    throw new InvalidOperationException($"Duplicate sprite name '{name}' in XML.");

                int yBottom = coordinateMode == CoordinateMode.XmlYFromTopLeft
                    ? textureHeight - y - height
                    : y;
                Rect rect = new Rect(x, yBottom, width, height);

                SpriteMetaData spriteMetaData = new SpriteMetaData
                {
                    name = name,
                    rect = rect,
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center
                };

                spriteMetaDataList.Add(spriteMetaData);
            }

            return spriteMetaDataList;
        }

        private static string RemoveNameTokens(string value, string removeNameTokens, bool removeTokensOnlyAtStart)
        {
            if (string.IsNullOrWhiteSpace(removeNameTokens))
                return value;

            string[] tokens = removeNameTokens.Split(',');
            string output = value;
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i].Trim();
                if (string.IsNullOrEmpty(token))
                    continue;

                if (removeTokensOnlyAtStart)
                {
                    if (output.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                        output = output.Substring(token.Length);

                    continue;
                }

                output = output.Replace(token, string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return output;
        }

        private static string RequireAttribute(XElement element, string attributeName)
        {
            XAttribute attribute = element.Attribute(attributeName);
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.Value))
                throw new InvalidOperationException($"Missing '{attributeName}' attribute.");

            return attribute.Value;
        }

        private static int ParseIntAttribute(XElement element, string attributeName)
        {
            string value = RequireAttribute(element, attributeName);
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                throw new InvalidOperationException($"Invalid integer '{value}' for '{attributeName}'.");

            return parsed;
        }
    }
}
