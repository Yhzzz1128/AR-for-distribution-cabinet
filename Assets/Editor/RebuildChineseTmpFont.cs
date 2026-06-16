using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class RebuildChineseTmpFont
{
    private const string ScenePath = "Assets/Peidiangui.unity";
    private const string FontAssetPath = "Assets/Fonts/ChineseFontSDF.asset";
    private const string FontBackupPath = "Assets/Fonts/ChineseFontSDF.before-rebuild.asset";
    private const string SourceFontPath = "Assets/Fonts/SimHei.ttf";

    public static void Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogError("Missing source font: " + SourceFontPath);
            EditorApplication.Exit(2);
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null)
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontBackupPath) == null)
            {
                AssetDatabase.CopyAsset(FontAssetPath, FontBackupPath);
            }
            AssetDatabase.DeleteAsset(FontAssetPath);
        }

        var characters = CollectSceneCharacters();
        var fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            72,
            8,
            GlyphRenderMode.SDFAA,
            4096,
            4096,
            AtlasPopulationMode.Dynamic);

        fontAsset.name = "ChineseFontSDF";
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.isMultiAtlasTexturesEnabled = true;
        fontAsset.TryAddCharacters(characters, out string missingCharacters);
        fontAsset.ReadFontAssetDefinition();
        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
        FixMaterial(fontAsset);
        PersistSubAssets(fontAsset);

        var scene = EditorSceneManager.OpenScene(ScenePath);
        int textCount = 0;
        foreach (var text in Object.FindObjectsOfType<TMP_Text>(true))
        {
            text.font = fontAsset;
            text.fontSharedMaterial = fontAsset.material;
            text.SetAllDirty();
            EditorUtility.SetDirty(text);
            textCount++;
        }

        foreach (var input in Object.FindObjectsOfType<TMP_InputField>(true))
        {
            input.fontAsset = fontAsset;
            if (input.textComponent != null)
            {
                input.textComponent.font = fontAsset;
                input.textComponent.fontSharedMaterial = fontAsset.material;
                input.textComponent.SetAllDirty();
                EditorUtility.SetDirty(input.textComponent);
            }
            if (input.placeholder is TMP_Text placeholder)
            {
                placeholder.font = fontAsset;
                placeholder.fontSharedMaterial = fontAsset.material;
                placeholder.SetAllDirty();
                EditorUtility.SetDirty(placeholder);
            }
            EditorUtility.SetDirty(input);
        }

        foreach (var controller in Object.FindObjectsOfType<TextToggleController>(true))
        {
            EditorUtility.SetDirty(controller);
        }

        EditorUtility.SetDirty(fontAsset);
        if (fontAsset.material != null) EditorUtility.SetDirty(fontAsset.material);
        if (fontAsset.atlasTexture != null) EditorUtility.SetDirty(fontAsset.atlasTexture);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        int nonZeroAlpha = CountNonZeroAtlasBytes(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("RebuildChineseTmpFont completed. Texts rebound: " + textCount +
                  ", requested chars: " + characters.Length +
                  ", missing chars: " + (missingCharacters ?? string.Empty) +
                  ", non-zero atlas bytes: " + nonZeroAlpha);

        if (!string.IsNullOrEmpty(missingCharacters) || nonZeroAlpha <= 0)
        {
            EditorApplication.Exit(3);
            return;
        }

        EditorApplication.Exit(0);
    }

    private static void FixMaterial(TMP_FontAsset fontAsset)
    {
        if (fontAsset.material == null)
        {
            var shader = Shader.Find("TextMeshPro/Distance Field");
            fontAsset.material = new Material(shader);
        }

        fontAsset.material.name = "ChineseFontSDF Material";
        fontAsset.material.SetTexture(ShaderUtilities.ID_MainTex, fontAsset.atlasTexture);
        fontAsset.material.SetFloat("_TextureWidth", fontAsset.atlasWidth);
        fontAsset.material.SetFloat("_TextureHeight", fontAsset.atlasHeight);
    }

    private static void PersistSubAssets(TMP_FontAsset fontAsset)
    {
        if (fontAsset.material != null && !EditorUtility.IsPersistent(fontAsset.material))
        {
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        if (fontAsset.atlasTextures != null)
        {
            foreach (var atlas in fontAsset.atlasTextures)
            {
                if (atlas == null) continue;
                atlas.name = "ChineseFontSDF Atlas";
                if (!EditorUtility.IsPersistent(atlas))
                {
                    AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                }
                EditorUtility.SetDirty(atlas);
            }
        }

        EditorUtility.SetDirty(fontAsset);
    }

    private static int CountNonZeroAtlasBytes(TMP_FontAsset fontAsset)
    {
        var atlas = fontAsset.atlasTexture;
        if (atlas == null) return 0;

        var bytes = atlas.GetRawTextureData<byte>();
        int count = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] != 0)
            {
                count++;
                if (count > 1024) break;
            }
        }
        return count;
    }

    private static string CollectSceneCharacters()
    {
        var chars = new SortedSet<char>();
        AddAsciiAndPunctuation(chars);

        string sceneText = File.ReadAllText(ScenePath, Encoding.UTF8);
        foreach (Match match in Regex.Matches(sceneText, @"\\u([0-9A-Fa-f]{4})"))
        {
            chars.Add((char)System.Convert.ToInt32(match.Groups[1].Value, 16));
        }

        foreach (char c in sceneText)
        {
            if (!char.IsControl(c))
            {
                chars.Add(c);
            }
        }

        var builder = new StringBuilder(chars.Count);
        foreach (char c in chars)
        {
            builder.Append(c);
        }
        return builder.ToString();
    }

    private static void AddAsciiAndPunctuation(ISet<char> chars)
    {
        const string ascii = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz ";
        foreach (char c in ascii) chars.Add(c);

        int[] punctuation =
        {
            0x0009, 0x000A, 0x000D, 0x0020, 0x0021, 0x0022, 0x0023, 0x0025,
            0x0026, 0x0027, 0x0028, 0x0029, 0x002B, 0x002C, 0x002D, 0x002E,
            0x002F, 0x003A, 0x003B, 0x003C, 0x003D, 0x003E, 0x003F, 0x005B,
            0x005D, 0x005F, 0x007B, 0x007D, 0x00B7, 0x201C, 0x201D, 0x3001,
            0x3002, 0x300A, 0x300B, 0x3010, 0x3011, 0xFF08, 0xFF09, 0xFF0C,
            0xFF0F, 0xFF1A, 0xFF1B, 0xFF1F
        };

        foreach (int code in punctuation)
        {
            chars.Add((char)code);
        }
    }
}
