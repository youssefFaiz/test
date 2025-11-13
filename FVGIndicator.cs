#region Using declarations
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
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class FVGIndicator : Indicator
    {
        #region FVG Classes

        private class FVGData
        {
            public DateTime OpenTime { get; set; }
            public DateTime CloseTime { get; set; }
            public double Middle { get; set; }
            public double Open { get; set; }
            public double Close { get; set; }
            public bool IsMitigated { get; set; }
            public DateTime MitigatedTime { get; set; }
            public string LabelText { get; set; }
            public string BoxTag { get; set; }
            public string LineTag { get; set; }
            public string TextTag { get; set; }
            public bool IsBullish { get; set; }
            public int StartBarIndex { get; set; }
            public int EndBarIndex { get; set; }

            public FVGData()
            {
                IsMitigated = false;
                BoxTag = string.Empty;
                LineTag = string.Empty;
                TextTag = string.Empty;
            }
        }

        #endregion

        #region Variables

        private List<FVGData> fvgList;
        private int maxFVGCount = 100;
        private int fvgCounter = 0;
        private BarsPeriod htfPeriod;

        #endregion

        #region OnStateChange

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Fair Value Gap Indicator - Detects and visualizes FVGs with extending boxes";
                Name = "FVG Indicator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = false;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;

                // General Settings
                ShowIndicator = true;
                RemoveFilled = true;
                LtfHide = true;

                // Timeframe Settings
                TimeframeValue = 1;
                TimeframeType = "Minute";
                LabelText = "1";
                MaxFVGCount = 100;

                // Box Visual Settings
                BullBoxColor = Brushes.Green;
                BullBoxOpacity = 10;
                BearBoxColor = Brushes.Red;
                BearBoxOpacity = 10;

                // Border Settings
                BorderWidth = 1;
                BullBorderColor = Brushes.Green;
                BullBorderOpacity = 0;
                BearBorderColor = Brushes.Red;
                BearBorderOpacity = 0;

                // Label Settings
                LabelBullColor = Brushes.Black;
                LabelBearColor = Brushes.Black;
                LabelFontSize = 10;
                LabelTextAlignment = TextAlignment.Right;

                // CE Line Settings
                BullCEColor = Brushes.Transparent;
                BearCEColor = Brushes.Transparent;
                CEPadding = 2;
            }
            else if (State == State.Configure)
            {
                fvgList = new List<FVGData>();

                // Add higher timeframe data series
                AddDataSeries(GetBarsPeriod());
            }
            else if (State == State.DataLoaded)
            {
                fvgCounter = 0;
            }
        }

        #endregion

        #region OnBarUpdate

        protected override void OnBarUpdate()
        {
            if (CurrentBars[0] < 4 || CurrentBars[1] < 4)
                return;

            if (!ShowIndicator)
                return;

            // Process higher timeframe data
            if (BarsInProgress == 1)
            {
                ProcessHTFData();
            }

            // Update all FVGs on primary bars
            if (BarsInProgress == 0)
            {
                CheckMitigatedFVGs();
                UpdateFVGDrawings();
            }
        }

        #endregion

        #region Core Logic Methods

        private void ProcessHTFData()
        {
            // Check if we have enough bars
            if (CurrentBars[1] < 4)
                return;

            // Get data from bars 1, 2, 3 (matching Pine Script behavior)
            int idx = 1; // BarsInProgress

            double o3 = Opens[idx][3];
            double h3 = Highs[idx][3];
            double l3 = Lows[idx][3];
            double c3 = Closes[idx][3];
            DateTime t3 = Times[idx][3];

            double o2 = Opens[idx][2];
            double h2 = Highs[idx][2];
            double l2 = Lows[idx][2];
            double c2 = Closes[idx][2];
            DateTime t2 = Times[idx][2];

            double o1 = Opens[idx][1];
            double h1 = Highs[idx][1];
            double l1 = Lows[idx][1];
            double c1 = Closes[idx][1];
            DateTime t1 = Times[idx][1];

            // Validate timeframe
            if (LtfHide && !ValidateTimeframe())
                return;

            // Check for FVG pattern (matching Pine Script: bar[1], bar[2], bar[3])
            // Bullish FVG: h1 < l3 (gap between bar 1 high and bar 3 low)
            // Bearish FVG: l1 > h3 (gap between bar 1 low and bar 3 high)

            if (h1 < l3) // Bullish FVG
            {
                CreateFVG(l3, h1, t3, true);
            }
            else if (l1 > h3) // Bearish FVG
            {
                CreateFVG(h3, l1, t3, false);
            }
        }

        private void CreateFVG(double open, double close, DateTime openTime, bool isBullish)
        {
            // Check if FVG already exists at this time
            if (fvgList.Any(f => f.OpenTime == openTime))
                return;

            // Create new FVG
            FVGData fvg = new FVGData
            {
                OpenTime = openTime,
                Open = open,
                Close = close,
                Middle = (open + close) / 2.0,
                LabelText = LabelText,
                IsBullish = isBullish,
                BoxTag = "FVGBox_" + fvgCounter,
                LineTag = "FVGLine_" + fvgCounter,
                TextTag = "FVGText_" + fvgCounter,
                StartBarIndex = CurrentBars[0]
            };

            fvgCounter++;

            // Add to beginning of list
            fvgList.Insert(0, fvg);

            // Remove oldest if exceeding max count
            if (fvgList.Count > maxFVGCount)
            {
                FVGData oldest = fvgList[fvgList.Count - 1];
                RemoveDrawing(oldest);
                fvgList.RemoveAt(fvgList.Count - 1);
            }
        }

        private void CheckMitigatedFVGs()
        {
            double currentClose = Close[0];
            DateTime currentTime = Time[0];

            for (int i = fvgList.Count - 1; i >= 0; i--)
            {
                FVGData fvg = fvgList[i];

                if (!fvg.IsMitigated)
                {
                    // Check mitigation based on close
                    bool mitigated = false;

                    if (fvg.IsBullish)
                    {
                        // Bullish FVG mitigated when close goes below FVG open
                        if (currentClose < fvg.Open)
                        {
                            mitigated = true;
                        }
                    }
                    else
                    {
                        // Bearish FVG mitigated when close goes above FVG open
                        if (currentClose > fvg.Open)
                        {
                            mitigated = true;
                        }
                    }

                    if (mitigated)
                    {
                        fvg.IsMitigated = true;
                        fvg.MitigatedTime = currentTime;
                        fvg.EndBarIndex = CurrentBar;

                        // If remove filled is enabled, remove from list
                        if (RemoveFilled)
                        {
                            RemoveDrawing(fvg);
                            fvgList.RemoveAt(i);
                        }
                    }
                }
            }
        }

        private void UpdateFVGDrawings()
        {
            int count = 0;

            foreach (FVGData fvg in fvgList)
            {
                if (count >= MaxFVGCount)
                {
                    RemoveDrawing(fvg);
                    continue;
                }

                DrawFVG(fvg);
                count++;
            }
        }

        private void DrawFVG(FVGData fvg)
        {
            // Determine base colors (without opacity)
            Brush boxColor = fvg.IsBullish ? BullBoxColor : BearBoxColor;
            Brush borderColor = fvg.IsBullish ? BullBorderColor : BearBorderColor;
            Brush labelColor = fvg.IsBullish ? LabelBullColor : LabelBearColor;
            Brush ceColor = fvg.IsBullish ? BullCEColor : BearCEColor;

            // Get opacity values
            int boxOpacity = fvg.IsBullish ? BullBoxOpacity : BearBoxOpacity;
            int borderOpacity = fvg.IsBullish ? BullBorderOpacity : BearBorderOpacity;

            // Calculate end time
            DateTime endTime;
            int endBarAgo;

            if (fvg.IsMitigated)
            {
                // Stop at mitigation bar
                endBarAgo = Bars.GetBar(fvg.MitigatedTime);
                endTime = fvg.MitigatedTime;
            }
            else
            {
                // Extend to current bar
                endBarAgo = 0;
                endTime = Time[0];
            }

            int startBarAgo = Bars.GetBar(fvg.OpenTime);

            if (startBarAgo < 0)
                return;

            // Draw Rectangle (Box) with proper opacity
            Draw.Rectangle(this, fvg.BoxTag, false, fvg.OpenTime, fvg.Open, endTime, fvg.Close,
                borderColor, boxColor, boxOpacity);

            // Draw CE Line
            if (ceColor != Brushes.Transparent && ceColor.Opacity > 0)
            {
                // Calculate line end time based on bar time difference
                TimeSpan barDuration = Time[0] - Time[1];
                DateTime lineEndTime = endTime - TimeSpan.FromTicks(barDuration.Ticks * CEPadding);

                Draw.Line(this, fvg.LineTag, false, fvg.OpenTime, fvg.Middle, lineEndTime, fvg.Middle,
                    ceColor, DashStyleHelper.Dot, 1);
            }

            // Draw Label Text on the box
            if (!string.IsNullOrEmpty(fvg.LabelText))
            {
                Draw.Text(this, fvg.TextTag, false, fvg.LabelText, endBarAgo, fvg.Close, 0,
                    labelColor, new SimpleFont("Arial", LabelFontSize), LabelTextAlignment,
                    Brushes.Transparent, Brushes.Transparent, 0);
            }
        }

        private void RemoveDrawing(FVGData fvg)
        {
            RemoveDrawObject(fvg.BoxTag);
            RemoveDrawObject(fvg.LineTag);
            RemoveDrawObject(fvg.TextTag);
        }

        private bool ValidateTimeframe()
        {
            // Check if HTF is greater than or equal to chart timeframe
            int chartSeconds = (int)BarsPeriod.Value * GetPeriodSeconds(BarsPeriod.BarsPeriodType);
            int htfSeconds = TimeframeValue * GetPeriodSeconds(GetBarsPeriodType());

            return chartSeconds <= htfSeconds;
        }

        private BarsPeriod GetBarsPeriod()
        {
            BarsPeriodType periodType = GetBarsPeriodType();

            return new BarsPeriod
            {
                BarsPeriodType = periodType,
                Value = TimeframeValue
            };
        }

        private BarsPeriodType GetBarsPeriodType()
        {
            switch (TimeframeType.ToLower())
            {
                case "tick":
                    return BarsPeriodType.Tick;
                case "volume":
                    return BarsPeriodType.Volume;
                case "second":
                    return BarsPeriodType.Second;
                case "minute":
                    return BarsPeriodType.Minute;
                case "day":
                    return BarsPeriodType.Day;
                case "week":
                    return BarsPeriodType.Week;
                case "month":
                    return BarsPeriodType.Month;
                default:
                    return BarsPeriodType.Minute;
            }
        }

        private int GetPeriodSeconds(BarsPeriodType type)
        {
            switch (type)
            {
                case BarsPeriodType.Tick:
                case BarsPeriodType.Volume:
                    return 1;
                case BarsPeriodType.Second:
                    return 1;
                case BarsPeriodType.Minute:
                    return 60;
                case BarsPeriodType.Day:
                    return 86400;
                case BarsPeriodType.Week:
                    return 604800;
                case BarsPeriodType.Month:
                    return 2592000;
                default:
                    return 60;
            }
        }

        private Brush GetBrushWithOpacity(Brush brush, int opacity)
        {
            if (brush == null)
                return Brushes.Transparent;

            Brush newBrush = brush.Clone();
            newBrush.Opacity = opacity / 100.0;
            newBrush.Freeze();
            return newBrush;
        }

        #endregion

        #region Properties

        [NinjaScriptProperty]
        [Display(Name = "Show Indicator", Description = "Enable/Disable the indicator", Order = 1, GroupName = "1. General Settings")]
        public bool ShowIndicator { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Remove Filled FVGs", Description = "Delete boxes after they are filled/mitigated", Order = 2, GroupName = "1. General Settings")]
        public bool RemoveFilled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Hide Lower Timeframes", Description = "Hide FVGs lower than the enabled timeframe", Order = 3, GroupName = "1. General Settings")]
        public bool LtfHide { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Timeframe Value", Description = "Timeframe value (e.g., 1 for 1 minute)", Order = 1, GroupName = "2. Timeframe Settings")]
        public int TimeframeValue { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Timeframe Type", Description = "Timeframe type (Minute, Hour, Day, etc.)", Order = 2, GroupName = "2. Timeframe Settings")]
        public string TimeframeType { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Label Text", Description = "Text to display on FVG boxes", Order = 3, GroupName = "2. Timeframe Settings")]
        public string LabelText { get; set; }

        [NinjaScriptProperty]
        [Range(1, 500)]
        [Display(Name = "Max FVG Count", Description = "Maximum number of FVGs to display", Order = 4, GroupName = "2. Timeframe Settings")]
        public int MaxFVGCount { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Bullish Box Color", Description = "Color for bullish FVG boxes", Order = 1, GroupName = "3. Box Visuals")]
        public Brush BullBoxColor { get; set; }

        [Browsable(false)]
        public string BullBoxColorSerializable
        {
            get { return Serialize.BrushToString(BullBoxColor); }
            set { BullBoxColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name = "Bullish Box Opacity", Description = "Opacity for bullish boxes (0-100)", Order = 2, GroupName = "3. Box Visuals")]
        public int BullBoxOpacity { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Bearish Box Color", Description = "Color for bearish FVG boxes", Order = 3, GroupName = "3. Box Visuals")]
        public Brush BearBoxColor { get; set; }

        [Browsable(false)]
        public string BearBoxColorSerializable
        {
            get { return Serialize.BrushToString(BearBoxColor); }
            set { BearBoxColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name = "Bearish Box Opacity", Description = "Opacity for bearish boxes (0-100)", Order = 4, GroupName = "3. Box Visuals")]
        public int BearBoxOpacity { get; set; }

        [NinjaScriptProperty]
        [Range(1, 3)]
        [Display(Name = "Border Width", Description = "Width of the box borders", Order = 1, GroupName = "4. Border Visuals")]
        public int BorderWidth { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Bullish Border Color", Description = "Color for bullish FVG borders", Order = 2, GroupName = "4. Border Visuals")]
        public Brush BullBorderColor { get; set; }

        [Browsable(false)]
        public string BullBorderColorSerializable
        {
            get { return Serialize.BrushToString(BullBorderColor); }
            set { BullBorderColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name = "Bullish Border Opacity", Description = "Opacity for bullish borders (0-100)", Order = 3, GroupName = "4. Border Visuals")]
        public int BullBorderOpacity { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Bearish Border Color", Description = "Color for bearish FVG borders", Order = 4, GroupName = "4. Border Visuals")]
        public Brush BearBorderColor { get; set; }

        [Browsable(false)]
        public string BearBorderColorSerializable
        {
            get { return Serialize.BrushToString(BearBorderColor); }
            set { BearBorderColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name = "Bearish Border Opacity", Description = "Opacity for bearish borders (0-100)", Order = 5, GroupName = "4. Border Visuals")]
        public int BearBorderOpacity { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Label Bull Color", Description = "Color for bullish FVG labels", Order = 1, GroupName = "5. Label Visuals")]
        public Brush LabelBullColor { get; set; }

        [Browsable(false)]
        public string LabelBullColorSerializable
        {
            get { return Serialize.BrushToString(LabelBullColor); }
            set { LabelBullColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Label Bear Color", Description = "Color for bearish FVG labels", Order = 2, GroupName = "5. Label Visuals")]
        public Brush LabelBearColor { get; set; }

        [Browsable(false)]
        public string LabelBearColorSerializable
        {
            get { return Serialize.BrushToString(LabelBearColor); }
            set { LabelBearColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(6, 24)]
        [Display(Name = "Label Font Size", Description = "Font size for labels", Order = 3, GroupName = "5. Label Visuals")]
        public int LabelFontSize { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Label Text Position", Description = "Label text alignment (Left, Center, Right)", Order = 4, GroupName = "5. Label Visuals")]
        public TextAlignment LabelTextAlignment { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Bullish CE Line Color", Description = "Color for bullish center equilibrium line", Order = 1, GroupName = "6. CE Line Visuals")]
        public Brush BullCEColor { get; set; }

        [Browsable(false)]
        public string BullCEColorSerializable
        {
            get { return Serialize.BrushToString(BullCEColor); }
            set { BullCEColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Bearish CE Line Color", Description = "Color for bearish center equilibrium line", Order = 2, GroupName = "6. CE Line Visuals")]
        public Brush BearCEColor { get; set; }

        [Browsable(false)]
        public string BearCEColorSerializable
        {
            get { return Serialize.BrushToString(BearCEColor); }
            set { BearCEColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(1, 50)]
        [Display(Name = "CE Line Padding", Description = "Space between CE line and label text (in bars)", Order = 3, GroupName = "6. CE Line Visuals")]
        public int CEPadding { get; set; }

        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
    {
        private FVGIndicator[] cacheFVGIndicator;
        public FVGIndicator FVGIndicator(bool showIndicator, bool removeFilled, bool ltfHide, int timeframeValue, string timeframeType, string labelText, int maxFVGCount, Brush bullBoxColor, int bullBoxOpacity, Brush bearBoxColor, int bearBoxOpacity, int borderWidth, Brush bullBorderColor, int bullBorderOpacity, Brush bearBorderColor, int bearBorderOpacity, Brush labelBullColor, Brush labelBearColor, int labelFontSize, TextAlignment labelTextAlignment, Brush bullCEColor, Brush bearCEColor, int cEPadding)
        {
            return FVGIndicator(Input, showIndicator, removeFilled, ltfHide, timeframeValue, timeframeType, labelText, maxFVGCount, bullBoxColor, bullBoxOpacity, bearBoxColor, bearBoxOpacity, borderWidth, bullBorderColor, bullBorderOpacity, bearBorderColor, bearBorderOpacity, labelBullColor, labelBearColor, labelFontSize, labelTextAlignment, bullCEColor, bearCEColor, cEPadding);
        }

        public FVGIndicator FVGIndicator(ISeries<double> input, bool showIndicator, bool removeFilled, bool ltfHide, int timeframeValue, string timeframeType, string labelText, int maxFVGCount, Brush bullBoxColor, int bullBoxOpacity, Brush bearBoxColor, int bearBoxOpacity, int borderWidth, Brush bullBorderColor, int bullBorderOpacity, Brush bearBorderColor, int bearBorderOpacity, Brush labelBullColor, Brush labelBearColor, int labelFontSize, TextAlignment labelTextAlignment, Brush bullCEColor, Brush bearCEColor, int cEPadding)
        {
            if (cacheFVGIndicator != null)
                for (int idx = 0; idx < cacheFVGIndicator.Length; idx++)
                    if (cacheFVGIndicator[idx] != null && cacheFVGIndicator[idx].ShowIndicator == showIndicator && cacheFVGIndicator[idx].RemoveFilled == removeFilled && cacheFVGIndicator[idx].LtfHide == ltfHide && cacheFVGIndicator[idx].TimeframeValue == timeframeValue && cacheFVGIndicator[idx].TimeframeType == timeframeType && cacheFVGIndicator[idx].LabelText == labelText && cacheFVGIndicator[idx].MaxFVGCount == maxFVGCount && cacheFVGIndicator[idx].BullBoxColor == bullBoxColor && cacheFVGIndicator[idx].BullBoxOpacity == bullBoxOpacity && cacheFVGIndicator[idx].BearBoxColor == bearBoxColor && cacheFVGIndicator[idx].BearBoxOpacity == bearBoxOpacity && cacheFVGIndicator[idx].BorderWidth == borderWidth && cacheFVGIndicator[idx].BullBorderColor == bullBorderColor && cacheFVGIndicator[idx].BullBorderOpacity == bullBorderOpacity && cacheFVGIndicator[idx].BearBorderColor == bearBorderColor && cacheFVGIndicator[idx].BearBorderOpacity == bearBorderOpacity && cacheFVGIndicator[idx].LabelBullColor == labelBullColor && cacheFVGIndicator[idx].LabelBearColor == labelBearColor && cacheFVGIndicator[idx].LabelFontSize == labelFontSize && cacheFVGIndicator[idx].LabelTextAlignment == labelTextAlignment && cacheFVGIndicator[idx].BullCEColor == bullCEColor && cacheFVGIndicator[idx].BearCEColor == bearCEColor && cacheFVGIndicator[idx].CEPadding == cEPadding && cacheFVGIndicator[idx].EqualsInput(input))
                        return cacheFVGIndicator[idx];
            return CacheIndicator<FVGIndicator>(new FVGIndicator(){ ShowIndicator = showIndicator, RemoveFilled = removeFilled, LtfHide = ltfHide, TimeframeValue = timeframeValue, TimeframeType = timeframeType, LabelText = labelText, MaxFVGCount = maxFVGCount, BullBoxColor = bullBoxColor, BullBoxOpacity = bullBoxOpacity, BearBoxColor = bearBoxColor, BearBoxOpacity = bearBoxOpacity, BorderWidth = borderWidth, BullBorderColor = bullBorderColor, BullBorderOpacity = bullBorderOpacity, BearBorderColor = bearBorderColor, BearBorderOpacity = bearBorderOpacity, LabelBullColor = labelBullColor, LabelBearColor = labelBearColor, LabelFontSize = labelFontSize, LabelTextAlignment = labelTextAlignment, BullCEColor = bullCEColor, BearCEColor = bearCEColor, CEPadding = cEPadding }, input, ref cacheFVGIndicator);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.FVGIndicator FVGIndicator(bool showIndicator, bool removeFilled, bool ltfHide, int timeframeValue, string timeframeType, string labelText, int maxFVGCount, Brush bullBoxColor, int bullBoxOpacity, Brush bearBoxColor, int bearBoxOpacity, int borderWidth, Brush bullBorderColor, int bullBorderOpacity, Brush bearBorderColor, int bearBorderOpacity, Brush labelBullColor, Brush labelBearColor, int labelFontSize, TextAlignment labelTextAlignment, Brush bullCEColor, Brush bearCEColor, int cEPadding)
        {
            return indicator.FVGIndicator(Input, showIndicator, removeFilled, ltfHide, timeframeValue, timeframeType, labelText, maxFVGCount, bullBoxColor, bullBoxOpacity, bearBoxColor, bearBoxOpacity, borderWidth, bullBorderColor, bullBorderOpacity, bearBorderColor, bearBorderOpacity, labelBullColor, labelBearColor, labelFontSize, labelTextAlignment, bullCEColor, bearCEColor, cEPadding);
        }

        public Indicators.FVGIndicator FVGIndicator(ISeries<double> input , bool showIndicator, bool removeFilled, bool ltfHide, int timeframeValue, string timeframeType, string labelText, int maxFVGCount, Brush bullBoxColor, int bullBoxOpacity, Brush bearBoxColor, int bearBoxOpacity, int borderWidth, Brush bullBorderColor, int bullBorderOpacity, Brush bearBorderColor, int bearBorderOpacity, Brush labelBullColor, Brush labelBearColor, int labelFontSize, TextAlignment labelTextAlignment, Brush bullCEColor, Brush bearCEColor, int cEPadding)
        {
            return indicator.FVGIndicator(input, showIndicator, removeFilled, ltfHide, timeframeValue, timeframeType, labelText, maxFVGCount, bullBoxColor, bullBoxOpacity, bearBoxColor, bearBoxOpacity, borderWidth, bullBorderColor, bullBorderOpacity, bearBorderColor, bearBorderOpacity, labelBullColor, labelBearColor, labelFontSize, labelTextAlignment, bullCEColor, bearCEColor, cEPadding);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.FVGIndicator FVGIndicator(bool showIndicator, bool removeFilled, bool ltfHide, int timeframeValue, string timeframeType, string labelText, int maxFVGCount, Brush bullBoxColor, int bullBoxOpacity, Brush bearBoxColor, int bearBoxOpacity, int borderWidth, Brush bullBorderColor, int bullBorderOpacity, Brush bearBorderColor, int bearBorderOpacity, Brush labelBullColor, Brush labelBearColor, int labelFontSize, TextAlignment labelTextAlignment, Brush bullCEColor, Brush bearCEColor, int cEPadding)
        {
            return indicator.FVGIndicator(Input, showIndicator, removeFilled, ltfHide, timeframeValue, timeframeType, labelText, maxFVGCount, bullBoxColor, bullBoxOpacity, bearBoxColor, bearBoxOpacity, borderWidth, bullBorderColor, bullBorderOpacity, bearBorderColor, bearBorderOpacity, labelBullColor, labelBearColor, labelFontSize, labelTextAlignment, bullCEColor, bearCEColor, cEPadding);
        }

        public Indicators.FVGIndicator FVGIndicator(ISeries<double> input , bool showIndicator, bool removeFilled, bool ltfHide, int timeframeValue, string timeframeType, string labelText, int maxFVGCount, Brush bullBoxColor, int bullBoxOpacity, Brush bearBoxColor, int bearBoxOpacity, int borderWidth, Brush bullBorderColor, int bullBorderOpacity, Brush bearBorderColor, int bearBorderOpacity, Brush labelBullColor, Brush labelBearColor, int labelFontSize, TextAlignment labelTextAlignment, Brush bullCEColor, Brush bearCEColor, int cEPadding)
        {
            return indicator.FVGIndicator(input, showIndicator, removeFilled, ltfHide, timeframeValue, timeframeType, labelText, maxFVGCount, bullBoxColor, bullBoxOpacity, bearBoxColor, bearBoxOpacity, borderWidth, bullBorderColor, bullBorderOpacity, bearBorderColor, bearBorderOpacity, labelBullColor, labelBearColor, labelFontSize, labelTextAlignment, bullCEColor, bearCEColor, cEPadding);
        }
    }
}

#endregion
