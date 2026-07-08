using MVVM;
using MVVM.Binders;
using R3;
using UltEvents;
using UnityEngine;

namespace Core.Client.UI.Components
{
    [RequireComponent(typeof(ISelectable))]
    public class SelectableStateBinder : GenericMethodBinder<SelectionState>
    {
        private ISelectable _selectable;

        public override void Bind(ViewModel viewModel)
        {
            base.Bind(viewModel);
            _selectable ??= GetComponent<ISelectable>();
            _selectable.SelectionStateTransition.Subscribe(Perform).AddTo(viewModel.Disposables);
        }
    }
}