using CommunityToolkit.Mvvm.Messaging;
using THWTicketApp.Messages;
using ZXing.Net.Maui;

namespace THWTicketApp.Views;

public partial class ScannerPage : ContentPage
{
    private string? _lastResult;
    private string? _lastFormat;
    private int _scanCount;

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
            _lastFormat = first.Format.ToString();
            _scanCount++;

            ScanResultLabel.Text = first.Value;
            ScanTypeLabel.Text = $"Format: {_lastFormat}";
            ResultFrame.IsVisible = true;
            ActionButtons.IsVisible = true;
            BarcodeReader.IsDetecting = false;

            if (_scanCount > 1)
            {
                ScanHistoryLabel.Text = $"{_scanCount} Codes gescannt in dieser Sitzung";
                ScanHistoryLabel.IsVisible = true;
            }
        });
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnCreateTicketClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_lastResult)) return;

        var addTicketPage = Handler?.MauiContext?.Services.GetRequiredService<AddTicketPage>();
        if (addTicketPage != null)
        {
            await Navigation.PopAsync();
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Page is NavigationPage nav)
            {
                var subject = $"[Scan: {_lastFormat}] {_lastResult}";
                addTicketPage.SetScannedSubject(subject);
                await nav.PushAsync(addTicketPage);
            }
        }
    }

    private async void OnSearchTicketClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_lastResult)) return;

        // Navigate back and search for the scanned value
        await Navigation.PopAsync();

        WeakReferenceMessenger.Default.Send(new SearchTicketMessage(_lastResult));
    }

    private void OnRescanClicked(object? sender, EventArgs e)
    {
        ResultFrame.IsVisible = false;
        ActionButtons.IsVisible = false;
        BarcodeReader.IsDetecting = true;
    }
}
