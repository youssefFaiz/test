// AllinOneStrategy.cs
// NinjaTrader Strategy with Multiple Entry Types and Combined Entry System

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class AllinOneStrategy : Strategy
    {
        #region Variables

        // ===== 001. General Strategy Settings =====
        private int trade1Quantity = 1;
        private int trade2Quantity = 0;
        private int trade3Quantity = 0;
        private int trade4Quantity = 0;
        private bool allowMultipleTrades = false;
        private int maxPositionsAllowed = 1;

        // ===== 005. BOS Entry Settings =====
        private bool useBOSEntry = false;
        private bool useBOSDisplayOnly = false;
        private bool useBOSCombinedEntry = false;
        private int bosSignalOrder = 0; // 0=Any, 1=1st, 2=2nd, etc.
        private int bosPivotLeftBars = 5;
        private int bosPivotRightBars = 5;
        private int bosMaxBarsToBreak = 30;
        private int bosMaxBarsBetween = 10;
        private int bosEntryPlusTicks = 0;
        private System.Windows.Media.Brush bosBullishColor = System.Windows.Media.Brushes.Green;
        private System.Windows.Media.Brush bosBearishColor = System.Windows.Media.Brushes.Red;

        // Track swing points
        private List<SwingPoint> swingHighs = new List<SwingPoint>();
        private List<SwingPoint> swingLows = new List<SwingPoint>();

        // ===== 006. CISD Entry Settings =====
        private bool useCISDEntry = false;
        private bool useCISDDisplayOnly = false;
        private bool useCISDCombinedEntry = false;
        private int cisdSignalOrder = 0; // 0=Any, 1=1st, 2=2nd, etc.
        private int cisdMaxBarsBetween = 10;
        private int cisdEntryPlusTicks = 0;
        private Brush cisdBullishColor = Brushes.Green;
        private Brush cisdBearishColor = Brushes.Red;

        // CISD Structure Tracking
        private double cisdStructureHigh = 0;
        private double cisdStructureLow = 0;
        private bool detectingBullishPullback = false;
        private bool detectingBearishPullback = false;
        private double candidateHighPrice = double.NaN;
        private double candidateLowPrice = double.NaN;
        private int bullishCandidateBar = -1;
        private int bearishCandidateBar = -1;

        // ===== 011. Time Session Entry Settings =====
        private bool useCustomEntryTimeFilter = false;
        private int sessionStartHour = 9;
        private int sessionStartMinute = 30;
        private int sessionEndHour = 16;
        private int sessionEndMinute = 0;

        // ===== 021. R:R Risk Reward Settings =====
        private bool useBOSStopLossRR = true;
        private int bosStopLossPlusTicks = 2;

        private bool useCISDStopLossRR = true;
        private int cisdStopLossPlusTicks = 2;

        private bool useFVGStopLossRR = true;
        private int fvgStopLossPlusTicks = 2;

        private bool useSweepSwingStopLossRR = true;
        private int sweepSwingStopLossPlusTicks = 2;

        // ===== 022. PreSet RR Ratios =====
        // RR 1:1
        private bool useRR_1_1_Trade1 = false;
        private bool useRR_1_1_Trade2 = false;
        private bool useRR_1_1_Trade3 = false;
        private bool useRR_1_1_Trade4 = false;

        // RR 1:1.5
        private bool useRR_1_1_5_Trade1 = false;
        private bool useRR_1_1_5_Trade2 = false;
        private bool useRR_1_1_5_Trade3 = false;
        private bool useRR_1_1_5_Trade4 = false;

        // RR 1:2
        private bool useRR_1_2_Trade1 = true;
        private bool useRR_1_2_Trade2 = false;
        private bool useRR_1_2_Trade3 = false;
        private bool useRR_1_2_Trade4 = false;

        // RR 1:2.5
        private bool useRR_1_2_5_Trade1 = false;
        private bool useRR_1_2_5_Trade2 = false;
        private bool useRR_1_2_5_Trade3 = false;
        private bool useRR_1_2_5_Trade4 = false;

        // RR 1:3
        private bool useRR_1_3_Trade1 = false;
        private bool useRR_1_3_Trade2 = false;
        private bool useRR_1_3_Trade3 = false;
        private bool useRR_1_3_Trade4 = false;

        // RR 1:3.5
        private bool useRR_1_3_5_Trade1 = false;
        private bool useRR_1_3_5_Trade2 = false;
        private bool useRR_1_3_5_Trade3 = false;
        private bool useRR_1_3_5_Trade4 = false;

        // RR 1:4
        private bool useRR_1_4_Trade1 = false;
        private bool useRR_1_4_Trade2 = false;
        private bool useRR_1_4_Trade3 = false;
        private bool useRR_1_4_Trade4 = false;

        // ===== 023. Custom Stops Targets =====
        private bool useCustomStopLoss1 = false;
        private int customStopLoss1Ticks = 25;
        private bool useCustomTarget1 = false;
        private int customTarget1Ticks = 50;

        private bool useCustomStopLoss2 = false;
        private int customStopLoss2Ticks = 25;
        private bool useCustomTarget2 = false;
        private int customTarget2Ticks = 50;

        private bool useCustomStopLoss3 = false;
        private int customStopLoss3Ticks = 25;
        private bool useCustomTarget3 = false;
        private int customTarget3Ticks = 50;

        private bool useCustomStopLoss4 = false;
        private int customStopLoss4Ticks = 25;
        private bool useCustomTarget4 = false;
        private int customTarget4Ticks = 50;

        // ===== Signal Management Variables =====
        private List<SignalInfo> activeSignals = new List<SignalInfo>();
        private int signalCounter = 0;

        // ===== CISD Level Tracking =====
        // Track only the LATEST green line and LATEST red line
        private CISDLevel latestBullishCISDLevel = null;  // Latest green line (wait for cross above)
        private CISDLevel latestBearishCISDLevel = null;  // Latest red line (wait for cross below)

        // ===== 002. FVG Entry Settings =====
        private bool useFVGEntry = false;
        private bool useFVGDisplayOnly = false;
        private bool useFVGCombinedEntry = false;
        private int fvgSignalOrder = 0; // 0=Any, 1=1st, 2=2nd, etc.
        private int fvgMaxBarsToRetest = 20;
        private int fvgMaxBarsAfterRetest = 10;
        private int fvgMaxBarsBetween = 10;
        private int fvgEntryPlusTicks = 0;
        private Brush fvgBullishColor = Brushes.Green;
        private Brush fvgBearishColor = Brushes.Red;
        private int fvgBoxOpacity = 10;

        // FVG Level Tracking
        private List<FVGLevel> activeFVGLevels = new List<FVGLevel>();

        // ===== 002B. IFVG (Inverse FVG) Entry Settings =====
        private bool useIFVGEntry = false;
        private bool useIFVGDisplayOnly = false;
        private bool useIFVGCombinedEntry = false;
        private int ifvgSignalOrder = 0; // 0=Any, 1=1st, 2=2nd, etc.
        private int ifvgMaxBarsBetween = 10;
        private int ifvgEntryPlusTicks = 0;
        private bool useIFVGStopLossRR = false;  // Use R:R for stop loss
        private int ifvgStopLossPlusTicks = 5;   // Plus ticks for stop loss
        private Brush ifvgBullishColor = Brushes.LimeGreen;  // Bullish IFVG (was bearish FVG, mitigated upward)
        private Brush ifvgBearishColor = Brushes.OrangeRed;  // Bearish IFVG (was bullish FVG, mitigated downward)
        private int ifvgBoxOpacity = 15;

        // IFVG Level Tracking
        private List<IFVGLevel> activeIFVGLevels = new List<IFVGLevel>();
        private List<IFVGLevel> pendingIFVGDetection = new List<IFVGLevel>();  // Potential FVGs waiting for mitigation

        // ===== 003. Sweep Entry Settings =====
        // LTF Sweep (Chart Timeframe)
        private bool useLTFSweepEntry = false;
        private bool useLTFSweepCombinedEntry = false;

        // HTF Sweep (15min default)
        private bool useHTFSweepEntry = false;
        private bool useHTFSweepCombinedEntry = false;

        // Common Sweep Settings
        private bool useSweepDisplayOnly = false;
        private int sweepSignalOrder = 0;  // 0=Any, 1=1st, 2=2nd, etc.
        private int sweepMaxBarsBetween = 10;
        private int sweepEntryPlusTicks = 0;
        private int sweepHTFTimeframe = 15;  // HTF timeframe (default 15min)
        private Brush sweepLTFBullishColor = Brushes.LimeGreen;
        private Brush sweepLTFBearishColor = Brushes.Red;
        private Brush sweepHTFBullishColor = Brushes.Lime;
        private Brush sweepHTFBearishColor = Brushes.OrangeRed;
        private const int SweepSwingLength = 5;  // Hardcoded swing sensitivity for sweep pivot detection
        private const int SwingStopLossSensitivity = 1;  // Universal swing stop loss sensitivity

        // Sweep Pivot Tracking
        private List<SweepPivotData> sweepLTFPivotHighs = new List<SweepPivotData>();
        private List<SweepPivotData> sweepLTFPivotLows = new List<SweepPivotData>();
        private List<SweepPivotData> sweepHTFPivotHighs = new List<SweepPivotData>();
        private List<SweepPivotData> sweepHTFPivotLows = new List<SweepPivotData>();
        private int sweepArrowCounter = 0;

        // ===== 006. HeikenAshi Entry Settings =====
        // LTF HeikenAshi (Chart Timeframe)
        private bool useLTFHeikenAshiEntry = false;
        private bool useLTFHeikenAshiCombinedEntry = false;
        private bool useLTFHeikenAshiDisplayOnly = false;

        // HTF HeikenAshi (15min default)
        private bool useHTFHeikenAshiEntry = false;
        private bool useHTFHeikenAshiCombinedEntry = false;
        private bool useHTFHeikenAshiDisplayOnly = false;

        // Common HeikenAshi Settings
        private int heikenAshiSignalOrder = 0;  // 0=Any, 1=1st, 2=2nd, etc.
        private int heikenAshiMaxBarsBetween = 10;
        private int heikenAshiEntryPlusTicks = 0;
        private int heikenAshiHTFTimeframe = 15;  // HTF timeframe (default 15min)
        private Brush heikenAshiLTFBullishColor = Brushes.Cyan;
        private Brush heikenAshiLTFBearishColor = Brushes.Magenta;
        private Brush heikenAshiHTFBullishColor = Brushes.Aqua;
        private Brush heikenAshiHTFBearishColor = Brushes.Purple;

        // HeikenAshi Indicator Instances
        private HeikenAshi8 heikenAshiLTF;  // Chart timeframe
        private HeikenAshi8 heikenAshiHTF;  // HTF timeframe

        // HeikenAshi State Tracking (for candle coloring)
        private int currentLTFTrend = 0;  // 0=none, 1=bullish, 2=bearish
        private int currentHTFTrend = 0;  // 0=none, 1=bullish, 2=bearish

        #endregion

        #region SignalInfo Class

        public class SwingPoint
        {
            public double Price { get; set; }
            public int BarIndex { get; set; }
            public DateTime Time { get; set; }
            public bool IsBroken { get; set; }
            public int BarsAgo { get; set; }  // Bars ago when detected
        }

        public class SignalInfo
        {
            public int SignalID { get; set; }
            public string SignalType { get; set; }  // "BOS", "FVG", "IFVG", etc.
            public int OrderPosition { get; set; }  // 0=Any, 1=1st, 2=2nd, etc.
            public bool IsLong { get; set; }        // true=Long, false=Short
            public int SignalBar { get; set; }      // Bar index when signal occurred
            public DateTime SignalTime { get; set; } // Time when signal occurred
            public double EntryPrice { get; set; }  // Entry price for this signal
            public double StopLoss { get; set; }    // Stop loss price
            public bool UseStopLossRR { get; set; } // Is R:R enabled for this signal?
            public int StopLossPlusTicks { get; set; } // Plus X ticks adjustment
            public int MaxBarsBetween { get; set; } // Max bars to wait for next signal
            public bool IsCombinedEntry { get; set; } // Is this part of combined entry?
            public bool IsStandalone { get; set; }  // Is this standalone entry?
            public bool HasGeneratedTrade { get; set; } // Has this signal generated a trade?

            // BOS specific
            public double SwingPoint { get; set; }  // The swing high/low that was broken
            public int SwingBar { get; set; }       // Bar index of the swing point
            public DateTime SwingTime { get; set; } // Time of the swing point
            public DateTime BreakTime { get; set; } // Time when break occurred
            public NinjaTrader.NinjaScript.DrawingTools.Line DrawnLine { get; set; } // Reference to drawn line

            // Generic tag for storing additional data (e.g., FVGLevel reference)
            public object Tag { get; set; }
        }

        public class CISDLevel
        {
            public double Price { get; set; }
            public bool IsBullish { get; set; }  // true = green line (wait for cross above), false = red line (wait for cross below)
            public DateTime TimeCreated { get; set; }
            public int BarCreated { get; set; }
            public int StartCandleBar { get; set; }  // The bar index where the green/red line STARTS (candidate bar)
            public DateTime StartCandleTime { get; set; }  // The time of the start candle
            public bool IsTriggered { get; set; }
            public NinjaTrader.NinjaScript.DrawingTools.Line DrawnLine { get; set; }
        }

        public class FVGLevel
        {
            public double Top { get; set; }       // Top of FVG (bullish = bar[1].Low, bearish = bar[3].Low)
            public double Bottom { get; set; }    // Bottom of FVG (bullish = bar[3].High, bearish = bar[1].High)
            public bool IsBullish { get; set; }   // true = bullish FVG, false = bearish FVG
            public DateTime StartTime { get; set; }  // Time when FVG was created
            public int StartBar { get; set; }     // Bar index when FVG was created
            public bool IsRetested { get; set; }  // Has the FVG been retested?
            public int RetestBar { get; set; }    // Bar index when retest occurred
            public DateTime RetestTime { get; set; }  // Time when retest occurred
            public bool IsMitigated { get; set; } // Has FVG been mitigated (close through it)?
            public bool HasGeneratedSignal { get; set; }  // Has this FVG generated an entry signal?
            public NinjaTrader.NinjaScript.DrawingTools.Rectangle DrawnBox { get; set; }  // Reference to drawn rectangle
        }

        // IFVG (Inverse FVG) - created when FVG gets mitigated
        public class IFVGLevel
        {
            public double Top { get; set; }       // Top of original FVG gap
            public double Bottom { get; set; }    // Bottom of original FVG gap
            public bool IsBullish { get; set; }   // true = Bullish IFVG (was bearish FVG, mitigated upward), false = Bearish IFVG
            public DateTime FVGStartTime { get; set; }  // Time when original FVG was created
            public int FVGStartBar { get; set; }  // Bar index when original FVG was created
            public DateTime MitigationTime { get; set; }  // Time when FVG was mitigated (IFVG created)
            public int MitigationBar { get; set; }  // Bar index when mitigated
            public bool HasGeneratedSignal { get; set; }  // Has this IFVG generated an entry signal?
            public NinjaTrader.NinjaScript.DrawingTools.Rectangle DrawnBox { get; set; }  // Reference to drawn rectangle
        }

        public class SweepPivotData
        {
            public double Price { get; set; }
            public int BarIndex { get; set; }
            public DateTime Time { get; set; }
            public bool IsBreak { get; set; }
            public bool IsMitigated { get; set; }
            public bool IsWick { get; set; }  // Has sweep been detected?
            public bool HasGeneratedSignal { get; set; }
            public bool IsHTF { get; set; }  // Is this a HTF (higher timeframe) sweep?
        }

        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"All-in-One Strategy with Multiple Entry Types and Combined Entry System";
                Name = "AllinOneStrategy";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 4;  // Allow up to 4 separate entries per direction
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;
                IsInstantiatedOnEachOptimizationIteration = true;
            }
            else if (State == State.Configure)
            {
                // Add HTF data series for Sweep detection (15min default)
                if (useHTFSweepEntry || useHTFSweepCombinedEntry || useSweepDisplayOnly)
                {
                    AddDataSeries(BarsPeriodType.Minute, sweepHTFTimeframe);
                }

                // Add HTF data series for HeikenAshi detection (15min default)
                // Note: If both Sweep and HeikenAshi use same HTF timeframe, only one series is added
                if (useHTFHeikenAshiEntry || useHTFHeikenAshiCombinedEntry || useHTFHeikenAshiDisplayOnly)
                {
                    // Check if HTF series already added by Sweep
                    bool htfAlreadyAdded = (useHTFSweepEntry || useHTFSweepCombinedEntry || useSweepDisplayOnly)
                                          && (sweepHTFTimeframe == heikenAshiHTFTimeframe);

                    if (!htfAlreadyAdded)
                    {
                        AddDataSeries(BarsPeriodType.Minute, heikenAshiHTFTimeframe);
                    }
                }
            }
            else if (State == State.DataLoaded)
            {
                // Initialize HeikenAshi indicators
                // LTF (chart timeframe) - initialize if any LTF HeikenAshi option is enabled
                if (useLTFHeikenAshiEntry || useLTFHeikenAshiCombinedEntry || useLTFHeikenAshiDisplayOnly)
                {
                    heikenAshiLTF = HeikenAshi8();
                }

                // HTF (higher timeframe) - initialize if any HTF HeikenAshi option is enabled
                if (useHTFHeikenAshiEntry || useHTFHeikenAshiCombinedEntry || useHTFHeikenAshiDisplayOnly)
                {
                    // Determine which BarsArray index to use for HTF
                    int htfIndex = 1; // Default to first added series

                    // If Sweep HTF is enabled and uses different timeframe, HeikenAshi HTF will be at index 2
                    if ((useHTFSweepEntry || useHTFSweepCombinedEntry || useSweepDisplayOnly)
                        && sweepHTFTimeframe != heikenAshiHTFTimeframe)
                    {
                        htfIndex = 2;
                    }

                    heikenAshiHTF = HeikenAshi8(BarsArray[htfIndex]);
                }
            }
        }

        protected override void OnBarUpdate()
        {
            try
            {
                if (CurrentBar < BarsRequiredToTrade)
                    return;

                // Check time filter
                if (!IsWithinTradingSession())
                    return;

                // Debug output every 100 bars
                if (CurrentBar % 100 == 0)
                    Print($"Strategy Running - Bar: {CurrentBar}, BOS Entry:{useBOSEntry}, Display:{useBOSDisplayOnly}, Combined:{useBOSCombinedEntry}");

                // Clean up expired signals
                RemoveExpiredSignals();

                // Check for BOS signals
                if (useBOSEntry || useBOSCombinedEntry || useBOSDisplayOnly)
                {
                    CheckForBOSSignal();
                }
                else
                {
                    if (CurrentBar % 100 == 0)
                        Print("BOS Detection SKIPPED - all BOS settings are OFF!");
                }

                // Check for CISD signals
                if (useCISDEntry || useCISDCombinedEntry || useCISDDisplayOnly)
                {
                    CheckForCISDSignal();
                    CheckCISDLevelCrosses();  // Check if price crossed any pending CISD levels
                }

                // Check for FVG signals
                if (useFVGEntry || useFVGCombinedEntry || useFVGDisplayOnly)
                {
                    CheckForFVGSignal();      // Detect new FVGs
                    CheckFVGRetests();        // Check for retests
                    CheckFVGEntrySignals();   // Check for entry signals after retest
                }

                // Check for IFVG signals
                if (useIFVGEntry || useIFVGCombinedEntry || useIFVGDisplayOnly)
                {
                    DetectIFVG();             // Detect potential IFVGs (FVG patterns)
                    CheckIFVGMitigation();    // Check for mitigation and generate entries
                }

                // Check for LTF HeikenAshi signals (chart timeframe)
                if (useLTFHeikenAshiEntry || useLTFHeikenAshiCombinedEntry || useLTFHeikenAshiDisplayOnly)
                {
                    CheckForLTFHeikenAshiSignal();  // Detect LTF HeikenAshi color changes
                }

                // Check for HTF HeikenAshi signals and color candles
                if (useHTFHeikenAshiEntry || useHTFHeikenAshiCombinedEntry || useHTFHeikenAshiDisplayOnly)
                {
                    CheckForHTFHeikenAshiSignal();  // Detect HTF color changes and color candles
                }

                // Process HTF data updates (15min timeframe) - detect new pivots only
                if (BarsInProgress == 1 && (useHTFSweepEntry || useHTFSweepCombinedEntry || useSweepDisplayOnly))
                {
                    UpdateHTFPivots();  // Only update HTF pivot list
                }

                // Process primary bars (chart timeframe)
                if (BarsInProgress == 0)
                {
                    // Check for LTF Sweep signals (chart timeframe)
                    if (useLTFSweepEntry || useLTFSweepCombinedEntry || useSweepDisplayOnly)
                    {
                        CheckForLTFSweepSignal();  // Detect LTF sweeps and generate signals
                    }

                    // Check for HTF Sweep signals - check EVERY primary bar against HTF pivots
                    if (useHTFSweepEntry || useHTFSweepCombinedEntry || useSweepDisplayOnly)
                    {
                        CheckForHTFSweepSignal();  // Check primary bars against HTF pivots
                    }

                    // Validate and execute entries
                    ValidateAndExecuteEntries();
                }
            }
            catch (Exception ex)
            {
                Print($"FATAL ERROR in OnBarUpdate at Bar {CurrentBar}: {ex.Message}");
                Print($"Stack: {ex.StackTrace}");
            }
        }

        #region Helper Methods

        // ===== Time Session Filter =====
        private bool IsWithinTradingSession()
        {
            if (!useCustomEntryTimeFilter)
                return true;

            // Convert chart time to EST
            TimeZoneInfo estZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime chartTime = Time[0];
            DateTime estTime = TimeZoneInfo.ConvertTime(chartTime, estZone);

            TimeSpan currentTime = estTime.TimeOfDay;
            TimeSpan sessionStart = new TimeSpan(sessionStartHour, sessionStartMinute, 0);
            TimeSpan sessionEnd = new TimeSpan(sessionEndHour, sessionEndMinute, 0);

            if (CurrentBar % 100 == 0)
                Print($"Time Filter: Chart Time={chartTime:HH:mm:ss}, EST Time={estTime:HH:mm:ss}, Session={sessionStart}-{sessionEnd}");

            if (sessionEnd > sessionStart)
                return currentTime >= sessionStart && currentTime <= sessionEnd;
            else
                // Handle overnight sessions
                return currentTime >= sessionStart || currentTime <= sessionEnd;
        }

        // ===== Signal Management =====
        private void RemoveExpiredSignals()
        {
            // Remove signals that exceeded max bars between
            activeSignals.RemoveAll(s =>
                CurrentBar - s.SignalBar > s.MaxBarsBetween
            );
        }

        private void AddSignal(SignalInfo signal)
        {
            signal.SignalID = ++signalCounter;
            signal.SignalTime = Time[0];
            activeSignals.Add(signal);
        }

        // ===== Combined Entry Validation =====
        private bool ValidateCombinedEntry(SignalInfo newSignal, out List<SignalInfo> combinedSignals)
        {
            combinedSignals = new List<SignalInfo>();

            // If signal order is "Any", check if we have at least 2 DIFFERENT signal types
            if (newSignal.OrderPosition == 0)
            {
                // Get all active combined signals with same direction (not yet generated trade)
                var matchingSignals = activeSignals
                    .Where(s => s.IsCombinedEntry && s.IsLong == newSignal.IsLong && !s.HasGeneratedTrade)
                    .ToList();

                // Add current signal
                matchingSignals.Add(newSignal);

                // Check if we have at least 2 DIFFERENT signal types
                var uniqueSignalTypes = matchingSignals.Select(s => s.SignalType).Distinct().Count();

                if (uniqueSignalTypes >= 2)
                {
                    combinedSignals = matchingSignals;
                    Print($"Combined Entry Valid (Any): {uniqueSignalTypes} different signal types found");
                    return true;
                }

                Print($"Combined Entry NOT Valid (Any): Only {uniqueSignalTypes} signal type(s), need at least 2");
                return false;
            }
            else
            {
                // Check sequential order (1st, 2nd, 3rd, etc.)
                return CheckSequentialOrder(newSignal, out combinedSignals);
            }
        }

        private bool CheckSequentialOrder(SignalInfo newSignal, out List<SignalInfo> combinedSignals)
        {
            combinedSignals = new List<SignalInfo>();

            Print($"Checking Sequential Order for {newSignal.SignalType} with OrderPosition={newSignal.OrderPosition}");

            // Check if all previous order positions exist
            for (int i = 1; i < newSignal.OrderPosition; i++)
            {
                var previousSignal = activeSignals.FirstOrDefault(s =>
                    s.OrderPosition == i &&
                    s.IsCombinedEntry &&
                    s.IsLong == newSignal.IsLong &&
                    !s.HasGeneratedTrade  // Not already used
                );

                if (previousSignal == null)
                {
                    Print($"  Missing signal at position {i}");
                    return false; // Missing previous signal
                }

                // CRITICAL: Check chronological order - previous signal must have occurred BEFORE current signal
                if (previousSignal.SignalBar >= newSignal.SignalBar)
                {
                    Print($"  Order violation: Signal at position {i} (bar {previousSignal.SignalBar}) is NOT before current signal (bar {newSignal.SignalBar})");
                    return false;
                }

                // Check if within max bars
                int barsBetween = CurrentBar - previousSignal.SignalBar;
                if (barsBetween > previousSignal.MaxBarsBetween)
                {
                    Print($"  Signal at position {i} expired: {barsBetween} bars > {previousSignal.MaxBarsBetween} max");
                    return false;
                }

                Print($"  Found signal {previousSignal.SignalType} at position {i}, at bar {previousSignal.SignalBar} ({barsBetween} bars ago)");
                combinedSignals.Add(previousSignal);
            }

            // Add current signal
            combinedSignals.Add(newSignal);

            // Check if we have at least 2 DIFFERENT signal types
            var uniqueSignalTypes = combinedSignals.Select(s => s.SignalType).Distinct().Count();
            if (uniqueSignalTypes < 2)
            {
                Print($"  Combined Entry NOT Valid: Only {uniqueSignalTypes} signal type(s), need at least 2 different types");
                return false;
            }

            Print($"  Combined Entry Valid: All {newSignal.OrderPosition} signals found with {uniqueSignalTypes} different types in correct chronological order");
            return true;
        }

        private double GetCombinedEntryStopLoss(List<SignalInfo> combinedSignals)
        {
            // Sort signals by bar index (most recent first)
            var signalsByTime = combinedSignals.OrderByDescending(s => s.SignalBar).ToList();

            // Find the last signal (chronologically) with R:R enabled
            foreach (var signal in signalsByTime)
            {
                if (signal.UseStopLossRR)
                {
                    return signal.StopLoss + (signal.StopLossPlusTicks * TickSize);
                }
            }

            return 0; // No R:R enabled (use first signal's SL as fallback)
        }

        private void ValidateAndExecuteEntries()
        {
            // Check if we have any signals ready to execute
            foreach (var signal in activeSignals.ToList())
            {
                if (signal.HasGeneratedTrade)
                    continue;

                // Display Only Mode for BOS - draw all lines without trading
                if (signal.SignalType == "BOS" && useBOSDisplayOnly)
                {
                    DrawBOSLine(signal);
                    signal.HasGeneratedTrade = true;
                    continue;
                }

                // Display Only Mode for FVG - draw all rectangles without trading
                if (signal.SignalType == "FVG" && useFVGDisplayOnly)
                {
                    DrawFVGRectangle(signal);
                    signal.HasGeneratedTrade = true;
                    continue;
                }

                // Display Only Mode for Sweep (LTF and HTF) - arrows already drawn, just mark and skip trading
                if (signal.SignalType.Contains("Sweep") && useSweepDisplayOnly)
                {
                    // Arrow was already drawn when sweep was detected in ProcessSweeps
                    signal.HasGeneratedTrade = true;
                    continue;
                }

                // Display Only Mode for IFVG - draw all rectangles without trading
                if (signal.SignalType == "IFVG" && useIFVGDisplayOnly)
                {
                    DrawIFVGRectangle(signal);
                    signal.HasGeneratedTrade = true;
                    continue;
                }

                // Note: CISD display-only is handled differently - levels are auto-drawn when created
                // and CheckCISDLevelCrosses doesn't generate signals in display-only mode

                // Standalone entry
                if (signal.IsStandalone && !signal.IsCombinedEntry)
                {
                    ExecuteEntry(signal, new List<SignalInfo> { signal });
                    signal.HasGeneratedTrade = true;
                }

                // Combined entry
                if (signal.IsCombinedEntry && !signal.IsStandalone)
                {
                    Print($"Checking combined entry for {signal.SignalType} with order={signal.OrderPosition}");

                    List<SignalInfo> combinedSignals;
                    if (ValidateCombinedEntry(signal, out combinedSignals))
                    {
                        Print($"Combined entry VALIDATED! Executing with {combinedSignals.Count} signals");
                        ExecuteEntry(signal, combinedSignals);

                        // Mark all combined signals as used
                        foreach (var s in combinedSignals)
                            s.HasGeneratedTrade = true;
                    }
                    else
                    {
                        Print($"Combined entry NOT validated yet for {signal.SignalType}");
                    }
                }
            }
        }

        private void ExecuteEntry(SignalInfo triggerSignal, List<SignalInfo> allSignals)
        {
            try
            {
                Print($"=== ExecuteEntry START ===");
                Print($"Position.MarketPosition: {Position.MarketPosition}");
                Print($"Position.Quantity: {Position.Quantity}");
                Print($"AllowMultipleTrades: {allowMultipleTrades}");
                Print($"MaxPositionsAllowed: {maxPositionsAllowed}");

                // Check if multiple trades are allowed
                if (!allowMultipleTrades && Position.MarketPosition != MarketPosition.Flat)
                {
                    Print($"Trade BLOCKED: Allow Multiple Trades is FALSE and position is not flat");
                    return;
                }

                // Get stop loss (from combined signals or standalone)
                bool isCombinedEntry = allSignals.Count > 1;
                double stopLoss = isCombinedEntry
                    ? GetCombinedEntryStopLoss(allSignals)
                    : triggerSignal.StopLoss + (triggerSignal.StopLossPlusTicks * TickSize);

                // Calculate entry price with ticks adjustment
                double entryPrice = triggerSignal.EntryPrice;

                if (isCombinedEntry)
                {
                    string signalTypes = string.Join(" + ", allSignals.Select(s => s.SignalType));
                    Print($"*** COMBINED ENTRY DETECTED: {signalTypes} ***");
                }

                Print($"StopLoss: {stopLoss:F2}, EntryPrice: {entryPrice:F2}");

            Print($"ExecuteEntry: Processing {triggerSignal.SignalType} signal. Quantities: [{trade1Quantity},{trade2Quantity},{trade3Quantity},{trade4Quantity}]");

            // ===== TRADE 1 =====
            if (trade1Quantity > 0)
            {
                string signalName1 = isCombinedEntry ? "CombinedEntry_1" : $"{triggerSignal.SignalType}_1";
                double finalStopLoss1 = useCustomStopLoss1
                    ? (triggerSignal.IsLong ? entryPrice - customStopLoss1Ticks * TickSize : entryPrice + customStopLoss1Ticks * TickSize)
                    : stopLoss;

                double risk1 = Math.Abs(entryPrice - finalStopLoss1);
                double profitTarget1 = 0;

                if (useCustomTarget1)
                    profitTarget1 = triggerSignal.IsLong ? entryPrice + customTarget1Ticks * TickSize : entryPrice - customTarget1Ticks * TickSize;
                else if (useRR_1_1_Trade1)
                    profitTarget1 = triggerSignal.IsLong ? entryPrice + risk1 : entryPrice - risk1;
                else if (useRR_1_1_5_Trade1)
                    profitTarget1 = triggerSignal.IsLong ? entryPrice + (risk1 * 1.5) : entryPrice - (risk1 * 1.5);
                else if (useRR_1_2_Trade1)
                    profitTarget1 = triggerSignal.IsLong ? entryPrice + (risk1 * 2) : entryPrice - (risk1 * 2);
                else if (useRR_1_2_5_Trade1)
                    profitTarget1 = triggerSignal.IsLong ? entryPrice + (risk1 * 2.5) : entryPrice - (risk1 * 2.5);
                else if (useRR_1_3_Trade1)
                    profitTarget1 = triggerSignal.IsLong ? entryPrice + (risk1 * 3) : entryPrice - (risk1 * 3);
                else if (useRR_1_3_5_Trade1)
                    profitTarget1 = triggerSignal.IsLong ? entryPrice + (risk1 * 3.5) : entryPrice - (risk1 * 3.5);
                else if (useRR_1_4_Trade1)
                    profitTarget1 = triggerSignal.IsLong ? entryPrice + (risk1 * 4) : entryPrice - (risk1 * 4);

                Print($"  Trade 1: Qty={trade1Quantity}, SL={finalStopLoss1:F2}, TP={profitTarget1:F2}");

                // SET STOP AND TARGET BEFORE ENTRY
                if (finalStopLoss1 > 0)
                    SetStopLoss(signalName1, CalculationMode.Price, finalStopLoss1, false);
                if (profitTarget1 > 0)
                    SetProfitTarget(signalName1, CalculationMode.Price, profitTarget1);

                if (triggerSignal.IsLong)
                {
                    EnterLong(trade1Quantity, signalName1);
                }
                else
                {
                    EnterShort(trade1Quantity, signalName1);
                }
                Print($"  Trade 1: {(triggerSignal.IsLong ? "LONG" : "SHORT")} order sent!");
            }

            // ===== TRADE 2 =====
            if (trade2Quantity > 0)
            {
                string signalName2 = isCombinedEntry ? "CombinedEntry_2" : $"{triggerSignal.SignalType}_2";
                double finalStopLoss2 = useCustomStopLoss2
                    ? (triggerSignal.IsLong ? entryPrice - customStopLoss2Ticks * TickSize : entryPrice + customStopLoss2Ticks * TickSize)
                    : stopLoss;

                double risk2 = Math.Abs(entryPrice - finalStopLoss2);
                double profitTarget2 = 0;

                if (useCustomTarget2)
                    profitTarget2 = triggerSignal.IsLong ? entryPrice + customTarget2Ticks * TickSize : entryPrice - customTarget2Ticks * TickSize;
                else if (useRR_1_1_Trade2)
                    profitTarget2 = triggerSignal.IsLong ? entryPrice + risk2 : entryPrice - risk2;
                else if (useRR_1_1_5_Trade2)
                    profitTarget2 = triggerSignal.IsLong ? entryPrice + (risk2 * 1.5) : entryPrice - (risk2 * 1.5);
                else if (useRR_1_2_Trade2)
                    profitTarget2 = triggerSignal.IsLong ? entryPrice + (risk2 * 2) : entryPrice - (risk2 * 2);
                else if (useRR_1_2_5_Trade2)
                    profitTarget2 = triggerSignal.IsLong ? entryPrice + (risk2 * 2.5) : entryPrice - (risk2 * 2.5);
                else if (useRR_1_3_Trade2)
                    profitTarget2 = triggerSignal.IsLong ? entryPrice + (risk2 * 3) : entryPrice - (risk2 * 3);
                else if (useRR_1_3_5_Trade2)
                    profitTarget2 = triggerSignal.IsLong ? entryPrice + (risk2 * 3.5) : entryPrice - (risk2 * 3.5);
                else if (useRR_1_4_Trade2)
                    profitTarget2 = triggerSignal.IsLong ? entryPrice + (risk2 * 4) : entryPrice - (risk2 * 4);

                Print($"  Trade 2: Qty={trade2Quantity}, SL={finalStopLoss2:F2}, TP={profitTarget2:F2}");

                // SET STOP AND TARGET BEFORE ENTRY
                if (finalStopLoss2 > 0)
                    SetStopLoss(signalName2, CalculationMode.Price, finalStopLoss2, false);
                if (profitTarget2 > 0)
                    SetProfitTarget(signalName2, CalculationMode.Price, profitTarget2);

                if (triggerSignal.IsLong)
                    EnterLong(trade2Quantity, signalName2);
                else
                    EnterShort(trade2Quantity, signalName2);

                Print($"  Trade 2: {(triggerSignal.IsLong ? "LONG" : "SHORT")} order sent!");
            }

            // ===== TRADE 3 =====
            if (trade3Quantity > 0)
            {
                string signalName3 = isCombinedEntry ? "CombinedEntry_3" : $"{triggerSignal.SignalType}_3";
                double finalStopLoss3 = useCustomStopLoss3
                    ? (triggerSignal.IsLong ? entryPrice - customStopLoss3Ticks * TickSize : entryPrice + customStopLoss3Ticks * TickSize)
                    : stopLoss;

                double risk3 = Math.Abs(entryPrice - finalStopLoss3);
                double profitTarget3 = 0;

                if (useCustomTarget3)
                    profitTarget3 = triggerSignal.IsLong ? entryPrice + customTarget3Ticks * TickSize : entryPrice - customTarget3Ticks * TickSize;
                else if (useRR_1_1_Trade3)
                    profitTarget3 = triggerSignal.IsLong ? entryPrice + risk3 : entryPrice - risk3;
                else if (useRR_1_1_5_Trade3)
                    profitTarget3 = triggerSignal.IsLong ? entryPrice + (risk3 * 1.5) : entryPrice - (risk3 * 1.5);
                else if (useRR_1_2_Trade3)
                    profitTarget3 = triggerSignal.IsLong ? entryPrice + (risk3 * 2) : entryPrice - (risk3 * 2);
                else if (useRR_1_2_5_Trade3)
                    profitTarget3 = triggerSignal.IsLong ? entryPrice + (risk3 * 2.5) : entryPrice - (risk3 * 2.5);
                else if (useRR_1_3_Trade3)
                    profitTarget3 = triggerSignal.IsLong ? entryPrice + (risk3 * 3) : entryPrice - (risk3 * 3);
                else if (useRR_1_3_5_Trade3)
                    profitTarget3 = triggerSignal.IsLong ? entryPrice + (risk3 * 3.5) : entryPrice - (risk3 * 3.5);
                else if (useRR_1_4_Trade3)
                    profitTarget3 = triggerSignal.IsLong ? entryPrice + (risk3 * 4) : entryPrice - (risk3 * 4);

                Print($"  Trade 3: Qty={trade3Quantity}, SL={finalStopLoss3:F2}, TP={profitTarget3:F2}");

                // SET STOP AND TARGET BEFORE ENTRY
                if (finalStopLoss3 > 0)
                    SetStopLoss(signalName3, CalculationMode.Price, finalStopLoss3, false);
                if (profitTarget3 > 0)
                    SetProfitTarget(signalName3, CalculationMode.Price, profitTarget3);

                if (triggerSignal.IsLong)
                    EnterLong(trade3Quantity, signalName3);
                else
                    EnterShort(trade3Quantity, signalName3);

                Print($"  Trade 3: {(triggerSignal.IsLong ? "LONG" : "SHORT")} order sent!");
            }

            // ===== TRADE 4 =====
            if (trade4Quantity > 0)
            {
                string signalName4 = isCombinedEntry ? "CombinedEntry_4" : $"{triggerSignal.SignalType}_4";
                double finalStopLoss4 = useCustomStopLoss4
                    ? (triggerSignal.IsLong ? entryPrice - customStopLoss4Ticks * TickSize : entryPrice + customStopLoss4Ticks * TickSize)
                    : stopLoss;

                double risk4 = Math.Abs(entryPrice - finalStopLoss4);
                double profitTarget4 = 0;

                if (useCustomTarget4)
                    profitTarget4 = triggerSignal.IsLong ? entryPrice + customTarget4Ticks * TickSize : entryPrice - customTarget4Ticks * TickSize;
                else if (useRR_1_1_Trade4)
                    profitTarget4 = triggerSignal.IsLong ? entryPrice + risk4 : entryPrice - risk4;
                else if (useRR_1_1_5_Trade4)
                    profitTarget4 = triggerSignal.IsLong ? entryPrice + (risk4 * 1.5) : entryPrice - (risk4 * 1.5);
                else if (useRR_1_2_Trade4)
                    profitTarget4 = triggerSignal.IsLong ? entryPrice + (risk4 * 2) : entryPrice - (risk4 * 2);
                else if (useRR_1_2_5_Trade4)
                    profitTarget4 = triggerSignal.IsLong ? entryPrice + (risk4 * 2.5) : entryPrice - (risk4 * 2.5);
                else if (useRR_1_3_Trade4)
                    profitTarget4 = triggerSignal.IsLong ? entryPrice + (risk4 * 3) : entryPrice - (risk4 * 3);
                else if (useRR_1_3_5_Trade4)
                    profitTarget4 = triggerSignal.IsLong ? entryPrice + (risk4 * 3.5) : entryPrice - (risk4 * 3.5);
                else if (useRR_1_4_Trade4)
                    profitTarget4 = triggerSignal.IsLong ? entryPrice + (risk4 * 4) : entryPrice - (risk4 * 4);

                Print($"  Trade 4: Qty={trade4Quantity}, SL={finalStopLoss4:F2}, TP={profitTarget4:F2}");

                // SET STOP AND TARGET BEFORE ENTRY
                if (finalStopLoss4 > 0)
                    SetStopLoss(signalName4, CalculationMode.Price, finalStopLoss4, false);
                if (profitTarget4 > 0)
                    SetProfitTarget(signalName4, CalculationMode.Price, profitTarget4);

                if (triggerSignal.IsLong)
                    EnterLong(trade4Quantity, signalName4);
                else
                    EnterShort(trade4Quantity, signalName4);

                Print($"  Trade 4: {(triggerSignal.IsLong ? "LONG" : "SHORT")} order sent!");
            }

                // Draw lines for ALL signals that contributed to this entry
                foreach (var signal in allSignals)
                {
                    // Draw BOS line
                    if (signal.SignalType == "BOS")
                    {
                        DrawBOSLine(signal);
                        Print($"Drawing BOS line for combined entry signal");
                    }

                    // Draw CISD line (only if it was triggered - CISD levels are already drawn)
                    if (signal.SignalType == "CISD")
                    {
                        DrawCISDLine(signal);
                        Print($"Drawing CISD line for combined entry signal");
                    }

                    // Draw FVG rectangle
                    if (signal.SignalType == "FVG")
                    {
                        DrawFVGRectangle(signal);
                        Print($"Drawing FVG rectangle for combined entry signal");
                    }

                    // Draw IFVG rectangle
                    if (signal.SignalType == "IFVG")
                    {
                        DrawIFVGRectangle(signal);
                        Print($"Drawing IFVG rectangle for combined entry signal");
                    }

                    // Draw Sweep arrow NOW for combined entries (standalone already drew it)
                    if (signal.SignalType.Contains("Sweep") && signal.IsCombinedEntry)
                    {
                        // Draw arrow on the bar where sweep was detected (signal.SignalBar)
                        bool isHTF = (bool)signal.Tag;
                        int barsAgo = CurrentBar - signal.SignalBar;

                        string arrowTag = $"Sweep_Arrow_{(isHTF ? "HTF" : "LTF")}_{(signal.IsLong ? "Bull" : "Bear")}_{signal.SignalBar}_{sweepArrowCounter++}";
                        Brush arrowColor = isHTF ?
                            (signal.IsLong ? sweepHTFBullishColor : sweepHTFBearishColor) :
                            (signal.IsLong ? sweepLTFBullishColor : sweepLTFBearishColor);

                        if (signal.IsLong)
                        {
                            Draw.ArrowUp(this, arrowTag, true, barsAgo, Lows[0][barsAgo] - (5 * TickSize), arrowColor);
                        }
                        else
                        {
                            Draw.ArrowDown(this, arrowTag, true, barsAgo, Highs[0][barsAgo] + (5 * TickSize), arrowColor);
                        }
                        Print($"Drawing {signal.SignalType} arrow for COMBINED entry at bar {signal.SignalBar} (barsAgo={barsAgo})");
                    }

                    // Note: HeikenAshi uses candle coloring instead of arrows
                    // LTF: Indicator colors candles automatically
                    // HTF: Strategy colors candles via BarBrush in CheckForHTFHeikenAshiSignal()
                }

                Print($"=== ExecuteEntry END ===");
            }
            catch (Exception ex)
            {
                Print($"ERROR in ExecuteEntry: {ex.Message}");
                Print($"Stack Trace: {ex.StackTrace}");
            }
        }

        // ===== BOS Detection Logic (TradingView Style) =====
        private void CheckForBOSSignal()
        {
            if (CurrentBar < bosPivotLeftBars + bosPivotRightBars + 1)
                return;

            // Step 1: Detect new pivot highs (like TradingView pivothigh())
            DetectPivotHighs();

            // Step 2: Detect new pivot lows (like TradingView pivotlow())
            DetectPivotLows();

            // Step 3: Remove old swings that weren't broken within max bars
            RemoveExpiredSwings();

            // Step 4: Check if current bar breaks any tracked swing high/low
            CheckForSwingBreaks();
        }

        private void DetectPivotHighs()
        {
            // Check if bar at 'rightbars' ago is a pivot high
            int pivotBar = bosPivotRightBars;

            if (CurrentBar < bosPivotLeftBars + bosPivotRightBars)
                return;

            double pivotHigh = High[pivotBar];
            bool isPivot = true;

            // Check left bars - must have lower highs
            for (int i = 1; i <= bosPivotLeftBars; i++)
            {
                if (High[pivotBar + i] >= pivotHigh)
                {
                    isPivot = false;
                    break;
                }
            }

            // Check right bars - must have lower highs
            if (isPivot)
            {
                for (int i = 1; i <= bosPivotRightBars; i++)
                {
                    if (High[pivotBar - i] > pivotHigh)
                    {
                        isPivot = false;
                        break;
                    }
                }
            }

            // Add new swing high if confirmed
            if (isPivot)
            {
                SwingPoint newSwing = new SwingPoint
                {
                    Price = pivotHigh,
                    BarIndex = CurrentBar - pivotBar,
                    Time = Time[pivotBar],
                    IsBroken = false,
                    BarsAgo = pivotBar
                };

                swingHighs.Add(newSwing);
                Print($"Pivot HIGH detected: Price:{pivotHigh:F2} at Bar:{CurrentBar - pivotBar}");
            }
        }

        private void DetectPivotLows()
        {
            // Check if bar at 'rightbars' ago is a pivot low
            int pivotBar = bosPivotRightBars;

            if (CurrentBar < bosPivotLeftBars + bosPivotRightBars)
                return;

            double pivotLow = Low[pivotBar];
            bool isPivot = true;

            // Check left bars - must have higher lows
            for (int i = 1; i <= bosPivotLeftBars; i++)
            {
                if (Low[pivotBar + i] <= pivotLow)
                {
                    isPivot = false;
                    break;
                }
            }

            // Check right bars - must have higher lows
            if (isPivot)
            {
                for (int i = 1; i <= bosPivotRightBars; i++)
                {
                    if (Low[pivotBar - i] < pivotLow)
                    {
                        isPivot = false;
                        break;
                    }
                }
            }

            // Add new swing low if confirmed
            if (isPivot)
            {
                SwingPoint newSwing = new SwingPoint
                {
                    Price = pivotLow,
                    BarIndex = CurrentBar - pivotBar,
                    Time = Time[pivotBar],
                    IsBroken = false,
                    BarsAgo = pivotBar
                };

                swingLows.Add(newSwing);
                Print($"Pivot LOW detected: Price:{pivotLow:F2} at Bar:{CurrentBar - pivotBar}");
            }
        }

        private void RemoveExpiredSwings()
        {
            // Remove swing highs older than max bars
            swingHighs.RemoveAll(s => !s.IsBroken && (CurrentBar - s.BarIndex) > bosMaxBarsToBreak);

            // Remove swing lows older than max bars
            swingLows.RemoveAll(s => !s.IsBroken && (CurrentBar - s.BarIndex) > bosMaxBarsToBreak);
        }

        private void CheckForSwingBreaks()
        {
            // Check for bullish BOS - breaking swing highs
            foreach (var swingHigh in swingHighs.ToList())
            {
                if (swingHigh.IsBroken)
                    continue;

                // Check if current close breaks above swing high
                if (Close[0] > swingHigh.Price)
                {
                    swingHigh.IsBroken = true;

                    // Calculate bars ago for stop loss calculation
                    int swingBarsAgo = CurrentBar - swingHigh.BarIndex;

                    // Calculate stop loss (lowest low between swing and break)
                    double stopLoss = GetLowestLowBetween(swingBarsAgo, 0);

                    // Create BOS signal
                    SignalInfo signal = new SignalInfo
                    {
                        SignalType = "BOS",
                        OrderPosition = bosSignalOrder,
                        IsLong = true,
                        SignalBar = CurrentBar,
                        SignalTime = Time[0],
                        EntryPrice = Close[0] + (bosEntryPlusTicks * TickSize),
                        StopLoss = stopLoss,
                        UseStopLossRR = useBOSStopLossRR,
                        StopLossPlusTicks = bosStopLossPlusTicks,
                        MaxBarsBetween = bosMaxBarsBetween,
                        IsCombinedEntry = useBOSCombinedEntry,
                        IsStandalone = useBOSEntry && !useBOSCombinedEntry,  // Only standalone if NOT using combined
                        HasGeneratedTrade = false,
                        SwingPoint = swingHigh.Price,
                        SwingBar = swingBarsAgo,
                        SwingTime = swingHigh.Time,
                        BreakTime = Time[0]
                    };

                    AddSignal(signal);
                    Print($"BOS Bullish! Close:{Close[0]:F2} broke SwingHigh:{swingHigh.Price:F2} (Bar:{swingHigh.BarIndex})");
                }
            }

            // Check for bearish BOS - breaking swing lows
            foreach (var swingLow in swingLows.ToList())
            {
                if (swingLow.IsBroken)
                    continue;

                // Check if current close breaks below swing low
                if (Close[0] < swingLow.Price)
                {
                    swingLow.IsBroken = true;

                    // Calculate bars ago for stop loss calculation
                    int swingBarsAgo = CurrentBar - swingLow.BarIndex;

                    // Calculate stop loss (highest high between swing and break)
                    double stopLoss = GetHighestHighBetween(swingBarsAgo, 0);

                    // Create BOS signal
                    SignalInfo signal = new SignalInfo
                    {
                        SignalType = "BOS",
                        OrderPosition = bosSignalOrder,
                        IsLong = false,
                        SignalBar = CurrentBar,
                        SignalTime = Time[0],
                        EntryPrice = Close[0] - (bosEntryPlusTicks * TickSize),
                        StopLoss = stopLoss,
                        UseStopLossRR = useBOSStopLossRR,
                        StopLossPlusTicks = bosStopLossPlusTicks,
                        MaxBarsBetween = bosMaxBarsBetween,
                        IsCombinedEntry = useBOSCombinedEntry,
                        IsStandalone = useBOSEntry && !useBOSCombinedEntry,  // Only standalone if NOT using combined
                        HasGeneratedTrade = false,
                        SwingPoint = swingLow.Price,
                        SwingBar = swingBarsAgo,
                        SwingTime = swingLow.Time,
                        BreakTime = Time[0]
                    };

                    AddSignal(signal);
                    Print($"BOS Bearish! Close:{Close[0]:F2} broke SwingLow:{swingLow.Price:F2} (Bar:{swingLow.BarIndex})");
                }
            }
        }

        private bool IsSwingHigh(int period, out int swingBar)
        {
            swingBar = period;

            if (CurrentBar < period * 2)
                return false;

            double pivotHigh = High[period];

            // Check left side
            for (int i = 1; i <= period; i++)
            {
                if (High[period + i] >= pivotHigh)
                    return false;
            }

            // Check right side
            for (int i = 1; i <= period; i++)
            {
                if (High[period - i] > pivotHigh)
                    return false;
            }

            return true;
        }

        private bool IsSwingLow(int period, out int swingBar)
        {
            swingBar = period;

            if (CurrentBar < period * 2)
                return false;

            double pivotLow = Low[period];

            // Check left side
            for (int i = 1; i <= period; i++)
            {
                if (Low[period + i] <= pivotLow)
                    return false;
            }

            // Check right side
            for (int i = 1; i <= period; i++)
            {
                if (Low[period - i] < pivotLow)
                    return false;
            }

            return true;
        }

        private double GetLowestLowBetween(int startBar, int endBar)
        {
            double lowestLow = double.MaxValue;

            for (int i = endBar; i <= startBar; i++)
            {
                if (Low[i] < lowestLow)
                    lowestLow = Low[i];
            }

            return lowestLow;
        }

        private double GetHighestHighBetween(int startBar, int endBar)
        {
            double highestHigh = double.MinValue;

            for (int i = endBar; i <= startBar; i++)
            {
                if (High[i] > highestHigh)
                    highestHigh = High[i];
            }

            return highestHigh;
        }

        private void DrawBOSLine(SignalInfo signal)
        {
            if (signal.SignalType != "BOS")
                return;

            string lineTag = $"BOS_{(signal.IsLong ? "Bull" : "Bear")}_{CurrentBar}_{DateTime.Now.Ticks}";

            // Draw line from swing time to break time at swing price level
            var drawnLine = Draw.Line(this, lineTag,
                false,
                signal.SwingTime, signal.SwingPoint,    // Start: swing point time and price
                signal.BreakTime, signal.SwingPoint,    // End: break time at same price
                signal.IsLong ? bosBullishColor : bosBearishColor,
                DashStyleHelper.Solid,
                2);

            // Store reference to drawn line
            signal.DrawnLine = drawnLine;

            Print($"Drawing BOS Line: {lineTag}, Direction: {(signal.IsLong ? "Bullish" : "Bearish")}, Price: {signal.SwingPoint}, SwingTime: {signal.SwingTime}, BreakTime: {signal.BreakTime}");
        }

        // ===== CISD Detection Methods =====
        private void CheckForCISDSignal()
        {
            if (CurrentBar < 2)
                return;

            // Get previous bar data
            double prevOpen = Open[1];
            double prevClose = Close[1];
            double prevHigh = High[1];
            double prevLow = Low[1];

            // Pullback Detection
            bool bearishPullbackFound = prevClose > prevOpen;
            bool bullishPullbackFound = prevClose < prevOpen;

            // Handle Pullback Detection
            HandleCISDPullbackDetection(bearishPullbackFound, bullishPullbackFound, prevOpen);

            // Update Candidate Levels During Pullbacks
            UpdateCISDCandidateLevels();

            // Handle Structure Changes and Generate Signals
            HandleCISDStructureChanges(prevClose, prevOpen, prevHigh, prevLow);
        }

        private void HandleCISDPullbackDetection(bool bearishPullbackFound, bool bullishPullbackFound, double prevOpen)
        {
            // Bearish Pullback Detection (price going up, potential for bullish CISD)
            if (bearishPullbackFound && !detectingBearishPullback)
            {
                detectingBearishPullback = true;
                candidateHighPrice = prevOpen;
                bullishCandidateBar = CurrentBar - 1;
                Print($"CISD: Bearish pullback detected at bar {CurrentBar - 1}, candidate high: {candidateHighPrice:F2}");
            }

            // Bullish Pullback Detection (price going down, potential for bearish CISD)
            if (bullishPullbackFound && !detectingBullishPullback)
            {
                detectingBullishPullback = true;
                candidateLowPrice = prevOpen;
                bearishCandidateBar = CurrentBar - 1;
                Print($"CISD: Bullish pullback detected at bar {CurrentBar - 1}, candidate low: {candidateLowPrice:F2}");
            }
        }

        private void UpdateCISDCandidateLevels()
        {
            // Update Candidate Levels During Bullish Pullbacks (looking for bearish CISD)
            if (detectingBullishPullback)
            {
                if (Open[0] < candidateLowPrice)
                {
                    candidateLowPrice = Open[0];
                    bearishCandidateBar = CurrentBar;
                }
                if ((Close[0] < Open[0]) && (Open[0] > candidateLowPrice))
                {
                    candidateLowPrice = Open[0];
                    bearishCandidateBar = CurrentBar;
                }
            }

            // Update Candidate Levels During Bearish Pullbacks (looking for bullish CISD)
            if (detectingBearishPullback)
            {
                if (Open[0] > candidateHighPrice)
                {
                    candidateHighPrice = Open[0];
                    bullishCandidateBar = CurrentBar;
                }
                if ((Close[0] > Open[0]) && Open[0] < candidateHighPrice)
                {
                    candidateHighPrice = Open[0];
                    bullishCandidateBar = CurrentBar;
                }
            }
        }

        private void HandleCISDStructureChanges(double prevClose, double prevOpen, double prevHigh, double prevLow)
        {
            // Structure Changes - Bearish Break (price breaks down → triggers BULLISH CISD signal for reversal)
            if (Low[0] < cisdStructureLow)
            {
                cisdStructureLow = Low[0];

                if (detectingBearishPullback && (CurrentBar - bullishCandidateBar != 0))
                {
                    int barsBack = CurrentBar - bullishCandidateBar;
                    cisdStructureHigh = Math.Max(High[barsBack], (barsBack + 1 < CurrentBar) ? High[barsBack + 1] : High[barsBack]);
                    detectingBearishPullback = false;

                    // Generate BULLISH CISD Signal (LONG) - price broke down, expecting reversal up
                    GenerateCISDSignal(true, candidateHighPrice, bullishCandidateBar);
                }
                else if (prevClose > prevOpen && Close[0] < Open[0])
                {
                    cisdStructureHigh = prevHigh;
                    detectingBearishPullback = false;

                    // Generate BULLISH CISD Signal (LONG)
                    GenerateCISDSignal(true, candidateHighPrice, bullishCandidateBar);
                }
            }

            // Structure Changes - Bullish Break (price breaks up → triggers BEARISH CISD signal for reversal)
            if (High[0] > cisdStructureHigh)
            {
                cisdStructureHigh = High[0];

                if (detectingBullishPullback && (CurrentBar - bearishCandidateBar != 0))
                {
                    int barsBack = CurrentBar - bearishCandidateBar;
                    cisdStructureLow = Math.Min(Low[barsBack], (barsBack + 1 < CurrentBar) ? Low[barsBack + 1] : Low[barsBack]);
                    detectingBullishPullback = false;

                    // Generate BEARISH CISD Signal (SHORT) - price broke up, expecting reversal down
                    GenerateCISDSignal(false, candidateLowPrice, bearishCandidateBar);
                }
                else if (prevClose < prevOpen && Close[0] > Open[0])
                {
                    cisdStructureLow = prevLow;
                    detectingBullishPullback = false;

                    // Generate BEARISH CISD Signal (SHORT)
                    GenerateCISDSignal(false, candidateLowPrice, bearishCandidateBar);
                }
            }
        }

        private void GenerateCISDSignal(bool isLong, double candidatePrice, int candidateBar)
        {
            if (double.IsNaN(candidatePrice))
                return;

            DateTime candidateTime = Time[CurrentBar - candidateBar];
            DateTime breakTime = Time[0];

            // Create CISD Level (not immediate signal - wait for close to cross)
            // NOTE: Stop loss will be calculated when the level is crossed, between start candle and break candle
            CISDLevel level = new CISDLevel
            {
                Price = candidatePrice,
                IsBullish = isLong,  // true = green line (wait for cross above), false = red line (wait for cross below)
                TimeCreated = breakTime,
                BarCreated = CurrentBar,
                StartCandleBar = candidateBar,  // Store the start candle bar index
                StartCandleTime = candidateTime,
                IsTriggered = false
            };

            // Replace old level with new one (only track the LATEST)
            if (isLong)
            {
                // Remove old green line if it exists
                if (latestBullishCISDLevel != null && latestBullishCISDLevel.DrawnLine != null)
                {
                    RemoveDrawObject(latestBullishCISDLevel.DrawnLine.Tag);
                    Print($"Removed old BULLISH CISD level at {latestBullishCISDLevel.Price:F2}");
                }
                latestBullishCISDLevel = level;
            }
            else
            {
                // Remove old red line if it exists
                if (latestBearishCISDLevel != null && latestBearishCISDLevel.DrawnLine != null)
                {
                    RemoveDrawObject(latestBearishCISDLevel.DrawnLine.Tag);
                    Print($"Removed old BEARISH CISD level at {latestBearishCISDLevel.Price:F2}");
                }
                latestBearishCISDLevel = level;
            }

            // Draw the level line
            DrawCISDLevelLine(level);

            Print($"CISD Level Created: {(isLong ? "BULLISH (Green)" : "BEARISH (Red)")} at {candidatePrice:F2}");
        }

        private void DrawCISDLevelLine(CISDLevel level)
        {
            string lineTag = $"CISD_Level_{(level.IsBullish ? "Bull" : "Bear")}_{CurrentBar}_{DateTime.Now.Ticks}";

            // Draw line from start candle time to current time
            var drawnLine = Draw.Line(this, lineTag,
                false,
                level.StartCandleTime, level.Price,    // Start: start candle time and price
                level.TimeCreated, level.Price,        // End: creation time at same price
                level.IsBullish ? cisdBullishColor : cisdBearishColor,
                DashStyleHelper.Solid,
                2);

            level.DrawnLine = drawnLine;

            Print($"Drawing CISD Level Line: {lineTag}, Direction: {(level.IsBullish ? "Bullish" : "Bearish")}, Price: {level.Price}");
        }

        private void CheckCISDLevelCrosses()
        {
            // In display-only mode, don't generate entry signals
            if (useCISDDisplayOnly)
                return;

            // Check LATEST bullish CISD level (green line) - wait for close ABOVE
            if (latestBullishCISDLevel != null && !latestBullishCISDLevel.IsTriggered)
            {
                // Check if close crossed above the level
                if (Close[0] > latestBullishCISDLevel.Price)
                {
                    latestBullishCISDLevel.IsTriggered = true;
                    Print($"CISD Bullish Level CROSSED! Price {latestBullishCISDLevel.Price:F2} crossed by close {Close[0]:F2}");

                    // Calculate stop loss: Lowest low between START candle and BREAK candle (current bar)
                    int startBar = latestBullishCISDLevel.StartCandleBar;
                    int barsBack = CurrentBar - startBar;
                    double stopLoss = GetLowestLowBetween(barsBack, 0);

                    Print($"Stop Loss Calculated: Lowest low between bar {startBar} and current bar {CurrentBar} = {stopLoss:F2}");

                    // Generate actual entry signal
                    SignalInfo signal = new SignalInfo
                    {
                        SignalType = "CISD",
                        OrderPosition = cisdSignalOrder,
                        IsLong = true,
                        SignalBar = CurrentBar,
                        SignalTime = Time[0],
                        EntryPrice = Close[0] + (cisdEntryPlusTicks * TickSize),
                        StopLoss = stopLoss,
                        UseStopLossRR = useCISDStopLossRR,
                        StopLossPlusTicks = cisdStopLossPlusTicks,
                        MaxBarsBetween = cisdMaxBarsBetween,
                        IsCombinedEntry = useCISDCombinedEntry,
                        IsStandalone = useCISDEntry && !useCISDCombinedEntry,  // Only standalone if NOT using combined
                        HasGeneratedTrade = false,
                        SwingPoint = latestBullishCISDLevel.Price,
                        SwingBar = latestBullishCISDLevel.StartCandleBar,
                        SwingTime = latestBullishCISDLevel.StartCandleTime,
                        BreakTime = Time[0],
                        DrawnLine = latestBullishCISDLevel.DrawnLine  // Keep reference to the line
                    };

                    AddSignal(signal);
                    Print($"CISD Entry Signal Generated: LONG at {Close[0]:F2}, StopLoss: {stopLoss:F2}");
                }
            }

            // Check LATEST bearish CISD level (red line) - wait for close BELOW
            if (latestBearishCISDLevel != null && !latestBearishCISDLevel.IsTriggered)
            {
                // Check if close crossed below the level
                if (Close[0] < latestBearishCISDLevel.Price)
                {
                    latestBearishCISDLevel.IsTriggered = true;
                    Print($"CISD Bearish Level CROSSED! Price {latestBearishCISDLevel.Price:F2} crossed by close {Close[0]:F2}");

                    // Calculate stop loss: Highest high between START candle and BREAK candle (current bar)
                    int startBar = latestBearishCISDLevel.StartCandleBar;
                    int barsBack = CurrentBar - startBar;
                    double stopLoss = GetHighestHighBetween(barsBack, 0);

                    Print($"Stop Loss Calculated: Highest high between bar {startBar} and current bar {CurrentBar} = {stopLoss:F2}");

                    // Generate actual entry signal
                    SignalInfo signal = new SignalInfo
                    {
                        SignalType = "CISD",
                        OrderPosition = cisdSignalOrder,
                        IsLong = false,
                        SignalBar = CurrentBar,
                        SignalTime = Time[0],
                        EntryPrice = Close[0] - (cisdEntryPlusTicks * TickSize),
                        StopLoss = stopLoss,
                        UseStopLossRR = useCISDStopLossRR,
                        StopLossPlusTicks = cisdStopLossPlusTicks,
                        MaxBarsBetween = cisdMaxBarsBetween,
                        IsCombinedEntry = useCISDCombinedEntry,
                        IsStandalone = useCISDEntry && !useCISDCombinedEntry,  // Only standalone if NOT using combined
                        HasGeneratedTrade = false,
                        SwingPoint = latestBearishCISDLevel.Price,
                        SwingBar = latestBearishCISDLevel.StartCandleBar,
                        SwingTime = latestBearishCISDLevel.StartCandleTime,
                        BreakTime = Time[0],
                        DrawnLine = latestBearishCISDLevel.DrawnLine  // Keep reference to the line
                    };

                    AddSignal(signal);
                    Print($"CISD Entry Signal Generated: SHORT at {Close[0]:F2}, StopLoss: {stopLoss:F2}");
                }
            }
        }

        private void DrawCISDLine(SignalInfo signal)
        {
            if (signal.SignalType != "CISD")
                return;

            // CISD line was already drawn when the level was created
            // The existing line (signal.DrawnLine) goes from candidate to structure break
            // We need to extend it to the crossing point (signal.BreakTime)

            if (signal.DrawnLine != null)
            {
                // Remove the old level line
                RemoveDrawObject(signal.DrawnLine.Tag);
            }

            // Draw new line from candidate time to CROSS time at candidate price level
            string lineTag = $"CISD_Trade_{(signal.IsLong ? "Bull" : "Bear")}_{CurrentBar}_{DateTime.Now.Ticks}";

            var drawnLine = Draw.Line(this, lineTag,
                false,
                signal.SwingTime, signal.SwingPoint,    // Start: candidate point time and price
                signal.BreakTime, signal.SwingPoint,    // End: cross time at same price
                signal.IsLong ? cisdBullishColor : cisdBearishColor,
                DashStyleHelper.Solid,
                2);

            // Store reference to updated line
            signal.DrawnLine = drawnLine;

            Print($"Drawing CISD Trade Line: {lineTag}, Direction: {(signal.IsLong ? "Bullish" : "Bearish")}, Price: {signal.SwingPoint}, StartTime: {signal.SwingTime}, CrossTime: {signal.BreakTime}");
        }

        // ===== FVG Entry Methods =====

        private void CheckForFVGSignal()
        {
            // Need at least 3 bars to detect FVG
            if (CurrentBar < 3)
                return;

            // Get bars 1, 2, 3 (matching FVG indicator logic)
            double h3 = High[3];
            double l3 = Low[3];
            DateTime t3 = Time[3];

            double h1 = High[1];
            double l1 = Low[1];

            // Check for FVG pattern
            // Bullish FVG: l1 > h3 (gap between bar 1 low and bar 3 high)
            if (l1 > h3)
            {
                // Check if FVG already exists at this time
                if (activeFVGLevels.Any(f => f.StartTime == t3 && f.IsBullish))
                    return;

                // Create bullish FVG
                FVGLevel fvg = new FVGLevel
                {
                    Top = l1,
                    Bottom = h3,
                    IsBullish = true,
                    StartTime = t3,
                    StartBar = CurrentBar - 3,
                    IsRetested = false,
                    IsMitigated = false,
                    HasGeneratedSignal = false,
                    DrawnBox = null
                };

                activeFVGLevels.Add(fvg);
                Print($"Bullish FVG Detected at bar {fvg.StartBar}: Top={fvg.Top:F2}, Bottom={fvg.Bottom:F2}");
            }
            // Bearish FVG: h1 < l3 (gap between bar 1 high and bar 3 low)
            else if (h1 < l3)
            {
                // Check if FVG already exists at this time
                if (activeFVGLevels.Any(f => f.StartTime == t3 && !f.IsBullish))
                    return;

                // Create bearish FVG
                FVGLevel fvg = new FVGLevel
                {
                    Top = l3,
                    Bottom = h1,
                    IsBullish = false,
                    StartTime = t3,
                    StartBar = CurrentBar - 3,
                    IsRetested = false,
                    IsMitigated = false,
                    HasGeneratedSignal = false,
                    DrawnBox = null
                };

                activeFVGLevels.Add(fvg);
                Print($"Bearish FVG Detected at bar {fvg.StartBar}: Top={fvg.Top:F2}, Bottom={fvg.Bottom:F2}");
            }
        }

        private void CheckFVGRetests()
        {
            // Check all active FVG levels for retests
            for (int i = activeFVGLevels.Count - 1; i >= 0; i--)
            {
                FVGLevel fvg = activeFVGLevels[i];

                // Skip if already retested, mitigated, or generated signal
                if (fvg.IsRetested || fvg.IsMitigated || fvg.HasGeneratedSignal)
                    continue;

                // Check if max bars to retest exceeded
                int barsSinceCreation = CurrentBar - fvg.StartBar;
                if (barsSinceCreation > fvgMaxBarsToRetest)
                {
                    Print($"FVG at bar {fvg.StartBar} expired - exceeded max bars to retest ({fvgMaxBarsToRetest})");
                    activeFVGLevels.RemoveAt(i);
                    continue;
                }

                // Check for retest
                if (fvg.IsBullish)
                {
                    // Bullish FVG retest: Low touches or goes below Top, but Close stays above Bottom
                    if (Low[0] <= fvg.Top && Close[0] > fvg.Bottom)
                    {
                        fvg.IsRetested = true;
                        fvg.RetestBar = CurrentBar;
                        fvg.RetestTime = Time[0];
                        Print($"Bullish FVG Retested at bar {CurrentBar}: Low={Low[0]:F2} touched Top={fvg.Top:F2}, Close={Close[0]:F2} > Bottom={fvg.Bottom:F2}");
                    }
                    // Check mitigation: Close below bottom
                    else if (Close[0] < fvg.Bottom)
                    {
                        fvg.IsMitigated = true;
                        Print($"Bullish FVG Mitigated at bar {CurrentBar}: Close={Close[0]:F2} < Bottom={fvg.Bottom:F2}");
                        activeFVGLevels.RemoveAt(i);
                    }
                }
                else
                {
                    // Bearish FVG retest: High touches or goes above Bottom, but Close stays below Top
                    if (High[0] >= fvg.Bottom && Close[0] < fvg.Top)
                    {
                        fvg.IsRetested = true;
                        fvg.RetestBar = CurrentBar;
                        fvg.RetestTime = Time[0];
                        Print($"Bearish FVG Retested at bar {CurrentBar}: High={High[0]:F2} touched Bottom={fvg.Bottom:F2}, Close={Close[0]:F2} < Top={fvg.Top:F2}");
                    }
                    // Check mitigation: Close above top
                    else if (Close[0] > fvg.Top)
                    {
                        fvg.IsMitigated = true;
                        Print($"Bearish FVG Mitigated at bar {CurrentBar}: Close={Close[0]:F2} > Top={fvg.Top:F2}");
                        activeFVGLevels.RemoveAt(i);
                    }
                }
            }
        }

        private void CheckFVGEntrySignals()
        {
            // Check all retested FVG levels for entry signals
            for (int i = activeFVGLevels.Count - 1; i >= 0; i--)
            {
                FVGLevel fvg = activeFVGLevels[i];

                // Skip if not retested, already generated signal, or mitigated
                if (!fvg.IsRetested || fvg.HasGeneratedSignal || fvg.IsMitigated)
                    continue;

                // Check if max bars after retest exceeded
                int barsSinceRetest = CurrentBar - fvg.RetestBar;
                if (barsSinceRetest > fvgMaxBarsAfterRetest)
                {
                    Print($"FVG at bar {fvg.StartBar} expired - exceeded max bars after retest ({fvgMaxBarsAfterRetest})");
                    activeFVGLevels.RemoveAt(i);
                    continue;
                }

                // CRITICAL: Check if FVG has been mitigated before generating entry
                if (fvg.IsBullish)
                {
                    // Check if current candle closed below FVG bottom (mitigation)
                    if (Close[0] < fvg.Bottom)
                    {
                        fvg.IsMitigated = true;
                        Print($"Bullish FVG at bar {fvg.StartBar} MITIGATED before entry - Close={Close[0]:F2} < Bottom={fvg.Bottom:F2}");
                        activeFVGLevels.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    // Check if current candle closed above FVG top (mitigation)
                    if (Close[0] > fvg.Top)
                    {
                        fvg.IsMitigated = true;
                        Print($"Bearish FVG at bar {fvg.StartBar} MITIGATED before entry - Close={Close[0]:F2} > Top={fvg.Top:F2}");
                        activeFVGLevels.RemoveAt(i);
                        continue;
                    }
                }

                // Check for entry signal after retest
                if (fvg.IsBullish)
                {
                    // Bullish entry: Green candle (Close > Open) closes above previous candle high AND above FVG top
                    bool isGreenCandle = Close[0] > Open[0];
                    bool closesAbovePreviousHigh = Close[0] > High[1];
                    bool closesAboveFVGTop = Close[0] > fvg.Top;

                    if (isGreenCandle && closesAbovePreviousHigh && closesAboveFVGTop)
                    {
                        fvg.HasGeneratedSignal = true;

                        // Calculate stop loss: FVG bottom - plus ticks
                        double stopLoss = fvg.Bottom - (fvgStopLossPlusTicks * TickSize);

                        Print($"FVG Bullish Entry Signal at bar {CurrentBar}: Green candle, Close={Close[0]:F2} > PrevHigh={High[1]:F2} AND > FVG Top={fvg.Top:F2}, StopLoss={stopLoss:F2}");

                        // Generate entry signal
                        SignalInfo signal = new SignalInfo
                        {
                            SignalType = "FVG",
                            OrderPosition = fvgSignalOrder,
                            IsLong = true,
                            SignalBar = CurrentBar,
                            SignalTime = Time[0],
                            EntryPrice = Close[0] + (fvgEntryPlusTicks * TickSize),
                            StopLoss = stopLoss,
                            UseStopLossRR = useFVGStopLossRR,
                            StopLossPlusTicks = fvgStopLossPlusTicks,
                            MaxBarsBetween = fvgMaxBarsBetween,
                            IsCombinedEntry = useFVGCombinedEntry,
                            IsStandalone = useFVGEntry && !useFVGCombinedEntry,
                            HasGeneratedTrade = false,
                            SwingPoint = fvg.Top,  // Store FVG top as swing point
                            SwingBar = fvg.StartBar,
                            SwingTime = fvg.StartTime,
                            BreakTime = Time[0]
                        };

                        // Store FVG reference in signal for later drawing
                        signal.Tag = fvg;

                        AddSignal(signal);
                        Print($"FVG Entry Signal Generated: LONG at {Close[0]:F2}, StopLoss: {stopLoss:F2}");
                    }
                }
                else
                {
                    // Bearish entry: Red candle (Close < Open) closes below previous candle low AND below FVG bottom
                    bool isRedCandle = Close[0] < Open[0];
                    bool closesBelowPreviousLow = Close[0] < Low[1];
                    bool closesBelowFVGBottom = Close[0] < fvg.Bottom;

                    if (isRedCandle && closesBelowPreviousLow && closesBelowFVGBottom)
                    {
                        fvg.HasGeneratedSignal = true;

                        // Calculate stop loss: FVG top + plus ticks
                        double stopLoss = fvg.Top + (fvgStopLossPlusTicks * TickSize);

                        Print($"FVG Bearish Entry Signal at bar {CurrentBar}: Red candle, Close={Close[0]:F2} < PrevLow={Low[1]:F2} AND < FVG Bottom={fvg.Bottom:F2}, StopLoss={stopLoss:F2}");

                        // Generate entry signal
                        SignalInfo signal = new SignalInfo
                        {
                            SignalType = "FVG",
                            OrderPosition = fvgSignalOrder,
                            IsLong = false,
                            SignalBar = CurrentBar,
                            SignalTime = Time[0],
                            EntryPrice = Close[0] - (fvgEntryPlusTicks * TickSize),
                            StopLoss = stopLoss,
                            UseStopLossRR = useFVGStopLossRR,
                            StopLossPlusTicks = fvgStopLossPlusTicks,
                            MaxBarsBetween = fvgMaxBarsBetween,
                            IsCombinedEntry = useFVGCombinedEntry,
                            IsStandalone = useFVGEntry && !useFVGCombinedEntry,
                            HasGeneratedTrade = false,
                            SwingPoint = fvg.Bottom,  // Store FVG bottom as swing point
                            SwingBar = fvg.StartBar,
                            SwingTime = fvg.StartTime,
                            BreakTime = Time[0]
                        };

                        // Store FVG reference in signal for later drawing
                        signal.Tag = fvg;

                        AddSignal(signal);
                        Print($"FVG Entry Signal Generated: SHORT at {Close[0]:F2}, StopLoss: {stopLoss:F2}");
                    }
                }
            }
        }

        private void DrawFVGRectangle(SignalInfo signal)
        {
            if (signal.SignalType != "FVG")
                return;

            // Get FVG from signal tag
            FVGLevel fvg = signal.Tag as FVGLevel;
            if (fvg == null)
                return;

            // Don't draw if already drawn
            if (fvg.DrawnBox != null)
                return;

            // Draw rectangle from FVG start time to entry signal time
            string boxTag = $"FVG_Trade_{(fvg.IsBullish ? "Bull" : "Bear")}_{fvg.StartBar}_{DateTime.Now.Ticks}";

            // Transparent outline, colored fill
            Brush fillColor = fvg.IsBullish ? fvgBullishColor : fvgBearishColor;

            var drawnBox = Draw.Rectangle(this, boxTag,
                false,
                fvg.StartTime, fvg.Top,      // Top-left corner
                signal.BreakTime, fvg.Bottom,  // Bottom-right corner
                Brushes.Transparent,         // Border color (transparent)
                fillColor,                   // Fill color
                fvgBoxOpacity);              // Opacity as integer percentage

            // Store reference
            fvg.DrawnBox = drawnBox;

            Print($"Drawing FVG Rectangle: {boxTag}, Direction: {(fvg.IsBullish ? "Bullish" : "Bearish")}, Top: {fvg.Top:F2}, Bottom: {fvg.Bottom:F2}, StartTime: {fvg.StartTime}, EndTime: {signal.BreakTime}, Opacity: {fvgBoxOpacity}%");
        }

        // ===== IFVG (Inverse FVG) Entry Methods =====

        private void DetectIFVG()
        {
            // Detect FVG patterns and add them to pending list (waiting for mitigation)
            if (CurrentBar < 4)
                return;

            // Get bars 1, 2, 3 (same logic as FVG)
            double h3 = High[3];
            double l3 = Low[3];
            DateTime t3 = Time[3];

            double h1 = High[1];
            double l1 = Low[1];

            // Check for Bullish FVG pattern (will become Bearish IFVG if mitigated)
            // Bullish FVG: l1 > h3 (gap between bar 1 low and bar 3 high)
            if (l1 > h3)
            {
                // Check if this FVG already exists in pending list
                if (pendingIFVGDetection.Any(f => f.FVGStartTime == t3 && !f.IsBullish))
                    return;

                // Create pending IFVG (will become Bearish IFVG when mitigated)
                IFVGLevel ifvg = new IFVGLevel
                {
                    Top = l1,
                    Bottom = h3,
                    IsBullish = false,  // Will be BEARISH IFVG (short) when bullish FVG is mitigated
                    FVGStartTime = t3,
                    FVGStartBar = CurrentBar - 3,
                    MitigationTime = DateTime.MinValue,
                    MitigationBar = -1,
                    HasGeneratedSignal = false,
                    DrawnBox = null
                };

                pendingIFVGDetection.Add(ifvg);
                Print($"Pending Bearish IFVG detected (Bullish FVG) at bar {ifvg.FVGStartBar}: Top={ifvg.Top:F2}, Bottom={ifvg.Bottom:F2}");
            }
            // Check for Bearish FVG pattern (will become Bullish IFVG if mitigated)
            // Bearish FVG: h1 < l3 (gap between bar 1 high and bar 3 low)
            else if (h1 < l3)
            {
                // Check if this FVG already exists in pending list
                if (pendingIFVGDetection.Any(f => f.FVGStartTime == t3 && f.IsBullish))
                    return;

                // Create pending IFVG (will become Bullish IFVG when mitigated)
                IFVGLevel ifvg = new IFVGLevel
                {
                    Top = l3,
                    Bottom = h1,
                    IsBullish = true,  // Will be BULLISH IFVG (long) when bearish FVG is mitigated
                    FVGStartTime = t3,
                    FVGStartBar = CurrentBar - 3,
                    MitigationTime = DateTime.MinValue,
                    MitigationBar = -1,
                    HasGeneratedSignal = false,
                    DrawnBox = null
                };

                pendingIFVGDetection.Add(ifvg);
                Print($"Pending Bullish IFVG detected (Bearish FVG) at bar {ifvg.FVGStartBar}: Top={ifvg.Top:F2}, Bottom={ifvg.Bottom:F2}");
            }
        }

        private void CheckIFVGMitigation()
        {
            // Check if any pending IFVGs have been mitigated
            for (int i = pendingIFVGDetection.Count - 1; i >= 0; i--)
            {
                IFVGLevel ifvg = pendingIFVGDetection[i];

                bool isMitigated = false;

                if (ifvg.IsBullish)
                {
                    // Bullish IFVG: Original was bearish FVG, mitigated when price closes ABOVE top
                    if (Close[0] > ifvg.Top)
                    {
                        isMitigated = true;
                        Print($"Bullish IFVG MITIGATED at bar {CurrentBar}: Close={Close[0]:F2} > Top={ifvg.Top:F2}");
                    }
                }
                else
                {
                    // Bearish IFVG: Original was bullish FVG, mitigated when price closes BELOW bottom
                    if (Close[0] < ifvg.Bottom)
                    {
                        isMitigated = true;
                        Print($"Bearish IFVG MITIGATED at bar {CurrentBar}: Close={Close[0]:F2} < Bottom={ifvg.Bottom:F2}");
                    }
                }

                if (isMitigated)
                {
                    // Update mitigation info
                    ifvg.MitigationTime = Time[0];
                    ifvg.MitigationBar = CurrentBar;

                    // Move from pending to active
                    activeIFVGLevels.Add(ifvg);
                    pendingIFVGDetection.RemoveAt(i);

                    // Generate entry signal immediately
                    GenerateIFVGEntry(ifvg);
                }
            }

            // Remove old pending IFVGs (older than 100 bars)
            pendingIFVGDetection.RemoveAll(ifvg => CurrentBar - ifvg.FVGStartBar > 100);
        }

        private void GenerateIFVGEntry(IFVGLevel ifvg)
        {
            if (ifvg.HasGeneratedSignal)
                return;

            // Calculate stop loss
            double stopLoss = 0;
            if (ifvg.IsBullish)
            {
                // Bullish IFVG (Long): Stop = Bottom of box - x ticks
                stopLoss = ifvg.Bottom - (ifvgStopLossPlusTicks * TickSize);
            }
            else
            {
                // Bearish IFVG (Short): Stop = Top of box + x ticks
                stopLoss = ifvg.Top + (ifvgStopLossPlusTicks * TickSize);
            }

            Print($"IFVG Entry: {(ifvg.IsBullish ? "LONG" : "SHORT")}, Entry={Close[0]:F2}, Stop={stopLoss:F2}");

            // Check if IFVG entry is enabled
            bool isStandalone = useIFVGEntry && !useIFVGCombinedEntry;
            bool willGenerateSignal = useIFVGEntry || useIFVGCombinedEntry || useIFVGDisplayOnly;

            if (!willGenerateSignal)
                return;

            // Generate signal
            SignalInfo signal = new SignalInfo
            {
                SignalType = "IFVG",
                OrderPosition = ifvgSignalOrder,
                IsLong = ifvg.IsBullish,
                SignalBar = CurrentBar,
                SignalTime = Time[0],
                EntryPrice = ifvg.IsBullish ? Close[0] + (ifvgEntryPlusTicks * TickSize) : Close[0] - (ifvgEntryPlusTicks * TickSize),
                StopLoss = stopLoss,
                UseStopLossRR = useIFVGStopLossRR,  // Use R:R if enabled
                StopLossPlusTicks = ifvgStopLossPlusTicks,
                MaxBarsBetween = ifvgMaxBarsBetween,
                IsCombinedEntry = useIFVGCombinedEntry,
                IsStandalone = isStandalone,
                HasGeneratedTrade = false,
                BreakTime = Time[0],
                Tag = ifvg  // Store IFVG reference
            };

            ifvg.HasGeneratedSignal = true;
            AddSignal(signal);
            Print($"IFVG Entry Signal Generated: {(ifvg.IsBullish ? "LONG" : "SHORT")} at {Close[0]:F2}, StopLoss: {stopLoss:F2}");
        }

        private void DrawIFVGRectangle(SignalInfo signal)
        {
            if (signal.SignalType != "IFVG")
                return;

            // Get IFVG from signal tag
            IFVGLevel ifvg = signal.Tag as IFVGLevel;
            if (ifvg == null)
                return;

            // Check if box already drawn
            if (ifvg.DrawnBox != null)
                return;

            string boxTag = $"IFVG_Box_{ifvg.MitigationBar}_{(ifvg.IsBullish ? "Bull" : "Bear")}";

            // Draw rectangle from FVG start to mitigation bar - transparent outline, colored fill
            Brush fillColor = ifvg.IsBullish ? ifvgBullishColor : ifvgBearishColor;

            var drawnBox = Draw.Rectangle(this,
                boxTag,
                false,
                ifvg.FVGStartTime,
                ifvg.Top,
                ifvg.MitigationTime,
                ifvg.Bottom,
                Brushes.Transparent,  // Border color (transparent)
                fillColor,            // Fill color
                ifvgBoxOpacity);

            // Store reference
            ifvg.DrawnBox = drawnBox;

            Print($"Drawing IFVG Rectangle: {boxTag}, Direction: {(ifvg.IsBullish ? "Bullish" : "Bearish")}, Top: {ifvg.Top:F2}, Bottom: {ifvg.Bottom:F2}, Mitigation Bar: {ifvg.MitigationBar}, Opacity: {ifvgBoxOpacity}%");
        }

        // ===== Sweep Entry Methods =====

        private void CheckForLTFSweepSignal()
        {
            // LTF Sweep uses chart timeframe (BarsInProgress == 0)
            if (CurrentBar < SweepSwingLength * 2 + 1)
                return;

            // Detect new pivot highs and lows using custom method
            double? pivotHigh = GetSweepPivotHigh(0, SweepSwingLength, SweepSwingLength);
            double? pivotLow = GetSweepPivotLow(0, SweepSwingLength, SweepSwingLength);

            // Add new LTF pivot highs
            if (pivotHigh.HasValue)
            {
                int pivotBar = CurrentBar - SweepSwingLength;

                // Check if pivot already exists
                if (!sweepLTFPivotHighs.Any(p => p.BarIndex == pivotBar))
                {
                    sweepLTFPivotHighs.Insert(0, new SweepPivotData
                    {
                        Price = pivotHigh.Value,
                        BarIndex = pivotBar,
                        Time = Time[SweepSwingLength],
                        IsBreak = false,
                        IsMitigated = false,
                        IsWick = false,
                        HasGeneratedSignal = false,
                        IsHTF = false
                    });
                    Print($"LTF Pivot HIGH detected at bar {pivotBar}: Price={pivotHigh.Value:F2}");
                }
            }

            // Add new LTF pivot lows
            if (pivotLow.HasValue)
            {
                int pivotBar = CurrentBar - SweepSwingLength;

                // Check if pivot already exists
                if (!sweepLTFPivotLows.Any(p => p.BarIndex == pivotBar))
                {
                    sweepLTFPivotLows.Insert(0, new SweepPivotData
                    {
                        Price = pivotLow.Value,
                        BarIndex = pivotBar,
                        Time = Time[SweepSwingLength],
                        IsBreak = false,
                        IsMitigated = false,
                        IsWick = false,
                        HasGeneratedSignal = false,
                        IsHTF = false
                    });
                    Print($"LTF Pivot LOW detected at bar {pivotBar}: Price={pivotLow.Value:F2}");
                }
            }

            // Process LTF sweeps
            ProcessSweeps(sweepLTFPivotHighs, sweepLTFPivotLows, High[0], Low[0], Close[0], false, "LTF_Sweep", CurrentBar, Time[0]);
        }

        // Update HTF pivot list when HTF bar closes (BarsInProgress == 1)
        private void UpdateHTFPivots()
        {
            int htfIndex = 1;

            if (CurrentBars[htfIndex] < SweepSwingLength * 2 + 1)
                return;

            // Detect new pivot highs and lows using custom method on HTF
            double? pivotHigh = GetSweepPivotHigh(htfIndex, SweepSwingLength, SweepSwingLength);
            double? pivotLow = GetSweepPivotLow(htfIndex, SweepSwingLength, SweepSwingLength);

            // Add new HTF pivot highs
            if (pivotHigh.HasValue)
            {
                int pivotBar = CurrentBars[htfIndex] - SweepSwingLength;

                // Check if pivot already exists
                if (!sweepHTFPivotHighs.Any(p => p.BarIndex == pivotBar))
                {
                    sweepHTFPivotHighs.Insert(0, new SweepPivotData
                    {
                        Price = pivotHigh.Value,
                        BarIndex = pivotBar,
                        Time = Times[htfIndex][SweepSwingLength],
                        IsBreak = false,
                        IsMitigated = false,
                        IsWick = false,
                        HasGeneratedSignal = false,
                        IsHTF = true
                    });
                    Print($"HTF Pivot HIGH detected at HTF bar {pivotBar}: Price={pivotHigh.Value:F2}");
                }
            }

            // Add new HTF pivot lows
            if (pivotLow.HasValue)
            {
                int pivotBar = CurrentBars[htfIndex] - SweepSwingLength;

                // Check if pivot already exists
                if (!sweepHTFPivotLows.Any(p => p.BarIndex == pivotBar))
                {
                    sweepHTFPivotLows.Insert(0, new SweepPivotData
                    {
                        Price = pivotLow.Value,
                        BarIndex = pivotBar,
                        Time = Times[htfIndex][SweepSwingLength],
                        IsBreak = false,
                        IsMitigated = false,
                        IsWick = false,
                        HasGeneratedSignal = false,
                        IsHTF = true
                    });
                    Print($"HTF Pivot LOW detected at HTF bar {pivotBar}: Price={pivotLow.Value:F2}");
                }
            }
        }

        // Check PRIMARY bars against HTF pivots for sweeps (runs on BarsInProgress == 0)
        private void CheckForHTFSweepSignal()
        {
            if (CurrentBar < SweepSwingLength * 2 + 1)
                return;

            // Process HTF sweeps using PRIMARY SERIES bar data
            // Check every primary bar against HTF pivots
            ProcessSweeps(sweepHTFPivotHighs, sweepHTFPivotLows, High[0], Low[0], Close[0], true, "HTF_Sweep", CurrentBar, Time[0]);
        }

        private double? GetSweepPivotHigh(int barsSeriesIndex, int leftBars, int rightBars)
        {
            if (CurrentBars[barsSeriesIndex] < leftBars + rightBars)
                return null;

            double pivotValue = Highs[barsSeriesIndex][rightBars];
            bool isPivot = true;

            // Check left bars
            for (int i = 1; i <= leftBars; i++)
            {
                if (Highs[barsSeriesIndex][rightBars + i] >= pivotValue)
                {
                    isPivot = false;
                    break;
                }
            }

            if (!isPivot)
                return null;

            // Check right bars
            for (int i = 0; i < rightBars; i++)
            {
                if (Highs[barsSeriesIndex][i] > pivotValue)
                {
                    isPivot = false;
                    break;
                }
            }

            return isPivot ? pivotValue : (double?)null;
        }

        private double? GetSweepPivotLow(int barsSeriesIndex, int leftBars, int rightBars)
        {
            if (CurrentBars[barsSeriesIndex] < leftBars + rightBars)
                return null;

            double pivotValue = Lows[barsSeriesIndex][rightBars];
            bool isPivot = true;

            // Check left bars
            for (int i = 1; i <= leftBars; i++)
            {
                if (Lows[barsSeriesIndex][rightBars + i] <= pivotValue)
                {
                    isPivot = false;
                    break;
                }
            }

            if (!isPivot)
                return null;

            // Check right bars
            for (int i = 0; i < rightBars; i++)
            {
                if (Lows[barsSeriesIndex][i] < pivotValue)
                {
                    isPivot = false;
                    break;
                }
            }

            return isPivot ? pivotValue : (double?)null;
        }

        private void ProcessSweeps(List<SweepPivotData> pivotHighs, List<SweepPivotData> pivotLows,
                                   double currHigh, double currLow, double currClose, bool isHTF, string signalType,
                                   int currentBarIndex, DateTime currentBarTime)
        {
            // Process pivot highs for bearish sweeps
            for (int i = pivotHighs.Count - 1; i >= 0; i--)
            {
                SweepPivotData pivot = pivotHighs[i];

                // Skip if already generated signal or mitigated
                if (pivot.HasGeneratedSignal || pivot.IsMitigated)
                {
                    // Remove old pivots
                    if (currentBarIndex - pivot.BarIndex > 2000)
                    {
                        pivotHighs.RemoveAt(i);
                    }
                    continue;
                }

                // Check for wick sweep (bearish): High > pivot but Close < pivot
                if (!pivot.IsWick && currHigh > pivot.Price && currClose < pivot.Price)
                {
                    pivot.IsWick = true;
                    pivot.HasGeneratedSignal = true;
                    Print($"{signalType} BEARISH SWEEP detected at Bar {currentBarIndex}: High={currHigh:F2} > Pivot={pivot.Price:F2}, Close={currClose:F2} < Pivot");

                    // Calculate stop loss: Last swing HIGH with sensitivity 5
                    double? lastSwingHigh = GetLastSwingHigh(SweepSwingLength);
                    double stopLoss = 0;

                    if (lastSwingHigh.HasValue)
                    {
                        stopLoss = lastSwingHigh.Value + (sweepSwingStopLossPlusTicks * TickSize);
                        Print($"{signalType} Bearish Stop Loss: Last Swing High={lastSwingHigh.Value:F2} + {sweepSwingStopLossPlusTicks} ticks = {stopLoss:F2}");
                    }
                    else
                    {
                        stopLoss = pivot.Price + (sweepSwingStopLossPlusTicks * TickSize);
                        Print($"{signalType} Bearish Stop Loss (fallback): Pivot={pivot.Price:F2} + {sweepSwingStopLossPlusTicks} ticks = {stopLoss:F2}");
                    }

                    // Check if this sweep will generate a signal (standalone or combined)
                    bool isStandalone = isHTF ? (useHTFSweepEntry && !useHTFSweepCombinedEntry) : (useLTFSweepEntry && !useLTFSweepCombinedEntry);
                    bool isCombined = isHTF ? useHTFSweepCombinedEntry : useLTFSweepCombinedEntry;
                    bool willGenerateSignal = isHTF ?
                        (useHTFSweepEntry || useHTFSweepCombinedEntry || useSweepDisplayOnly) :
                        (useLTFSweepEntry || useLTFSweepCombinedEntry || useSweepDisplayOnly);

                    if (willGenerateSignal)
                    {
                        // ONLY draw arrow for standalone or display-only, NOT for combined entries
                        // Combined entries will draw their arrow when the trade actually executes
                        if (isStandalone || useSweepDisplayOnly)
                        {
                            DrawSweepArrowNow(false, isHTF);  // Bearish = false
                        }

                        // Generate BEARISH entry signal
                        SignalInfo signal = new SignalInfo
                        {
                            SignalType = signalType,
                            OrderPosition = sweepSignalOrder,
                            IsLong = false,
                            SignalBar = currentBarIndex,
                            SignalTime = currentBarTime,
                            EntryPrice = Close[0] - (sweepEntryPlusTicks * TickSize),
                            StopLoss = stopLoss,
                            UseStopLossRR = useSweepSwingStopLossRR,
                            StopLossPlusTicks = sweepSwingStopLossPlusTicks,
                            MaxBarsBetween = sweepMaxBarsBetween,
                            IsCombinedEntry = isCombined,
                            IsStandalone = isStandalone,
                            HasGeneratedTrade = false,
                            SwingPoint = pivot.Price,
                            SwingBar = pivot.BarIndex,
                            SwingTime = pivot.Time,
                            BreakTime = currentBarTime,
                            Tag = isHTF  // Store HTF flag in Tag
                        };

                        AddSignal(signal);
                        Print($"{signalType} Entry Signal Generated: SHORT at {Close[0]:F2}, StopLoss: {stopLoss:F2}, Bar: {currentBarIndex}, Standalone: {isStandalone}, Combined: {isCombined}");
                    }
                }

                // Check if mitigated (close above pivot)
                if (currClose > pivot.Price && !pivot.IsWick)
                {
                    pivot.IsMitigated = true;
                    Print($"{signalType} Pivot HIGH at {pivot.Price:F2} MITIGATED - Close={currClose:F2} > Pivot");
                }
            }

            // Process pivot lows for bullish sweeps
            for (int i = pivotLows.Count - 1; i >= 0; i--)
            {
                SweepPivotData pivot = pivotLows[i];

                // Skip if already generated signal or mitigated
                if (pivot.HasGeneratedSignal || pivot.IsMitigated)
                {
                    // Remove old pivots
                    if (currentBarIndex - pivot.BarIndex > 2000)
                    {
                        pivotLows.RemoveAt(i);
                    }
                    continue;
                }

                // Check for wick sweep (bullish): Low < pivot but Close > pivot
                if (!pivot.IsWick && currLow < pivot.Price && currClose > pivot.Price)
                {
                    pivot.IsWick = true;
                    pivot.HasGeneratedSignal = true;
                    Print($"{signalType} BULLISH SWEEP detected at Bar {currentBarIndex}: Low={currLow:F2} < Pivot={pivot.Price:F2}, Close={currClose:F2} > Pivot");

                    // Calculate stop loss: Last swing LOW with sensitivity 5
                    double? lastSwingLow = GetLastSwingLow(SweepSwingLength);
                    double stopLoss = 0;

                    if (lastSwingLow.HasValue)
                    {
                        stopLoss = lastSwingLow.Value - (sweepSwingStopLossPlusTicks * TickSize);
                        Print($"{signalType} Bullish Stop Loss: Last Swing Low={lastSwingLow.Value:F2} - {sweepSwingStopLossPlusTicks} ticks = {stopLoss:F2}");
                    }
                    else
                    {
                        stopLoss = pivot.Price - (sweepSwingStopLossPlusTicks * TickSize);
                        Print($"{signalType} Bullish Stop Loss (fallback): Pivot={pivot.Price:F2} - {sweepSwingStopLossPlusTicks} ticks = {stopLoss:F2}");
                    }

                    // Check if this sweep will generate a signal (standalone or combined)
                    bool isStandalone = isHTF ? (useHTFSweepEntry && !useHTFSweepCombinedEntry) : (useLTFSweepEntry && !useLTFSweepCombinedEntry);
                    bool isCombined = isHTF ? useHTFSweepCombinedEntry : useLTFSweepCombinedEntry;
                    bool willGenerateSignal = isHTF ?
                        (useHTFSweepEntry || useHTFSweepCombinedEntry || useSweepDisplayOnly) :
                        (useLTFSweepEntry || useLTFSweepCombinedEntry || useSweepDisplayOnly);

                    if (willGenerateSignal)
                    {
                        // ONLY draw arrow for standalone or display-only, NOT for combined entries
                        // Combined entries will draw their arrow when the trade actually executes
                        if (isStandalone || useSweepDisplayOnly)
                        {
                            DrawSweepArrowNow(true, isHTF);  // Bullish = true
                        }

                        // Generate BULLISH entry signal
                        SignalInfo signal = new SignalInfo
                        {
                            SignalType = signalType,
                            OrderPosition = sweepSignalOrder,
                            IsLong = true,
                            SignalBar = currentBarIndex,
                            SignalTime = currentBarTime,
                            EntryPrice = Close[0] + (sweepEntryPlusTicks * TickSize),
                            StopLoss = stopLoss,
                            UseStopLossRR = useSweepSwingStopLossRR,
                            StopLossPlusTicks = sweepSwingStopLossPlusTicks,
                            MaxBarsBetween = sweepMaxBarsBetween,
                            IsCombinedEntry = isCombined,
                            IsStandalone = isStandalone,
                            HasGeneratedTrade = false,
                            SwingPoint = pivot.Price,
                            SwingBar = pivot.BarIndex,
                            SwingTime = pivot.Time,
                            BreakTime = currentBarTime,
                            Tag = isHTF  // Store HTF flag in Tag
                        };

                        AddSignal(signal);
                        Print($"{signalType} Entry Signal Generated: LONG at {Close[0]:F2}, StopLoss: {stopLoss:F2}, Bar: {currentBarIndex}, Standalone: {isStandalone}, Combined: {isCombined}");
                    }
                }

                // Check if mitigated (close below pivot)
                if (currClose < pivot.Price && !pivot.IsWick)
                {
                    pivot.IsMitigated = true;
                    Print($"{signalType} Pivot LOW at {pivot.Price:F2} MITIGATED - Close={currClose:F2} < Pivot");
                }
            }
        }

        // Helper method to get last swing high before current bar
        private double? GetLastSwingHigh(int sensitivity)
        {
            try
            {
                double? swingHigh = Swing(sensitivity).SwingHigh[0];
                if (swingHigh != null && swingHigh > 0)
                {
                    return swingHigh.Value;
                }
            }
            catch (Exception ex)
            {
                Print($"Error getting last swing high: {ex.Message}");
            }
            return null;
        }

        // Helper method to get last swing low before current bar
        private double? GetLastSwingLow(int sensitivity)
        {
            try
            {
                double? swingLow = Swing(sensitivity).SwingLow[0];
                if (swingLow != null && swingLow > 0)
                {
                    return swingLow.Value;
                }
            }
            catch (Exception ex)
            {
                Print($"Error getting last swing low: {ex.Message}");
            }
            return null;
        }

        // Draw sweep arrow IMMEDIATELY when sweep is detected
        private void DrawSweepArrowNow(bool isBullish, bool isHTF)
        {
            string arrowTag = $"Sweep_Arrow_{(isHTF ? "HTF" : "LTF")}_{(isBullish ? "Bull" : "Bear")}_{CurrentBar}_{sweepArrowCounter++}";

            // Select color based on LTF/HTF and direction
            Brush arrowColor;
            if (isHTF)
            {
                arrowColor = isBullish ? sweepHTFBullishColor : sweepHTFBearishColor;
            }
            else
            {
                arrowColor = isBullish ? sweepLTFBullishColor : sweepLTFBearishColor;
            }

            if (isBullish)
            {
                // Bullish sweep - arrow pointing up below the low
                Draw.ArrowUp(this, arrowTag, true, 0, Low[0] - (5 * TickSize), arrowColor);
            }
            else
            {
                // Bearish sweep - arrow pointing down above the high
                Draw.ArrowDown(this, arrowTag, true, 0, High[0] + (5 * TickSize), arrowColor);
            }

            Print($"Drawing {(isHTF ? "HTF" : "LTF")} Sweep Arrow: {arrowTag} at bar {CurrentBar}, Direction: {(isBullish ? "Bullish" : "Bearish")}, Time: {Time[0]}, Price: {(isBullish ? Low[0] : High[0]):F2}");
        }

        #endregion

        #region HeikenAshi Entry Detection

        private void CheckForLTFHeikenAshiSignal()
        {
            if (CurrentBar < 1 || heikenAshiLTF == null)
            {
                if (CurrentBar % 100 == 0)
                    Print($"LTF HeikenAshi check skipped - CurrentBar: {CurrentBar}, Indicator null: {heikenAshiLTF == null}");
                return;
            }

            // Get current LTF HeikenAshi signal
            int currentSignal = heikenAshiLTF.signal[0];
            int previousSignal = heikenAshiLTF.signal[1];

            // COLOR PRIMARY CHART CANDLES based on current LTF trend (happens EVERY bar)
            if (currentSignal == 1)
            {
                // LTF is bullish - color primary chart candle with bullish color
                BarBrush = heikenAshiLTFBullishColor;
                currentLTFTrend = 1;
            }
            else if (currentSignal == 2)
            {
                // LTF is bearish - color primary chart candle with bearish color
                BarBrush = heikenAshiLTFBearishColor;
                currentLTFTrend = 2;
            }

            // Debug every 50 bars
            if (CurrentBar % 50 == 0)
                Print($"LTF HeikenAshi - Bar: {CurrentBar}, Current: {currentSignal}, Previous: {previousSignal}, Entry: {useLTFHeikenAshiEntry}, Combined: {useLTFHeikenAshiCombinedEntry}, Display: {useLTFHeikenAshiDisplayOnly}");

            // DETECT COLOR CHANGE for entry signal generation
            bool bullishChange = (previousSignal == 2 && currentSignal == 1);  // Red to Cyan = LONG
            bool bearishChange = (previousSignal == 1 && currentSignal == 2);  // Cyan to Red = SHORT

            if (bullishChange)
            {
                Print($"*** LTF HeikenAshi BULLISH CHANGE detected at bar {CurrentBar}! Previous: {previousSignal}, Current: {currentSignal}");
                GenerateHeikenAshiEntry(true, false);  // isLong=true, isHTF=false
            }
            else if (bearishChange)
            {
                Print($"*** LTF HeikenAshi BEARISH CHANGE detected at bar {CurrentBar}! Previous: {previousSignal}, Current: {currentSignal}");
                GenerateHeikenAshiEntry(false, false);  // isLong=false, isHTF=false
            }
        }

        private void CheckForHTFHeikenAshiSignal()
        {
            if (CurrentBar < 1 || heikenAshiHTF == null)
            {
                if (CurrentBar % 100 == 0)
                    Print($"HTF HeikenAshi check skipped - CurrentBar: {CurrentBar}, Indicator null: {heikenAshiHTF == null}");
                return;
            }

            // Determine HTF index
            int htfIndex = 1;
            if ((useHTFSweepEntry || useHTFSweepCombinedEntry || useSweepDisplayOnly)
                && sweepHTFTimeframe != heikenAshiHTFTimeframe)
            {
                htfIndex = 2;
            }

            // Make sure HTF bar is available
            if (CurrentBars[htfIndex] < 1)
            {
                if (CurrentBar % 100 == 0)
                    Print($"HTF HeikenAshi check skipped - HTF bars not available yet");
                return;
            }

            // Get current HTF HeikenAshi signal
            int currentSignal = heikenAshiHTF.signal[0];
            int previousSignal = heikenAshiHTF.signal[1];

            // COLOR PRIMARY CHART CANDLES based on current HTF trend (happens EVERY bar)
            if (currentSignal == 1)
            {
                // HTF is bullish - color primary chart candle with bullish color
                BarBrush = heikenAshiHTFBullishColor;
                currentHTFTrend = 1;
            }
            else if (currentSignal == 2)
            {
                // HTF is bearish - color primary chart candle with bearish color
                BarBrush = heikenAshiHTFBearishColor;
                currentHTFTrend = 2;
            }

            // Debug every 50 bars
            if (CurrentBar % 50 == 0)
                Print($"HTF HeikenAshi - Bar: {CurrentBar}, Current: {currentSignal}, Previous: {previousSignal}, Entry: {useHTFHeikenAshiEntry}, Combined: {useHTFHeikenAshiCombinedEntry}, Display: {useHTFHeikenAshiDisplayOnly}");

            // DETECT COLOR CHANGE for entry signal generation
            bool bullishChange = (previousSignal == 2 && currentSignal == 1);  // Red to Aqua = LONG
            bool bearishChange = (previousSignal == 1 && currentSignal == 2);  // Aqua to Red = SHORT

            if (bullishChange)
            {
                Print($"*** HTF HeikenAshi BULLISH CHANGE detected at bar {CurrentBar}! Previous: {previousSignal}, Current: {currentSignal}");
                GenerateHeikenAshiEntry(true, true);  // isLong=true, isHTF=true
            }
            else if (bearishChange)
            {
                Print($"*** HTF HeikenAshi BEARISH CHANGE detected at bar {CurrentBar}! Previous: {previousSignal}, Current: {currentSignal}");
                GenerateHeikenAshiEntry(false, true);  // isLong=false, isHTF=true
            }
        }

        private void GenerateHeikenAshiEntry(bool isLong, bool isHTF)
        {
            string entryType = isHTF ? "HTF_HeikenAshi" : "LTF_HeikenAshi";

            // Determine if this is standalone, combined, or display only
            bool isStandalone = isHTF
                ? (useHTFHeikenAshiEntry && !useHTFHeikenAshiCombinedEntry)
                : (useLTFHeikenAshiEntry && !useLTFHeikenAshiCombinedEntry);

            bool isCombined = isHTF ? useHTFHeikenAshiCombinedEntry : useLTFHeikenAshiCombinedEntry;
            bool isDisplayOnly = isHTF ? useHTFHeikenAshiDisplayOnly : useLTFHeikenAshiDisplayOnly;

            // Update HTF trend state if this is HTF signal (used for candle coloring)
            if (isHTF)
            {
                currentHTFTrend = isLong ? 1 : 2;  // 1=bullish, 2=bearish
            }

            // Skip signal generation if ONLY display-only is enabled (no entry, no combined)
            if (isDisplayOnly && !isCombined && !isStandalone)
            {
                Print($"{entryType} Display Only mode - no trade signal generated");
                return;
            }

            // Calculate entry price
            double entryPrice = isLong
                ? Close[0] + (heikenAshiEntryPlusTicks * TickSize)
                : Close[0] - (heikenAshiEntryPlusTicks * TickSize);

            // For HeikenAshi, stop loss is handled by swing stop if enabled, else no stop
            // This will be handled in ExecuteEntry method
            double stopLoss = 0;  // No integrated stop loss for HeikenAshi
            bool useStopLossRR = false;  // HeikenAshi uses swing stop or no stop

            SignalInfo signal = new SignalInfo
            {
                SignalID = signalCounter++,
                SignalType = entryType,
                IsLong = isLong,
                EntryPrice = entryPrice,
                StopLoss = stopLoss,
                SignalTime = Time[0],
                SignalBar = CurrentBar,
                OrderPosition = heikenAshiSignalOrder,
                MaxBarsBetween = heikenAshiMaxBarsBetween,
                IsCombinedEntry = isCombined,
                IsStandalone = isStandalone,  // CRITICAL: Must set this for standalone entries to work!
                UseStopLossRR = useStopLossRR,
                Tag = isHTF  // Store whether this is HTF
            };

            AddSignal(signal);

            Print($"{entryType} {(isLong ? "LONG" : "SHORT")} signal generated at bar {CurrentBar}, Price: {Close[0]:F2}, Entry: {entryPrice:F2}, Standalone: {isStandalone}, Combined: {isCombined}, signal.IsStandalone: {signal.IsStandalone}");
        }

        #endregion

        #region Properties

        // ===== 001. =====General Strategy Settings===== =====
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name="001.01 1st Trade Quantity", Order=10101, GroupName="001. =====General Strategy Settings=====")]
        public int Trade1Quantity
        {
            get { return trade1Quantity; }
            set { trade1Quantity = Math.Max(0, value); }
        }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name="001.02 2nd Trade Quantity", Order=10102, GroupName="001. =====General Strategy Settings=====")]
        public int Trade2Quantity
        {
            get { return trade2Quantity; }
            set { trade2Quantity = Math.Max(0, value); }
        }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name="001.03 3rd Trade Quantity", Order=10103, GroupName="001. =====General Strategy Settings=====")]
        public int Trade3Quantity
        {
            get { return trade3Quantity; }
            set { trade3Quantity = Math.Max(0, value); }
        }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name="001.04 4th Trade Quantity", Order=10104, GroupName="001. =====General Strategy Settings=====")]
        public int Trade4Quantity
        {
            get { return trade4Quantity; }
            set { trade4Quantity = Math.Max(0, value); }
        }

        [NinjaScriptProperty]
        [Display(Name="001.07 Allow Multiple Trades", Order=10107, GroupName="001. =====General Strategy Settings=====")]
        public bool AllowMultipleTrades
        {
            get { return allowMultipleTrades; }
            set { allowMultipleTrades = value; }
        }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="001.08 Max Positions Allowed", Order=10108, GroupName="001. =====General Strategy Settings=====")]
        public int MaxPositionsAllowed
        {
            get { return maxPositionsAllowed; }
            set { maxPositionsAllowed = Math.Max(1, value); }
        }

        // ===== 005. BOS Entry Settings =====
        [NinjaScriptProperty]
        [Display(Name="005.01 Use BOS Entry", Order=50101, GroupName="005. =====BOS Entry=====")]
        public bool UseBOSEntry
        {
            get { return useBOSEntry; }
            set { useBOSEntry = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="005.02 Use BOS Display Only", Order=50102, GroupName="005. =====BOS Entry=====")]
        public bool UseBOSDisplayOnly
        {
            get { return useBOSDisplayOnly; }
            set { useBOSDisplayOnly = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="005.03 Use BOS Combined Entry", Order=50103, GroupName="005. =====BOS Entry=====")]
        public bool UseBOSCombinedEntry
        {
            get { return useBOSCombinedEntry; }
            set { useBOSCombinedEntry = value; }
        }

        [NinjaScriptProperty]
        [Range(0, 8)]
        [Display(Name="005.04 Signal Order (0=Any, 1=1st, 2=2nd...)", Order=50104, GroupName="005. =====BOS Entry=====")]
        public int BOSSignalOrder
        {
            get { return bosSignalOrder; }
            set { bosSignalOrder = Math.Max(0, Math.Min(8, value)); }
        }

        [NinjaScriptProperty]
        [Range(1, 50)]
        [Display(Name="005.05 Pivot Left Bars", Order=50105, GroupName="005. =====BOS Entry=====")]
        public int BOSPivotLeftBars
        {
            get { return bosPivotLeftBars; }
            set { bosPivotLeftBars = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [Range(1, 50)]
        [Display(Name="005.06 Pivot Right Bars", Order=50106, GroupName="005. =====BOS Entry=====")]
        public int BOSPivotRightBars
        {
            get { return bosPivotRightBars; }
            set { bosPivotRightBars = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [Range(1, 200)]
        [Display(Name="005.07 Max Bars To Break (default 30)", Order=50107, GroupName="005. =====BOS Entry=====")]
        public int BOSMaxBarsToBreak
        {
            get { return bosMaxBarsToBreak; }
            set { bosMaxBarsToBreak = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [Range(1, 500)]
        [Display(Name="005.08 Max Bars Between Signals", Order=50108, GroupName="005. =====BOS Entry=====")]
        public int BOSMaxBarsBetween
        {
            get { return bosMaxBarsBetween; }
            set { bosMaxBarsBetween = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name="005.09 BOS Entry Plus Ticks", Order=50109, GroupName="005. =====BOS Entry=====")]
        public int BOSEntryPlusTicks
        {
            get { return bosEntryPlusTicks; }
            set { bosEntryPlusTicks = Math.Max(0, value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="005.10 BOS Bullish Line Color", Order=50110, GroupName="005. =====BOS Entry=====")]
        public System.Windows.Media.Brush BOSBullishColor
        {
            get { return bosBullishColor; }
            set { bosBullishColor = value; }
        }

        [Browsable(false)]
        public string BOSBullishColorSerializable
        {
            get { return Serialize.BrushToString(bosBullishColor); }
            set { bosBullishColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="005.11 BOS Bearish Line Color", Order=50111, GroupName="005. =====BOS Entry=====")]
        public System.Windows.Media.Brush BOSBearishColor
        {
            get { return bosBearishColor; }
            set { bosBearishColor = value; }
        }

        [Browsable(false)]
        public string BOSBearishColorSerializable
        {
            get { return Serialize.BrushToString(bosBearishColor); }
            set { bosBearishColor = Serialize.StringToBrush(value); }
        }

        // ===== 004. CISD Entry Settings =====
        [NinjaScriptProperty]
        [Display(Name="004.01 Use CISD Entry", Order=60101, GroupName="004. =====CISD Entry=====")]
        public bool UseCISDEntry
        {
            get { return useCISDEntry; }
            set { useCISDEntry = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="004.02 Use CISD Display Only", Order=60102, GroupName="004. =====CISD Entry=====")]
        public bool UseCISDDisplayOnly
        {
            get { return useCISDDisplayOnly; }
            set { useCISDDisplayOnly = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="004.03 Use CISD Combined Entry", Order=60103, GroupName="004. =====CISD Entry=====")]
        public bool UseCISDCombinedEntry
        {
            get { return useCISDCombinedEntry; }
            set { useCISDCombinedEntry = value; }
        }

        [NinjaScriptProperty]
        [Range(0, 8)]
        [Display(Name="004.04 Signal Order (0=Any, 1=1st, 2=2nd...)", Order=60104, GroupName="004. =====CISD Entry=====")]
        public int CISDSignalOrder
        {
            get { return cisdSignalOrder; }
            set { cisdSignalOrder = Math.Max(0, Math.Min(8, value)); }
        }

        [NinjaScriptProperty]
        [Range(1, 500)]
        [Display(Name="004.05 Max Bars Between Signals", Order=60105, GroupName="004. =====CISD Entry=====")]
        public int CISDMaxBarsBetween
        {
            get { return cisdMaxBarsBetween; }
            set { cisdMaxBarsBetween = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name="004.06 CISD Entry Plus Ticks", Order=60106, GroupName="004. =====CISD Entry=====")]
        public int CISDEntryPlusTicks
        {
            get { return cisdEntryPlusTicks; }
            set { cisdEntryPlusTicks = Math.Max(0, value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="004.07 CISD Bullish Line Color", Order=60107, GroupName="004. =====CISD Entry=====")]
        public Brush CISDBullishColor
        {
            get { return cisdBullishColor; }
            set { cisdBullishColor = value; }
        }

        [Browsable(false)]
        public string CISDBullishColorSerializable
        {
            get { return Serialize.BrushToString(cisdBullishColor); }
            set { cisdBullishColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="004.08 CISD Bearish Line Color", Order=60108, GroupName="004. =====CISD Entry=====")]
        public Brush CISDBearishColor
        {
            get { return cisdBearishColor; }
            set { cisdBearishColor = value; }
        }

        [Browsable(false)]
        public string CISDBearishColorSerializable
        {
            get { return Serialize.BrushToString(cisdBearishColor); }
            set { cisdBearishColor = Serialize.StringToBrush(value); }
        }

        // ===== 002. =====FVG Entry===== Settings =====
        [NinjaScriptProperty]
        [Display(Name="002.01 Use FVG Entry", Order=70101, GroupName="002. =====FVG Entry=====")]
        public bool UseFVGEntry
        {
            get { return useFVGEntry; }
            set { useFVGEntry = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="002.02 Use FVG Display Only", Order=70102, GroupName="002. =====FVG Entry=====")]
        public bool UseFVGDisplayOnly
        {
            get { return useFVGDisplayOnly; }
            set { useFVGDisplayOnly = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="002.03 Use FVG Combined Entry", Order=70103, GroupName="002. =====FVG Entry=====")]
        public bool UseFVGCombinedEntry
        {
            get { return useFVGCombinedEntry; }
            set { useFVGCombinedEntry = value; }
        }

        [NinjaScriptProperty]
        [Range(0, 8)]
        [Display(Name="002.04 Signal Order (0=Any, 1=1st, 2=2nd...)", Order=70104, GroupName="002. =====FVG Entry=====")]
        public int FVGSignalOrder
        {
            get { return fvgSignalOrder; }
            set { fvgSignalOrder = Math.Max(0, Math.Min(8, value)); }
        }

        [NinjaScriptProperty]
        [Range(1, 200)]
        [Display(Name="002.05 Max Bars To Retest (default 20)", Order=70105, GroupName="002. =====FVG Entry=====")]
        public int FVGMaxBarsToRetest
        {
            get { return fvgMaxBarsToRetest; }
            set { fvgMaxBarsToRetest = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name="002.06 Max Bars After Retest (default 10)", Order=70106, GroupName="002. =====FVG Entry=====")]
        public int FVGMaxBarsAfterRetest
        {
            get { return fvgMaxBarsAfterRetest; }
            set { fvgMaxBarsAfterRetest = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [Range(1, 500)]
        [Display(Name="002.07 Max Bars Between Signals", Order=70107, GroupName="002. =====FVG Entry=====")]
        public int FVGMaxBarsBetween
        {
            get { return fvgMaxBarsBetween; }
            set { fvgMaxBarsBetween = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name="002.08 FVG Entry Plus Ticks", Order=70108, GroupName="002. =====FVG Entry=====")]
        public int FVGEntryPlusTicks
        {
            get { return fvgEntryPlusTicks; }
            set { fvgEntryPlusTicks = Math.Max(0, value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="002.09 FVG Bullish Box Color", Order=70109, GroupName="002. =====FVG Entry=====")]
        public Brush FVGBullishColor
        {
            get { return fvgBullishColor; }
            set { fvgBullishColor = value; }
        }

        [Browsable(false)]
        public string FVGBullishColorSerializable
        {
            get { return Serialize.BrushToString(fvgBullishColor); }
            set { fvgBullishColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="002.10 FVG Bearish Box Color", Order=70110, GroupName="002. =====FVG Entry=====")]
        public Brush FVGBearishColor
        {
            get { return fvgBearishColor; }
            set { fvgBearishColor = value; }
        }

        [Browsable(false)]
        public string FVGBearishColorSerializable
        {
            get { return Serialize.BrushToString(fvgBearishColor); }
            set { fvgBearishColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name="002.11 FVG Box Opacity %", Order=70111, GroupName="002. =====FVG Entry=====")]
        public int FVGBoxOpacity
        {
            get { return fvgBoxOpacity; }
            set { fvgBoxOpacity = Math.Max(1, Math.Min(100, value)); }
        }

        // ===== 003. IFVG (Inverse FVG) Entry Settings =====
        [NinjaScriptProperty]
        [Display(Name="003.01 Use IFVG Entry", Order=75101, GroupName="003. =====IFVG Entry=====")]
        public bool UseIFVGEntry
        {
            get { return useIFVGEntry; }
            set { useIFVGEntry = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="003.02 Use IFVG Display Only", Order=75102, GroupName="003. =====IFVG Entry=====")]
        public bool UseIFVGDisplayOnly
        {
            get { return useIFVGDisplayOnly; }
            set { useIFVGDisplayOnly = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="003.03 Use IFVG Combined Entry", Order=75103, GroupName="003. =====IFVG Entry=====")]
        public bool UseIFVGCombinedEntry
        {
            get { return useIFVGCombinedEntry; }
            set { useIFVGCombinedEntry = value; }
        }

        [NinjaScriptProperty]
        [Range(0, 8)]
        [Display(Name="003.04 Signal Order (0=Any, 1=1st, 2=2nd...)", Order=75104, GroupName="003. =====IFVG Entry=====")]
        public int IFVGSignalOrder
        {
            get { return ifvgSignalOrder; }
            set { ifvgSignalOrder = Math.Max(0, Math.Min(8, value)); }
        }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="003.05 Max Bars Between Signals", Order=75105, GroupName="003. =====IFVG Entry=====")]
        public int IFVGMaxBarsBetween
        {
            get { return ifvgMaxBarsBetween; }
            set { ifvgMaxBarsBetween = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name="003.06 IFVG Entry Plus Ticks", Order=75106, GroupName="003. =====IFVG Entry=====")]
        public int IFVGEntryPlusTicks
        {
            get { return ifvgEntryPlusTicks; }
            set { ifvgEntryPlusTicks = Math.Max(0, value); }
        }


        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="003.09 IFVG Bullish Box Color", Order=75109, GroupName="003. =====IFVG Entry=====")]
        public Brush IFVGBullishColor
        {
            get { return ifvgBullishColor; }
            set { ifvgBullishColor = value; }
        }

        [Browsable(false)]
        public string IFVGBullishColorSerializable
        {
            get { return Serialize.BrushToString(ifvgBullishColor); }
            set { ifvgBullishColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="003.10 IFVG Bearish Box Color", Order=75110, GroupName="003. =====IFVG Entry=====")]
        public Brush IFVGBearishColor
        {
            get { return ifvgBearishColor; }
            set { ifvgBearishColor = value; }
        }

        [Browsable(false)]
        public string IFVGBearishColorSerializable
        {
            get { return Serialize.BrushToString(ifvgBearishColor); }
            set { ifvgBearishColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name="003.11 IFVG Box Opacity %", Order=75111, GroupName="003. =====IFVG Entry=====")]
        public int IFVGBoxOpacity
        {
            get { return ifvgBoxOpacity; }
            set { ifvgBoxOpacity = Math.Max(1, Math.Min(100, value)); }
        }

        // ===== 007. Sweep Entry Settings =====
        [NinjaScriptProperty]
        [Display(Name="007.01 Use LTF Sweep Entry", Order=80101, GroupName="007. =====Sweep Entry=====")]
        public bool UseLTFSweepEntry
        {
            get { return useLTFSweepEntry; }
            set { useLTFSweepEntry = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="007.02 Use HTF Sweep Entry", Order=80102, GroupName="007. =====Sweep Entry=====")]
        public bool UseHTFSweepEntry
        {
            get { return useHTFSweepEntry; }
            set { useHTFSweepEntry = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="007.03 Use Sweep Display Only", Order=80103, GroupName="007. =====Sweep Entry=====")]
        public bool UseSweepDisplayOnly
        {
            get { return useSweepDisplayOnly; }
            set { useSweepDisplayOnly = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="007.04 Use LTF Sweep as Combined Entry", Order=80104, GroupName="007. =====Sweep Entry=====")]
        public bool UseLTFSweepCombinedEntry
        {
            get { return useLTFSweepCombinedEntry; }
            set { useLTFSweepCombinedEntry = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="007.05 Use HTF Sweep as Combined Entry", Order=80105, GroupName="007. =====Sweep Entry=====")]
        public bool UseHTFSweepCombinedEntry
        {
            get { return useHTFSweepCombinedEntry; }
            set { useHTFSweepCombinedEntry = value; }
        }

        [NinjaScriptProperty]
        [Range(0, 8)]
        [Display(Name="007.06 Signal Order (0=Any, 1=1st, 2=2nd...)", Order=80106, GroupName="007. =====Sweep Entry=====")]
        public int SweepSignalOrder
        {
            get { return sweepSignalOrder; }
            set { sweepSignalOrder = Math.Max(0, Math.Min(8, value)); }
        }

        [NinjaScriptProperty]
        [Range(1, 500)]
        [Display(Name="007.07 Max Bars Between Signals", Order=80107, GroupName="007. =====Sweep Entry=====")]
        public int SweepMaxBarsBetween
        {
            get { return sweepMaxBarsBetween; }
            set { sweepMaxBarsBetween = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name="007.08 Sweep Entry Plus Ticks", Order=80108, GroupName="007. =====Sweep Entry=====")]
        public int SweepEntryPlusTicks
        {
            get { return sweepEntryPlusTicks; }
            set { sweepEntryPlusTicks = Math.Max(0, value); }
        }

        [NinjaScriptProperty]
        [Display(Name="007.09 HTF Timeframe (minutes)", Order=80109, GroupName="007. =====Sweep Entry=====")]
        public int SweepHTFTimeframe
        {
            get { return sweepHTFTimeframe; }
            set { sweepHTFTimeframe = value; }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="007.10 LTF Bullish Arrow Color", Order=80110, GroupName="007. =====Sweep Entry=====")]
        public Brush SweepLTFBullishColor
        {
            get { return sweepLTFBullishColor; }
            set { sweepLTFBullishColor = value; }
        }

        [Browsable(false)]
        public string SweepLTFBullishColorSerializable
        {
            get { return Serialize.BrushToString(sweepLTFBullishColor); }
            set { sweepLTFBullishColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="007.11 LTF Bearish Arrow Color", Order=80111, GroupName="007. =====Sweep Entry=====")]
        public Brush SweepLTFBearishColor
        {
            get { return sweepLTFBearishColor; }
            set { sweepLTFBearishColor = value; }
        }

        [Browsable(false)]
        public string SweepLTFBearishColorSerializable
        {
            get { return Serialize.BrushToString(sweepLTFBearishColor); }
            set { sweepLTFBearishColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="007.12 HTF Bullish Arrow Color", Order=80112, GroupName="007. =====Sweep Entry=====")]
        public Brush SweepHTFBullishColor
        {
            get { return sweepHTFBullishColor; }
            set { sweepHTFBullishColor = value; }
        }

        [Browsable(false)]
        public string SweepHTFBullishColorSerializable
        {
            get { return Serialize.BrushToString(sweepHTFBullishColor); }
            set { sweepHTFBullishColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="007.13 HTF Bearish Arrow Color", Order=80113, GroupName="007. =====Sweep Entry=====")]
        public Brush SweepHTFBearishColor
        {
            get { return sweepHTFBearishColor; }
            set { sweepHTFBearishColor = value; }
        }

        [Browsable(false)]
        public string SweepHTFBearishColorSerializable
        {
            get { return Serialize.BrushToString(sweepHTFBearishColor); }
            set { sweepHTFBearishColor = Serialize.StringToBrush(value); }
        }

        // ===== 006. HeikenAshi Entry Settings =====
        [NinjaScriptProperty]
        [Display(Name="006.01 Use LTF HeikenAshi Entry", Order=60101, GroupName="006. =====HeikenAshi Entry=====")]
        public bool UseLTFHeikenAshiEntry
        {
            get { return useLTFHeikenAshiEntry; }
            set { useLTFHeikenAshiEntry = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="006.02 Use HTF HeikenAshi Entry", Order=60102, GroupName="006. =====HeikenAshi Entry=====")]
        public bool UseHTFHeikenAshiEntry
        {
            get { return useHTFHeikenAshiEntry; }
            set { useHTFHeikenAshiEntry = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="006.03 Use LTF HeikenAshi Display Only", Order=60103, GroupName="006. =====HeikenAshi Entry=====")]
        public bool UseLTFHeikenAshiDisplayOnly
        {
            get { return useLTFHeikenAshiDisplayOnly; }
            set { useLTFHeikenAshiDisplayOnly = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="006.04 Use HTF HeikenAshi Display Only", Order=60104, GroupName="006. =====HeikenAshi Entry=====")]
        public bool UseHTFHeikenAshiDisplayOnly
        {
            get { return useHTFHeikenAshiDisplayOnly; }
            set { useHTFHeikenAshiDisplayOnly = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="006.05 Use LTF HeikenAshi as Combined Entry", Order=60105, GroupName="006. =====HeikenAshi Entry=====")]
        public bool UseLTFHeikenAshiCombinedEntry
        {
            get { return useLTFHeikenAshiCombinedEntry; }
            set { useLTFHeikenAshiCombinedEntry = value; }
        }

        [NinjaScriptProperty]
        [Display(Name="006.06 Use HTF HeikenAshi as Combined Entry", Order=60106, GroupName="006. =====HeikenAshi Entry=====")]
        public bool UseHTFHeikenAshiCombinedEntry
        {
            get { return useHTFHeikenAshiCombinedEntry; }
            set { useHTFHeikenAshiCombinedEntry = value; }
        }

        [NinjaScriptProperty]
        [Range(0, 8)]
        [Display(Name="006.07 Signal Order (0=Any, 1=1st, 2=2nd...)", Order=60107, GroupName="006. =====HeikenAshi Entry=====")]
        public int HeikenAshiSignalOrder
        {
            get { return heikenAshiSignalOrder; }
            set { heikenAshiSignalOrder = Math.Max(0, Math.Min(8, value)); }
        }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name="006.08 Max Bars Between Signals", Order=60108, GroupName="006. =====HeikenAshi Entry=====")]
        public int HeikenAshiMaxBarsBetween
        {
            get { return heikenAshiMaxBarsBetween; }
            set { heikenAshiMaxBarsBetween = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name="006.09 HeikenAshi Entry Plus Ticks", Order=60109, GroupName="006. =====HeikenAshi Entry=====")]
        public int HeikenAshiEntryPlusTicks
        {
            get { return heikenAshiEntryPlusTicks; }
            set { heikenAshiEntryPlusTicks = Math.Max(0, value); }
        }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="006.10 HTF Timeframe (minutes)", Order=60110, GroupName="006. =====HeikenAshi Entry=====")]
        public int HeikenAshiHTFTimeframe
        {
            get { return heikenAshiHTFTimeframe; }
            set { heikenAshiHTFTimeframe = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="006.11 LTF Bullish Candle Color", Order=60111, GroupName="006. =====HeikenAshi Entry=====")]
        public Brush HeikenAshiLTFBullishColor
        {
            get { return heikenAshiLTFBullishColor; }
            set { heikenAshiLTFBullishColor = value; }
        }

        [Browsable(false)]
        public string HeikenAshiLTFBullishColorSerializable
        {
            get { return Serialize.BrushToString(heikenAshiLTFBullishColor); }
            set { heikenAshiLTFBullishColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="006.12 LTF Bearish Candle Color", Order=60112, GroupName="006. =====HeikenAshi Entry=====")]
        public Brush HeikenAshiLTFBearishColor
        {
            get { return heikenAshiLTFBearishColor; }
            set { heikenAshiLTFBearishColor = value; }
        }

        [Browsable(false)]
        public string HeikenAshiLTFBearishColorSerializable
        {
            get { return Serialize.BrushToString(heikenAshiLTFBearishColor); }
            set { heikenAshiLTFBearishColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="006.13 HTF Bullish Candle Color", Order=60113, GroupName="006. =====HeikenAshi Entry=====")]
        public Brush HeikenAshiHTFBullishColor
        {
            get { return heikenAshiHTFBullishColor; }
            set { heikenAshiHTFBullishColor = value; }
        }

        [Browsable(false)]
        public string HeikenAshiHTFBullishColorSerializable
        {
            get { return Serialize.BrushToString(heikenAshiHTFBullishColor); }
            set { heikenAshiHTFBullishColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="006.14 HTF Bearish Candle Color", Order=60114, GroupName="006. =====HeikenAshi Entry=====")]
        public Brush HeikenAshiHTFBearishColor
        {
            get { return heikenAshiHTFBearishColor; }
            set { heikenAshiHTFBearishColor = value; }
        }

        [Browsable(false)]
        public string HeikenAshiHTFBearishColorSerializable
        {
            get { return Serialize.BrushToString(heikenAshiHTFBearishColor); }
            set { heikenAshiHTFBearishColor = Serialize.StringToBrush(value); }
        }

        // ===== 011. Time Session Entry Settings =====
        [NinjaScriptProperty]
        [Display(Name="011.01 Use Custom Entry Time Filter", Order=110101, GroupName="011. Time Session Entry")]
        public bool UseCustomEntryTimeFilter
        {
            get { return useCustomEntryTimeFilter; }
            set { useCustomEntryTimeFilter = value; }
        }

        [NinjaScriptProperty]
        [Range(0, 23)]
        [Display(Name="011.02 Session Start Hour (EST)", Order=110102, GroupName="011. Time Session Entry")]
        public int SessionStartHour
        {
            get { return sessionStartHour; }
            set { sessionStartHour = Math.Max(0, Math.Min(23, value)); }
        }

        [NinjaScriptProperty]
        [Range(0, 59)]
        [Display(Name="011.03 Session Start Minute (EST)", Order=110103, GroupName="011. Time Session Entry")]
        public int SessionStartMinute
        {
            get { return sessionStartMinute; }
            set { sessionStartMinute = Math.Max(0, Math.Min(59, value)); }
        }

        [NinjaScriptProperty]
        [Range(0, 23)]
        [Display(Name="011.04 Session End Hour (EST)", Order=110104, GroupName="011. Time Session Entry")]
        public int SessionEndHour
        {
            get { return sessionEndHour; }
            set { sessionEndHour = Math.Max(0, Math.Min(23, value)); }
        }

        [NinjaScriptProperty]
        [Range(0, 59)]
        [Display(Name="011.05 Session End Minute (EST)", Order=110105, GroupName="011. Time Session Entry")]
        public int SessionEndMinute
        {
            get { return sessionEndMinute; }
            set { sessionEndMinute = Math.Max(0, Math.Min(59, value)); }
        }

        // ===== 021. R:R Risk Reward Settings =====
        [NinjaScriptProperty]
        [Display(Name="021.01 Use FVG StopLoss with RR", Order=210111, GroupName="021. =====Risk Reward Settings=====")]
        public bool UseFVGStopLossRR
        {
            get { return useFVGStopLossRR; }
            set { useFVGStopLossRR = value; }
        }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name="021.02 FVG StopLoss Plus Ticks", Order=210112, GroupName="021. =====Risk Reward Settings=====")]
        public int FVGStopLossPlusTicks
        {
            get { return fvgStopLossPlusTicks; }
            set { fvgStopLossPlusTicks = Math.Max(0, value); }
        }

        [NinjaScriptProperty]
        [Display(Name="021.03 Use IFVG StopLoss with RR", Order=75107, GroupName="021. =====Risk Reward Settings=====")]
        public bool UseIFVGStopLossRR
        {
            get { return useIFVGStopLossRR; }
            set { useIFVGStopLossRR = value; }
        }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name="021.04 IFVG Stop Loss Plus Ticks (with RR)", Order=75108, GroupName="021. =====Risk Reward Settings=====")]
        public int IFVGStopLossPlusTicks
        {
            get { return ifvgStopLossPlusTicks; }
            set { ifvgStopLossPlusTicks = Math.Max(0, value); }
        }
		
        [NinjaScriptProperty]
        [Display(Name="021.05 Use CISD StopLoss with RR", Order=210109, GroupName="021. =====Risk Reward Settings=====")]
        public bool UseCISDStopLossRR
        {
            get { return useCISDStopLossRR; }
            set { useCISDStopLossRR = value; }
        }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name="021.06 CISD StopLoss Plus Ticks", Order=210110, GroupName="021. =====Risk Reward Settings=====")]
        public int CISDStopLossPlusTicks
        {
            get { return cisdStopLossPlusTicks; }
            set { cisdStopLossPlusTicks = Math.Max(0, value); }
        }
		
        [NinjaScriptProperty]
        [Display(Name="021.07 Use BOS StopLoss with RR", Order=210107, GroupName="021. =====Risk Reward Settings=====")]
        public bool UseBOSStopLossRR
        {
            get { return useBOSStopLossRR; }
            set { useBOSStopLossRR = value; }
        }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name="021.08 BOS StopLoss Plus Ticks", Order=210108, GroupName="021. =====Risk Reward Settings=====")]
        public int BOSStopLossPlusTicks
        {
            get { return bosStopLossPlusTicks; }
            set { bosStopLossPlusTicks = Math.Max(0, value); }
        }


        [NinjaScriptProperty]
        [Display(Name="021.09 Use Sweep Swing StopLoss with RR", Order=210113, GroupName="021. =====Risk Reward Settings=====")]
        public bool UseSweepSwingStopLossRR
        {
            get { return useSweepSwingStopLossRR; }
            set { useSweepSwingStopLossRR = value; }
        }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name="021.10 Sweep Swing StopLoss Plus Ticks", Order=210114, GroupName="021. =====Risk Reward Settings=====")]
        public int SweepSwingStopLossPlusTicks
        {
            get { return sweepSwingStopLossPlusTicks; }
            set { sweepSwingStopLossPlusTicks = Math.Max(0, value); }
        }

        // ===== 022. PreSet RR Ratios - 1:1 =====
        [NinjaScriptProperty]
        [Display(Name="022.01.1 Use RR 1:1 for Trade 1", Order=220111, GroupName="022. RR Ratios")]
        public bool UseRR_1_1_Trade1
        { get { return useRR_1_1_Trade1; } set { useRR_1_1_Trade1 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.01.2 Use RR 1:1 for Trade 2", Order=220112, GroupName="022. RR Ratios")]
        public bool UseRR_1_1_Trade2
        { get { return useRR_1_1_Trade2; } set { useRR_1_1_Trade2 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.01.3 Use RR 1:1 for Trade 3", Order=220113, GroupName="022. RR Ratios")]
        public bool UseRR_1_1_Trade3
        { get { return useRR_1_1_Trade3; } set { useRR_1_1_Trade3 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.01.4 Use RR 1:1 for Trade 4", Order=220114, GroupName="022. RR Ratios")]
        public bool UseRR_1_1_Trade4
        { get { return useRR_1_1_Trade4; } set { useRR_1_1_Trade4 = value; } }

        // ===== 022. PreSet RR Ratios - 1:1.5 =====
        [NinjaScriptProperty]
        [Display(Name="022.02.1 Use RR 1:1.5 for Trade 1", Order=220211, GroupName="022. RR Ratios")]
        public bool UseRR_1_1_5_Trade1
        { get { return useRR_1_1_5_Trade1; } set { useRR_1_1_5_Trade1 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.02.2 Use RR 1:1.5 for Trade 2", Order=220212, GroupName="022. RR Ratios")]
        public bool UseRR_1_1_5_Trade2
        { get { return useRR_1_1_5_Trade2; } set { useRR_1_1_5_Trade2 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.02.3 Use RR 1:1.5 for Trade 3", Order=220213, GroupName="022. RR Ratios")]
        public bool UseRR_1_1_5_Trade3
        { get { return useRR_1_1_5_Trade3; } set { useRR_1_1_5_Trade3 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.02.4 Use RR 1:1.5 for Trade 4", Order=220214, GroupName="022. RR Ratios")]
        public bool UseRR_1_1_5_Trade4
        { get { return useRR_1_1_5_Trade4; } set { useRR_1_1_5_Trade4 = value; } }

        // ===== 022. PreSet RR Ratios - 1:2 =====
        [NinjaScriptProperty]
        [Display(Name="022.03.1 Use RR 1:2 for Trade 1", Order=220311, GroupName="022. RR Ratios")]
        public bool UseRR_1_2_Trade1
        { get { return useRR_1_2_Trade1; } set { useRR_1_2_Trade1 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.03.2 Use RR 1:2 for Trade 2", Order=220312, GroupName="022. RR Ratios")]
        public bool UseRR_1_2_Trade2
        { get { return useRR_1_2_Trade2; } set { useRR_1_2_Trade2 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.03.3 Use RR 1:2 for Trade 3", Order=220313, GroupName="022. RR Ratios")]
        public bool UseRR_1_2_Trade3
        { get { return useRR_1_2_Trade3; } set { useRR_1_2_Trade3 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.03.4 Use RR 1:2 for Trade 4", Order=220314, GroupName="022. RR Ratios")]
        public bool UseRR_1_2_Trade4
        { get { return useRR_1_2_Trade4; } set { useRR_1_2_Trade4 = value; } }

        // ===== 022. PreSet RR Ratios - 1:2.5 =====
        [NinjaScriptProperty]
        [Display(Name="022.04.1 Use RR 1:2.5 for Trade 1", Order=220411, GroupName="022. RR Ratios")]
        public bool UseRR_1_2_5_Trade1
        { get { return useRR_1_2_5_Trade1; } set { useRR_1_2_5_Trade1 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.04.2 Use RR 1:2.5 for Trade 2", Order=220412, GroupName="022. RR Ratios")]
        public bool UseRR_1_2_5_Trade2
        { get { return useRR_1_2_5_Trade2; } set { useRR_1_2_5_Trade2 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.04.3 Use RR 1:2.5 for Trade 3", Order=220413, GroupName="022. RR Ratios")]
        public bool UseRR_1_2_5_Trade3
        { get { return useRR_1_2_5_Trade3; } set { useRR_1_2_5_Trade3 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.04.4 Use RR 1:2.5 for Trade 4", Order=220414, GroupName="022. RR Ratios")]
        public bool UseRR_1_2_5_Trade4
        { get { return useRR_1_2_5_Trade4; } set { useRR_1_2_5_Trade4 = value; } }

        // ===== 022. PreSet RR Ratios - 1:3 =====
        [NinjaScriptProperty]
        [Display(Name="022.05.1 Use RR 1:3 for Trade 1", Order=220511, GroupName="022. RR Ratios")]
        public bool UseRR_1_3_Trade1
        { get { return useRR_1_3_Trade1; } set { useRR_1_3_Trade1 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.05.2 Use RR 1:3 for Trade 2", Order=220512, GroupName="022. RR Ratios")]
        public bool UseRR_1_3_Trade2
        { get { return useRR_1_3_Trade2; } set { useRR_1_3_Trade2 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.05.3 Use RR 1:3 for Trade 3", Order=220513, GroupName="022. RR Ratios")]
        public bool UseRR_1_3_Trade3
        { get { return useRR_1_3_Trade3; } set { useRR_1_3_Trade3 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.05.4 Use RR 1:3 for Trade 4", Order=220514, GroupName="022. RR Ratios")]
        public bool UseRR_1_3_Trade4
        { get { return useRR_1_3_Trade4; } set { useRR_1_3_Trade4 = value; } }

        // ===== 022. PreSet RR Ratios - 1:3.5 =====
        [NinjaScriptProperty]
        [Display(Name="022.06.1 Use RR 1:3.5 for Trade 1", Order=220611, GroupName="022. RR Ratios")]
        public bool UseRR_1_3_5_Trade1
        { get { return useRR_1_3_5_Trade1; } set { useRR_1_3_5_Trade1 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.06.2 Use RR 1:3.5 for Trade 2", Order=220612, GroupName="022. RR Ratios")]
        public bool UseRR_1_3_5_Trade2
        { get { return useRR_1_3_5_Trade2; } set { useRR_1_3_5_Trade2 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.06.3 Use RR 1:3.5 for Trade 3", Order=220613, GroupName="022. RR Ratios")]
        public bool UseRR_1_3_5_Trade3
        { get { return useRR_1_3_5_Trade3; } set { useRR_1_3_5_Trade3 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.06.4 Use RR 1:3.5 for Trade 4", Order=220614, GroupName="022. RR Ratios")]
        public bool UseRR_1_3_5_Trade4
        { get { return useRR_1_3_5_Trade4; } set { useRR_1_3_5_Trade4 = value; } }

        // ===== 022. PreSet RR Ratios - 1:4 =====
        [NinjaScriptProperty]
        [Display(Name="022.07.1 Use RR 1:4 for Trade 1", Order=220711, GroupName="022. RR Ratios")]
        public bool UseRR_1_4_Trade1
        { get { return useRR_1_4_Trade1; } set { useRR_1_4_Trade1 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.07.2 Use RR 1:4 for Trade 2", Order=220712, GroupName="022. RR Ratios")]
        public bool UseRR_1_4_Trade2
        { get { return useRR_1_4_Trade2; } set { useRR_1_4_Trade2 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.07.3 Use RR 1:4 for Trade 3", Order=220713, GroupName="022. RR Ratios")]
        public bool UseRR_1_4_Trade3
        { get { return useRR_1_4_Trade3; } set { useRR_1_4_Trade3 = value; } }

        [NinjaScriptProperty]
        [Display(Name="022.07.4 Use RR 1:4 for Trade 4", Order=220714, GroupName="022. RR Ratios")]
        public bool UseRR_1_4_Trade4
        { get { return useRR_1_4_Trade4; } set { useRR_1_4_Trade4 = value; } }

        // ===== 023. Custom Stops Targets - Trade 1 =====
        [NinjaScriptProperty]
        [Display(Name="023.01.1 Use Custom StopLoss 1", Order=230111, GroupName="023. Custom Stops/Targets")]
        public bool UseCustomStopLoss1
        { get { return useCustomStopLoss1; } set { useCustomStopLoss1 = value; } }

        [NinjaScriptProperty]
        [Range(1, 1000)]
        [Display(Name="023.01.2 Custom StopLoss 1 Ticks", Order=230112, GroupName="023. Custom Stops/Targets")]
        public int CustomStopLoss1Ticks
        { get { return customStopLoss1Ticks; } set { customStopLoss1Ticks = Math.Max(1, value); } }

        [NinjaScriptProperty]
        [Display(Name="023.01.3 Use Custom Target 1", Order=230113, GroupName="023. Custom Stops/Targets")]
        public bool UseCustomTarget1
        { get { return useCustomTarget1; } set { useCustomTarget1 = value; } }

        [NinjaScriptProperty]
        [Range(1, 1000)]
        [Display(Name="023.01.4 Custom Target 1 Ticks", Order=230114, GroupName="023. Custom Stops/Targets")]
        public int CustomTarget1Ticks
        { get { return customTarget1Ticks; } set { customTarget1Ticks = Math.Max(1, value); } }

        // ===== 023. Custom Stops Targets - Trade 2 =====
        [NinjaScriptProperty]
        [Display(Name="023.02.1 Use Custom StopLoss 2", Order=230211, GroupName="023. Custom Stops/Targets")]
        public bool UseCustomStopLoss2
        { get { return useCustomStopLoss2; } set { useCustomStopLoss2 = value; } }

        [NinjaScriptProperty]
        [Range(1, 1000)]
        [Display(Name="023.02.2 Custom StopLoss 2 Ticks", Order=230212, GroupName="023. Custom Stops/Targets")]
        public int CustomStopLoss2Ticks
        { get { return customStopLoss2Ticks; } set { customStopLoss2Ticks = Math.Max(1, value); } }

        [NinjaScriptProperty]
        [Display(Name="023.02.3 Use Custom Target 2", Order=230213, GroupName="023. Custom Stops/Targets")]
        public bool UseCustomTarget2
        { get { return useCustomTarget2; } set { useCustomTarget2 = value; } }

        [NinjaScriptProperty]
        [Range(1, 1000)]
        [Display(Name="023.02.4 Custom Target 2 Ticks", Order=230214, GroupName="023. Custom Stops/Targets")]
        public int CustomTarget2Ticks
        { get { return customTarget2Ticks; } set { customTarget2Ticks = Math.Max(1, value); } }

        // ===== 023. Custom Stops Targets - Trade 3 =====
        [NinjaScriptProperty]
        [Display(Name="023.03.1 Use Custom StopLoss 3", Order=230311, GroupName="023. Custom Stops/Targets")]
        public bool UseCustomStopLoss3
        { get { return useCustomStopLoss3; } set { useCustomStopLoss3 = value; } }

        [NinjaScriptProperty]
        [Range(1, 1000)]
        [Display(Name="023.03.2 Custom StopLoss 3 Ticks", Order=230312, GroupName="023. Custom Stops/Targets")]
        public int CustomStopLoss3Ticks
        { get { return customStopLoss3Ticks; } set { customStopLoss3Ticks = Math.Max(1, value); } }

        [NinjaScriptProperty]
        [Display(Name="023.03.3 Use Custom Target 3", Order=230313, GroupName="023. Custom Stops/Targets")]
        public bool UseCustomTarget3
        { get { return useCustomTarget3; } set { useCustomTarget3 = value; } }

        [NinjaScriptProperty]
        [Range(1, 1000)]
        [Display(Name="023.03.4 Custom Target 3 Ticks", Order=230314, GroupName="023. Custom Stops/Targets")]
        public int CustomTarget3Ticks
        { get { return customTarget3Ticks; } set { customTarget3Ticks = Math.Max(1, value); } }

        // ===== 023. Custom Stops Targets - Trade 4 =====
        [NinjaScriptProperty]
        [Display(Name="023.04.1 Use Custom StopLoss 4", Order=230411, GroupName="023. Custom Stops/Targets")]
        public bool UseCustomStopLoss4
        { get { return useCustomStopLoss4; } set { useCustomStopLoss4 = value; } }

        [NinjaScriptProperty]
        [Range(1, 1000)]
        [Display(Name="023.04.2 Custom StopLoss 4 Ticks", Order=230412, GroupName="023. Custom Stops/Targets")]
        public int CustomStopLoss4Ticks
        { get { return customStopLoss4Ticks; } set { customStopLoss4Ticks = Math.Max(1, value); } }

        [NinjaScriptProperty]
        [Display(Name="023.04.3 Use Custom Target 4", Order=230413, GroupName="023. Custom Stops/Targets")]
        public bool UseCustomTarget4
        { get { return useCustomTarget4; } set { useCustomTarget4 = value; } }

        [NinjaScriptProperty]
        [Range(1, 1000)]
        [Display(Name="023.04.4 Custom Target 4 Ticks", Order=230414, GroupName="023. Custom Stops/Targets")]
        public int CustomTarget4Ticks
        { get { return customTarget4Ticks; } set { customTarget4Ticks = Math.Max(1, value); } }

        #endregion
    }
}
