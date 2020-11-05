using System;
using MVVMUtilities.Types;
using WPFUtilities.Types;

namespace FilesWidget
{
    public class FileRequestVM
    {
        public string Name { get; }
        public bool IsSupported { get; }
        public ActionCommand ReadAndSave { get; }
        public ActionCommand Burn { get; }

        public FileRequestVM(string name, bool isSupported, ActionCommand readAndSave, ActionCommand burn)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            IsSupported = isSupported;
            ReadAndSave = readAndSave ?? throw new ArgumentNullException(nameof(readAndSave));
            Burn = burn ?? throw new ArgumentNullException(nameof(burn));
        }
    }
}
