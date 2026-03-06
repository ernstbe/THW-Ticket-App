using System.Globalization;

namespace THWTicketApp.Converters;

public class DueDateColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dueDate && dueDate != DateTime.MinValue)
        {
            if (dueDate < DateTime.Now)
                return Application.Current?.Resources["Danger"] ?? Colors.Red;
            if (dueDate < DateTime.Now.AddDays(2))
                return Application.Current?.Resources["Warning"] ?? Colors.Orange;
        }
        return Application.Current?.Resources["Gray400"] ?? Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
