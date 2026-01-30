using System.Globalization;

namespace THWTicketApp.Converters;

public class FilterColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string activeFilter && parameter is string buttonFilter)
        {
            if (activeFilter == buttonFilter)
            {
                return Application.Current?.Resources["Primary"] ?? Colors.Blue;
            }
        }
        return Application.Current?.Resources["Gray300"] ?? Colors.LightGray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
