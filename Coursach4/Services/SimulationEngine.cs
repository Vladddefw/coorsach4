using System.Collections.Concurrent;
using Coursach4.Models;

namespace Coursach4.Services;

public sealed class SimulationEngine
{
    private readonly SimulationConfig _config;
    private readonly ConcurrentQueue<Fan>[] _queues;
    private readonly SemaphoreSlim[] _queueSignals;
    private readonly object[] _kioskLocks;
    private readonly KioskWorkerState[] _states;
    private readonly KioskMetrics[] _metrics;
    private readonly Random _random = new();
    private readonly object _randomLock = new();

    private CancellationTokenSource? _cts;
    private readonly List<Task> _workers = new();
    private Task? _fanGeneratorTask;
    private int _fanId;
    private int _rejectedByQueueCount;

    public event Action<SimulationEvent>? EventOccurred;
    public event Action<KioskSnapshot>? KioskUpdated;

    public DateTime StartUtc { get; private set; }
    public int RejectedByQueueCount => Volatile.Read(ref _rejectedByQueueCount);

    public SimulationEngine(SimulationConfig config)
    {
        _config = config;
        _queues = Enumerable.Range(0, config.KioskCount).Select(_ => new ConcurrentQueue<Fan>()).ToArray();
        _queueSignals = Enumerable.Range(0, config.KioskCount).Select(_ => new SemaphoreSlim(0)).ToArray();
        _kioskLocks = Enumerable.Range(0, config.KioskCount).Select(_ => new object()).ToArray();
        _states = Enumerable.Range(0, config.KioskCount).Select(_ => new KioskWorkerState(config.InitialVegetablePortions)).ToArray();
        _metrics = Enumerable.Range(0, config.KioskCount).Select(i => new KioskMetrics { KioskId = i + 1 }).ToArray();
    }

    public IReadOnlyList<KioskMetrics> Metrics => _metrics;

    public bool IsRunning => _cts is not null;

    public void Start()
    {
        if (_cts is not null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        StartUtc = DateTime.UtcNow;
        Volatile.Write(ref _rejectedByQueueCount, 0);
        _workers.Clear();

        for (var i = 0; i < _config.KioskCount; i++)
        {
            var kioskIndex = i;
            _workers.Add(Task.Run(() => RunKioskAsync(kioskIndex, _cts.Token)));
        }

        _fanGeneratorTask = Task.Run(() => RunFanGeneratorAsync(_cts.Token));
        Emit("Система", null, "Симуляцію запущено");
    }

    public async Task StopAsync()
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();

        try
        {
            if (_fanGeneratorTask is not null)
            {
                await _fanGeneratorTask;
            }

            await Task.WhenAll(_workers);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            Emit("Система", null, "Симуляцію зупинено");
        }
    }

    private async Task RunFanGeneratorAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var delay = MinutesToDelayMs(NextRandom(_config.FanArrivalMinMinutes, _config.FanArrivalMaxMinutes + 1));
            await Task.Delay(delay, token);

            var fan = new Fan(Interlocked.Increment(ref _fanId), DateTime.UtcNow);
            var kioskIndex = NextRandom(0, _config.KioskCount);
            var queue = _queues[kioskIndex];

            if (queue.Count >= _config.QueueLimit)
            {
                Interlocked.Increment(ref _rejectedByQueueCount);
                Emit("Відмова через чергу", kioskIndex + 1, $"Фанат #{fan.Id} пішов: черга не менше за {_config.QueueLimit}");
                continue;
            }

            queue.Enqueue(fan);
            _metrics[kioskIndex].PeakQueueLength = Math.Max(_metrics[kioskIndex].PeakQueueLength, queue.Count);
            _queueSignals[kioskIndex].Release();
            Emit("Фанат у черзі", kioskIndex + 1, $"Фанат #{fan.Id} став у чергу");
            PushSnapshot(kioskIndex, KioskPhase.WaitingForFan, 0);
        }
    }

    private async Task RunKioskAsync(int kioskIndex, CancellationToken token)
    {
        var queue = _queues[kioskIndex];
        var signal = _queueSignals[kioskIndex];

        while (!token.IsCancellationRequested)
        {
            if (!queue.TryPeek(out _))
            {
                PushSnapshot(kioskIndex, KioskPhase.IdleCutting, _config.CutMinutes);
                await SimulatePhaseAsync(kioskIndex, KioskPhase.IdleCutting, _config.CutMinutes, token);
                lock (_kioskLocks[kioskIndex])
                {
                    _states[kioskIndex].VegetablePortions += 1;
                }

                Emit("Нарізка овочів", kioskIndex + 1, "Підготовлено 1 порцію овочів");
                PushSnapshot(kioskIndex, KioskPhase.WaitingForFan, 0);
                continue;
            }

            await signal.WaitAsync(token);
            if (!queue.TryDequeue(out var fan))
            {
                continue;
            }

            var serviceMinutes = NextRandom(_config.ServiceMinMinutes, _config.ServiceMaxMinutes + 1);
            PushSnapshot(kioskIndex, KioskPhase.Serving, serviceMinutes);
            await SimulatePhaseAsync(kioskIndex, KioskPhase.Serving, serviceMinutes, token);

            var hadVegetables = false;
            lock (_kioskLocks[kioskIndex])
            {
                if (_states[kioskIndex].VegetablePortions > 0)
                {
                    _states[kioskIndex].VegetablePortions -= 1;
                    hadVegetables = true;
                }
            }

            if (hadVegetables)
            {
                _metrics[kioskIndex].SuccessCount += 1;
                var wait = DateTime.UtcNow - fan.ArrivedAtUtc;
                _states[kioskIndex].TotalWait += wait;
                _states[kioskIndex].ServedFans += 1;
                Emit("Успішне замовлення", kioskIndex + 1, $"Фанат #{fan.Id} отримав замовлення");
            }
            else
            {
                _metrics[kioskIndex].RejectedByVegetablesCount += 1;
                Emit("Відмова через овочі", kioskIndex + 1, $"Фанат #{fan.Id} пішов: недостатньо овочів");
            }

            PushSnapshot(kioskIndex, KioskPhase.WaitingForFan, 0);
        }
    }

    private async Task SimulatePhaseAsync(int kioskIndex, KioskPhase phase, int phaseMinutes, CancellationToken token)
    {
        var remainingMs = MinutesToDelayMs(phaseMinutes);
        var step = 200;
        var phaseStart = DateTime.UtcNow;

        while (remainingMs > 0)
        {
            var currentStep = Math.Min(step, remainingMs);
            await Task.Delay(currentStep, token);
            remainingMs -= currentStep;
            PushSnapshot(kioskIndex, phase, remainingMs / 1000.0);
        }

        var phaseElapsed = DateTime.UtcNow - phaseStart;
        if (phase == KioskPhase.Serving)
        {
            _metrics[kioskIndex].BusyTime += phaseElapsed;
        }
    }

    private int MinutesToDelayMs(int minutes)
    {
        return Math.Max(1, minutes * _config.TimeScaleMsPerMinute);
    }

    private void PushSnapshot(int kioskIndex, KioskPhase phase, double remainingSeconds)
    {
        var state = _states[kioskIndex];
        var queueLength = _queues[kioskIndex].Count;
        var totalRunSeconds = Math.Max(0.001, (DateTime.UtcNow - StartUtc).TotalSeconds);

        KioskUpdated?.Invoke(new KioskSnapshot(
            kioskIndex + 1,
            phase,
            queueLength,
            state.VegetablePortions,
            _metrics[kioskIndex].SuccessCount,
            _metrics[kioskIndex].RejectedByVegetablesCount,
            _metrics[kioskIndex].PeakQueueLength,
            remainingSeconds,
            _metrics[kioskIndex].UtilizationPercent(totalRunSeconds),
            state.ServedFans > 0 ? state.TotalWait.TotalSeconds / state.ServedFans : 0));
    }

    private int NextRandom(int minInclusive, int maxExclusive)
    {
        lock (_randomLock)
        {
            return _random.Next(minInclusive, maxExclusive);
        }
    }

    private void Emit(string eventType, int? kioskId, string message)
    {
        var kind = eventType switch
        {
            "Успішне замовлення" => SimulationEventKind.Success,
            "Відмова через овочі" => SimulationEventKind.Rejected,
            "Відмова через чергу" => SimulationEventKind.Rejected,
            _ => SimulationEventKind.Neutral
        };

        EventOccurred?.Invoke(new SimulationEvent(
            DateTime.UtcNow,
            DateTime.UtcNow - StartUtc,
            kioskId,
            eventType,
            message,
            kind));
    }

    private sealed class KioskWorkerState
    {
        public KioskWorkerState(int vegetables)
        {
            VegetablePortions = vegetables;
        }

        public int VegetablePortions { get; set; }
        public int ServedFans { get; set; }
        public TimeSpan TotalWait { get; set; }
    }
}

public sealed record KioskSnapshot(
    int KioskId,
    KioskPhase Phase,
    int QueueLength,
    int VegetablePortions,
    int SuccessCount,
    int RejectedByVegetablesCount,
    int PeakQueueLength,
    double RemainingSeconds,
    double UtilizationPercent,
    double AverageWaitSeconds
);

