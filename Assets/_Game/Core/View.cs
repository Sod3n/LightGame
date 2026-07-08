using UnityEngine;

namespace LightGame.Core
{
    /// <summary>
    /// Base class for MonoBehaviour views that render a <typeparamref name="TModel"/>.
    /// Mirrors Godot's <c>class_name FooView extends Control</c> pattern:
    /// bind a data model, subscribe to <see cref="Model.PropertyChanged"/>, re-render.
    /// </summary>
    public abstract class View<TModel> : MonoBehaviour where TModel : Model
    {
        protected TModel Data { get; private set; }

        public void SetData(TModel data)
        {
            if (Data != null) Data.PropertyChanged -= OnPropertyChanged;
            Data = data;
            if (Data != null)
            {
                Data.PropertyChanged += OnPropertyChanged;
                RenderAll();
            }
        }

        protected virtual void OnDisable()
        {
            if (Data != null) Data.PropertyChanged -= OnPropertyChanged;
        }

        /// <summary>Called for each individual property change.</summary>
        protected abstract void OnPropertyChanged(string prop, object value);

        /// <summary>Called once when data is bound; render all visuals from scratch.</summary>
        protected abstract void RenderAll();
    }
}
