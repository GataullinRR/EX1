using Common;
using System;
using System.Collections.Generic;

namespace WidgetsCompositionRoot
{
    class SteppedDIInstantiationStrategy : IDIInstantiationStrategy
    {
        const int MAX_RESOLUTION_STEPS = 10;

        public void ExecuteCoroutines(IEnumerator<ResolutionStepResult>[] coroutines)
        {
            var queue = new List<IEnumerator<ResolutionStepResult>>(coroutines);
            for (int i = 0; i < MAX_RESOLUTION_STEPS; i++)
            {
                Logger.LogInfo(null, $"Шаг разрешения: {i} незавершенных процессов: {queue.Count}");

                for (int k = 0; k < queue.Count; k++)
                {
                    var coroutine = queue[k];
                    coroutine.MoveNext();
                    switch (coroutine.Current)
                    {
                        case ResolutionStepResult.WAITING_FOR_SERVICE:
                            break;
                        case ResolutionStepResult.RESOLVED:
                            queue.RemoveAt(k);
                            k--;
                            break;

                        default:
                            throw new NotSupportedException();
                    }
                }

                if (queue.Count == 0)
                {
                    Logger.LogOK(null, $"Разрешение завершено");

                    break;
                }
            }

            if (queue.Count > 0)
            {
                throw new InvalidOperationException("Could not resolve services in a given amount of steps. See resolution trace.");
            }
        }
    }
}
