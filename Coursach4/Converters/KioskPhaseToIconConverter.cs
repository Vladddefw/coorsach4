using System.Globalization;
using System.Windows.Data;
using Coursach4.Models;

namespace Coursach4.Converters;

public sealed class KioskPhaseToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not KioskPhase phase)
        {
            return "Assets/Icons/waiting.png";
        }

        return phase switch
        {
            KioskPhase.WaitingForFan => "Assets/Icons/waiting.png",
            KioskPhase.IdleCutting => "Assets/Icons/cutting.png",
            KioskPhase.FinishingCurrentCut => "Assets/Icons/finishing.png",
            KioskPhase.Serving => "Assets/Icons/serving.png",
            _ => "Assets/Icons/waiting.png"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

