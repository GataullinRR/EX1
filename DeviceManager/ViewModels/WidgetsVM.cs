using Common;
using System.Collections.Generic;
using System;
using System.Linq;

namespace RUSManagingTool.ViewModels
{
    public class WidgetsVM
    {
        readonly IEnumerable<IWidget> _widgets;

        public IEnumerable<IWidget> AllWidgets => _widgets;
        public IEnumerable<IWidget> DataWidgets => _widgets.Where(w => w.Type == WidgetType.DATA);
        public IEnumerable<IWidget> ControlWidgets => _widgets.Where(w => w.Type == WidgetType.CONTROL);

        public WidgetsVM(IEnumerable<IWidget> widgets)
        {
            _widgets = widgets ?? throw new ArgumentNullException(nameof(widgets));
        }
    }
}
