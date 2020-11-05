using System;
using System.ComponentModel;

namespace Calibrators.Models
{
    enum ProgressState
    {
        WORKING = 0,
        ABORTED,
        FAILED
    }

    interface IRichProgress : IProgress<double>, INotifyPropertyChanged
    {
        double ProgressInPercents { get; }
        double Progress { get; }
        double MaxProgress { get; set; }

        ProgressState State { get; set; }
    }
}
