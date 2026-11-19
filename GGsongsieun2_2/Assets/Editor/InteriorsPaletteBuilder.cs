
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;

public class InteriorsPaletteBuilder : EditorWindow
{
    private Texture2D sourceTexture;
    private GameObject palettePrefab;

    [MenuItem("Tools/Interiors Palette Builder")]
    public static void OpenWindow()
    {
        GetWindow<InteriorsPaletteBuilder>("Palette Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Interiors Auto Palette", EditorStyles.boldLabel);

        sourceTexture = (Texture2D)EditorGUILayout.ObjectField(
            "Source Atlas", sourceTexture,
            typeof(Texture2D), false);

        palettePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Target Palette", palettePrefab,
            typeof(GameObject), false);

        GUILayout.Space(10);

        GUI.enabled = sourceTexture != null && palettePrefab != null;

        if (GUILayout.Button("Build Palette"))
        {
            BuildPalette();
        }

        GUI.enabled = true;
    }

    private void BuildPalette()
    {
        string texturePath = AssetDatabase.GetAssetPath(sourceTexture);
        string palettePath = AssetDatabase.GetAssetPath(palettePrefab);

        if (string.IsNullOrEmpty(palettePath) ||
            !PrefabUtility.IsPartOfPrefabAsset(palettePrefab))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Target Palette에 Tile Palette 프리팹을 넣어 줘.",
                "확인");
            return;
        }

        string paletteName = Path.GetFileNameWithoutExtension(palettePath);

        string folder = Path.GetDirectoryName(palettePath)
            .Replace("\\", "/") + "/" + paletteName + "_Tiles";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            string parentFolder = Path.GetDirectoryName(palettePath)
                .Replace("\\", "/");

            string folderName = Path.GetFileName(folder);

            string guid = AssetDatabase.CreateFolder(
                parentFolder,
                folderName
            );

            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError("타일 폴더 생성 실패: " + folder);
                return;
            }
        }

        var sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        GameObject paletteRoot = PrefabUtility.LoadPrefabContents(palettePath);

        Tilemap tilemap = paletteRoot.GetComponentInChildren<Tilemap>();

        if (tilemap == null)
        {
            PrefabUtility.UnloadPrefabContents(paletteRoot);
            EditorUtility.DisplayDialog(
                "Error",
                "팔레트 안에서 Tilemap을 찾을 수 없어.",
                "확인");
            return;
        }

        tilemap.ClearAllTiles();

        int count = 0;

        foreach (Object asset in sprites)
        {
            Sprite sprite = asset as Sprite;

            if (sprite == null)
                continue;

            Rect rect = sprite.rect;

            if (Mathf.Abs(rect.width - 16) > 0.01f ||
                Mathf.Abs(rect.height - 16) > 0.01f)
                continue;

            int x = Mathf.RoundToInt(rect.x / 16f);
            int y = Mathf.RoundToInt(rect.y / 16f);

            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;

            string tilePath = AssetDatabase.GenerateUniqueAssetPath(
                $"{folder}/Tile_{x}_{y}.asset"
            );

            AssetDatabase.CreateAsset(tile, tilePath);

            tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            count++;
        }

        PrefabUtility.SaveAsPrefabAsset(paletteRoot, palettePath);
        PrefabUtility.UnloadPrefabContents(paletteRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "완료!",
            $"총 {count}개의 타일을 배치했어.",
            "확인");
    }
}