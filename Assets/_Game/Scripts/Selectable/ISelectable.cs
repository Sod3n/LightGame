using Core.Client.UI.Components;
using R3;

namespace Core.Client.UI.Components
{
    public interface ISelectable
    {
        public ReactiveProperty<SelectionState> SelectionStateTransition { get; }
    }
}