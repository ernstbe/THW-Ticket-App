using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace THWTicketApp.ViewModels
{
    public class MainPageViewModel : BindableObject
    {
        private int _count;
        public int Count
        {
            get => _count;
            set { _count = value; OnPropertyChanged(); }
        }

        public ICommand CounterCommand { get; }

        public string CounterText => Count == 1 ? $"Clicked {Count} time" : $"Clicked {Count} times";

        public MainPageViewModel()
        {
            CounterCommand = new Command(OnCounterClicked);
        }

        private void OnCounterClicked()
        {
            Count++;
            OnPropertyChanged(nameof(CounterText));
        }
    }
}
