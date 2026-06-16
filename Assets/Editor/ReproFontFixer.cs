using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ReproFontFixer
{
    private const string ScenePath = "Assets/Peidiangui.unity";
    private const string FontAssetPath = "Assets/Fonts/ChineseFontSDF.asset";
    private const string SourceFontPath = "Assets/Fonts/SimHei.ttf";

    public static void Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                Debug.LogError("Missing source font: " + SourceFontPath);
                EditorApplication.Exit(2);
                return;
            }

            fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                72,
                8,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                4096,
                4096,
                AtlasPopulationMode.Dynamic);
            fontAsset.name = "ChineseFontSDF";
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
        }

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        var characters = CollectSceneCharacters();
        fontAsset.TryAddCharacters(characters, out string missingCharacters);
        EnsureAtlasAndMaterial(fontAsset);
        if (!string.IsNullOrEmpty(missingCharacters))
        {
            Debug.LogWarning("TMP font missing characters: " + missingCharacters);
        }

        var scene = EditorSceneManager.OpenScene(ScenePath);
        foreach (var text in Object.FindObjectsOfType<TMP_Text>(true))
        {
            text.font = fontAsset;
            text.fontSharedMaterial = fontAsset.material;
            EditorUtility.SetDirty(text);
        }

        foreach (var input in Object.FindObjectsOfType<TMP_InputField>(true))
        {
            input.fontAsset = fontAsset;
            if (input.textComponent != null)
            {
                input.textComponent.font = fontAsset;
                input.textComponent.fontSharedMaterial = fontAsset.material;
                EditorUtility.SetDirty(input.textComponent);
            }
            EditorUtility.SetDirty(input);
        }

        foreach (var behaviour in Object.FindObjectsOfType<MonoBehaviour>(true))
        {
            if (behaviour == null) continue;
            var serialized = new SerializedObject(behaviour);
            var chineseFont = serialized.FindProperty("chineseFont");
            if (chineseFont != null && chineseFont.propertyType == SerializedPropertyType.ObjectReference)
            {
                chineseFont.objectReferenceValue = fontAsset;
                serialized.ApplyModifiedProperties();
            }
        }

        EditorUtility.SetDirty(fontAsset);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("ReproFontFixer completed: Chinese TMP glyphs generated and scene text rebound.");
        EditorApplication.Exit(0);
    }

    private static void EnsureAtlasAndMaterial(TMP_FontAsset fontAsset)
    {
        var atlas = fontAsset.atlasTexture;
        if (atlas == null)
        {
            atlas = new Texture2D(4096, 4096, TextureFormat.Alpha8, false);
            atlas.name = "ChineseFontSDF Atlas";
            fontAsset.atlasTextures = new[] { atlas };
        }

        if (!EditorUtility.IsPersistent(atlas))
        {
            atlas.name = "ChineseFontSDF Atlas";
            AssetDatabase.AddObjectToAsset(atlas, fontAsset);
        }

        var material = fontAsset.material;
        if (material == null)
        {
            var shader = Shader.Find("TextMeshPro/Distance Field");
            material = new Material(shader);
            material.name = "ChineseFontSDF Material";
            fontAsset.material = material;
        }

        material.SetTexture(ShaderUtilities.ID_MainTex, atlas);
        if (!EditorUtility.IsPersistent(material))
        {
            AssetDatabase.AddObjectToAsset(material, fontAsset);
        }

        EditorUtility.SetDirty(atlas);
        EditorUtility.SetDirty(material);
        EditorUtility.SetDirty(fontAsset);
    }

    private static string CollectSceneCharacters()
    {
        var result = new HashSet<char>();
        string sceneText = File.ReadAllText(ScenePath, Encoding.UTF8);
        foreach (Match match in Regex.Matches(sceneText, @"\\u([0-9A-Fa-f]{4})"))
        {
            result.Add((char)System.Convert.ToInt32(match.Groups[1].Value, 16));
        }

        foreach (char c in sceneText)
        {
            if (c >= 0x4E00 && c <= 0x9FFF)
            {
                result.Add(c);
            }
        }

        const string essentials = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz .,;:!?()[]{}<>/-_+=%#\"'，。；：！？（）【】《》、·“”‘’\r\n";
        foreach (char c in essentials)
        {
            result.Add(c);
        }

        var builder = new StringBuilder(result.Count);
        foreach (char c in result)
        {
            builder.Append(c);
        }
        return builder.ToString();
    }
}
