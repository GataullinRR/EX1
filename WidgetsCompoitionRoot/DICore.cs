using Common;
using Utilities.Types;

namespace WidgetsCompositionRoot
{
    class DICore : Disposable, IDICore
    {
        public IDIInstantiationStrategy InstantiationStrategy { get; }
        public IDIContainer Container { get; }

        public DICore()
        {
            Container = new DIContainerLoggingProxy(new DIContainer());
            InstantiationStrategy = new WidgetOrderPreservingDIInstantiationStrategyProxy(Container, new SteppedDIInstantiationStrategy());
        }

        protected override void disposeManagedState()
        {
            Container.Dispose();
        }
    }
}
