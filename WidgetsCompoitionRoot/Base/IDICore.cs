using Common;
using System;

namespace WidgetsCompositionRoot
{
    interface IDICore : IDisposable
    {
        IDIInstantiationStrategy InstantiationStrategy { get; }
        IDIContainer Container { get; }
    }
}
