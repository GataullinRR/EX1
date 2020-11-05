using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Common
{
    public interface IWidget
    {
        WidgetIdentity FunctionId { get; }
        Control View { get; }
        WidgetType Type { get; }
    }
}
