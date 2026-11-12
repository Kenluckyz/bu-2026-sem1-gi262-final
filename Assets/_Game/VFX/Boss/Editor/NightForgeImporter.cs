// Game Mellow VFX Lab: builds a ParticleSystem prefab for every exported sprite sheet.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NightForge
{
    class NightForgeImporter : AssetPostprocessor
    {
        const string App = "night-forge-vfx-lab";
        const float Size = 2f; // world units for the frame height

        [System.Serializable]
        class Meta
        {
            public string app;
            public int frames;
            public float fps;
            public int frame_width;
            public int frame_height;
            public int columns;
            public int rows;
            public float duration;
            public string background;
            public string style;
            public string[] sheets;
        }

        static Meta ReadMeta(string jsonPath)
        {
            if (!File.Exists(jsonPath)) return null;
            var m = JsonUtility.FromJson<Meta>(File.ReadAllText(jsonPath));
            return m != null && m.app == App ? m : null;
        }

        void OnPreprocessTexture()
        {
            if (ReadMeta(Path.GetDirectoryName(assetPath) + "/sheet.json") == null) return;
            var ti = (TextureImporter)assetImporter;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = 8192;
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported)
            {
                if (Path.GetFileName(path) != "sheet.json") continue;
                var p = path;
                // creating assets inside the import callback is unsafe; wait for it to finish
                EditorApplication.delayCall += () => Build(p);
            }
        }

        static void Build(string jsonPath)
        {
            var meta = ReadMeta(jsonPath);
            if (meta == null || meta.sheets == null) return;
            var dir = Path.GetDirectoryName(jsonPath).Replace('\\', '/');
            bool additive = (meta.background ?? "").StartsWith("black") || (meta.style ?? "glow") == "glow";
            foreach (var sheet in meta.sheets)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/" + sheet);
                if (tex == null) continue;
                var name = Path.GetFileNameWithoutExtension(sheet);
                var mat = MakeMaterial(dir + "/" + name + ".mat", tex, additive);
                var go = new GameObject(name);
                try
                {
                    Setup(go, meta, mat);
                    PrefabUtility.SaveAsPrefabAsset(go, dir + "/" + name + ".prefab");
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }
            AssetDatabase.SaveAssets();
        }

        static Material MakeMaterial(string path, Texture2D tex, bool additive)
        {
            // legacy particle shaders have no LightMode tag, so they render in Built-in and URP
            var shader = Shader.Find(additive ? "Legacy Shaders/Particles/Additive" : "Legacy Shaders/Particles/Alpha Blended")
                ?? Shader.Find("Particles/Standard Unlit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;
            mat.mainTexture = tex;
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f)); // x2 in shader = 1
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void Setup(GameObject go, Meta meta, Material mat)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            float len = Mathf.Max(0.05f, meta.duration > 0 ? meta.duration : meta.frames / Mathf.Max(1f, meta.fps));
            float aspect = meta.frame_height > 0 ? (float)meta.frame_width / meta.frame_height : 1f;

            var main = ps.main;
            main.duration = len;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = len;
            main.startSpeed = 0f;
            main.startSize3D = true;
            main.startSizeX = Size * aspect;
            main.startSizeY = Size;
            main.startSizeZ = 1f;
            main.startColor = Color.white;
            main.maxParticles = 4;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var shape = ps.shape;
            shape.enabled = false;

            int cols = Mathf.Max(1, meta.columns), rows = Mathf.Max(1, meta.rows);
            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = cols;
            sheet.numTilesY = rows;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.cycleCount = 1;
            // the last row can be part-empty: stop on the last real frame
            float end = (meta.frames - 0.5f) / (cols * rows);
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, end));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = mat;
        }
    }
}
#endif
