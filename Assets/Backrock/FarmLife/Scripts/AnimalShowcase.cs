using System.Collections.Generic;
using UnityEngine;

namespace Backrock.FarmLife
{
    /// <summary>
    /// Drives the demo scenes: every animal in the scene steps through each clip
    /// its Animator Controller holds, and with <see cref="tour"/> on, the main
    /// camera glides from species to species, framing each group in turn.
    ///
    /// It needs no input and no setup, which is all it is for. Nothing in the
    /// animals depends on it: drive the Animator from your own code instead.
    /// </summary>
    [AddComponentMenu("Backrock/Animal Showcase")]
    public class AnimalShowcase : MonoBehaviour
    {
        [Tooltip("Seconds each animal spends on a clip before the next.")]
        public float secondsPerClip = 4f;

        [Tooltip("Blend time between clips.")]
        public float blend = 0.25f;

        [Tooltip("Move the camera from one species to the next.")]
        public bool tour = true;

        [Tooltip("Seconds the camera spends on each species.")]
        public float secondsPerStop = 9f;

        class Actor
        {
            public Animator animator;
            public string[] states;
            public int index;
            public float timer;
        }

        readonly List<Actor> actors = new List<Actor>();
        readonly List<Bounds> stops = new List<Bounds>();
        Camera view;
        int stop;
        float stopTimer;
        Vector3 fromPosition, toPosition;
        Quaternion fromRotation, toRotation;
        float glide = 1f;

        void Start()
        {
            var groups = new Dictionary<string, Bounds>();
            var order = new List<string>();
#if UNITY_6000_5_OR_NEWER
            var coats = FindObjectsByType<AnimalCoat>();
#elif UNITY_2023_1_OR_NEWER
            var coats = FindObjectsByType<AnimalCoat>(FindObjectsSortMode.None);
#else
            var coats = FindObjectsOfType<AnimalCoat>();
#endif
            foreach (var coat in coats)
            {
                var animator = coat.GetComponentInChildren<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    var clips = animator.runtimeAnimatorController.animationClips;
                    var actor = new Actor { animator = animator, states = new string[clips.Length] };
                    for (int i = 0; i < clips.Length; i++) actor.states[i] = clips[i].name;
                    actor.index = Random.Range(0, Mathf.Max(1, clips.Length));
                    actor.timer = Random.Range(0f, secondsPerClip);
                    actors.Add(actor);
                    Play(actor);
                }

                // Group by species: prefab names are Species_Coat.
                var name = coat.gameObject.name;
                var key = name.Contains("_") ? name.Substring(0, name.IndexOf('_')) : name;
                var renderer = coat.GetComponentInChildren<Renderer>();
                if (renderer == null) continue;
                if (groups.TryGetValue(key, out var b)) { b.Encapsulate(renderer.bounds); groups[key] = b; }
                else { groups[key] = renderer.bounds; order.Add(key); }
            }
            order.Sort((a, b) => groups[a].center.z.CompareTo(groups[b].center.z));
            foreach (var key in order) stops.Add(groups[key]);

            view = Camera.main;
            if (tour && view != null && stops.Count > 1) Frame(0, instant: true);
        }

        void Update()
        {
            foreach (var actor in actors)
            {
                if (actor.states.Length == 0) continue;
                actor.timer += Time.deltaTime;
                if (actor.timer < secondsPerClip) continue;
                actor.timer = 0f;
                actor.index = (actor.index + 1) % actor.states.Length;
                Play(actor);
            }

            if (!tour || view == null || stops.Count < 2) return;
            stopTimer += Time.deltaTime;
            if (stopTimer >= secondsPerStop)
            {
                stopTimer = 0f;
                Frame((stop + 1) % stops.Count, instant: false);
            }
            if (glide < 1f)
            {
                glide = Mathf.Min(1f, glide + Time.deltaTime / 1.5f);
                float t = Mathf.SmoothStep(0f, 1f, glide);
                view.transform.SetPositionAndRotation(Vector3.Lerp(fromPosition, toPosition, t),
                                                      Quaternion.Slerp(fromRotation, toRotation, t));
            }
        }

        void Play(Actor actor) => actor.animator.CrossFadeInFixedTime(actor.states[actor.index], blend);

        void Frame(int index, bool instant)
        {
            stop = index;
            var b = stops[index];
            float radius = b.extents.magnitude;
            float distance = radius / Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.1f;
            var direction = new Vector3(0.35f, 0.45f, 1f).normalized;
            fromPosition = view.transform.position;
            fromRotation = view.transform.rotation;
            toPosition = b.center + direction * distance;
            toRotation = Quaternion.LookRotation(b.center - toPosition);
            glide = instant ? 1f : 0f;
            if (instant) view.transform.SetPositionAndRotation(toPosition, toRotation);
        }
    }
}
