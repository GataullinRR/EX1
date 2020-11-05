using System;

namespace Common
{
    public class WidgetIdentity
    {
        public string Name { get; }
        /// <summary>
        /// Name of the device widget is scoped to
        /// </summary>
        public string ScopeName { get; }
        /// <summary>
        /// When this widget is active (selected in the UI) all the widgets with the same scope will be activated. If <see cref="null"/> then widget will always be active
        /// </summary>
        public object ActivationScope { get; }

        public WidgetIdentity(string name, string scopeName, object activationScope)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ScopeName = scopeName ?? throw new ArgumentNullException(nameof(scopeName));
            ActivationScope = activationScope;
        }
    }
}
