using System;
using System.ComponentModel;

namespace DeviceBase.Models
{
    public interface ICurveInfo : INotifyPropertyChanged
    {
        string Title { get; }
        bool IsShown { get; set; }
    }

    public class CurveInfo : ICurveInfo
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Title { get; }
        public bool IsShown { get; set; }

        public CurveInfo(string title, bool isShown)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            IsShown = isShown;
        }

        public override string ToString()
        {
            return $"Title: \"{Title}\" IsShown: {IsShown}";
        }
    }
}
