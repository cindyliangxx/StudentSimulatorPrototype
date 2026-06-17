using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class CardImageAutoBinder
{
    private const string CardAssetRoot = "Assets/Data/EventCards";
    private const string CardImageRoot = "Assets/Art/Cards";

    [MenuItem("Tools/Card Config/Auto Bind Card Images")]
    public static void AutoBindCardImages()
    {
        EnsureImageFolderExists();
        Dictionary<string, Sprite> spritesByKey = BuildSpriteLookup();
        string[] cardGuids = AssetDatabase.FindAssets("t:EventCardAsset", new[] { CardAssetRoot });

        int boundCount = 0;
        int unchangedCount = 0;
        int missingCount = 0;

        foreach (string cardGuid in cardGuids)
        {
            string cardPath = AssetDatabase.GUIDToAssetPath(cardGuid);
            EventCardAsset cardAsset = AssetDatabase.LoadAssetAtPath<EventCardAsset>(cardPath);
            if (cardAsset == null)
            {
                continue;
            }

            Sprite matchedSprite = FindSpriteForCard(cardAsset, cardPath, spritesByKey);
            if (matchedSprite == null)
            {
                missingCount++;
                Debug.LogWarning($"[CardImageBinder] No image matched card '{cardAsset.ResolveCardId()}' at {cardPath}.");
                continue;
            }

            if (cardAsset.cardImage == matchedSprite)
            {
                unchangedCount++;
                continue;
            }

            Undo.RecordObject(cardAsset, "Auto Bind Card Image");
            cardAsset.cardImage = matchedSprite;
            EditorUtility.SetDirty(cardAsset);
            boundCount++;
            Debug.Log($"[CardImageBinder] Bound '{cardAsset.ResolveCardId()}' -> {AssetDatabase.GetAssetPath(matchedSprite)}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"[CardImageBinder] Finished. Bound: {boundCount}, unchanged: {unchangedCount}, missing: {missingCount}. " +
            $"Image folder: {CardImageRoot}");
    }

    private static void EnsureImageFolderExists()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art"))
        {
            AssetDatabase.CreateFolder("Assets", "Art");
        }

        if (!AssetDatabase.IsValidFolder(CardImageRoot))
        {
            AssetDatabase.CreateFolder("Assets/Art", "Cards");
        }
    }

    private static Dictionary<string, Sprite> BuildSpriteLookup()
    {
        Dictionary<string, Sprite> spritesByKey = new Dictionary<string, Sprite>();
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { CardImageRoot });

        foreach (string textureGuid in textureGuids)
        {
            string texturePath = AssetDatabase.GUIDToAssetPath(textureGuid);
            Sprite sprite = LoadAsSingleSprite(texturePath);
            if (sprite == null)
            {
                continue;
            }

            foreach (string key in BuildImageKeys(texturePath))
            {
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                if (spritesByKey.TryGetValue(key, out Sprite existingSprite))
                {
                    string existingPath = AssetDatabase.GetAssetPath(existingSprite);
                    if (ShouldPreferSprite(texturePath, existingPath))
                    {
                        spritesByKey[key] = sprite;
                    }

                    continue;
                }

                spritesByKey.Add(key, sprite);
            }
        }

        return spritesByKey;
    }

    private static Sprite LoadAsSingleSprite(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
    }

    private static Sprite FindSpriteForCard(
        EventCardAsset cardAsset,
        string cardPath,
        Dictionary<string, Sprite> spritesByKey)
    {
        foreach (string key in BuildCardKeys(cardAsset, cardPath))
        {
            if (spritesByKey.TryGetValue(key, out Sprite sprite))
            {
                return sprite;
            }
        }

        return null;
    }

    private static IEnumerable<string> BuildCardKeys(EventCardAsset cardAsset, string cardPath)
    {
        yield return NormalizeKey(cardAsset.ResolveCardId());
        yield return NormalizeKey(cardAsset.name);

        string assetName = Path.GetFileNameWithoutExtension(cardPath);
        yield return NormalizeKey(assetName);
        yield return StripKnownCardAssetPrefix(NormalizeKey(assetName));
    }

    private static IEnumerable<string> BuildImageKeys(string imagePath)
    {
        string imageName = Path.GetFileNameWithoutExtension(imagePath);
        string normalized = NormalizeKey(imageName);
        yield return normalized;

        string withoutSpritePrefix = StripPrefix(normalized, "spr_card_");
        yield return withoutSpritePrefix;
        yield return StripImageVariantSuffix(withoutSpritePrefix);
    }

    private static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = value.Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^a-z0-9]+", "_");
        normalized = Regex.Replace(normalized, @"_+", "_");
        return normalized.Trim('_');
    }

    private static string StripKnownCardAssetPrefix(string value)
    {
        value = StripPrefix(value, "ec_");
        value = StripPrefix(value, "ne_test_");
        value = StripPrefix(value, "ne_");
        return value;
    }

    private static string StripImageVariantSuffix(string value)
    {
        value = Regex.Replace(value, @"_(main|tmp)$", string.Empty);
        value = Regex.Replace(value, @"_alt\d+$", string.Empty);
        value = Regex.Replace(value, @"_v\d+$", string.Empty);
        return value;
    }

    private static string StripPrefix(string value, string prefix)
    {
        return value.StartsWith(prefix) ? value.Substring(prefix.Length) : value;
    }

    private static bool ShouldPreferSprite(string candidatePath, string existingPath)
    {
        string candidateName = Path.GetFileNameWithoutExtension(candidatePath).ToLowerInvariant();
        string existingName = Path.GetFileNameWithoutExtension(existingPath).ToLowerInvariant();

        if (candidateName.EndsWith("_main") && !existingName.EndsWith("_main"))
        {
            return true;
        }

        if (!candidateName.EndsWith("_tmp") && existingName.EndsWith("_tmp"))
        {
            return true;
        }

        return candidatePath.Length < existingPath.Length;
    }
}
