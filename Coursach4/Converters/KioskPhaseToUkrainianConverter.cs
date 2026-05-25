using System.Globalization;
using System.Windows.Data;
using Coursach4.Models;

namespace Coursach4.Converters;

public sealed class KioskPhaseToUkrainianConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not KioskPhase phase)
        {
            return "Невідомо";
        }

        return phase switch
        {
            KioskPhase.WaitingForFan => "Очікує фаната",
            KioskPhase.IdleCutting => "Нарізає овочі",
            KioskPhase.FinishingCurrentCut => "Завершує поточну нарізку",
            KioskPhase.Serving => "Обслуговує замовлення",
            _ => "Невідомо"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

