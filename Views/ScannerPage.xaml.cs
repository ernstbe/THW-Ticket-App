using ZXing.Net.Maui;

namespace THWTicketApp.Views;

public partial class ScannerPage : ContentPage
{
    private string? _lastResult;

    public string? ScannedValue => _lastResult;

    public ScannerPage()
    {
        InitializeComponent();
        BarcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.All,
            AutoRotate = true,
            Multiple = false
        };
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var first = e.Results?.FirstOrDefault();
        if (first == null) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _lastResult = first.Value;
            ScanResultLabel.Text = $"Erkannt: {first.Value}";
            UseResultButton.IsEnabled = true;
            BarcodeReader.IsDetecting = false;
        });
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnUseResultClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_lastResult)) return;

        // Navigate to AddTicketPage with the scanned value as subject
        var addTicketPage = Handler?.MauiContext?.Services.GetRequiredService<AddTicketPage>();
        if (addTicketPage != null)
        {
            await Navigation.PopAsync();
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is NavigationPage nav)
            {
                addTicketPage.SetScannedSubject(_lastResult);
                await nav.PushAsync(addTicketPage);
            }
        }
    }
}
