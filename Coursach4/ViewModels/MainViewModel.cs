using System.Collections.ObjectModel;
using System.Windows;
using System.Diagnostics;
using Coursach4.Models;
using Coursach4.Services;
using Microsoft.Win32;

namespace Coursach4.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly SimulationEngine _engine;
    private readonly HtmlExportService _htmlExport;
    private readonly List<SimulationEvent> _eventBuffer = new();

    private bool _isRunning;
    private int _rejectedByQueue;
    private double _averageWaitSeconds;
    private int _totalSuccess;
    private int _totalRejectedVegetables;

    public MainViewModel()
    {
        var config = new SimulationConfig();
        _engine = new SimulationEngine(config);
        _htmlExport = new HtmlExportService();

        for (var i = 1; i <= config.KioskCount; i++)
        {
            Kiosks.Add(new KioskViewModel
            {
                KioskId = i,
                VegetablePortions = config.InitialVegetablePortions
            });
        }

        _engine.KioskUpdated += OnKioskUpdated;
        _engine.EventOccurred += OnEventOccurred;

        StartCommand = new RelayCommand(Start, () => !IsRunning);
        StopCommand = new RelayCommand(async () => await StopAsync(), () => IsRunning);
        ResetCommand = new RelayCommand(Reset, () => !IsRunning);
        ExportCsvCommand = new RelayCommand(ExportHtml);
        ClearLogCommand = new RelayCommand(ClearVisualLog);
    }

    public ObservableCollection<KioskViewModel> Kiosks { get; } = new();
    public ObservableCollection<SimulationEvent> Events { get; } = new();

    public RelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand ResetCommand { get; }
    public RelayCommand ExportCsvCommand { get; }
    public RelayCommand ClearLogCommand { get; }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                StartCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
                ResetCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int RejectedByQueue
    {
        get => _rejectedByQueue;
        private set => SetProperty(ref _rejectedByQueue, value);
    }

    public double AverageWaitSeconds
    {
        get => _averageWaitSeconds;
        private set => SetProperty(ref _averageWaitSeconds, value);
    }

    public int TotalSuccess
    {
        get => _totalSuccess;
        private set => SetProperty(ref _totalSuccess, value);
    }

    public int TotalRejectedVegetables
    {
        get => _totalRejectedVegetables;
        private set => SetProperty(ref _totalRejectedVegetables, value);
    }

    private void Start()
    {
        _engine.Start();
        IsRunning = true;
    }

    private async Task StopAsync()
    {
        await _engine.StopAsync();
        IsRunning = false;
        RejectedByQueue = _engine.RejectedByQueueCount;
    }

    private void Reset()
    {
        Events.Clear();
        _eventBuffer.Clear();
        RejectedByQueue = 0;
        AverageWaitSeconds = 0;
        TotalSuccess = 0;
        TotalRejectedVegetables = 0;

        foreach (var kiosk in Kiosks)
        {
            kiosk.Phase = KioskPhase.WaitingForFan;
            kiosk.QueueLength = 0;
            kiosk.SuccessCount = 0;
            kiosk.RejectedByVegetablesCount = 0;
            kiosk.PeakQueueLength = 0;
            kiosk.PhaseRemaining = "0.0";
            kiosk.Utilization = "0.0%";
        }
    }

    private void ExportHtml()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Оберіть папку для HTML звіту"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var (htmlPath, jsonPath) = _htmlExport.Export(dialog.FolderName, _engine.Metrics, _eventBuffer, RejectedByQueue);
        OpenInBrowser(htmlPath);
        MessageBox.Show($"Звіт збережено:\n{htmlPath}\n{jsonPath}", "HTML експорт", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static void OpenInBrowser(string path)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

    private void ClearVisualLog()
    {
        Events.Clear();
    }

    private void OnKioskUpdated(KioskSnapshot snapshot)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var kiosk = Kiosks[snapshot.KioskId - 1];
            kiosk.Phase = snapshot.Phase;
            kiosk.QueueLength = snapshot.QueueLength;
            kiosk.VegetablePortions = snapshot.VegetablePortions;
            kiosk.SuccessCount = snapshot.SuccessCount;
            kiosk.RejectedByVegetablesCount = snapshot.RejectedByVegetablesCount;
            kiosk.PeakQueueLength = snapshot.PeakQueueLength;
            kiosk.PhaseRemaining = snapshot.RemainingSeconds.ToString("F1");
            kiosk.Utilization = snapshot.UtilizationPercent.ToString("F1") + "%";

            TotalSuccess = Kiosks.Sum(x => x.SuccessCount);
            TotalRejectedVegetables = Kiosks.Sum(x => x.RejectedByVegetablesCount);
            AverageWaitSeconds = snapshot.AverageWaitSeconds;
            RejectedByQueue = _engine.RejectedByQueueCount;
        });
    }

    private void OnEventOccurred(SimulationEvent evt)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _eventBuffer.Add(evt);
            Events.Add(evt);
        });
    }
}
