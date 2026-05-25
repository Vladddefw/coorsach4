using Coursach4.Models;

namespace Coursach4.ViewModels;

public sealed class KioskViewModel : ObservableObject
{
    private KioskPhase _phase = KioskPhase.WaitingForFan;
    private int _queueLength;
    private int _vegetablePortions;
    private int _successCount;
    private int _rejectedByVegetablesCount;
    private int _peakQueueLength;
    private string _phaseRemaining = "0.0";
    private string _utilization = "0.0%";

    public int KioskId { get; init; }

    public KioskPhase Phase
    {
        get => _phase;
        set => SetProperty(ref _phase, value);
    }

    public int QueueLength
    {
        get => _queueLength;
        set => SetProperty(ref _queueLength, value);
    }

    public int VegetablePortions
    {
        get => _vegetablePortions;
        set => SetProperty(ref _vegetablePortions, value);
    }

    public int SuccessCount
    {
        get => _successCount;
        set => SetProperty(ref _successCount, value);
    }

    public int RejectedByVegetablesCount
    {
        get => _rejectedByVegetablesCount;
        set => SetProperty(ref _rejectedByVegetablesCount, value);
    }

    public int PeakQueueLength
    {
        get => _peakQueueLength;
        set => SetProperty(ref _peakQueueLength, value);
    }

    public string PhaseRemaining
    {
        get => _phaseRemaining;
        set => SetProperty(ref _phaseRemaining, value);
    }

    public string Utilization
    {
        get => _utilization;
        set => SetProperty(ref _utilization, value);
    }
}

