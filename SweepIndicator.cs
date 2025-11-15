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

        // Hardcoded values
        private const int SwingLength = 5;
        private const string DetectionModeValue = "Only Wicks";

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
                EnableHTF = true;
                HTFTimeframe = "15";
                ShowLTF = true;

                // LTF Colors
                LTFBullColor = Brushes.LimeGreen;
                LTFBearColor = Brushes.Red;

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

                // Parse detection mode (hardcoded)
                switch (DetectionModeValue)
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

            // Process lower timeframe (LTF)
            if (BarsInProgress == 0 && ShowLTF)
            {
                ProcessLTF();
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

        private void ProcessLTF()
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
                (direction == 1 ? LTFBearColor : LTFBullColor);

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
        [Display(Name = "Enable Higher Timeframe", Description = "Enable HTF sweep detection", Order = 1, GroupName = "1. Settings")]
        public bool EnableHTF { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Higher Timeframe", Description = "Higher timeframe (e.g., 15, 60, D)", Order = 2, GroupName = "1. Settings")]
        public string HTFTimeframe { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show LTF", Description = "Show lower timeframe sweeps", Order = 3, GroupName = "1. Settings")]
        public bool ShowLTF { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "LTF Bull", Description = "Color for LTF bullish sweeps", Order = 1, GroupName = "2. Colors")]
        public Brush LTFBullColor { get; set; }

        [Browsable(false)]
        public string LTFBullColorSerializable
        {
            get { return Serialize.BrushToString(LTFBullColor); }
            set { LTFBullColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "LTF Bear", Description = "Color for LTF bearish sweeps", Order = 2, GroupName = "2. Colors")]
        public Brush LTFBearColor { get; set; }

        [Browsable(false)]
        public string LTFBearColorSerializable
        {
            get { return Serialize.BrushToString(LTFBearColor); }
            set { LTFBearColor = Serialize.StringToBrush(value); }
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
        public SweepIndicator SweepIndicator(bool enableHTF, string hTFTimeframe, bool showLTF, Brush lTFBullColor, Brush lTFBearColor, Brush hTFBullColor, Brush hTFBearColor)
        {
            return SweepIndicator(Input, enableHTF, hTFTimeframe, showLTF, lTFBullColor, lTFBearColor, hTFBullColor, hTFBearColor);
        }

        public SweepIndicator SweepIndicator(ISeries<double> input, bool enableHTF, string hTFTimeframe, bool showLTF, Brush lTFBullColor, Brush lTFBearColor, Brush hTFBullColor, Brush hTFBearColor)
        {
            if (cacheSweepIndicator != null)
                for (int idx = 0; idx < cacheSweepIndicator.Length; idx++)
                    if (cacheSweepIndicator[idx] != null && cacheSweepIndicator[idx].EnableHTF == enableHTF && cacheSweepIndicator[idx].HTFTimeframe == hTFTimeframe && cacheSweepIndicator[idx].ShowLTF == showLTF && cacheSweepIndicator[idx].LTFBullColor == lTFBullColor && cacheSweepIndicator[idx].LTFBearColor == lTFBearColor && cacheSweepIndicator[idx].HTFBullColor == hTFBullColor && cacheSweepIndicator[idx].HTFBearColor == hTFBearColor && cacheSweepIndicator[idx].EqualsInput(input))
                        return cacheSweepIndicator[idx];
            return CacheIndicator<SweepIndicator>(new SweepIndicator(){ EnableHTF = enableHTF, HTFTimeframe = hTFTimeframe, ShowLTF = showLTF, LTFBullColor = lTFBullColor, LTFBearColor = lTFBearColor, HTFBullColor = hTFBullColor, HTFBearColor = hTFBearColor }, input, ref cacheSweepIndicator);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.SweepIndicator SweepIndicator(bool enableHTF, string hTFTimeframe, bool showLTF, Brush lTFBullColor, Brush lTFBearColor, Brush hTFBullColor, Brush hTFBearColor)
        {
            return indicator.SweepIndicator(Input, enableHTF, hTFTimeframe, showLTF, lTFBullColor, lTFBearColor, hTFBullColor, hTFBearColor);
        }

        public Indicators.SweepIndicator SweepIndicator(ISeries<double> input , bool enableHTF, string hTFTimeframe, bool showLTF, Brush lTFBullColor, Brush lTFBearColor, Brush hTFBullColor, Brush hTFBearColor)
        {
            return indicator.SweepIndicator(input, enableHTF, hTFTimeframe, showLTF, lTFBullColor, lTFBearColor, hTFBullColor, hTFBearColor);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.SweepIndicator SweepIndicator(bool enableHTF, string hTFTimeframe, bool showLTF, Brush lTFBullColor, Brush lTFBearColor, Brush hTFBullColor, Brush hTFBearColor)
        {
            return indicator.SweepIndicator(Input, enableHTF, hTFTimeframe, showLTF, lTFBullColor, lTFBearColor, hTFBullColor, hTFBearColor);
        }

        public Indicators.SweepIndicator SweepIndicator(ISeries<double> input , bool enableHTF, string hTFTimeframe, bool showLTF, Brush lTFBullColor, Brush lTFBearColor, Brush hTFBullColor, Brush hTFBearColor)
        {
            return indicator.SweepIndicator(input, enableHTF, hTFTimeframe, showLTF, lTFBullColor, lTFBearColor, hTFBullColor, hTFBearColor);
        }
    }
}

#endregion
