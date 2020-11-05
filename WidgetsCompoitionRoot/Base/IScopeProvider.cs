using DeviceBase.Devices;

namespace WidgetsCompositionRoot
{
    public delegate void ScopeChangedDelegate(IRUSDevice oldScope, IRUSDevice newScope);

    public interface IScopeProvider
    {
        IRUSDevice Scope { get; }
        event ScopeChangedDelegate ScopeChanged;
    }
}
