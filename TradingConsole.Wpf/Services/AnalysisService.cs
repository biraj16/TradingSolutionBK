// In TradingConsole.Wpf/Services/AnalysisService.cs

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TradingConsole.Core.Models;
using TradingConsole.DhanApi;
using TradingConsole.Wpf.ViewModels;

namespace TradingConsole.Wpf.Services
{
    #region Data Models
    public class Candle
    {
        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
        public long OpenInterest { get; set; }

        // --- NEW: Properties for per-candle VWAP calculation ---
        public decimal Vwap { get; set; }
        internal decimal CumulativePriceVolume { get; set; } = 0;
        internal long CumulativeVolume { get; set; } = 0;


        public override string ToString()
        {
            return $"T: {Timestamp:HH:mm:ss}, O: {Open}, H: {High}, L: {Low}, C: {Close}, V: {Volume}";
        }
    }

    public class EmaState
    {
        public decimal CurrentShortEma { get; set; }
        public decimal CurrentLongEma { get; set; }
    }

    internal enum PriceZone { Inside, Above, Below }
    internal class CustomLevelState
    {
        public int BreakoutCount { get; set; }
        public int BreakdownCount { get; set; }
        public PriceZone LastZone { get; set; } = PriceZone.Inside;
    }

    public class AnalysisResult : ObservableModel
    {
        private string _securityId = string.Empty;
        private string _symbol = string.Empty;
        private decimal _vwap;
        private decimal _currentIv;
        private decimal _avgIv;
        private string _ivSignal = "Neutral";
        private long _currentVolume;
        private long _avgVolume;
        private string _volumeSignal = "Neutral";
        private string _oiSignal = "N/A";
        private string _instrumentGroup = string.Empty;
        private string _underlyingGroup = string.Empty;
        private string _emaSignal1Min = "N/A";
        private string _emaSignal5Min = "N/A";
        private string _emaSignal15Min = "N/A";
        // --- NEW: Properties for the VWAP EMA signals ---
        private string _vwapEmaSignal1Min = "N/A";
        private string _vwapEmaSignal5Min = "N/A";
        private string _vwapEmaSignal15Min = "N/A";
        private string _priceVsVwapSignal = "Neutral";
        private string _priceVsCloseSignal = "Neutral";
        private string _dayRangeSignal = "Neutral";
        private string _openDriveSignal = "Neutral";
        private string _customLevelSignal = "N/A";
        private string _candleSignal1Min = "N/A";
        private string _candleSignal5Min = "N/A";

        public string CandleSignal1Min { get => _candleSignal1Min; set { if (_candleSignal1Min != value) { _candleSignal1Min = value; OnPropertyChanged(); } } }
        public string CandleSignal5Min { get => _candleSignal5Min; set { if (_candleSignal5Min != value) { _candleSignal5Min = value; OnPropertyChanged(); } } }
        public string CustomLevelSignal { get => _customLevelSignal; set { if (_customLevelSignal != value) { _customLevelSignal = value; OnPropertyChanged(); } } }
        public string SecurityId { get => _securityId; set { _securityId = value; OnPropertyChanged(); } }
        public string Symbol { get => _symbol; set { _symbol = value; OnPropertyChanged(); } }
        public decimal Vwap { get => _vwap; set { if (_vwap != value) { _vwap = value; OnPropertyChanged(); } } }
        public decimal CurrentIv { get => _currentIv; set { if (_currentIv != value) { _currentIv = value; OnPropertyChanged(); } } }
        public decimal AvgIv { get => _avgIv; set { if (_avgIv != value) { _avgIv = value; OnPropertyChanged(); } } }
        public string IvSignal { get => _ivSignal; set { if (_ivSignal != value) { _ivSignal = value; OnPropertyChanged(); } } }
        public long CurrentVolume { get => _currentVolume; set { if (_currentVolume != value) { _currentVolume = value; OnPropertyChanged(); } } }
        public long AvgVolume { get => _avgVolume; set { if (_avgVolume != value) { _avgVolume = value; OnPropertyChanged(); } } }
        public string VolumeSignal { get => _volumeSignal; set { if (_volumeSignal != value) { _volumeSignal = value; OnPropertyChanged(); } } }
        public string OiSignal { get => _oiSignal; set { if (_oiSignal != value) { _oiSignal = value; OnPropertyChanged(); } } }
        public string InstrumentGroup { get => _instrumentGroup; set { if (_instrumentGroup != value) { _instrumentGroup = value; OnPropertyChanged(); } } }
        public string UnderlyingGroup { get => _underlyingGroup; set { if (_underlyingGroup != value) { _underlyingGroup = value; OnPropertyChanged(); } } }
        public string EmaSignal1Min { get => _emaSignal1Min; set { if (_emaSignal1Min != value) { _emaSignal1Min = value; OnPropertyChanged(); } } }
        public string EmaSignal5Min { get => _emaSignal5Min; set { if (_emaSignal5Min != value) { _emaSignal5Min = value; OnPropertyChanged(); } } }
        public string EmaSignal15Min { get => _emaSignal15Min; set { if (_emaSignal15Min != value) { _emaSignal15Min = value; OnPropertyChanged(); } } }
        // --- NEW: Getters and setters for VWAP EMA signals ---
        public string VwapEmaSignal1Min { get => _vwapEmaSignal1Min; set { if (_vwapEmaSignal1Min != value) { _vwapEmaSignal1Min = value; OnPropertyChanged(); } } }
        public string VwapEmaSignal5Min { get => _vwapEmaSignal5Min; set { if (_vwapEmaSignal5Min != value) { _vwapEmaSignal5Min = value; OnPropertyChanged(); } } }
        public string VwapEmaSignal15Min { get => _vwapEmaSignal15Min; set { if (_vwapEmaSignal15Min != value) { _vwapEmaSignal15Min = value; OnPropertyChanged(); } } }
        public string PriceVsVwapSignal { get => _priceVsVwapSignal; set { if (_priceVsVwapSignal != value) { _priceVsVwapSignal = value; OnPropertyChanged(); } } }
        public string PriceVsCloseSignal { get => _priceVsCloseSignal; set { if (_priceVsCloseSignal != value) { _priceVsCloseSignal = value; OnPropertyChanged(); } } }
        public string DayRangeSignal { get => _dayRangeSignal; set { if (_dayRangeSignal != value) { _dayRangeSignal = value; OnPropertyChanged(); } } }
        public string OpenDriveSignal { get => _openDriveSignal; set { if (_openDriveSignal != value) { _openDriveSignal = value; OnPropertyChanged(); } } }

        public string FullGroupIdentifier
        {
            get
            {
                if (InstrumentGroup == "Options")
                {
                    if (UnderlyingGroup.ToUpper().Contains("NIFTY") && !UnderlyingGroup.ToUpper().Contains("BANK")) return "Nifty Options";
                    if (UnderlyingGroup.ToUpper().Contains("BANK")) return "Banknifty Options";
                    if (UnderlyingGroup.ToUpper().Contains("SENSEX")) return "Sensex Options";
                    return "Other Stock Options";
                }
                if (InstrumentGroup == "Futures")
                {
                    if (UnderlyingGroup.ToUpper().Contains("NIFTY") || UnderlyingGroup.ToUpper().Contains("BANKNIFTY") || UnderlyingGroup.ToUpper().Contains("SENSEX"))
                        return "Index Futures";
                    return "Stock Futures";
                }
                return InstrumentGroup;
            }
        }
    }
    #endregion

    public class AnalysisService : INotifyPropertyChanged
    {
        #region Parameters and State
        private readonly SettingsViewModel _settingsViewModel;
        private readonly DhanApiClient _apiClient;
        private readonly ScripMasterService _scripMasterService;
        private readonly Dictionary<string, CustomLevelState> _customLevelStates = new();
        private readonly HashSet<string> _backfilledInstruments = new HashSet<string>();
        private readonly Dictionary<string, AnalysisResult> _analysisResults = new();

        public int ShortEmaLength { get; set; } = 9;
        public int LongEmaLength { get; set; } = 21;
        private readonly int _ivHistoryLength = 15;
        private readonly decimal _ivSpikeThreshold = 0.01m;
        private readonly int _volumeHistoryLength = 12;
        private readonly double _volumeBurstMultiplier = 2.0;
        private const int MinIvHistoryForSignal = 2;
        private const int MaxCandlesToStore = 30;
        private readonly List<TimeSpan> _timeframes = new()
        {
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15)
        };
        private readonly Dictionary<string, (decimal cumulativePriceVolume, long cumulativeVolume, List<decimal> ivHistory)> _tickAnalysisState = new();
        private readonly Dictionary<string, Dictionary<TimeSpan, List<Candle>>> _multiTimeframeCandles = new();
        // --- MODIFIED: Renamed for clarity and added a new state dictionary for VWAP EMA ---
        private readonly Dictionary<string, Dictionary<TimeSpan, EmaState>> _multiTimeframePriceEmaState = new();
        private readonly Dictionary<string, Dictionary<TimeSpan, EmaState>> _multiTimeframeVwapEmaState = new();
        public event Action<AnalysisResult>? OnAnalysisUpdated;
        #endregion

        public AnalysisService(SettingsViewModel settingsViewModel, DhanApiClient apiClient, ScripMasterService scripMasterService)
        {
            _settingsViewModel = settingsViewModel;
            _apiClient = apiClient;
            _scripMasterService = scripMasterService;
        }

        public async void OnInstrumentDataReceived(DashboardInstrument instrument)
        {
            if (string.IsNullOrEmpty(instrument.SecurityId)) return;

            bool isNewInstrument = !_backfilledInstruments.Contains(instrument.SecurityId);
            if (isNewInstrument)
            {
                _tickAnalysisState[instrument.SecurityId] = (0, 0, new List<decimal>());
                _multiTimeframeCandles[instrument.SecurityId] = new Dictionary<TimeSpan, List<Candle>>();
                _multiTimeframePriceEmaState[instrument.SecurityId] = new Dictionary<TimeSpan, EmaState>();
                _multiTimeframeVwapEmaState[instrument.SecurityId] = new Dictionary<TimeSpan, EmaState>();

                foreach (var tf in _timeframes)
                {
                    _multiTimeframeCandles[instrument.SecurityId][tf] = new List<Candle>();
                    _multiTimeframePriceEmaState[instrument.SecurityId][tf] = new EmaState();
                    _multiTimeframeVwapEmaState[instrument.SecurityId][tf] = new EmaState();
                }

                if (instrument.SegmentId == 0)
                {
                    _customLevelStates[instrument.Symbol] = new CustomLevelState();
                }

                await BackfillDataIfNeededAsync(instrument);
            }

            foreach (var timeframe in _timeframes)
            {
                AggregateIntoCandle(instrument, timeframe);
            }

            RunComplexAnalysis(instrument);
        }

        private async Task BackfillDataIfNeededAsync(DashboardInstrument instrument)
        {
            _backfilledInstruments.Add(instrument.SecurityId);

            try
            {
                var scripInfo = _scripMasterService.FindBySecurityId(instrument.SecurityId);
                if (scripInfo == null) return;

                var historicalDataPoints = await _apiClient.GetIntradayHistoricalDataAsync(scripInfo);

                if (historicalDataPoints == null || !historicalDataPoints.StartTime.Any()) return;

                for (int i = 0; i < historicalDataPoints.StartTime.Count; i++)
                {
                    var candle = new Candle
                    {
                        Timestamp = DateTimeOffset.FromUnixTimeSeconds(historicalDataPoints.StartTime[i]).UtcDateTime,
                        Open = historicalDataPoints.Open[i],
                        High = historicalDataPoints.High[i],
                        Low = historicalDataPoints.Low[i],
                        Close = historicalDataPoints.Close[i],
                        Volume = historicalDataPoints.Volume[i],
                        OpenInterest = historicalDataPoints.OpenInterest.Count > i ? historicalDataPoints.OpenInterest[i] : 0,
                        // --- NEW: Use Typical Price as a proxy for historical VWAP ---
                        Vwap = (historicalDataPoints.High[i] + historicalDataPoints.Low[i] + historicalDataPoints.Close[i]) / 3
                    };

                    foreach (var timeframe in _timeframes)
                    {
                        AggregateHistoricalCandle(instrument.SecurityId, candle, timeframe);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Backfill] Failed for {instrument.Symbol}: {ex.Message}");
            }
        }

        private void AggregateHistoricalCandle(string securityId, Candle historicalCandle, TimeSpan timeframe)
        {
            var candles = _multiTimeframeCandles[securityId][timeframe];
            var candleTimestamp = new DateTime(historicalCandle.Timestamp.Ticks - (historicalCandle.Timestamp.Ticks % timeframe.Ticks), DateTimeKind.Utc);

            var existingCandle = candles.FirstOrDefault(c => c.Timestamp == candleTimestamp);

            if (existingCandle == null)
            {
                candles.Add(historicalCandle); // Add the candle directly as it's already complete
            }
            else
            {
                existingCandle.High = Math.Max(existingCandle.High, historicalCandle.High);
                existingCandle.Low = Math.Min(existingCandle.Low, historicalCandle.Low);
                existingCandle.Close = historicalCandle.Close;
                existingCandle.Volume += historicalCandle.Volume;
                existingCandle.OpenInterest = historicalCandle.OpenInterest;
                // Recalculate VWAP proxy for the aggregated historical candle
                existingCandle.Vwap = (existingCandle.High + existingCandle.Low + existingCandle.Close) / 3;
            }
        }

        private void AggregateIntoCandle(DashboardInstrument instrument, TimeSpan timeframe)
        {
            if (!_multiTimeframeCandles.ContainsKey(instrument.SecurityId) || !_multiTimeframeCandles[instrument.SecurityId].ContainsKey(timeframe))
            {
                return;
            }

            var candles = _multiTimeframeCandles[instrument.SecurityId][timeframe];
            var now = DateTime.UtcNow;
            var candleTimestamp = new DateTime(now.Ticks - (now.Ticks % timeframe.Ticks), now.Kind);

            var currentCandle = candles.LastOrDefault();

            if (currentCandle == null || currentCandle.Timestamp != candleTimestamp)
            {
                candles.Add(new Candle
                {
                    Timestamp = candleTimestamp,
                    Open = instrument.LTP,
                    High = instrument.LTP,
                    Low = instrument.LTP,
                    Close = instrument.LTP,
                    Volume = instrument.LastTradedQuantity,
                    OpenInterest = instrument.OpenInterest,
                    // --- NEW: Initialize VWAP calculation for the new candle ---
                    CumulativePriceVolume = instrument.AvgTradePrice * instrument.LastTradedQuantity,
                    CumulativeVolume = instrument.LastTradedQuantity,
                    Vwap = instrument.AvgTradePrice
                });

                if (candles.Count > MaxCandlesToStore)
                {
                    candles.RemoveAt(0);
                }
            }
            else
            {
                currentCandle.High = Math.Max(currentCandle.High, instrument.LTP);
                currentCandle.Low = Math.Min(currentCandle.Low, instrument.LTP);
                currentCandle.Close = instrument.LTP;
                currentCandle.Volume += instrument.LastTradedQuantity;
                currentCandle.OpenInterest = instrument.OpenInterest;
                // --- NEW: Update VWAP calculation for the current candle ---
                currentCandle.CumulativePriceVolume += instrument.AvgTradePrice * instrument.LastTradedQuantity;
                currentCandle.CumulativeVolume += instrument.LastTradedQuantity;
                currentCandle.Vwap = (currentCandle.CumulativeVolume > 0)
                    ? currentCandle.CumulativePriceVolume / currentCandle.CumulativeVolume
                    : currentCandle.Close; // Fallback to Close if volume is zero
            }
        }

        private void RunComplexAnalysis(DashboardInstrument instrument)
        {
            if (!_analysisResults.TryGetValue(instrument.SecurityId, out var result))
            {
                result = new AnalysisResult { SecurityId = instrument.SecurityId };
                _analysisResults[instrument.SecurityId] = result;
            }

            var tickState = _tickAnalysisState[instrument.SecurityId];
            tickState.cumulativePriceVolume += instrument.AvgTradePrice * instrument.LastTradedQuantity;
            tickState.cumulativeVolume += instrument.LastTradedQuantity;
            decimal dayVwap = (tickState.cumulativeVolume > 0) ? tickState.cumulativePriceVolume / tickState.cumulativeVolume : 0;

            if (instrument.ImpliedVolatility > 0) tickState.ivHistory.Add(instrument.ImpliedVolatility);
            if (tickState.ivHistory.Count > _ivHistoryLength) tickState.ivHistory.RemoveAt(0);
            var (avgIv, ivSignal) = CalculateIvSignal(instrument.ImpliedVolatility, tickState.ivHistory);

            _tickAnalysisState[instrument.SecurityId] = tickState;

            // --- MODIFIED: Calculate both Price EMA and VWAP EMA ---
            var priceEmaSignals = new Dictionary<TimeSpan, string>();
            var vwapEmaSignals = new Dictionary<TimeSpan, string>();
            foreach (var timeframe in _timeframes)
            {
                var candles = _multiTimeframeCandles[instrument.SecurityId].GetValueOrDefault(timeframe);
                if (candles == null || !candles.Any()) continue;
                priceEmaSignals[timeframe] = CalculateEmaSignal(instrument.SecurityId, candles, _multiTimeframePriceEmaState, useVwap: false);
                vwapEmaSignals[timeframe] = CalculateEmaSignal(instrument.SecurityId, candles, _multiTimeframeVwapEmaState, useVwap: true);
            }

            var oneMinCandles = _multiTimeframeCandles[instrument.SecurityId].GetValueOrDefault(TimeSpan.FromMinutes(1));

            var (volumeSignal, currentCandleVolume, avgCandleVolume) = ("Neutral", 0L, 0L);
            if (oneMinCandles != null && oneMinCandles.Any())
            {
                (volumeSignal, currentCandleVolume, avgCandleVolume) = CalculateVolumeSignalForTimeframe(oneMinCandles);
            }

            string oiSignal = "N/A";
            if (oneMinCandles != null && oneMinCandles.Any())
            {
                oiSignal = CalculateOiSignal(oneMinCandles);
            }

            var paSignals = CalculatePriceActionSignals(instrument, dayVwap);
            string customLevelSignal = CalculateCustomLevelSignal(instrument);

            string candleSignal1Min = "N/A";
            if (oneMinCandles != null) candleSignal1Min = RecognizeCandlestickPattern(oneMinCandles);

            string candleSignal5Min = "N/A";
            var fiveMinCandles = _multiTimeframeCandles[instrument.SecurityId].GetValueOrDefault(TimeSpan.FromMinutes(5));
            if (fiveMinCandles != null) candleSignal5Min = RecognizeCandlestickPattern(fiveMinCandles);

            result.Symbol = instrument.DisplayName;
            result.Vwap = dayVwap;
            result.CurrentIv = instrument.ImpliedVolatility;
            result.AvgIv = avgIv;
            result.IvSignal = ivSignal;
            result.CurrentVolume = currentCandleVolume;
            result.AvgVolume = avgCandleVolume;
            result.VolumeSignal = volumeSignal;
            result.OiSignal = oiSignal;
            result.CustomLevelSignal = customLevelSignal;
            result.CandleSignal1Min = candleSignal1Min;
            result.CandleSignal5Min = candleSignal5Min;
            result.EmaSignal1Min = priceEmaSignals.GetValueOrDefault(TimeSpan.FromMinutes(1), "N/A");
            result.EmaSignal5Min = priceEmaSignals.GetValueOrDefault(TimeSpan.FromMinutes(5), "N/A");
            result.EmaSignal15Min = priceEmaSignals.GetValueOrDefault(TimeSpan.FromMinutes(15), "N/A");
            // --- NEW: Populate the VWAP EMA signals ---
            result.VwapEmaSignal1Min = vwapEmaSignals.GetValueOrDefault(TimeSpan.FromMinutes(1), "N/A");
            result.VwapEmaSignal5Min = vwapEmaSignals.GetValueOrDefault(TimeSpan.FromMinutes(5), "N/A");
            result.VwapEmaSignal15Min = vwapEmaSignals.GetValueOrDefault(TimeSpan.FromMinutes(15), "N/A");
            result.InstrumentGroup = GetInstrumentGroup(instrument);
            result.UnderlyingGroup = instrument.UnderlyingSymbol;
            result.PriceVsVwapSignal = paSignals.priceVsVwap;
            result.PriceVsCloseSignal = paSignals.priceVsClose;
            result.DayRangeSignal = paSignals.dayRange;
            result.OpenDriveSignal = paSignals.openDrive;

            OnAnalysisUpdated?.Invoke(result);
        }

        /// <summary>
        /// A generic method to calculate EMA signals based on either Price or VWAP.
        /// </summary>
        private string CalculateEmaSignal(string securityId, List<Candle> candles, Dictionary<string, Dictionary<TimeSpan, EmaState>> stateDictionary, bool useVwap)
        {
            if (candles.Count < LongEmaLength) return "Building History...";

            var timeframe = candles[1].Timestamp - candles[0].Timestamp;
            var state = stateDictionary[securityId][timeframe];
            var lastCandle = candles.Last();

            // Determine the source value for the EMA calculation
            Func<Candle, decimal> sourceSelector = useVwap ? (c => c.Vwap) : (c => c.Close);
            decimal lastValue = sourceSelector(lastCandle);

            if (state.CurrentShortEma == 0 || state.CurrentLongEma == 0)
            {
                state.CurrentShortEma = candles.Skip(candles.Count - ShortEmaLength).Average(sourceSelector);
                state.CurrentLongEma = candles.Skip(candles.Count - LongEmaLength).Average(sourceSelector);
            }
            else
            {
                decimal shortMultiplier = 2.0m / (ShortEmaLength + 1);
                state.CurrentShortEma = ((lastValue - state.CurrentShortEma) * shortMultiplier) + state.CurrentShortEma;

                decimal longMultiplier = 2.0m / (LongEmaLength + 1);
                state.CurrentLongEma = ((lastValue - state.CurrentLongEma) * longMultiplier) + state.CurrentLongEma;
            }

            if (state.CurrentShortEma > state.CurrentLongEma) return "Bullish Cross";
            if (state.CurrentShortEma < state.CurrentLongEma) return "Bearish Cross";
            return "Neutral";
        }


        #region Helper Calculation Methods
        private (decimal avgIv, string ivSignal) CalculateIvSignal(decimal currentIv, List<decimal> ivHistory)
        {
            string signal = "Neutral";
            decimal avgIv = 0;
            var validIvHistory = ivHistory.Where(iv => iv > 0).ToList();

            if (validIvHistory.Any() && validIvHistory.Count >= MinIvHistoryForSignal)
            {
                avgIv = validIvHistory.Average();
                if (currentIv > (avgIv + _ivSpikeThreshold)) signal = "IV Spike Up";
                else if (currentIv < (avgIv - _ivSpikeThreshold)) signal = "IV Drop Down";
            }
            else if (currentIv > 0)
            {
                signal = "Building History...";
            }
            return (avgIv, signal);
        }

        private (string signal, long currentVolume, long averageVolume) CalculateVolumeSignalForTimeframe(List<Candle> candles)
        {
            if (!candles.Any()) return ("N/A", 0, 0);

            long currentCandleVolume = candles.Last().Volume;
            if (candles.Count < 2) return ("Building History...", currentCandleVolume, 0);

            var historyCandles = candles.Take(candles.Count - 1).ToList();
            if (historyCandles.Count > _volumeHistoryLength)
            {
                historyCandles = historyCandles.Skip(historyCandles.Count - _volumeHistoryLength).ToList();
            }

            if (!historyCandles.Any()) return ("Building History...", currentCandleVolume, 0);

            double averageVolume = historyCandles.Average(c => (double)c.Volume);
            if (averageVolume > 0 && currentCandleVolume > (averageVolume * _volumeBurstMultiplier))
            {
                return ("Volume Burst", currentCandleVolume, (long)averageVolume);
            }
            return ("Neutral", currentCandleVolume, (long)averageVolume);
        }

        private string CalculateOiSignal(List<Candle> candles)
        {
            if (candles.Count < 2) return "Building History...";

            var currentCandle = candles.Last();
            var previousCandle = candles[candles.Count - 2];

            if (previousCandle.OpenInterest == 0 || currentCandle.OpenInterest == 0)
            {
                return "Building History...";
            }

            bool isPriceUp = currentCandle.Close > previousCandle.Close;
            bool isPriceDown = currentCandle.Close < previousCandle.Close;
            bool isOiUp = currentCandle.OpenInterest > previousCandle.OpenInterest;
            bool isOiDown = currentCandle.OpenInterest < previousCandle.OpenInterest;

            if (isPriceUp && isOiUp) return "Long Buildup";
            if (isPriceUp && isOiDown) return "Short Covering";
            if (isPriceDown && isOiUp) return "Short Buildup";
            if (isPriceDown && isOiDown) return "Long Unwinding";

            return "Neutral";
        }

        private (string priceVsVwap, string priceVsClose, string dayRange, string openDrive) CalculatePriceActionSignals(DashboardInstrument instrument, decimal vwap)
        {
            string priceVsVwap = "Neutral";
            if (vwap > 0)
            {
                if (instrument.LTP > vwap) priceVsVwap = "Above VWAP";
                else if (instrument.LTP < vwap) priceVsVwap = "Below VWAP";
            }

            string priceVsClose = "Neutral";
            if (instrument.Close > 0)
            {
                if (instrument.LTP > instrument.Close) priceVsClose = "Above Close";
                else if (instrument.LTP < instrument.Close) priceVsClose = "Below Close";
            }

            string dayRange = "Neutral";
            decimal range = instrument.High - instrument.Low;
            if (range > 0)
            {
                decimal positionInDayRange = (instrument.LTP - instrument.Low) / range;
                if (positionInDayRange > 0.8m) dayRange = "Near High";
                else if (positionInDayRange < 0.2m) dayRange = "Near Low";
                else dayRange = "Mid-Range";
            }

            string openDrive = "No";
            if (instrument.Open > 0 && instrument.Low > 0 && instrument.High > 0)
            {
                if (instrument.Open == instrument.Low) openDrive = "Drive Up";
                else if (instrument.Open == instrument.High) openDrive = "Drive Down";
            }

            return (priceVsVwap, priceVsClose, dayRange, openDrive);
        }

        private string CalculateCustomLevelSignal(DashboardInstrument instrument)
        {
            if (instrument.SegmentId != 0) return "N/A";

            var levels = _settingsViewModel.GetLevelsForIndex(instrument.Symbol);
            if (levels == null) return "No Levels Set";

            if (!_customLevelStates.ContainsKey(instrument.Symbol))
            {
                _customLevelStates[instrument.Symbol] = new CustomLevelState();
            }
            var state = _customLevelStates[instrument.Symbol];

            decimal ltp = instrument.LTP;
            PriceZone currentZone;

            if (ltp > levels.NoTradeUpperBand) currentZone = PriceZone.Above;
            else if (ltp < levels.NoTradeLowerBand) currentZone = PriceZone.Below;
            else currentZone = PriceZone.Inside;

            if (currentZone != state.LastZone)
            {
                if (state.LastZone == PriceZone.Inside && currentZone == PriceZone.Above) state.BreakoutCount++;
                else if (state.LastZone == PriceZone.Inside && currentZone == PriceZone.Below) state.BreakdownCount++;
                state.LastZone = currentZone;
            }

            switch (currentZone)
            {
                case PriceZone.Inside: return "No trade zone";
                case PriceZone.Above: return $"{GetOrdinal(state.BreakoutCount)} Breakout";
                case PriceZone.Below: return $"{GetOrdinal(state.BreakdownCount)} Breakdown";
                default: return "N/A";
            }
        }

        private string RecognizeCandlestickPattern(List<Candle> candles)
        {
            if (candles.Count >= 3)
            {
                var c1 = candles.Last();
                var c2 = candles[candles.Count - 2];
                var c3 = candles[candles.Count - 3];
                string volInfo = GetVolumeConfirmation(c1, c2);

                bool isMorningStar = c3.Close < c3.Open && Math.Max(c2.Open, c2.Close) < c3.Close && c1.Close > c1.Open && c1.Close > (c3.Open + c3.Close) / 2;
                if (isMorningStar) return $"Morning Star{volInfo}";

                bool isEveningStar = c3.Close > c3.Open && Math.Min(c2.Open, c2.Close) > c3.Close && c1.Close < c1.Open && c1.Close < (c3.Open + c3.Close) / 2;
                if (isEveningStar) return $"Evening Star{volInfo}";

                bool areThreeWhiteSoldiers = c3.Close > c3.Open && c2.Close > c2.Open && c1.Close > c1.Open && c2.Open > c3.Open && c2.Close > c3.Close && c1.Open > c2.Open && c1.Close > c2.Close;
                if (areThreeWhiteSoldiers) return "Three White Soldiers";

                bool areThreeBlackCrows = c3.Close < c3.Open && c2.Close < c2.Open && c1.Close < c1.Open && c2.Open < c3.Open && c2.Close < c3.Close && c1.Open < c2.Open && c1.Close < c2.Close;
                if (areThreeBlackCrows) return "Three Black Crows";
            }

            if (candles.Count >= 2)
            {
                var current = candles.Last();
                var previous = candles[candles.Count - 2];
                string volInfo = GetVolumeConfirmation(current, previous);

                if (current.Close > current.Open && previous.Close < previous.Open && current.Close > previous.Open && current.Open < previous.Close)
                {
                    return $"Bullish Engulfing{volInfo}";
                }

                if (current.Close < current.Open && previous.Close > previous.Open && current.Open > previous.Close && current.Close < previous.Open)
                {
                    return $"Bearish Engulfing{volInfo}";
                }
            }

            if (candles.Any())
            {
                var current = candles.Last();
                string volInfo = candles.Count > 1 ? GetVolumeConfirmation(current, candles[candles.Count - 2]) : "";
                decimal bodySize = Math.Abs(current.Open - current.Close);
                decimal range = current.High - current.Low;

                if (range > 0 && bodySize / range > 0.95m)
                {
                    if (current.Close > current.Open) return $"Bullish Marubozu{volInfo}";
                    if (current.Close < current.Open) return $"Bearish Marubozu{volInfo}";
                }

                if (range > 0 && bodySize / range < 0.1m)
                {
                    return "Doji";
                }
            }

            return "N/A";
        }

        private string GetVolumeConfirmation(Candle current, Candle previous)
        {
            if (previous.Volume > 0)
            {
                decimal volChange = ((decimal)current.Volume - previous.Volume) / previous.Volume;
                if (volChange > 0.2m)
                {
                    return $" (+{volChange:P0} Vol)";
                }
            }
            return "";
        }

        private string GetOrdinal(int num)
        {
            if (num <= 0) return num.ToString();
            switch (num % 100)
            {
                case 11: case 12: case 13: return num + "th";
            }
            switch (num % 10)
            {
                case 1: return num + "st";
                case 2: return num + "nd";
                case 3: return num + "rd";
                default: return num + "th";
            }
        }

        private string GetInstrumentGroup(DashboardInstrument instrument)
        {
            if (instrument.SegmentId == 0) return "Indices";
            if (instrument.IsFuture) return "Futures";
            if (instrument.DisplayName.ToUpper().Contains("CALL") || instrument.DisplayName.ToUpper().Contains("PUT")) return "Options";
            return "Stocks";
        }

        private string GetExchangeSegmentString(int segmentId)
        {
            return segmentId switch
            {
                1 => "NSE",
                2 => "NFO",
                3 => "BSE",
                8 => "BFO",
                0 => "IDX",
                _ => "NSE"
            };
        }

        private string GetInstrumentTypeString(DashboardInstrument instrument)
        {
            if (instrument.IsFuture) return "FUT";
            if (instrument.DisplayName.ToUpper().Contains("CALL") || instrument.DisplayName.ToUpper().Contains("PUT")) return "OPT";
            if (instrument.SegmentId == 0) return "INDEX";
            return "EQUITY";
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
