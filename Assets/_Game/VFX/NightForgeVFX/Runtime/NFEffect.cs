// Game Mellow VFX player for Unity 2022.3+ (Built-in and URP, Linear color space).
// Plays an effect exported with "Export 3D": the same meshes and shaders as the Lab.
// Drop the unzipped export into Assets/; the importer makes a prefab with this component.
//
//   var fx = Instantiate(slashPrefab, hitPoint, transform.rotation).GetComponent<NFEffect>();
//   fx.Play();                         // fx.finished fires at the end
//
// Export ID: gRR42b5Kl8uc_ISDDRRV4zjbTFhNIhnJwM-V-IZvA6oY6Zm7Gfoa_AiWtGKfMKrvkJMJggKPFt_VkEKp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NightForge
{
    [ExecuteAlways]
    public class NFEffect : MonoBehaviour
    {
        public TextAsset json;
        public TextAsset bytes;
        public Texture2D[] textures = new Texture2D[0];
        public Shader[] shaders = new Shader[0];
        public string[] shaderKeys = new string[0]; // "m3", "m3_inst"
        [Tooltip("Hidden/NightForge/Warp: screen warp in the Built-in pipeline (URP: see README)")]
        public Shader warpShader;
        [Tooltip("Light lit 3D pieces (rocks, chains) with the Lab's scene lights")]
        public bool labLights = true;

        public bool playOnEnable = true;
        public bool loop;
        public float speed = 1f;
        [Tooltip("Scrub in the editor")] public float time;
        public bool playing;
        public event Action finished;

        public float Duration => _duration;

        // three.js data uses world-space positions for these uniforms (mirror Z with the scene)
        static readonly HashSet<string> WorldVectors = new HashSet<string> { "uPinP", "uPinD", "uLightPos" };

        Dictionary<string, object> _j;
        byte[] _bin;
        float _duration, _fps;
        int _frames;
        readonly List<NodeRec> _nodes = new List<NodeRec>();
        readonly List<MatRec> _mats = new List<MatRec>();
        readonly Dictionary<string, Mesh> _meshes = new Dictionary<string, Mesh>();
        readonly Dictionary<long, Array> _cache = new Dictionary<long, Array>();
        Texture[] _tex;
        Material _warpMat;
        Vector3 _warpWorld; Vector2 _warpView; bool _warpHasWorld;
        static NFEffect s_warp; // the effect warping the screen this frame

        class MatRec { public Dictionary<string, object> d; public Dictionary<string, Material> mats = new Dictionary<string, Material>(); }
        class NodeRec
        {
            public Dictionary<string, object> d; public string kind, billboard; public Material[] mats; public Mesh mesh;
            public Matrix4x4[] inst; public Vector4[] instCol; public MaterialPropertyBlock mpb;
        }

        void OnEnable()
        {
            Build();
            Camera.onPreCull += WarpForCamera;
            RenderPipelineManager.beginCameraRendering += WarpForCamera;
            if (playOnEnable && Application.isPlaying) Play();
        }

        void OnDisable()
        {
            Camera.onPreCull -= WarpForCamera;
            RenderPipelineManager.beginCameraRendering -= WarpForCamera;
            SetWarp(null);
            Clear();
        }

        public void Play(float from = 0f) { time = from; playing = true; }
        public void Stop() => playing = false;

        void Update()
        {
            if (_j == null) return;
            if (playing && Application.isPlaying)
            {
                time += Time.deltaTime * speed;
                if (time > _duration)
                {
                    if (loop) time %= _duration;
                    else { time = _duration; playing = false; finished?.Invoke(); }
                }
            }
            Pose(time);
        }

        // ------------------------------------------------------------ loading
        void Clear()
        {
            foreach (var m in _meshes.Values) if (m) DestroyImmediate(m);
            foreach (var M in _mats) foreach (var m in M.mats.Values) if (m) DestroyImmediate(m);
            if (_warpMat) DestroyImmediate(_warpMat);
            _meshes.Clear(); _mats.Clear(); _nodes.Clear(); _cache.Clear(); _j = null;
        }

        public void Build()
        {
            Clear();
            if (!json || !bytes) return;
            _j = (Dictionary<string, object>)MiniJson.Parse(json.text);
            _bin = bytes.bytes;
            _duration = F(_j["duration"]); _fps = F(_j["fps"]); _frames = I(_j["frames"]);
            var texDefs = L(_j["textures"]);
            _tex = new Texture[texDefs.Count];
            for (int i = 0; i < texDefs.Count; i++) _tex[i] = PrepareTexture(i < textures.Length ? textures[i] : null, D(texDefs[i]));
            foreach (var m in L(_j["materials"])) _mats.Add(new MatRec { d = D(m) });
            foreach (var n in L(_j["nodes"])) _nodes.Add(MakeNode(D(n)));
            Pose(time);
        }

        Texture PrepareTexture(Texture2D t, Dictionary<string, object> d)
        {
            if (!t) return null;
            var wrapS = (string)L(d["wrap"])[0];
            t.wrapMode = wrapS == "repeat" ? TextureWrapMode.Repeat : wrapS == "mirror" ? TextureWrapMode.Mirror : TextureWrapMode.Clamp;
            // three.js flipY = false textures (data textures) are stored top row first
            if (d.TryGetValue("flipY", out var fy) && fy is bool b && !b && t.isReadable)
            {
                var px = t.GetPixels32(); int w = t.width, h = t.height;
                var o = new Color32[px.Length];
                for (int y = 0; y < h; y++) Array.Copy(px, y * w, o, (h - 1 - y) * w, w);
                var c = new Texture2D(w, h, TextureFormat.RGBA32, t.mipmapCount > 1, !(bool)d["srgb"]) { wrapMode = t.wrapMode, filterMode = t.filterMode };
                c.SetPixels32(o); c.Apply();
                return c;
            }
            return t;
        }

        Material GetMaterial(int i, bool inst)
        {
            var M = _mats[i];
            var key = "m" + i + (inst ? "_inst" : "");
            if (M.mats.TryGetValue(key, out var m)) return m;
            var idx = Array.IndexOf(shaderKeys, key);
            if (idx < 0 || !shaders[idx]) { Debug.LogWarning("NFEffect: missing shader " + key); return null; }
            m = new Material(shaders[idx]) { enableInstancing = inst, hideFlags = HideFlags.HideAndDontSave };
            m.renderQueue = m.shader.renderQueue;
            M.mats[key] = m;
            return m;
        }

        NodeRec MakeNode(Dictionary<string, object> d)
        {
            var rec = new NodeRec { d = d, kind = (string)d["kind"], billboard = d["billboard"] as string };
            if (rec.kind == "light") return rec;
            bool inst = rec.kind == "instanced";
            var mi = L(d["materials"]);
            rec.mats = new Material[mi.Count];
            for (int k = 0; k < mi.Count; k++) rec.mats[k] = GetMaterial(I(mi[k]), inst);
            if (rec.kind == "sprite") rec.mesh = SpriteMesh();
            else rec.mesh = GetMesh(FirstGeo(d), rec.kind, d.TryGetValue("type", out var ty) ? ty as string : "");
            if (inst)
            {
                var I_ = D(d["instances"]);
                int max = I(I_["max"]);
                rec.inst = new Matrix4x4[max];
                if (I_.ContainsKey("colors")) { rec.instCol = new Vector4[max]; rec.mpb = new MaterialPropertyBlock(); }
            }
            return rec;
        }

        int FirstGeo(Dictionary<string, object> d)
        {
            var g = D(d["geometry"]);
            if (g.TryGetValue("value", out var v)) return v is List<object> l ? I(l[0]) : I(v);
            foreach (var x in (int[])Read(D(g["track"]))) if (x >= 0) return x;
            return 0;
        }

        Array Read(Dictionary<string, object> acc)
        {
            long off = (long)F(acc["offset"]);
            if (_cache.TryGetValue(off, out var a)) return a;
            int count = I(acc["count"]);
            Array outArr = (string)acc["type"] == "f32" ? new float[count] : (Array)new int[count];
            Buffer.BlockCopy(_bin, (int)off, outArr, 0, count * 4);
            _cache[off] = outArr;
            return outArr;
        }

        // three.js is right-handed (+Z toward the viewer), Unity left-handed: mirror Z and
        // reverse the triangle winding so faces stay front-facing
        Mesh GetMesh(int gi, string kind, string type)
        {
            var key = gi + kind;
            if (_meshes.TryGetValue(key, out var mesh)) return mesh;
            var g = D(L(_j["geometries"])[gi]);
            var A = D(g["attributes"]);
            mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            var pos = (float[])Read(D(A["position"]));
            var vtx = new Vector3[pos.Length / 3];
            for (int k = 0; k < vtx.Length; k++) vtx[k] = new Vector3(pos[k * 3], pos[k * 3 + 1], -pos[k * 3 + 2]);
            Vector3[] nn = null; Vector2[] uu = null; Color[] cc = null;
            if (A.ContainsKey("normal"))
            {
                var n = (float[])Read(D(A["normal"]));
                nn = new Vector3[n.Length / 3];
                for (int k = 0; k < nn.Length; k++) nn[k] = new Vector3(n[k * 3], n[k * 3 + 1], -n[k * 3 + 2]);
            }
            if (A.ContainsKey("uv"))
            {
                var u = (float[])Read(D(A["uv"]));
                uu = new Vector2[u.Length / 2];
                for (int k = 0; k < uu.Length; k++) uu[k] = new Vector2(u[k * 2], u[k * 2 + 1]);
            }
            if (A.ContainsKey("color"))
            {
                var ca = D(A["color"]); var c = (float[])Read(ca); int s = I(ca["size"]);
                cc = new Color[c.Length / s];
                for (int k = 0; k < cc.Length; k++) cc[k] = new Color(c[k * s], c[k * s + 1], c[k * s + 2], s > 3 ? c[k * s + 3] : 1f);
            }
            int[] idx;
            if (g["index"] is Dictionary<string, object> ia) idx = (int[])((int[])ReadInts(ia)).Clone();
            else { idx = new int[vtx.Length]; for (int k = 0; k < idx.Length; k++) idx[k] = k; }
            var range = L(g["drawRange"]);
            int first = I(range[0]), count = I(range[1]);
            if (count >= 0 && count < idx.Length) { var sub = new int[count]; Array.Copy(idx, first, sub, 0, count); idx = sub; }
            if (kind == "points")
            {
                // Unity draws points 1px wide: four copies per point, the shader spreads them by
                // the point size using the corner in TEXCOORD1
                var corners = new[] { new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), new Vector2(0.5f, 0.5f), new Vector2(-0.5f, 0.5f) };
                int np = idx.Length;
                var pv = new Vector3[np * 4]; var pc = cc != null ? new Color[np * 4] : null; var corner = new Vector2[np * 4];
                var tri = new int[np * 6];
                for (int k = 0; k < np; k++)
                {
                    for (int c = 0; c < 4; c++) { pv[k * 4 + c] = vtx[idx[k]]; corner[k * 4 + c] = corners[c]; if (pc != null) pc[k * 4 + c] = cc[idx[k]]; }
                    int b = k * 4, t = k * 6;
                    tri[t] = b; tri[t + 1] = b + 1; tri[t + 2] = b + 2; tri[t + 3] = b; tri[t + 4] = b + 2; tri[t + 5] = b + 3;
                }
                mesh.vertices = pv;
                if (pc != null) mesh.colors = pc;
                mesh.SetUVs(1, corner);
                mesh.SetTriangles(tri, 0);
            }
            else
            {
                mesh.vertices = vtx;
                if (nn != null) mesh.normals = nn;
                if (uu != null) mesh.uv = uu;
                if (cc != null) mesh.colors = cc;
                if (kind == "line") mesh.SetIndices(idx, type == "LineSegments" ? MeshTopology.Lines : MeshTopology.LineStrip, 0);
                else
                {
                    for (int k = 0; k + 2 < idx.Length; k += 3) { var t = idx[k + 1]; idx[k + 1] = idx[k + 2]; idx[k + 2] = t; }
                    mesh.SetTriangles(idx, 0);
                }
            }
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e4f);
            _meshes[key] = mesh;
            return mesh;
        }

        Array ReadInts(Dictionary<string, object> acc)
        {
            long off = (long)F(acc["offset"]);
            if (_cache.TryGetValue(off, out var a)) return a;
            int count = I(acc["count"]);
            var arr = new int[count];
            Buffer.BlockCopy(_bin, (int)off, arr, 0, count * 4); // u32 indices fit in int
            _cache[off] = arr;
            return arr;
        }

        Mesh SpriteMesh()
        {
            if (_meshes.TryGetValue("sprite", out var m)) return m;
            m = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.bounds = new Bounds(Vector3.zero, Vector3.one * 1e4f);
            _meshes["sprite"] = m;
            return m;
        }

        // ------------------------------------------------------------ playback
        float[] At(Dictionary<string, object> s, float f, bool step = false)
        {
            if (s.TryGetValue("value", out var v)) { var l = L(v); var o = new float[l.Count]; for (int k = 0; k < o.Length; k++) o[k] = F(l[k]); return o; }
            var acc = D(s["track"]);
            var data = (string)acc["type"] == "f32" ? (float[])Read(acc) : Array.ConvertAll((int[])Read(acc), x => (float)x);
            int w = I(s["width"]), last = data.Length / w - 1;
            int i0 = Mathf.Clamp(Mathf.FloorToInt(f), 0, last), i1 = Mathf.Min(last, i0 + 1);
            float t = step ? 0 : f - i0;
            var outv = new float[w];
            for (int k = 0; k < w; k++)
            {
                float a = data[i0 * w + k], b = data[i1 * w + k];
                outv[k] = t == 0 || float.IsNaN(a) || float.IsNaN(b) ? a : a + (b - a) * t;
            }
            return outv;
        }

        static readonly Dictionary<string, string> Builtin = new Dictionary<string, string>
        {
            { "color", "nf_color" }, { "opacity", "nf_opacity" }, { "map", "nf_map" }, { "rotation", "nf_rotation" },
            { "size", "nf_size" }, { "emissive", "nf_emissive" }, { "pbr", "nf_pbr" },
        };

        void ApplyMaterial(MatRec M, float f)
        {
            if (M.mats.Count == 0) return;
            var props = D(M.d["props"]);
            var types = M.d.TryGetValue("uniformTypes", out var ut) ? D(ut) : null;
            bool sprite = (string)M.d["kind"] == "sprite";
            foreach (var kv in props)
            {
                var s = D(kv.Value);
                string name = kv.Key, glsl = null; bool arr = false;
                if (types != null && types.TryGetValue(kv.Key, out var tdef)) { var td = D(tdef); glsl = (string)td["glsl"]; arr = td["array"] is bool b && b; }
                else if (Builtin.TryGetValue(kv.Key, out var bn)) name = bn;
                else continue;
                foreach (var m in M.mats.Values)
                {
                    if (s.TryGetValue("texture", out var isTex) && isTex is bool tb && tb)
                    {
                        int ti = (int)At(s, f, true)[0];
                        m.SetTexture(name, ti >= 0 && ti < _tex.Length ? _tex[ti] : Texture2D.whiteTexture);
                        continue;
                    }
                    var v = At(s, f);
                    bool world = WorldVectors.Contains(name);
                    if (arr)
                    {
                        int size = glsl == "vec2" ? 2 : glsl == "vec3" ? 3 : glsl == "vec4" ? 4 : 1;
                        if (size == 1) { m.SetFloatArray(name, v); continue; }
                        var va = new Vector4[v.Length / size];
                        for (int k = 0; k < va.Length; k++)
                            va[k] = new Vector4(v[k * size], v[k * size + 1], size > 2 ? (world ? -1 : 1) * v[k * size + 2] : 0, size > 3 ? v[k * size + 3] : 0);
                        m.SetVectorArray(name, va);
                    }
                    else if (v.Length == 1) { if (glsl == "int") m.SetInteger(name, (int)v[0]); else m.SetFloat(name, v[0]); }
                    else if (v.Length == 16) m.SetMatrix(name, ColumnMajor(v));
                    else m.SetVector(name, new Vector4(v[0], v.Length > 1 ? v[1] : 0, v.Length > 2 ? (world ? -1 : 1) * v[2] : 0, v.Length > 3 ? v[3] : 0));
                    if (sprite) m.SetVector("nf_center", new Vector4(0.5f, 0.5f, 0, 0));
                }
            }
        }

        static Matrix4x4 ColumnMajor(float[] e)
        {
            var m = new Matrix4x4();
            for (int c = 0; c < 4; c++) for (int r = 0; r < 4; r++) m[r, c] = e[c * 4 + r];
            return m;
        }

        /// Pose and draw everything at time t (seconds since the effect started).
        public void Pose(float t)
        {
            if (_j == null) return;
            float f = Mathf.Clamp(t * _fps, 0, _frames - 1);
            foreach (var M in _mats) ApplyMaterial(M, f);
            ApplyLights(f);
            PoseWarp(f);
            var cam = Camera.main;
#if UNITY_EDITOR
            if (!Application.isPlaying && UnityEditor.SceneView.lastActiveSceneView) cam = UnityEditor.SceneView.lastActiveSceneView.camera;
#endif
            var root = transform.localToWorldMatrix;
            foreach (var rec in _nodes)
            {
                if (rec.kind == "light" || rec.mesh == null) continue;
                var s = At(D(rec.d["trs"]), f);
                if (float.IsNaN(s[0])) continue;
                if (rec.d["geometry"] is Dictionary<string, object> gd && gd.ContainsKey("track"))
                {
                    int gi = (int)At(gd, f, true)[0];
                    if (gi >= 0) rec.mesh = GetMesh(gi, rec.kind, rec.d.TryGetValue("type", out var ty) ? ty as string : "");
                }
                // facing is worked out in the file's space (three.js, right-handed), then mirrored
                var p = new Vector3(s[0], s[1], s[2]);
                var q = new Quaternion(s[3], s[4], s[5], s[6]);
                if (cam)
                {
                    var camQ = Mirror(Quaternion.Inverse(transform.rotation) * cam.transform.rotation);
                    var camP = Mirror(transform.InverseTransformPoint(cam.transform.position));
                    // nodes of a layer facing the camera are stored unfaced around the layer pivot
                    bool faced = false; Vector3 piv = Vector3.zero; Quaternion fq = Quaternion.identity;
                    Dictionary<string, object> layer = null;
                    if (rec.d.TryGetValue("layer", out var lo) && _j.TryGetValue("layers", out var lso))
                    {
                        var ls = L(lso); int li = I(lo);
                        if (li >= 0 && li < ls.Count) layer = D(ls[li]);
                    }
                    if (layer != null && layer.TryGetValue("face", out var fo))
                    {
                        var fd = D(fo); var pv = At(D(fd["pivot"]), f);
                        piv = new Vector3(pv[0], pv[1], pv[2]);
                        fq = Facing((string)fd["mode"], piv, camQ, camP, null, Vector3.up);
                        faced = true;
                    }
                    // what the facing rolls or turns with (the layer's up/right, the piece's axis)
                    Vector3[] axes = null; var axis = Vector3.up;
                    if (rec.d.ContainsKey("rel"))
                    {
                        if (rec.billboard == "cam" && layer != null && layer.TryGetValue("axes", out var ao))
                        {
                            var a = At(D(ao), f);
                            axes = new[] { fq * new Vector3(a[0], a[1], a[2]), fq * new Vector3(a[3], a[4], a[5]) };
                        }
                        if (rec.d.TryGetValue("axis", out var xo)) { var a = At(D(xo), f); axis = fq * new Vector3(a[0], a[1], a[2]).normalized; }
                    }
                    // under a group facing the camera: turn around the group's point (itself refaced)
                    bool hasPf = rec.d.TryGetValue("pivot", out var po);
                    var pf = Quaternion.identity;
                    if (hasPf)
                    {
                        var pd = D(po); var pa = At(D(pd["pos"]), f);
                        var pv = new Vector3(pa[0], pa[1], pa[2]);
                        var pg = faced ? piv + fq * (pv - piv) : pv;
                        pf = Facing((string)pd["mode"], pg, camQ, camP, axes, axis);
                        p = pg + pf * (p - pv);
                    }
                    else if (faced) p = piv + fq * (p - piv);
                    // rel: the stored rotation is relative to the facing, else replaced
                    if (rec.billboard == "cam" || rec.billboard == "y")
                    {
                        var bq = hasPf ? pf : Facing(rec.billboard, p, camQ, camP, axes, axis);
                        q = rec.d.ContainsKey("rel") ? bq * q : bq;
                    }
                    else if (faced) q = fq * q;
                }
                p = Mirror(p);
                q = Mirror(q);
                var m = root * Matrix4x4.TRS(p, q.normalized, new Vector3(s[7], s[8], s[9]));
                for (int sub = 0; sub < rec.mats.Length; sub++)
                {
                    var mat = rec.mats[sub];
                    if (!mat) continue;
                    var rp = new RenderParams(mat) { worldBounds = new Bounds(m.GetPosition(), Vector3.one * 1e4f), layer = gameObject.layer };
                    if (rec.inst != null)
                    {
                        var I_ = D(rec.d["instances"]);
                        int fi = Mathf.RoundToInt(f);
                        int cnt = ((int[])Read(D(I_["counts"])))[fi];
                        var trs = At(D(I_["trs"]), fi, true);
                        for (int k = 0; k < cnt; k++)
                        {
                            int b = k * 10;
                            rec.inst[k] = m * Matrix4x4.TRS(new Vector3(trs[b], trs[b + 1], -trs[b + 2]), new Quaternion(-trs[b + 3], -trs[b + 4], trs[b + 5], trs[b + 6]).normalized, new Vector3(trs[b + 7], trs[b + 8], trs[b + 9]));
                        }
                        if (rec.instCol != null)
                        {
                            var cols = At(D(I_["colors"]), fi, true);
                            for (int k = 0; k < cnt; k++) rec.instCol[k] = new Vector4(cols[k * 3], cols[k * 3 + 1], cols[k * 3 + 2], 1);
                            rec.mpb.SetVectorArray("_NfInstColor", rec.instCol);
                            rp.matProps = rec.mpb;
                        }
                        if (cnt > 0) Graphics.RenderMeshInstanced(rp, rec.mesh, 0, rec.inst, cnt);
                    }
                    else Graphics.RenderMesh(rp, rec.mesh, Mathf.Min(sub, rec.mesh.subMeshCount - 1), m);
                }
            }
            if (s_warp == this && GraphicsSettings.currentRenderPipeline == null && warpShader)
            {
                if (!_warpMat) _warpMat = new Material(warpShader) { hideFlags = HideFlags.HideAndDontSave };
                var rp = new RenderParams(_warpMat) { worldBounds = new Bounds(transform.position, Vector3.one * 1e5f), layer = gameObject.layer };
                Graphics.RenderMesh(rp, SpriteMesh(), 0, Matrix4x4.identity);
            }
        }

        // three.js position (effect space) -> Unity world
        Vector3 World(float x, float y, float z) => transform.TransformPoint(new Vector3(x, y, -z));

        static readonly Vector4[] s_plPos = new Vector4[4], s_plCol = new Vector4[4];

        // the file's space (three.js) <-> Unity: Z mirrored
        static Vector3 Mirror(Vector3 v) => new Vector3(v.x, v.y, -v.z);
        static Quaternion Mirror(Quaternion q) => new Quaternion(-q.x, -q.y, q.z, q.w);

        /// <summary>
        /// Rotation that faces the camera from p, all in the file's space (as the Lab does):
        /// "cam" = the camera's rotation rolled the way the layer's up (else right) shows on
        /// screen; "y" = turned around axis towards the camera.
        /// </summary>
        static Quaternion Facing(string mode, Vector3 p, Quaternion camQ, Vector3 camP, Vector3[] axes, Vector3 axis)
        {
            if (mode == "cam")
            {
                if (axes == null) return camQ;
                var inv = Quaternion.Inverse(camQ);
                var v = inv * axes[0];
                float a = v.x * v.x + v.y * v.y > 0.04f ? Mathf.Atan2(-v.x, v.y) : 0;
                if (v.x * v.x + v.y * v.y <= 0.04f) { v = inv * axes[1]; a = Mathf.Atan2(v.y, v.x); }
                return camQ * Quaternion.AngleAxis(a * Mathf.Rad2Deg, Vector3.forward);
            }
            var q0 = Quaternion.FromToRotation(Vector3.up, axis);
            var c = Quaternion.Inverse(q0) * (camP - p);
            return q0 * Quaternion.AngleAxis(Mathf.Atan2(c.x, c.z) * Mathf.Rad2Deg, Vector3.up);
        }

        // lit (standard) materials: the Lab's scene lights + the effect's point lights, world space
        void ApplyLights(float f)
        {
            Vector3 sky = Vector3.zero, ground = Vector3.zero, dirDir = Vector3.up, dirCol = Vector3.zero;
            if (labLights && _j.TryGetValue("lights", out var lo))
                foreach (var l in L(lo))
                {
                    var d = D(l); var c = L(d["color"]); var p = L(d["position"]);
                    float k = F(d["intensity"]);
                    var col = new Vector3(F(c[0]), F(c[1]), F(c[2])) * k;
                    var type = (string)d["type"];
                    if (type == "HemisphereLight")
                    {
                        var g = L(d["groundColor"]);
                        sky += col; ground += new Vector3(F(g[0]), F(g[1]), F(g[2])) * k;
                    }
                    else if (type == "AmbientLight") { sky += col; ground += col; }
                    else if (type == "DirectionalLight")
                    {
                        dirDir = transform.TransformDirection(new Vector3(F(p[0]), F(p[1]), -F(p[2])).normalized);
                        dirCol = col;
                    }
                }
            int n = 0;
            foreach (var rec in _nodes)
            {
                if (rec.kind != "light" || n == 4) continue;
                var s = At(D(rec.d["trs"]), f);
                if (float.IsNaN(s[0])) continue;
                var v = At(D(rec.d["light"]), f); // r, g, b, intensity, distance, decay
                s_plPos[n] = World(s[0], s[1], s[2]); s_plPos[n].w = v[4] * transform.lossyScale.x;
                s_plCol[n] = new Vector4(v[0] * v[3], v[1] * v[3], v[2] * v[3], v[5]);
                n++;
            }
            for (int k = n; k < 4; k++) { s_plPos[k] = Vector4.zero; s_plCol[k] = new Vector4(0, 0, 0, 2); }
            foreach (var M in _mats)
            {
                if ((string)M.d["kind"] != "standard") continue;
                foreach (var m in M.mats.Values)
                {
                    m.SetVector("nf_hemiSky", sky); m.SetVector("nf_hemiGround", ground);
                    m.SetVector("nf_dirDir", dirDir); m.SetVector("nf_dirCol", dirCol);
                    m.SetVectorArray("nf_plPos", s_plPos); m.SetVectorArray("nf_plCol", s_plCol);
                }
            }
        }

        // screen warp (Arcane, shields): globals read by NFWarp.shader / NFWarpURP.shader
        void PoseWarp(float f)
        {
            if (!(_j.TryGetValue("post", out var po) && D(po).TryGetValue("warp", out var wo) && wo is Dictionary<string, object> ws))
            {
                if (s_warp == this) SetWarp(null);
                return;
            }
            // [cx, cy, age0, age1, heat, heatR, time, x, y, z]; ages step on and off (-1 = none)
            var v = At(ws, f); var st = At(ws, f, true);
            for (int k = 2; k < 4; k++) if (st[k] < 0 || v[k] < st[k]) v[k] = st[k];
            bool on = v[2] >= 0 || v[3] >= 0 || v[4] > 0;
            if (!on) { if (s_warp == this) SetWarp(null); return; }
            _warpView = new Vector2(v[0], v[1]);
            _warpHasWorld = v.Length >= 10 && !float.IsNaN(v[7]);
            if (_warpHasWorld) _warpWorld = World(v[7], v[8], v[9]);
            Shader.SetGlobalVector("_NfWarpA", new Vector4(v[2], v[3], v[4], v[5]));
            SetWarp(this, v[6]);
        }

        static void SetWarp(NFEffect fx, float time = 0)
        {
            if (fx == null && s_warp != null) Shader.SetGlobalVector("_NfWarpB", Vector4.zero);
            s_warp = fx;
            if (fx != null) { var b = Shader.GetGlobalVector("_NfWarpB"); Shader.SetGlobalVector("_NfWarpB", new Vector4(b.x, b.y, time, 1)); }
        }

        // the warp centre follows the effect on screen, per camera
        void WarpForCamera(ScriptableRenderContext ctx, Camera cam) => WarpForCamera(cam);
        void WarpForCamera(Camera cam)
        {
            if (s_warp != this || !cam) return;
            var c = _warpHasWorld ? (Vector2)cam.WorldToViewportPoint(_warpWorld) : _warpView;
            var b = Shader.GetGlobalVector("_NfWarpB");
            Shader.SetGlobalVector("_NfWarpB", new Vector4(c.x, c.y, b.z, b.w));
        }

        static Dictionary<string, object> D(object o) => (Dictionary<string, object>)o;
        static List<object> L(object o) => (List<object>)o;
        static float F(object o) => o == null ? 0 : Convert.ToSingle(o);
        static int I(object o) => o == null ? 0 : Convert.ToInt32(o);
    }

    /// Small JSON reader (objects, arrays, numbers, strings, true/false/null).
    public static class MiniJson
    {
        public static object Parse(string s) { int i = 0; return Value(s, ref i); }

        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static object Value(string s, ref int i)
        {
            Ws(s, ref i);
            char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>(); i++;
                Ws(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true)
                {
                    Ws(s, ref i); var k = Str(s, ref i); Ws(s, ref i); i++; // :
                    d[k] = Value(s, ref i); Ws(s, ref i);
                    if (s[i++] == '}') return d;
                }
            }
            if (c == '[')
            {
                var l = new List<object>(); i++;
                Ws(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Value(s, ref i)); Ws(s, ref i);
                    if (s[i++] == ']') return l;
                }
            }
            if (c == '"') return Str(s, ref i);
            if (s.Substring(i).StartsWith("true")) { i += 4; return true; }
            if (s.Substring(i, Math.Min(5, s.Length - i)) == "false") { i += 5; return false; }
            if (s.Substring(i, Math.Min(4, s.Length - i)) == "null") { i += 4; return null; }
            int st = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(st, i - st), System.Globalization.CultureInfo.InvariantCulture);
        }

        static string Str(string s, ref int i)
        {
            var sb = new System.Text.StringBuilder(); i++;
            while (s[i] != '"')
            {
                if (s[i] == '\\')
                {
                    i++;
                    char e = s[i];
                    if (e == 'n') sb.Append('\n'); else if (e == 't') sb.Append('\t'); else if (e == 'r') sb.Append('\r');
                    else if (e == 'u') { sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; }
                    else sb.Append(e);
                }
                else sb.Append(s[i]);
                i++;
            }
            i++;
            return sb.ToString();
        }
    }
}
