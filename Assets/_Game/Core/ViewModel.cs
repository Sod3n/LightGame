using System;

namespace LightGame.Core
{
    /// <summary>
    /// Wraps a <see cref="Model"/> and adds view-only state such as
    /// optimistic predictions or transient UI flags. Mirrors Godot's
    /// <c>class_name ViewModel extends Resource</c>.
    /// </summary>
    public abstract class ViewModel<TModel> where TModel : Model
    {
        public TModel Model { get; }
        public event Action<string, object> PropertyChanged;

        protected ViewModel(TModel model)
        {
            Model = model;
            Model.PropertyChanged += OnModelChanged;
        }

        public void Dispose()
        {
            if (Model != null) Model.PropertyChanged -= OnModelChanged;
        }

        /// <summary>Default forwards model changes; override to intercept shadowed fields.</summary>
        protected virtual void OnModelChanged(string prop, object value) => Emit(prop, value);

        protected bool Set<T>(ref T backing, T value, string propertyName)
        {
            if (backing == null ? value == null : backing.Equals(value)) return false;
            backing = value;
            Emit(propertyName, value);
            return true;
        }

        protected void Emit(string prop, object value) => PropertyChanged?.Invoke(prop, value);
    }
}
