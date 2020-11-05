using Common;
using System;
using System.Collections.Generic;
using Utilities.Extensions;
using System.Linq;

namespace WidgetsCompositionRoot
{
    /// <summary>
    /// Dirty hack class
    /// </summary>
    class WidgetOrderPreservingDIInstantiationStrategyProxy : DIInstantiationStrategyProxyBase
    {
        readonly IDIContainer _container;

        public WidgetOrderPreservingDIInstantiationStrategyProxy(IDIContainer container, IDIInstantiationStrategy @base) : base(@base)
        {
            _container = container;
        }

        public override void ExecuteCoroutines(IEnumerator<ResolutionStepResult>[] coroutines)
        {
            var widgets = new List<IWidget>[coroutines.Length];
            widgets.SetAll(() => new List<IWidget>());

            for (int i = 0; i < coroutines.Length; i++)
            {
                var unwrapped = coroutines[i];
                coroutines[i] = wrap(i, unwrapped);
            }

            base.ExecuteCoroutines(coroutines);

            reregisterInCorrectOrder();

            IEnumerator<ResolutionStepResult> wrap(int coroutineIndex, IEnumerator<ResolutionStepResult> coroutine)
            {
                var old = getAllWidgets();
                while (coroutine.MoveNext())
                {
                    var current = getAllWidgets();
                    var delta = current.Where(w => !old.Contains(w)).ToArray();

                    switch (coroutine.Current)
                    {
                        case ResolutionStepResult.WAITING_FOR_SERVICE:
                            break;
                        case ResolutionStepResult.RESOLVED:
                            widgets[coroutineIndex].AddRange(delta);
                            break;

                        default:
                            throw new NotSupportedException();
                    }

                    yield return coroutine.Current;

                    old = getAllWidgets();
                }

                IEnumerable<IWidget> getAllWidgets() 
                { 
                    return _container.TryResolveAll<IWidget>()?.ToArray() ?? new IWidget[0]; 
                }
            }

            void reregisterInCorrectOrder()
            {
                var distinctWidgets = new HashSet<IWidget>();
                foreach (var coroutineWidgets in widgets)
                {
                    foreach (var widget in coroutineWidgets.Distinct())
                    {
                        distinctWidgets.Add(widget);
                    }
                }

                _container.RemoveRegistration<IWidget>();
                foreach (var widget in distinctWidgets)
                {
                    _container.Register<IWidget>(widget);
                }
            }
        }
    }
}
