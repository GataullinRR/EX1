using Common;
using System;
using System.Collections.Generic;

namespace WidgetsCompositionRoot
{
    class DIInstantiationStrategyProxyBase : IDIInstantiationStrategy
    {
        readonly IDIInstantiationStrategy _base;

        public DIInstantiationStrategyProxyBase(IDIInstantiationStrategy @base)
        {
            _base = @base ?? throw new ArgumentNullException(nameof(@base));
        }

        public virtual void ExecuteCoroutines(IEnumerator<ResolutionStepResult>[] coroutines)
        {
            _base.ExecuteCoroutines(coroutines);
        }
    }
}
