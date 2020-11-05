using Common;
using System.Threading.Tasks;
using Utilities.Types;

namespace CommandExports
{
    public interface ICommandHandlerWidget : IWidget
    {
        CommandHandlerWidgetSettings Settings { get; }
        new ICommandHandlerModel Model { get; }
    }
}
