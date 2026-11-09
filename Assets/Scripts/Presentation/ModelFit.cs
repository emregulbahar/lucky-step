using UnityEngine;

namespace LuckyStep
{
    /// <summary>
    /// Places imported models at a known size regardless of their file's units or axis conversion (Story 005).
    /// The model keeps its own import rotation inside an unrotated holder; the holder is scaled to fit.
    /// </summary>
    public static class ModelFit
    {
        /// <summary>
        /// Instantiates <paramref name="prefab"/> inside a new holder under <paramref name="parent"/>. The model's
        /// bottom-centre sits at the holder origin and the holder is scaled so the model's size matches
        /// <paramref name="size"/>. Components of <paramref name="size"/> that are 0 or less are not constrained.
        /// Uniform: one scale, the smallest that fits every constrained axis. Non-uniform: per-axis scale,
        /// unconstrained axes take the mean of the constrained ones.
        /// </summary>
        public static Transform Spawn(GameObject prefab, Transform parent, string name, Vector3 size, bool uniform)
        {
            var holder = new GameObject(name).transform;
            GameObject model = Object.Instantiate(prefab, holder, false);
            Bounds b = WorldBounds(model);
            model.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
            holder.localScale = FitScale(b.size, size, uniform);
            holder.SetParent(parent, false);
            return holder;
        }

        /// <summary>World-space bounds of every renderer under <paramref name="root"/>; zero-size at its position if none.</summary>
        public static Bounds WorldBounds(GameObject root)
        {
            // Particle renderers are skipped: a world-space system (the cat puff) reports bounds far from the board
            // and pushed the camera framing out to the whole room.
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            bool any = false;
            Bounds b = new Bounds(root.transform.position, Vector3.zero);
            foreach (Renderer r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;
                if (any) b.Encapsulate(r.bounds);
                else { b = r.bounds; any = true; }
            }
            return b;
        }

        private static Vector3 FitScale(Vector3 have, Vector3 want, bool uniform)
        {
            float[] s = new float[3];
            float sum = 0f, min = float.MaxValue;
            int count = 0;
            for (int i = 0; i < 3; i++)
            {
                if (want[i] <= 0f || have[i] <= 1e-5f) { s[i] = -1f; continue; }
                s[i] = want[i] / have[i];
                sum += s[i];
                min = Mathf.Min(min, s[i]);
                count++;
            }
            if (count == 0) return Vector3.one;
            if (uniform) return Vector3.one * min;
            float mean = sum / count;
            return new Vector3(s[0] < 0f ? mean : s[0], s[1] < 0f ? mean : s[1], s[2] < 0f ? mean : s[2]);
        }
    }
}
