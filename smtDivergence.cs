#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml.Serialization;
using System.Windows.Media;
using System.Windows;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Core.FloatingPoint;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class SMTDivergenceIndicator : Indicator
    {
        #region Variables

        // Swing point tracking
        private List<SwingPoint> swingHighs;
        private List<SwingPoint> swingLows;

        // Comparison symbol tracking
        private List<SwingPoint> symbol1SwingHighs;
        private List<SwingPoint> symbol1SwingLows;
        private List<SwingPoint> symbol2SwingHighs;
        private List<SwingPoint> symbol2SwingLows;

        // Divergence counters
        private int phSmt1 = 0;
        private int plSmt1 = 0;
        private int phSmt2 = 0;
        private int plSmt2 = 0;

        // Divergence tracking for labels
        private int lastPhSmt1 = 0;
        private int lastPlSmt1 = 0;
        private int lastPhSmt2 = 0;
        private int lastPlSmt2 = 0;

        // Bar indices for additional data series
        private int symbol1Index = -1;
        private int symbol2Index = -1;

        // Drawing counters
        private int lineCounter = 0;
        private int labelCounter = 0;

        // Track active SMT divergences for removal
        private List<SMTDivergence> activeDivergences;

        // NEW: Signal tracking for strategy access
        private int barsWithShortSignal = 0;
        private int barsWithLongSignal = 0;
        private int lastHighDivergenceBar = -1;
        private int lastLowDivergenceBar = -1;

        #endregion

        #region Swing Point Class

        private class SwingPoint
        {
            public double Price { get; set; }
            public int BarIndex { get; set; }
            public DateTime Time { get; set; }
            public bool IsHigh { get; set; }
            public bool IsBearishCandle { get; set; }

            public SwingPoint(double price, int barIndex, DateTime time, bool isHigh, bool isBearishCandle = false)
            {
                Price = price;
                BarIndex = barIndex;
                Time = time;
                IsHigh = isHigh;
                IsBearishCandle = isBearishCandle;
            }
        }

        #endregion

        #region SMT Divergence Class

        private class SMTDivergence
        {
            public string LineTag { get; set; }
            public string LabelTag { get; set; }
            public bool IsHigh { get; set; }
            public double OutermostPrice { get; set; }
            public DateTime CreationTime { get; set; }

            public SMTDivergence(string lineTag, string labelTag, bool isHigh, double outermostPrice, DateTime creationTime)
            {
                LineTag = lineTag;
                LabelTag = labelTag;
                IsHigh = isHigh;
                OutermostPrice = outermostPrice;
                CreationTime = creationTime;
            }
        }

        #endregion

        #region OnStateChange

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"SMT Divergence Indicator - Detects Smart Money Technique divergences between multiple symbols";
                Name = "SMTDivergenceIndicator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;

                // Default Settings
                PivotLookback = 3;

                // Symbol A
                UseSymbol1 = true;
                Symbol1Name = "ES 12-24";

                // Symbol B
                UseSymbol2 = true;
                Symbol2Name = "YM 12-24";

                // Style
                SwingHighColor = Brushes.Red;
                SwingLowColor = Brushes.Blue;
                LineWidth = 2;
                LabelTextColor = Brushes.White;

                // New Features
                CandleDirectionValidation = false;
                RemoveBrokenSMTs = false;

                // NEW: Signal duration settings
                ShortSignalBars = 10;
                LongSignalBars = 10;

                // NEW: Add plots for strategy access
                AddPlot(Brushes.Transparent, "ShortSignal");
                AddPlot(Brushes.Transparent, "LongSignal");
            }
            else if (State == State.Configure)
            {
                // Only add data series if running as standalone indicator (on chart)
                // When called from a strategy, the strategy must add these data series
                if (ChartControl != null)
                {
                    // Add data series for comparison symbols
                    if (UseSymbol1 && !string.IsNullOrEmpty(Symbol1Name))
                    {
                        AddDataSeries(Symbol1Name, BarsPeriod);
                        symbol1Index = 1;
                    }

                    if (UseSymbol2 && !string.IsNullOrEmpty(Symbol2Name))
                    {
                        int nextIndex = UseSymbol1 ? 2 : 1;
                        AddDataSeries(Symbol2Name, BarsPeriod);
                        symbol2Index = nextIndex;
                    }
                }
                // When called from strategy, data series are already loaded
                // Strategy will need to configure these indices appropriately
            }
            else if (State == State.DataLoaded)
            {
                // Initialize lists
                swingHighs = new List<SwingPoint>();
                swingLows = new List<SwingPoint>();
                symbol1SwingHighs = new List<SwingPoint>();
                symbol1SwingLows = new List<SwingPoint>();
                symbol2SwingHighs = new List<SwingPoint>();
                symbol2SwingLows = new List<SwingPoint>();
                activeDivergences = new List<SMTDivergence>();
            }
        }

        #endregion

        #region OnBarUpdate

        protected override void OnBarUpdate()
        {
            if (CurrentBars[0] < PivotLookback * 2 + 1) return;

            int activeSeries = BarsInProgress;

            // Process primary chart
            if (activeSeries == 0)
            {
                // Check for broken SMTs if feature is enabled
                if (RemoveBrokenSMTs)
                {
                    CheckAndRemoveBrokenSMTs();
                }

                ProcessPrimaryChart();

                // NEW: Update signal counters and set plot values
                UpdateSignals();
            }
            // Process Symbol 1
            else if (UseSymbol1 && activeSeries == symbol1Index)
            {
                if (CurrentBars[symbol1Index] < PivotLookback * 2 + 1) return;
                ProcessComparisonSymbol(symbol1Index, symbol1SwingHighs, symbol1SwingLows);
            }
            // Process Symbol 2
            else if (UseSymbol2 && activeSeries == symbol2Index)
            {
                if (CurrentBars[symbol2Index] < PivotLookback * 2 + 1) return;
                ProcessComparisonSymbol(symbol2Index, symbol2SwingHighs, symbol2SwingLows);
            }
        }

        #endregion

        #region Process Methods

        private void ProcessPrimaryChart()
        {
            // Detect swing highs and lows
            double swingHigh = GetSwingHigh(0);
            double swingLow = GetSwingLow(0);

            // Check for new swing high
            if (!double.IsNaN(swingHigh))
            {
                bool isBearishCandle = IsBearishCandle(0, PivotLookback);
                SwingPoint newHigh = new SwingPoint(swingHigh, CurrentBars[0] - PivotLookback,
                    Times[0][PivotLookback], true, isBearishCandle);

                bool divergenceDetected = false;

                // Check for divergences with comparison symbols
                if (UseSymbol1 && symbol1SwingHighs.Count > 0)
                {
                    if (CheckDivergence(true, newHigh, symbol1SwingHighs, ref phSmt1, 1))
                        divergenceDetected = true;
                }

                if (UseSymbol2 && symbol2SwingHighs.Count > 0)
                {
                    if (CheckDivergence(true, newHigh, symbol2SwingHighs, ref phSmt2, 2))
                        divergenceDetected = true;
                }

                swingHighs.Add(newHigh);

                // NEW: Track divergence for signal generation
                if (divergenceDetected)
                {
                    lastHighDivergenceBar = CurrentBar;
                    barsWithShortSignal = 0;
                }

                // Draw label if divergence detected
                DrawDivergenceLabel(true, newHigh);
            }

            // Check for new swing low
            if (!double.IsNaN(swingLow))
            {
                bool isBearishCandle = IsBearishCandle(0, PivotLookback);
                SwingPoint newLow = new SwingPoint(swingLow, CurrentBars[0] - PivotLookback,
                    Times[0][PivotLookback], false, isBearishCandle);

                bool divergenceDetected = false;

                // Check for divergences with comparison symbols
                if (UseSymbol1 && symbol1SwingLows.Count > 0)
                {
                    if (CheckDivergence(false, newLow, symbol1SwingLows, ref plSmt1, 1))
                        divergenceDetected = true;
                }

                if (UseSymbol2 && symbol2SwingLows.Count > 0)
                {
                    if (CheckDivergence(false, newLow, symbol2SwingLows, ref plSmt2, 2))
                        divergenceDetected = true;
                }

                swingLows.Add(newLow);

                // NEW: Track divergence for signal generation
                if (divergenceDetected)
                {
                    lastLowDivergenceBar = CurrentBar;
                    barsWithLongSignal = 0;
                }

                // Draw label if divergence detected
                DrawDivergenceLabel(false, newLow);
            }
        }

        private void ProcessComparisonSymbol(int seriesIndex, List<SwingPoint> highs, List<SwingPoint> lows)
        {
            // Detect swing highs and lows for comparison symbol
            double swingHigh = GetSwingHigh(seriesIndex);
            double swingLow = GetSwingLow(seriesIndex);

            if (!double.IsNaN(swingHigh))
            {
                bool isBearishCandle = IsBearishCandle(seriesIndex, PivotLookback);
                SwingPoint newHigh = new SwingPoint(swingHigh, CurrentBars[seriesIndex] - PivotLookback,
                    Times[seriesIndex][PivotLookback], true, isBearishCandle);
                highs.Add(newHigh);
            }

            if (!double.IsNaN(swingLow))
            {
                bool isBearishCandle = IsBearishCandle(seriesIndex, PivotLookback);
                SwingPoint newLow = new SwingPoint(swingLow, CurrentBars[seriesIndex] - PivotLookback,
                    Times[seriesIndex][PivotLookback], false, isBearishCandle);
                lows.Add(newLow);
            }
        }

        // NEW: Update signal values for strategy access
        private void UpdateSignals()
        {
            // Update short signal (high divergence)
            if (lastHighDivergenceBar >= 0 && barsWithShortSignal < ShortSignalBars)
            {
                barsWithShortSignal = CurrentBar - lastHighDivergenceBar;
                Values[0][0] = (barsWithShortSignal < ShortSignalBars) ? 1 : 0;
            }
            else
            {
                Values[0][0] = 0;
            }

            // Update long signal (low divergence)
            if (lastLowDivergenceBar >= 0 && barsWithLongSignal < LongSignalBars)
            {
                barsWithLongSignal = CurrentBar - lastLowDivergenceBar;
                Values[1][0] = (barsWithLongSignal < LongSignalBars) ? 1 : 0;
            }
            else
            {
                Values[1][0] = 0;
            }
        }

        #endregion

        #region Swing Detection

        private double GetSwingHigh(int seriesIndex)
        {
            if (CurrentBars[seriesIndex] < PivotLookback * 2 + 1)
                return double.NaN;

            double testHigh = Highs[seriesIndex][PivotLookback];

            // Check left side
            for (int i = 1; i <= PivotLookback; i++)
            {
                if (Highs[seriesIndex][PivotLookback + i] >= testHigh)
                    return double.NaN;
            }

            // Check right side
            for (int i = 1; i <= PivotLookback; i++)
            {
                if (Highs[seriesIndex][PivotLookback - i] > testHigh)
                    return double.NaN;
            }

            return testHigh;
        }

        private double GetSwingLow(int seriesIndex)
        {
            if (CurrentBars[seriesIndex] < PivotLookback * 2 + 1)
                return double.NaN;

            double testLow = Lows[seriesIndex][PivotLookback];

            // Check left side
            for (int i = 1; i <= PivotLookback; i++)
            {
                if (Lows[seriesIndex][PivotLookback + i] <= testLow)
                    return double.NaN;
            }

            // Check right side
            for (int i = 1; i <= PivotLookback; i++)
            {
                if (Lows[seriesIndex][PivotLookback - i] < testLow)
                    return double.NaN;
            }

            return testLow;
        }

        private bool IsBearishCandle(int seriesIndex, int barsAgo)
        {
            return Closes[seriesIndex][barsAgo] < Opens[seriesIndex][barsAgo];
        }

        #endregion

        #region Divergence Detection

        private bool CheckDivergence(bool isHigh, SwingPoint currentSwing, List<SwingPoint> comparisonSwings,
            ref int smtCounter, int symbolNum)
        {
            if (comparisonSwings.Count < 2) return false;

            // Get the last two swing points from primary chart
            List<SwingPoint> primarySwings = isHigh ? swingHighs : swingLows;
            if (primarySwings.Count < 1) return false;

            SwingPoint lastPrimary = primarySwings[primarySwings.Count - 1];

            // Find the closest comparison swing point to the last primary swing
            SwingPoint lastComparison = FindClosestSwing(lastPrimary.Time, comparisonSwings);
            SwingPoint currentComparison = FindClosestSwing(currentSwing.Time, comparisonSwings);

            if (lastComparison == null || currentComparison == null) return false;

            // Calculate price changes
            double primaryChange = currentSwing.Price - lastPrimary.Price;
            double comparisonChange = currentComparison.Price - lastComparison.Price;

            // Check for divergence (opposite direction movements)
            bool isDivergence = (primaryChange * comparisonChange) < 0;

            // Apply candle direction validation if enabled
            if (isDivergence && CandleDirectionValidation)
            {
                if (isHigh)
                {
                    if (!currentSwing.IsBearishCandle || !currentComparison.IsBearishCandle)
                    {
                        isDivergence = false;
                    }
                }
                else
                {
                    if (currentSwing.IsBearishCandle || currentComparison.IsBearishCandle)
                    {
                        isDivergence = false;
                    }
                }
            }

            if (isDivergence)
            {
                smtCounter++;

                // Draw divergence line
                DrawDivergenceLine(lastPrimary, currentSwing, isHigh);
                
                return true;
            }

            return false;
        }

        private SwingPoint FindClosestSwing(DateTime targetTime, List<SwingPoint> swings)
        {
            if (swings.Count == 0) return null;

            SwingPoint closest = swings[0];
            double minDiff = Math.Abs((targetTime - closest.Time).TotalSeconds);

            foreach (var swing in swings)
            {
                double diff = Math.Abs((targetTime - swing.Time).TotalSeconds);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = swing;
                }
            }

            return closest;
        }

        #endregion

        #region SMT Break Detection

        private void CheckAndRemoveBrokenSMTs()
        {
            if (activeDivergences.Count == 0) return;

            List<SMTDivergence> toRemove = new List<SMTDivergence>();

            foreach (var divergence in activeDivergences)
            {
                bool isBroken = false;

                if (divergence.IsHigh)
                {
                    if (Close[0] > divergence.OutermostPrice)
                    {
                        isBroken = true;
                    }
                }
                else
                {
                    if (Close[0] < divergence.OutermostPrice)
                    {
                        isBroken = true;
                    }
                }

                if (isBroken)
                {
                    RemoveDrawObject(divergence.LineTag);
                    RemoveDrawObject(divergence.LabelTag);
                    toRemove.Add(divergence);
                }
            }

            foreach (var divergence in toRemove)
            {
                activeDivergences.Remove(divergence);
            }
        }

        #endregion

        #region Drawing Methods

        private void DrawDivergenceLine(SwingPoint point1, SwingPoint point2, bool isHigh)
        {
            try
            {
                Brush lineColor = isHigh ? SwingHighColor : SwingLowColor;
                string tag = $"SMT_Line_{lineCounter++}";

                Draw.Line(this, tag, false,
                    point1.Time, point1.Price,
                    point2.Time, point2.Price,
                    lineColor, DashStyleHelper.Solid, LineWidth);

                if (RemoveBrokenSMTs)
                {
                    double outermostPrice = isHigh ? 
                        Math.Max(point1.Price, point2.Price) : 
                        Math.Min(point1.Price, point2.Price);

                    activeDivergences.Add(new SMTDivergence(tag, "PENDING", isHigh, outermostPrice, Time[0]));
                }
            }
            catch (Exception ex)
            {
                Print($"Error drawing divergence line: {ex.Message}");
            }
        }

        private void DrawDivergenceLabel(bool isHigh, SwingPoint swing)
        {
            try
            {
                string labelText = "";
                bool hasDivergence = false;

                if (isHigh)
                {
                    if (UseSymbol1 && phSmt1 > lastPhSmt1)
                    {
                        labelText = GetSymbolTicker(Symbol1Name);
                        hasDivergence = true;
                        lastPhSmt1 = phSmt1;
                    }

                    if (UseSymbol2 && phSmt2 > lastPhSmt2)
                    {
                        if (labelText != "")
                            labelText += " | ";
                        labelText += GetSymbolTicker(Symbol2Name);
                        hasDivergence = true;
                        lastPhSmt2 = phSmt2;
                    }
                }
                else
                {
                    if (UseSymbol1 && plSmt1 > lastPlSmt1)
                    {
                        labelText = GetSymbolTicker(Symbol1Name);
                        hasDivergence = true;
                        lastPlSmt1 = plSmt1;
                    }

                    if (UseSymbol2 && plSmt2 > lastPlSmt2)
                    {
                        if (labelText != "")
                            labelText += " | ";
                        labelText += GetSymbolTicker(Symbol2Name);
                        hasDivergence = true;
                        lastPlSmt2 = plSmt2;
                    }
                }

                if (hasDivergence && labelText != "")
                {
                    Brush bgColor = isHigh ? SwingHighColor : SwingLowColor;
                    string tag = $"SMT_Label_{labelCounter++}";
                    int yOffset = isHigh ? 10 : -10;

                    Draw.Text(this, tag, false, labelText,
                        swing.Time, swing.Price, yOffset,
                        LabelTextColor, new SimpleFont("Arial", 10),
                        TextAlignment.Center, bgColor, bgColor, 100);

                    if (RemoveBrokenSMTs && activeDivergences.Count > 0)
                    {
                        for (int i = activeDivergences.Count - 1; i >= 0; i--)
                        {
                            if (activeDivergences[i].LabelTag == "PENDING")
                            {
                                activeDivergences[i].LabelTag = tag;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Print($"Error drawing label: {ex.Message}");
            }
        }

        private string GetSymbolTicker(string fullSymbolName)
        {
            if (string.IsNullOrEmpty(fullSymbolName)) return "";

            int spaceIndex = fullSymbolName.IndexOf(' ');
            return spaceIndex > 0 ? fullSymbolName.Substring(0, spaceIndex) : fullSymbolName;
        }

        #endregion

        #region Properties

        [NinjaScriptProperty]
        [Range(2, int.MaxValue)]
        [Display(Name = "Pivot Lookback", Order = 1, GroupName = "Parameters")]
        public int PivotLookback { get; set; }

        // Symbol A
        [NinjaScriptProperty]
        [Display(Name = "Use Comparison Symbol", Order = 2, GroupName = "Symbol A")]
        public bool UseSymbol1 { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Symbol Name", Order = 3, GroupName = "Symbol A")]
        public string Symbol1Name { get; set; }

        // Symbol B
        [NinjaScriptProperty]
        [Display(Name = "Use Comparison Symbol", Order = 4, GroupName = "Symbol B")]
        public bool UseSymbol2 { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Symbol Name", Order = 5, GroupName = "Symbol B")]
        public string Symbol2Name { get; set; }

        // Validation Features
        [NinjaScriptProperty]
        [Display(Name = "Candle Direction Validation", Description = "For bullish SMTs, highs must be formed by down candles. For bearish SMTs, lows must be formed by up candles.", Order = 1, GroupName = "Validation")]
        public bool CandleDirectionValidation { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Remove Broken SMTs", Description = "Automatically removes SMT divergences when price closes beyond the outernmost edge.", Order = 2, GroupName = "Validation")]
        public bool RemoveBrokenSMTs { get; set; }

        // NEW: Signal Duration Properties
        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Short Signal Duration (Bars)", Description = "Number of bars to allow short trades after high divergence", Order = 1, GroupName = "Signal Settings")]
        public int ShortSignalBars { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Long Signal Duration (Bars)", Description = "Number of bars to allow long trades after low divergence", Order = 2, GroupName = "Signal Settings")]
        public int LongSignalBars { get; set; }

        // Style
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Swing High Color", Order = 6, GroupName = "Style")]
        public Brush SwingHighColor { get; set; }

        [Browsable(false)]
        public string SwingHighColorSerializable
        {
            get { return Serialize.BrushToString(SwingHighColor); }
            set { SwingHighColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Swing Low Color", Order = 7, GroupName = "Style")]
        public Brush SwingLowColor { get; set; }

        [Browsable(false)]
        public string SwingLowColorSerializable
        {
            get { return Serialize.BrushToString(SwingLowColor); }
            set { SwingLowColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name = "Line Width", Order = 8, GroupName = "Style")]
        public int LineWidth { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Label Text Color", Order = 9, GroupName = "Style")]
        public Brush LabelTextColor { get; set; }

        [Browsable(false)]
        public string LabelTextColorSerializable
        {
            get { return Serialize.BrushToString(LabelTextColor); }
            set { LabelTextColor = Serialize.StringToBrush(value); }
        }

        // NEW: Plot accessors for strategy
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> ShortSignal
        {
            get { return Values[0]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> LongSignal
        {
            get { return Values[1]; }
        }

        // NEW: Methods to configure symbol indices when called from strategy
        public void SetSymbol1Index(int index)
        {
            symbol1Index = index;
        }

        public void SetSymbol2Index(int index)
        {
            symbol2Index = index;
        }

        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SMTDivergenceIndicator[] cacheSMTDivergenceIndicator;
		public SMTDivergenceIndicator SMTDivergenceIndicator(int pivotLookback, bool useSymbol1, string symbol1Name, bool useSymbol2, string symbol2Name, bool candleDirectionValidation, bool removeBrokenSMTs, int shortSignalBars, int longSignalBars, Brush swingHighColor, Brush swingLowColor, int lineWidth, Brush labelTextColor)
		{
			return SMTDivergenceIndicator(Input, pivotLookback, useSymbol1, symbol1Name, useSymbol2, symbol2Name, candleDirectionValidation, removeBrokenSMTs, shortSignalBars, longSignalBars, swingHighColor, swingLowColor, lineWidth, labelTextColor);
		}

		public SMTDivergenceIndicator SMTDivergenceIndicator(ISeries<double> input, int pivotLookback, bool useSymbol1, string symbol1Name, bool useSymbol2, string symbol2Name, bool candleDirectionValidation, bool removeBrokenSMTs, int shortSignalBars, int longSignalBars, Brush swingHighColor, Brush swingLowColor, int lineWidth, Brush labelTextColor)
		{
			if (cacheSMTDivergenceIndicator != null)
				for (int idx = 0; idx < cacheSMTDivergenceIndicator.Length; idx++)
					if (cacheSMTDivergenceIndicator[idx] != null && cacheSMTDivergenceIndicator[idx].PivotLookback == pivotLookback && cacheSMTDivergenceIndicator[idx].UseSymbol1 == useSymbol1 && cacheSMTDivergenceIndicator[idx].Symbol1Name == symbol1Name && cacheSMTDivergenceIndicator[idx].UseSymbol2 == useSymbol2 && cacheSMTDivergenceIndicator[idx].Symbol2Name == symbol2Name && cacheSMTDivergenceIndicator[idx].CandleDirectionValidation == candleDirectionValidation && cacheSMTDivergenceIndicator[idx].RemoveBrokenSMTs == removeBrokenSMTs && cacheSMTDivergenceIndicator[idx].ShortSignalBars == shortSignalBars && cacheSMTDivergenceIndicator[idx].LongSignalBars == longSignalBars && cacheSMTDivergenceIndicator[idx].SwingHighColor == swingHighColor && cacheSMTDivergenceIndicator[idx].SwingLowColor == swingLowColor && cacheSMTDivergenceIndicator[idx].LineWidth == lineWidth && cacheSMTDivergenceIndicator[idx].LabelTextColor == labelTextColor && cacheSMTDivergenceIndicator[idx].EqualsInput(input))
						return cacheSMTDivergenceIndicator[idx];
			return CacheIndicator<SMTDivergenceIndicator>(new SMTDivergenceIndicator(){ PivotLookback = pivotLookback, UseSymbol1 = useSymbol1, Symbol1Name = symbol1Name, UseSymbol2 = useSymbol2, Symbol2Name = symbol2Name, CandleDirectionValidation = candleDirectionValidation, RemoveBrokenSMTs = removeBrokenSMTs, ShortSignalBars = shortSignalBars, LongSignalBars = longSignalBars, SwingHighColor = swingHighColor, SwingLowColor = swingLowColor, LineWidth = lineWidth, LabelTextColor = labelTextColor }, input, ref cacheSMTDivergenceIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SMTDivergenceIndicator SMTDivergenceIndicator(int pivotLookback, bool useSymbol1, string symbol1Name, bool useSymbol2, string symbol2Name, bool candleDirectionValidation, bool removeBrokenSMTs, int shortSignalBars, int longSignalBars, Brush swingHighColor, Brush swingLowColor, int lineWidth, Brush labelTextColor)
		{
			return indicator.SMTDivergenceIndicator(Input, pivotLookback, useSymbol1, symbol1Name, useSymbol2, symbol2Name, candleDirectionValidation, removeBrokenSMTs, shortSignalBars, longSignalBars, swingHighColor, swingLowColor, lineWidth, labelTextColor);
		}

		public Indicators.SMTDivergenceIndicator SMTDivergenceIndicator(ISeries<double> input , int pivotLookback, bool useSymbol1, string symbol1Name, bool useSymbol2, string symbol2Name, bool candleDirectionValidation, bool removeBrokenSMTs, int shortSignalBars, int longSignalBars, Brush swingHighColor, Brush swingLowColor, int lineWidth, Brush labelTextColor)
		{
			return indicator.SMTDivergenceIndicator(input, pivotLookback, useSymbol1, symbol1Name, useSymbol2, symbol2Name, candleDirectionValidation, removeBrokenSMTs, shortSignalBars, longSignalBars, swingHighColor, swingLowColor, lineWidth, labelTextColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SMTDivergenceIndicator SMTDivergenceIndicator(int pivotLookback, bool useSymbol1, string symbol1Name, bool useSymbol2, string symbol2Name, bool candleDirectionValidation, bool removeBrokenSMTs, int shortSignalBars, int longSignalBars, Brush swingHighColor, Brush swingLowColor, int lineWidth, Brush labelTextColor)
		{
			return indicator.SMTDivergenceIndicator(Input, pivotLookback, useSymbol1, symbol1Name, useSymbol2, symbol2Name, candleDirectionValidation, removeBrokenSMTs, shortSignalBars, longSignalBars, swingHighColor, swingLowColor, lineWidth, labelTextColor);
		}

		public Indicators.SMTDivergenceIndicator SMTDivergenceIndicator(ISeries<double> input , int pivotLookback, bool useSymbol1, string symbol1Name, bool useSymbol2, string symbol2Name, bool candleDirectionValidation, bool removeBrokenSMTs, int shortSignalBars, int longSignalBars, Brush swingHighColor, Brush swingLowColor, int lineWidth, Brush labelTextColor)
		{
			return indicator.SMTDivergenceIndicator(input, pivotLookback, useSymbol1, symbol1Name, useSymbol2, symbol2Name, candleDirectionValidation, removeBrokenSMTs, shortSignalBars, longSignalBars, swingHighColor, swingLowColor, lineWidth, labelTextColor);
		}
	}
}

#endregion
