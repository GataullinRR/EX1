using System;
using System.Collections.Generic;
using System.Text;

namespace UpdateManager.ViewModels
{
    class VersionInfoVM
    {
        public string Version { get; }
        public string[] Changes { get; }
        public Dictionary<string, byte[]> Signatures { get; }

        public VersionInfoVM(string version, string[] changes, Dictionary<string, byte[]> signatures)
        {
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Changes = changes ?? throw new ArgumentNullException(nameof(changes));
            Signatures = signatures ?? throw new ArgumentNullException(nameof(signatures));
        }
    }
}
