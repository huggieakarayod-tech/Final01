using System.Collections.Generic;
using UnityEngine;

namespace Backrock.FarmLife
{
    /// <summary>
    /// Chooses which painted coat an animal wears.
    ///
    /// Every coat of a species is a base colour map painted onto the same UV
    /// layout, so changing coat is only a texture swap: no second mesh, no second
    /// rig. The model's own material is left alone (Unity builds it from the FBX
    /// with whichever render pipeline is active), and the coat is written into
    /// its base map property: _BaseMap (URP), _BaseColorMap (HDRP) or _MainTex
    /// (Built-in), whichever the material has.
    ///
    /// In the editor the coat is shown with a MaterialPropertyBlock, so nothing
    /// is written into your project. In play mode each (material, coat) pair gets
    /// one shared material instance, so a herd of the same coat still batches.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Backrock/Animal Coat")]
    public class AnimalCoat : MonoBehaviour
    {
        [Tooltip("Every coat this animal can wear.")]
        public Texture2D[] coats = new Texture2D[0];

        [Tooltip("The coat worn: an index into Coats.")]
        public int coat;

        static readonly int MainTex = Shader.PropertyToID("_MainTex");
        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int BaseColorMap = Shader.PropertyToID("_BaseColorMap");
        static readonly Dictionary<(Material, Texture), Material> Shared =
            new Dictionary<(Material, Texture), Material>();

        Renderer target;
        Material original;
        MaterialPropertyBlock block;

        /// <summary>How many coats this animal has.</summary>
        public int CoatCount => coats == null ? 0 : coats.Length;

        /// <summary>The map of the coat worn now, or null if there are none.</summary>
        public Texture2D Current =>
            CoatCount == 0 ? null : coats[Mathf.Clamp(coat, 0, CoatCount - 1)];

        /// <summary>Wear coat <paramref name="index"/> (wrapped into range).</summary>
        public void SetCoat(int index)
        {
            if (CoatCount == 0) return;
            coat = ((index % CoatCount) + CoatCount) % CoatCount;
            Apply();
        }

        /// <summary>Wear a coat picked at random: one call per spawned animal gives a varied herd.</summary>
        public void RandomCoat()
        {
            if (CoatCount > 0) SetCoat(Random.Range(0, CoatCount));
        }

        void OnEnable() => Apply();

        void OnValidate() => Apply();

        /// <summary>Push the chosen coat onto the renderer.</summary>
        public void Apply()
        {
            if (target == null) target = GetComponentInChildren<Renderer>(true);
            if (target == null) return;
            if (original == null) original = target.sharedMaterial;
            var map = Current;
            if (original == null || map == null) return;

            if (Application.isPlaying)
            {
                target.SetPropertyBlock(null);
                if (!Shared.TryGetValue((original, map), out var material) || material == null)
                {
                    material = new Material(original) { name = original.name + " " + map.name };
                    Write(material, map);
                    Shared[(original, map)] = material;
                }
                target.sharedMaterial = material;
            }
            else
            {
                if (block == null) block = new MaterialPropertyBlock();
                target.GetPropertyBlock(block);
                if (original.HasProperty(BaseMap)) block.SetTexture(BaseMap, map);
                if (original.HasProperty(BaseColorMap)) block.SetTexture(BaseColorMap, map);
                if (original.HasProperty(MainTex)) block.SetTexture(MainTex, map);
                target.SetPropertyBlock(block);
            }
        }

        static void Write(Material material, Texture map)
        {
            if (material.HasProperty(BaseMap)) material.SetTexture(BaseMap, map);
            if (material.HasProperty(BaseColorMap)) material.SetTexture(BaseColorMap, map);
            if (material.HasProperty(MainTex)) material.SetTexture(MainTex, map);
        }
    }
}
