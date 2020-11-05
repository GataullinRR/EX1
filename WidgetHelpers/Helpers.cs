using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace WidgetHelpers
{
    public static class Helpers
    {
        public static IEnumerator<ResolutionStepResult> Coroutine(Action instantiatingAction)
        {
            return CoroutineEnumerable(instantiatingAction).GetEnumerator();
        }

        public static IEnumerable<ResolutionStepResult> CoroutineEnumerable(Action instantiatingAction)
        {
            while (true)
            {
                yield return CommonUtils.TrySelectively<ServiceIsNotYetAwailableException>(instantiatingAction)
                    ? ResolutionStepResult.RESOLVED
                    : ResolutionStepResult.WAITING_FOR_SERVICE;
            }
        }
    }
}
