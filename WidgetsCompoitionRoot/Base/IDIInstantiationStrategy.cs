using Common;
using System.Collections.Generic;

namespace WidgetsCompositionRoot
{
    interface IDIInstantiationStrategy
    {
        void ExecuteCoroutines(IEnumerator<ResolutionStepResult>[] coroutines);
    }
}
