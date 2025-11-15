#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class SweepIndicator : Indicator
    {
        #region Pivot Classes

        private class PivotData
        {
            public double Price { get; set; }
            public int BarIndex { get; set; }
            public bool IsBreak { get; set; }
            public bool IsMitigated { get; set; }
            public bool IsTaken { get; set; }
            public bool IsWick { get; set; }
            public bool IsHTF { get; set; }

            public PivotData(double price, int barIndex, bool isHTF = false)
            {
                Price = price;
                BarIndex = barIndex;
                IsBreak = false;
                IsMitigated = false;
                IsTaken = false;
                IsWick = false;
                IsHTF = isHTF;
            }
        }

        #endregion

        #region Variables

        private List<PivotData> pivotHighs;
        private List<PivotData> pivotLows;
        private List<PivotData> htfPivotHighs;
        private List<PivotData> htfPivotLows;

        private int arrowCounter = 0;

        private enum DetectionMode
        {
            OnlyWicks,
            OnlyOutbreaksRetest,
            WicksPlusOutbreaksRetest
        }

        private DetectionMode currentMode;

        #endregion

        #region OnStateChange

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"MTF Sweep Detector - Identifies liquidity sweeps on multiple timeframes";
                Name = "Sweep Indicator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                PaintPriceMarkers = false;
                ScaleJustification = ScaleJustification.Right;
                IsSuspendedWhileInactive = true;

                // Settings
                SwingLength = 5;
                DetectionModeString = "Only Wicks";
                EnableHTF = true;
                HTFTimeframe = "15";
                ShowCurrentTF = true;

                // Current TF Colors
                CurrentTFBullColor = Brushes.LimeGreen;
                CurrentTFBearColor = Brushes.Red;

                // HTF Colors
                HTFBullColor = Brushes.Lime;
                HTFBearColor = Brushes.OrangeRed;
            }
            else if (State == State.Configure)
            {
                pivotHighs = new List<PivotData>();
                pivotLows = new List<PivotData>();
                htfPivotHighs = new List<PivotData>();
                htfPivotLows = new List<PivotData>();

                // Parse detection mode
                switch (DetectionModeString)
                {
                    case "Only Wicks":
                        currentMode = DetectionMode.OnlyWicks;
                        break;
                    case "Only Outbreaks & Retest":
                        currentMode = DetectionMode.OnlyOutbreaksRetest;
                        break;
                    case "Wicks + Outbreaks & Retest":
                        currentMode = DetectionMode.WicksPlusOutbreaksRetest;
                        break;
                    default:
                        currentMode = DetectionMode.OnlyWicks;
                        break;
                }

                // Add HTF data series if enabled
                if (EnableHTF && !string.IsNullOrEmpty(HTFTimeframe))
                {
                    try
                    {
                        AddDataSeries(GetHTFPeriod());
                    }
                    catch
                    {
                        // Invalid timeframe, disable HTF
                        EnableHTF = false;
                    }
                }
            }
            else if (State == State.DataLoaded)
            {
                arrowCounter = 0;
            }
        }

        #endregion

        #region OnBarUpdate

        protected override void OnBarUpdate()
        {
            if (CurrentBars[0] < SwingLength * 2 + 1)
                return;

            // Process current timeframe
            if (BarsInProgress == 0 && ShowCurrentTF)
            {
                ProcessCurrentTF();
            }

            // Process higher timeframe
            if (EnableHTF && BarsInProgress == 1)
            {
                if (CurrentBars[1] < SwingLength * 2 + 1)
                    return;

                ProcessHTF();
            }
        }

        #endregion

        #region Core Logic Methods

        private void ProcessCurrentTF()
        {
            // Detect pivot highs and lows
            double? pivotHigh = GetPivotHigh(0, SwingLength, SwingLength);
            double? pivotLow = GetPivotLow(0, SwingLength, SwingLength);

            // Add new pivots
            if (pivotHigh.HasValue)
            {
                pivotHighs.Insert(0, new PivotData(pivotHigh.Value, CurrentBar - SwingLength, false));
            }

            if (pivotLow.HasValue)
            {
                pivotLows.Insert(0, new PivotData(pivotLow.Value, CurrentBar - SwingLength, false));
            }

            // Process sweeps
            ProcessSweeps(pivotHighs, pivotLows, High[0], Low[0], Close[0], false, "");
        }

        private void ProcessHTF()
        {
            int htfIndex = 1;

            // Detect HTF pivot highs and lows
            double? htfPivotHigh = GetPivotHigh(htfIndex, SwingLength, SwingLength);
            double? htfPivotLow = GetPivotLow(htfIndex, SwingLength, SwingLength);

            // Add new HTF pivots
            if (htfPivotHigh.HasValue)
            {
                htfPivotHighs.Insert(0, new PivotData(htfPivotHigh.Value, CurrentBars[htfIndex] - SwingLength, true));
            }

            if (htfPivotLow.HasValue)
            {
                htfPivotLows.Insert(0, new PivotData(htfPivotLow.Value, CurrentBars[htfIndex] - SwingLength, true));
            }

            // Process HTF sweeps
            ProcessSweeps(htfPivotHighs, htfPivotLows, Highs[htfIndex][0], Lows[htfIndex][0], Closes[htfIndex][0], true, HTFTimeframe);
        }

        private void ProcessSweeps(List<PivotData> pivHArray, List<PivotData> pivLArray,
                                   double currHigh, double currLow, double currClose,
                                   bool isHTF, string tf)
        {
            bool onlyWicks = currentMode == DetectionMode.OnlyWicks;
            bool onlyOutbreaks = currentMode == DetectionMode.OnlyOutbreaksRetest;

            // Process high pivots
            for (int i = pivHArray.Count - 1; i >= 0; i--)
            {
                PivotData pivot = pivHArray[i];

                if (!pivot.IsMitigated)
                {
                    if (!pivot.IsBreak)
                    {
                        // Check if broken
                        if (currClose > pivot.Price)
                        {
                            if (!onlyWicks)
                            {
                                pivot.IsBreak = true;
                            }
                            else
                            {
                                pivot.IsMitigated = true;
                            }
                        }

                        // Check for wick sweep (bearish)
                        if (!onlyOutbreaks && !pivot.IsWick)
                        {
                            if (currHigh > pivot.Price && currClose < pivot.Price)
                            {
                                DrawSweepArrow(1, isHTF, tf);
                                pivot.IsWick = true;
                            }
                        }
                    }
                    else
                    {
                        // Already broken, check for retest
                        if (currClose < pivot.Price)
                        {
                            pivot.IsMitigated = true;
                        }

                        // Check for outbreak retest (bullish)
                        if (!onlyWicks && currLow < pivot.Price && currClose > pivot.Price)
                        {
                            DrawSweepArrow(-1, isHTF, tf);
                            pivot.IsTaken = true;
                        }
                    }
                }

                // Remove old or completed pivots
                if (CurrentBar - pivot.BarIndex > 2000 || pivot.IsMitigated || pivot.IsTaken)
                {
                    pivHArray.RemoveAt(i);
                }
            }

            // Process low pivots
            for (int i = pivLArray.Count - 1; i >= 0; i--)
            {
                PivotData pivot = pivLArray[i];

                if (!pivot.IsMitigated)
                {
                    if (!pivot.IsBreak)
                    {
                        // Check if broken
                        if (currClose < pivot.Price)
                        {
                            if (!onlyWicks)
                            {
                                pivot.IsBreak = true;
                            }
                            else
                            {
                                pivot.IsMitigated = true;
                            }
                        }

                        // Check for wick sweep (bullish)
                        if (!onlyOutbreaks && !pivot.IsWick)
                        {
                            if (currLow < pivot.Price && currClose > pivot.Price)
                            {
                                DrawSweepArrow(-1, isHTF, tf);
                                pivot.IsWick = true;
                            }
                        }
                    }
                    else
                    {
                        // Already broken, check for retest
                        if (currClose > pivot.Price)
                        {
                            pivot.IsMitigated = true;
                        }

                        // Check for outbreak retest (bearish)
                        if (!onlyWicks && currHigh > pivot.Price && currClose < pivot.Price)
                        {
                            DrawSweepArrow(1, isHTF, tf);
                            pivot.IsTaken = true;
                        }
                    }
                }

                // Remove old or completed pivots
                if (CurrentBar - pivot.BarIndex > 2000 || pivot.IsMitigated || pivot.IsTaken)
                {
                    pivLArray.RemoveAt(i);
                }
            }
        }

        private void DrawSweepArrow(int direction, bool isHTF, string tf)
        {
            string tag = "Sweep_" + CurrentBar + "_" + arrowCounter++;
            Brush arrowColor = isHTF ?
                (direction == 1 ? HTFBearColor : HTFBullColor) :
                (direction == 1 ? CurrentTFBearColor : CurrentTFBullColor);

            if (direction == 1) // Bearish
            {
                Draw.ArrowDown(this, tag, true, 0, High[0] + (TickSize * 5), arrowColor);
            }
            else // Bullish
            {
                Draw.ArrowUp(this, tag, true, 0, Low[0] - (TickSize * 5), arrowColor);
            }
        }

        private double? GetPivotHigh(int barsSeriesIndex, int leftBars, int rightBars)
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

        private double? GetPivotLow(int barsSeriesIndex, int leftBars, int rightBars)
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

        private BarsPeriod GetHTFPeriod()
        {
            // Parse HTF timeframe string (e.g., "15", "60", "D")
            int value = 1;
            BarsPeriodType type = BarsPeriodType.Minute;

            if (string.IsNullOrEmpty(HTFTimeframe))
                return new BarsPeriod { BarsPeriodType = BarsPeriodType.Minute, Value = 15 };

            string tfUpper = HTFTimeframe.ToUpper();

            if (tfUpper.EndsWith("D"))
            {
                type = BarsPeriodType.Day;
                int.TryParse(tfUpper.Replace("D", ""), out value);
                if (value == 0) value = 1;
            }
            else if (tfUpper.EndsWith("W"))
            {
                type = BarsPeriodType.Week;
                int.TryParse(tfUpper.Replace("W", ""), out value);
                if (value == 0) value = 1;
            }
            else if (tfUpper.EndsWith("M"))
            {
                type = BarsPeriodType.Month;
                int.TryParse(tfUpper.Replace("M", ""), out value);
                if (value == 0) value = 1;
            }
            else
            {
                // Assume minutes
                if (!int.TryParse(HTFTimeframe, out value))
                    value = 15;
            }

            return new BarsPeriod
            {
                BarsPeriodType = type,
                Value = value
            };
        }

        #endregion

        #region Properties

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Swing Length", Description = "Number of bars for swing detection", Order = 1, GroupName = "1. Settings")]
        public int SwingLength { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Detection Mode", Description = "Sweep detection mode", Order = 2, GroupName = "1. Settings")]
        public string DetectionModeString { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Enable Higher Timeframe", Description = "Enable HTF sweep detection", Order = 3, GroupName = "1. Settings")]
        public bool EnableHTF { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Higher Timeframe", Description = "Higher timeframe (e.g., 15, 60, D)", Order = 4, GroupName = "1. Settings")]
        public string HTFTimeframe { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Current TF", Description = "Show current timeframe sweeps", Order = 5, GroupName = "1. Settings")]
        public bool ShowCurrentTF { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Current TF Bull", Description = "Color for current TF bullish sweeps", Order = 1, GroupName = "2. Colors")]
        public Brush CurrentTFBullColor { get; set; }

        [Browsable(false)]
        public string CurrentTFBullColorSerializable
        {
            get { return Serialize.BrushToString(CurrentTFBullColor); }
            set { CurrentTFBullColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Current TF Bear", Description = "Color for current TF bearish sweeps", Order = 2, GroupName = "2. Colors")]
        public Brush CurrentTFBearColor { get; set; }

        [Browsable(false)]
        public string CurrentTFBearColorSerializable
        {
            get { return Serialize.BrushToString(CurrentTFBearColor); }
            set { CurrentTFBearColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "HTF Bull", Description = "Color for HTF bullish sweeps", Order = 3, GroupName = "2. Colors")]
        public Brush HTFBullColor { get; set; }

        [Browsable(false)]
        public string HTFBullColorSerializable
        {
            get { return Serialize.BrushToString(HTFBullColor); }
            set { HTFBullColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "HTF Bear", Description = "Color for HTF bearish sweeps", Order = 4, GroupName = "2. Colors")]
        public Brush HTFBearColor { get; set; }

        [Browsable(false)]
        public string HTFBearColorSerializable
        {
            get { return Serialize.BrushToString(HTFBearColor); }
            set { HTFBearColor = Serialize.StringToBrush(value); }
        }

        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
    {
        private SweepIndicator[] cacheSweepIndicator;
        public SweepIndicator SweepIndicator(int swingLength, string detectionModeString, bool enableHTF, string hTFTimeframe, bool showCurrentTF, Brush currentTFBullColor, Brush currentTFBearColor, Brush hTFBullColor, Brush hTFBearColor)
        {
            return SweepIndicator(Input, swingLength, detectionModeString, enableHTF, hTFTimeframe, showCurrentTF, currentTFBullColor, currentTFBearColor, hTFBullColor, hTFBearColor);
        }

        public SweepIndicator SweepIndicator(ISeries<double> input, int swingLength, string detectionModeString, bool enableHTF, string hTFTimeframe, bool showCurrentTF, Brush currentTFBullColor, Brush currentTFBearColor, Brush hTFBullColor, Brush hTFBearColor)
        {
            if (cacheSweepIndicator != null)
                for (int idx = 0; idx < cacheSweepIndicator.Length; idx++)
                    if (cacheSweepIndicator[idx] != null && cacheSweepIndicator[idx].SwingLength == swingLength && cacheSweepIndicator[idx].DetectionModeString == detectionModeString && cacheSweepIndicator[idx].EnableHTF == enableHTF && cacheSweepIndicator[idx].HTFTimeframe == hTFTimeframe && cacheSweepIndicator[idx].ShowCurrentTF == showCurrentTF && cacheSweepIndicator[idx].CurrentTFBullColor == currentTFBullColor && cacheSweepIndicator[idx].CurrentTFBearColor == currentTFBearColor && cacheSweepIndicator[idx].HTFBullColor == hTFBullColor && cacheSweepIndicator[idx].HTFBearColor == hTFBearColor && cacheSweepIndicator[idx].EqualsInput(input))
                        return cacheSweepIndicator[idx];
            return CacheIndicator<SweepIndicator>(new SweepIndicator(){ SwingLength = swingLength, DetectionModeString = detectionModeString, EnableHTF = enableHTF, HTFTimeframe = hTFTimeframe, ShowCurrentTF = showCurrentTF, CurrentTFBullColor = currentTFBullColor, CurrentTFBearColor = currentTFBearColor, HTFBullColor = hTFBullColor, HTFBearColor = hTFBearColor }, input, ref cacheSweepIndicator);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.SweepIndicator SweepIndicator(int swingLength, string detectionModeString, bool enableHTF, string hTFTimeframe, bool showCurrentTF, Brush currentTFBullColor, Brush currentTFBearColor, Brush hTFBullColor, Brush hTFBearColor)
        {
            return indicator.SweepIndicator(Input, swingLength, detectionModeString, enableHTF, hTFTimeframe, showCurrentTF, currentTFBullColor, currentTFBearColor, hTFBullColor, hTFBearColor);
        }

        public Indicators.SweepIndicator SweepIndicator(ISeries<double> input , int swingLength, string detectionModeString, bool enableHTF, string hTFTimeframe, bool showCurrentTF, Brush currentTFBullColor, Brush currentTFBearColor, Brush hTFBullColor, Brush hTFBearColor)
        {
            return indicator.SweepIndicator(input, swingLength, detectionModeString, enableHTF, hTFTimeframe, showCurrentTF, currentTFBullColor, currentTFBearColor, hTFBullColor, hTFBearColor);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.SweepIndicator SweepIndicator(int swingLength, string detectionModeString, bool enableHTF, string hTFTimeframe, bool showCurrentTF, Brush currentTFBullColor, Brush currentTFBearColor, Brush hTFBullColor, Brush hTFBearColor)
        {
            return indicator.SweepIndicator(Input, swingLength, detectionModeString, enableHTF, hTFTimeframe, showCurrentTF, currentTFBullColor, currentTFBearColor, hTFBullColor, hTFBearColor);
        }

        public Indicators.SweepIndicator SweepIndicator(ISeries<double> input , int swingLength, string detectionModeString, bool enableHTF, string hTFTimeframe, bool showCurrentTF, Brush currentTFBullColor, Brush currentTFBearColor, Brush hTFBullColor, Brush hTFBearColor)
        {
            return indicator.SweepIndicator(input, swingLength, detectionModeString, enableHTF, hTFTimeframe, showCurrentTF, currentTFBullColor, currentTFBearColor, hTFBullColor, hTFBearColor);
        }
    }
}

#endregion
