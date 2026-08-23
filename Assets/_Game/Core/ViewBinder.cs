using System;
using System.Collections.Generic;
using UnityEngine;

namespace LightGame.Core
{
    /// <summary>
    /// Reconciles a list of model instances to child views inside a container.
    /// C# port of Godot's <c>ViewBinder.gd</c> from card-game-nakama.
    ///
    /// Callers keep an <c>id → view</c> map across frames; each call to <see cref="Sync"/>
    /// spawns new views, destroys vanished ones, calls <c>SetData</c> on each, and orders
    /// child transforms to match the model list order.
    ///
    /// The view type must implement <see cref="IBoundView{TModel}"/> so the utility can
    /// bind data and read model IDs generically.
    /// </summary>
    public static class ViewBinder
    {
        /// <summary>
        /// Sync <paramref name="models"/> into <paramref name="container"/>.
        /// - New model IDs → instantiate <paramref name="prefab"/> and call <see cref="IBoundView{TModel}.SetData"/>.
        /// - Existing model IDs → re-bind by calling <see cref="IBoundView{TModel}.SetData"/>.
        /// - Missing model IDs → destroy the view.
        /// - Sibling order in <paramref name="container"/> is set to match <paramref name="models"/>.
        /// </summary>
        /// <param name="views">Caller-owned map keyed by model id. Mutated in-place.</param>
        /// <param name="onCreate">Optional hook fired once per new view, right after instantiation.</param>
        public static void Sync<TModel, TView>(
            Transform container,
            IReadOnlyList<TModel> models,
            TView prefab,
            Dictionary<string, TView> views,
            Action<TView> onCreate = null)
            where TView : Component, IBoundView<TModel>
        {
            var seen = new HashSet<string>();

            for (int i = 0; i < models.Count; i++)
            {
                var m = models[i];
                string id = GetId(m);
                seen.Add(id);

                if (!views.TryGetValue(id, out var view))
                {
                    view = UnityEngine.Object.Instantiate(prefab, container);
                    views[id] = view;
                    onCreate?.Invoke(view);
                }
                view.SetData(m);
                view.transform.SetSiblingIndex(i);
            }

            // Remove views whose model is gone.
            var stale = new List<string>();
            foreach (var kv in views)
                if (!seen.Contains(kv.Key)) stale.Add(kv.Key);

            foreach (var id in stale)
            {
                var v = views[id];
                views.Remove(id);
                if (v != null) UnityEngine.Object.Destroy(v.gameObject);
            }
        }

        private static string GetId<T>(T model)
        {
            if (model is IHasId hasId) return hasId.Id;
            // Fallback: use object identity as string.
            return model?.GetHashCode().ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Contract for models synced by <see cref="ViewBinder"/>. If the model type doesn't
    /// implement this, <c>ViewBinder</c> falls back to <c>GetHashCode().ToString()</c>.
    /// </summary>
    public interface IHasId
    {
        string Id { get; }
    }

    /// <summary>
    /// Contract for view components managed by <see cref="ViewBinder"/>.
    /// Views usually inherit MonoBehaviour and implement <c>SetData</c> to (re)render.
    /// </summary>
    public interface IBoundView<TModel>
    {
        void SetData(TModel model);
    }
}
