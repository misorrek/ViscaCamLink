namespace ViscaCamLink.ViewModels
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class SimpleInputViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public SimpleInputViewModel(String currentInput = "")
        {
            _input = currentInput;
        }

        public String Input
        {
            get => _input;

            set
            {
                _input = value;
                NotifyPropertyChanged();
            }
        }

        private String _input;

        protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
