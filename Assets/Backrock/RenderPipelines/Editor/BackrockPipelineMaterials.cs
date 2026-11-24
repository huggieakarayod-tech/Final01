// Keeps the materials of Backrock packs that include a RenderPipelines folder
// on the render pipeline the project uses.
//
// Such a pack ships each material on URP Lit in its own folders, and again in
// RenderPipelines/<Pack>_BuiltIn, _URP and _HDRP.unitypackage on Standard,
// URP Lit and HDRP Lit, under the same GUIDs. When a shipped material is on
// another pipeline's shader than the project's (or its shader is missing),
// this script copies that material's version for the project's pipeline out
// of the matching package over it, in place, so every prefab, scene and model
// that uses it follows. It runs after the pack is imported, after scripts
// reload and when the active pipeline changes.
//
// It only ever writes the materials a set ships, found by GUID wherever they
// have been moved, and only ones still on Standard, URP Lit or HDRP Lit or
// with a missing shader: a shipped material you have put on a shader of your
// own, and any material of your own, is left alone. Edits to a shipped
// material survive as long as the pipeline does not change.
//
// Tools > Backrock > Render Pipeline Materials: Match Active Pipeline does the
// same on demand; Use Built-in, URP or HDRP Materials puts every set on that
// pipeline and keeps it there (no automatic matching) until you choose Match
// Active Pipeline again.
//
// Packs that include this folder share one copy of it: the file sits at the
// same path with the same GUID in each.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Backrock.RenderPipelineMaterials
{
    [InitializeOnLoad]
    public static class PipelineMaterialSets
    {
        public const string BuiltIn = "BuiltIn";
        public const string Urp = "URP";
        public const string Hdrp = "HDRP";

        const string ErrorShader = "Hidden/InternalErrorShader";
        const string Menu = "Tools/Backrock/Render Pipeline Materials/";
        const string SeenKey = "Backrock.PipelineMaterials.Seen";

        static readonly Dictionary<string, string> LitShader = new Dictionary<string, string>
        {
            { BuiltIn, "Standard" },
            { Urp, "Universal Render Pipeline/Lit" },
            { Hdrp, "HDRP/Lit" },
        };

        static readonly Regex SetFile =
            new Regex(@"^(?<dir>.+/RenderPipelines)/(?<pack>[^/]+)_(?<rp>BuiltIn|URP|HDRP)\.unitypackage$");

        static PipelineMaterialSets()
        {
            Schedule();
            RenderPipelineManager.activeRenderPipelineTypeChanged += Schedule;
            // The event above waits for a camera to render; this catches a
            // pipeline change made by script with nothing rendering.
            EditorApplication.update += WatchPipeline;
        }

        /// <summary>The pipeline this project renders with: BuiltIn, URP, HDRP, or null for a custom one.</summary>
        public static string ActivePipeline()
        {
            var asset = GraphicsSettings.currentRenderPipeline;
            if (asset == null) return BuiltIn;
            var type = asset.GetType().FullName ?? "";
            if (type.Contains("Universal")) return Urp;
            if (type.Contains("HighDefinition")) return Hdrp;
            return null;
        }

        /// <summary>One pack's three material packages.</summary>
        public class Set
        {
            public string Pack;
            public string Folder;   // the RenderPipelines folder
            public string Package(string pipeline) => $"{Folder}/{Pack}_{pipeline}.unitypackage";
        }

        public static List<Set> Sets()
        {
            var sets = new Dictionary<string, Set>();
            foreach (var path in AssetDatabase.GetAllAssetPaths())
            {
                var m = SetFile.Match(path);
                if (!m.Success) continue;
                var key = m.Groups["dir"].Value + "|" + m.Groups["pack"].Value;
                if (!sets.ContainsKey(key))
                    sets[key] = new Set { Pack = m.Groups["pack"].Value, Folder = m.Groups["dir"].Value };
            }
            return sets.Values.OrderBy(s => s.Folder).ThenBy(s => s.Pack).ToList();
        }

        /// <summary>The materials in a set's package for one pipeline: GUID to .mat bytes.</summary>
        public static Dictionary<string, byte[]> Materials(Set set, string pipeline)
        {
            var package = set.Package(pipeline);
            var found = new Dictionary<string, byte[]>();
            if (!File.Exists(package)) return found;
            var assets = new Dictionary<string, byte[]>();
            var names = new Dictionary<string, string>();
            foreach (var (name, data) in ReadTarGz(package))
            {
                var parts = name.TrimStart('.', '/').Split('/');
                if (parts.Length != 2) continue;
                if (parts[1] == "asset") assets[parts[0]] = data;
                else if (parts[1] == "pathname") names[parts[0]] = Encoding.UTF8.GetString(data).Split('\n')[0].Trim();
            }
            foreach (var pair in assets)
                if (names.TryGetValue(pair.Key, out var path) && path.EndsWith(".mat"))
                    found[pair.Key] = pair.Value;
            return found;
        }

        /// <summary>The set's own materials that are on another pipeline's shader, or have lost theirs.</summary>
        public static List<string> Mismatched(Set set, string pipeline, IEnumerable<string> guids)
        {
            var want = LitShader[pipeline];
            var ours = new HashSet<string>(LitShader.Values) { ErrorShader };
            var wrong = new List<string>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) continue;
                var shader = material.shader == null ? ErrorShader : material.shader.name;
                if (shader != want && ours.Contains(shader)) wrong.Add(guid);
            }
            return wrong;
        }

        /// <summary>Bring every set onto the active pipeline, unless a set was chosen from the menu.</summary>
        public static int MatchAll()
        {
            var pipeline = ActivePipeline();
            if (pipeline == null || Chosen() != null) return 0;
            int written = 0;
            foreach (var set in Sets())
            {
                var materials = Materials(set, pipeline);
                var wrong = Mismatched(set, pipeline, materials.Keys);
                if (wrong.Count == 0) continue;
                // If writing did not fix a mismatch, saying so once beats
                // writing it again on every reload; a write that worked
                // leaves nothing behind, so a later mismatch is tried afresh.
                var key = "Backrock.PipelineMaterials|" + set.Package(pipeline) + "|" + string.Join(",", wrong.OrderBy(g => g));
                if (SessionState.GetBool(key, false)) continue;
                written += Write(set, pipeline, materials, wrong);
                var still = Mismatched(set, pipeline, wrong);
                if (still.Count > 0)
                {
                    SessionState.SetBool(key, true);
                    Debug.LogWarning($"Backrock: {still.Count} {set.Pack} materials are still not on {LitShader[pipeline]} " +
                                     $"after copying them from {set.Package(pipeline)}.");
                }
            }
            return written;
        }

        /// <summary>Copy the given materials' versions for <paramref name="pipeline"/> over them.</summary>
        public static int Write(Set set, string pipeline, Dictionary<string, byte[]> materials, IEnumerable<string> guids)
        {
            var paths = new List<string>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !materials.TryGetValue(guid, out var data)) continue;
                File.WriteAllBytes(path, data);
                paths.Add(path);
            }
            if (paths.Count == 0) return 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in paths) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            Debug.Log($"Backrock: {paths.Count} {set.Pack} materials set to {LitShader[pipeline]} from {set.Package(pipeline)}.");
            return paths.Count;
        }

        // ---- the menu choice, kept per project

        static string ChoiceKey => "Backrock.PipelineMaterials.Choice|" + Application.dataPath;

        /// <summary>The pipeline chosen from the menu, or null when sets follow the active pipeline.</summary>
        public static string Chosen()
        {
            var value = EditorPrefs.GetString(ChoiceKey, "");
            return value == "" ? null : value;
        }

        [MenuItem(Menu + "Match Active Pipeline")]
        static void MenuMatch()
        {
            EditorPrefs.DeleteKey(ChoiceKey);
            if (ActivePipeline() == null)
                Debug.LogWarning("Backrock: this project uses a custom render pipeline; choose a set from this menu instead.");
            else
                MatchAll();
        }

        [MenuItem(Menu + "Use Built-in Materials")]
        static void MenuBuiltIn() => Choose(BuiltIn);

        [MenuItem(Menu + "Use URP Materials")]
        static void MenuUrp() => Choose(Urp);

        [MenuItem(Menu + "Use HDRP Materials")]
        static void MenuHdrp() => Choose(Hdrp);

        static void Choose(string pipeline)
        {
            EditorPrefs.SetString(ChoiceKey, pipeline);
            var ours = new HashSet<string>(LitShader.Values) { ErrorShader };
            foreach (var set in Sets())
            {
                var materials = Materials(set, pipeline);
                // Every shipped material still on one of the three shaders,
                // matching or not: a shader of your own is left alone here too.
                var guids = materials.Keys.Where(g =>
                {
                    var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
                    return m != null && ours.Contains(m.shader == null ? ErrorShader : m.shader.name)
                           && (m.shader == null || m.shader.name != LitShader[pipeline]);
                }).ToList();
                Write(set, pipeline, materials, guids);
            }
            Debug.Log($"Backrock: materials kept on {LitShader[pipeline]} until Match Active Pipeline is chosen.");
        }

        // ---- scheduling

        static void WatchPipeline()
        {
            var now = ActivePipeline() ?? "";
            if (SessionState.GetString(SeenKey, now) == now) { SessionState.SetString(SeenKey, now); return; }
            SessionState.SetString(SeenKey, now);
            Schedule();
        }

        // Removing before adding keeps one pending check however often it is
        // asked for, without a static flag to reset.
        static void Schedule()
        {
            EditorApplication.delayCall -= Scheduled;
            EditorApplication.delayCall += Scheduled;
        }

        static void Scheduled()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                Schedule();
                return;
            }
            MatchAll();
        }

        // A pack imported over another (Lite then full) brings new materials
        // without a script reload, so imports schedule a check too.
        class Watch : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            {
                if (imported.Any(p => p.EndsWith(".mat") || SetFile.IsMatch(p))) Schedule();
            }
        }

        // ---- .unitypackage reading: a gzipped tar of <guid>/asset, <guid>/pathname, ...

        static IEnumerable<(string, byte[])> ReadTarGz(string path)
        {
            using (var file = File.OpenRead(path))
            using (var gz = new GZipStream(file, CompressionMode.Decompress))
            using (var tar = new MemoryStream())
            {
                gz.CopyTo(tar);
                var bytes = tar.ToArray();
                int at = 0;
                string longName = null;
                while (at + 512 <= bytes.Length)
                {
                    var name = Field(bytes, at, 100);
                    if (name.Length == 0) break;
                    var sizeText = Field(bytes, at + 124, 12).Trim();
                    var size = sizeText.Length == 0 ? 0 : Convert.ToInt64(sizeText, 8);
                    var type = (char)bytes[at + 156];
                    var prefix = Field(bytes, at + 345, 155);
                    if (prefix.Length > 0) name = prefix + "/" + name;
                    var start = at + 512;
                    var data = new byte[size];
                    Array.Copy(bytes, start, data, 0, size);
                    at = start + (int)((size + 511) / 512 * 512);
                    if (type == 'L') { longName = Encoding.UTF8.GetString(data).TrimEnd('\0'); continue; }
                    if (longName != null) { name = longName; longName = null; }
                    if (type == '0' || type == '\0') yield return (name, data);
                }
            }
        }

        static string Field(byte[] bytes, int at, int length)
        {
            int end = at;
            while (end < at + length && bytes[end] != 0) end++;
            return Encoding.UTF8.GetString(bytes, at, end - at);
        }
    }
}
