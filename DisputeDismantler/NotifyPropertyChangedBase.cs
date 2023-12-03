using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DisputeDismantler
{
    class NotifyPropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T property, T value, [CallerMemberName] string? propertyName = null, params string[] propNames)
        {
            if (EqualityComparer<T>.Default.Equals(property, value)) return false;
            property = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (propNames != null)
                for (int i = 0; i < propNames.Length; i++)
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propNames[i]));
            return true;
        }

        protected void RaisePropertyChange(string propertyname) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
    }
}
