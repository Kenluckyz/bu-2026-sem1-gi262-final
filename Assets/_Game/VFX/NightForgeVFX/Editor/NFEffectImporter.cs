// Game Mellow VFX: when an exported effect lands in Assets/ (effect.nf.json + effect.nf.bytes +
// unity/*.shader + tex/*.png), set its texture import settings and build <folder>.prefab
// with an NFEffect wired to everything. Re-import the json to rebuild.
// Export ID: gRR42b5Kl8uc_ISDDRRV4zjbTFhNIhnJwM-V-IZvA6oY6Zm7Gfoa_AiWtGKfMKrvkJMJggKPFt_VkEKp
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NightForge.EditorTools
{
    public class NFEffectImporter : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported.Concat(moved))
                if (path.EndsWith("effect.nf.json"))
                    EditorApplication.delayCall += () => BuildPrefab(Path.GetDirectoryName(path).Replace('\\', '/'));
        }

        public static string BuildPrefab(string dir)
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(dir + "/effect.nf.json");
            var bytes = AssetDatabase.LoadAssetAtPath<TextAsset>(dir + "/effect.nf.bytes");
            if (!json || !bytes) return null;
            var j = (Dictionary<string, object>)MiniJson.Parse(json.text);

            // textures: colour space, wrap, filter and readability as the Lab used them
            var texDefs = (List<object>)j["textures"];
            var textures = new Texture2D[texDefs.Count];
            for (int i = 0; i < texDefs.Count; i++)
            {
                var d = (Dictionary<string, object>)texDefs[i];
                var p = dir + "/" + (string)d["file"];
                if (AssetImporter.GetAtPath(p) is TextureImporter ti)
                {
                    bool srgb = (bool)d["srgb"], flipY = !(d["flipY"] is bool b) || b;
                    var wrap = (string)((List<object>)d["wrap"])[0];
                    bool changed = ti.sRGBTexture != srgb || ti.isReadable != !flipY || ti.textureCompression != TextureImporterCompression.Uncompressed;
                    ti.sRGBTexture = srgb;
                    ti.isReadable = !flipY; // the player flips data textures at load
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    ti.mipmapEnabled = (bool)d["mipmaps"];
                    ti.alphaIsTransparency = false;
                    ti.wrapMode = wrap == "repeat" ? TextureWrapMode.Repeat : wrap == "mirror" ? TextureWrapMode.Mirror : TextureWrapMode.Clamp;
                    ti.filterMode = (string)d["filter"] == "nearest" ? FilterMode.Point : FilterMode.Bilinear;
                    ti.npotScale = TextureImporterNPOTScale.None;
                    if (changed) ti.SaveAndReimport();
                }
                textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            }

            var shaderPaths = Directory.Exists(dir + "/unity") ? Directory.GetFiles(dir + "/unity", "*.shader") : new string[0];
            var keys = shaderPaths.Select(p => Path.GetFileNameWithoutExtension(p)).ToArray();
            var shaders = shaderPaths.Select(p => AssetDatabase.LoadAssetAtPath<Shader>(p.Replace('\\', '/'))).ToArray();

            var go = new GameObject(Path.GetFileName(dir));
            var fx = go.AddComponent<NFEffect>();
            fx.json = json;
            fx.bytes = bytes;
            fx.textures = textures;
            fx.shaders = shaders;
            fx.shaderKeys = keys;
            fx.warpShader = Shader.Find("Hidden/NightForge/Warp");
            var prefabPath = dir + "/" + Path.GetFileName(dir) + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("Game Mellow VFX: built " + prefabPath);
            return prefabPath;
        }
    }
}
