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
    public class PremiumDiscountZone : Indicator
    {
        #region Variables
        private int swingLength = 20;
        private Brush premiumColor = Brushes.Green;
        private Brush discountColor = Brushes.Red;

        // Swing tracking variables
        private double swingHighPrice = 0;
        private int swingHighBar = -1;
        private int swingHighLastBar = -1;

        private double swingLowPrice = 0;
        private int swingLowBar = -1;
        private int swingLowLastBar = -1;

        private int trendState = -1; // 0 = swing high detected, 1 = swing low detected

        // Zone boundaries
        private double premiumTop = 0;
        private double premiumBottom = 0;
        private double discountTop = 0;
        private double discountBottom = 0;
        private double equilibrium = 0;

        // Public properties for strategy use
        public bool IsInPremiumZone { get; private set; }
        public bool IsInDiscountZone { get; private set; }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Premium & Discount Zone Indicator - Identifies premium and discount zones based on swing highs and lows";
                Name = "PremiumDiscountZone";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;

                SwingLength = 20;
                PremiumColor = Brushes.Green;
                DiscountColor = Brushes.Red;
            }
            else if (State == State.Configure)
            {
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < SwingLength)
                return;

            // Detect swings using the swing algorithm
            DetectSwings();

            // Calculate zone boundaries and draw
            if (swingHighPrice > 0 && swingLowPrice > 0)
            {
                CalculateZones();

                // Check if current price is in premium or discount zone
                CheckPriceInZone();

                // Draw zones on every bar to keep them visible
                DrawZones();
            }
        }

        private void DetectSwings()
        {
            // Find highest high over the lookback period
            double upperBound = MAX(High, SwingLength)[0];
            // Find lowest low over the lookback period
            double lowerBound = MIN(Low, SwingLength)[0];

            // Check for swing high
            if (High[SwingLength] >= upperBound && trendState != 0)
            {
                trendState = 0;
                swingHighPrice = High[SwingLength];
                swingHighBar = CurrentBar - SwingLength;
                swingHighLastBar = CurrentBar - SwingLength;
            }

            // Check for swing low
            if (Low[SwingLength] <= lowerBound && trendState != 1)
            {
                trendState = 1;
                swingLowPrice = Low[SwingLength];
                swingLowBar = CurrentBar - SwingLength;
                swingLowLastBar = CurrentBar - SwingLength;
            }

            // Update swing high if current high is higher (while in uptrend)
            if (trendState == 0)
            {
                if (High[0] > swingHighPrice)
                {
                    swingHighPrice = High[0];
                    swingHighLastBar = CurrentBar;
                }
            }

            // Update swing low if current low is lower (while in downtrend)
            if (trendState == 1)
            {
                if (Low[0] < swingLowPrice)
                {
                    swingLowPrice = Low[0];
                    swingLowLastBar = CurrentBar;
                }
            }
        }

        private void CalculateZones()
        {
            // Premium zone: from swing high to equilibrium (top 50% of range)
            premiumTop = swingHighPrice;
            equilibrium = (swingHighPrice + swingLowPrice) / 2.0;
            premiumBottom = equilibrium;

            // Discount zone: from equilibrium to swing low (bottom 50% of range)
            discountTop = equilibrium;
            discountBottom = swingLowPrice;
        }

        private void CheckPriceInZone()
        {
            double currentPrice = Close[0];

            // Check if in premium zone (from equilibrium to swing high)
            IsInPremiumZone = currentPrice >= equilibrium && currentPrice <= swingHighPrice;

            // Check if in discount zone (from swing low to equilibrium)
            IsInDiscountZone = currentPrice >= swingLowPrice && currentPrice <= equilibrium;
        }

        private void DrawZones()
        {
            // Calculate barsAgo from absolute bar numbers
            int leftBarsAgo = CurrentBar - Math.Min(swingHighBar, swingLowBar);

            // Use time-based drawing to extend into the future
            DateTime startTime = Time[leftBarsAgo];
            DateTime endTime = Time[0].AddMinutes(BarsPeriod.Value * 20); // Extend 20 bars into future

            // Draw Premium Zone (Pro mode: line + filled box)
            // Premium line at top
            Draw.Line(this, "PremiumLine", false,
                startTime, premiumTop,
                endTime, premiumTop,
                PremiumColor, DashStyleHelper.Solid, 2);

            // Premium filled box (from top to equilibrium) - transparent border
            Draw.Rectangle(this, "PremiumBox", false,
                startTime, premiumTop,
                endTime, equilibrium,
                Brushes.Transparent, PremiumColor, 90);

            // Draw Equilibrium line
            Draw.Line(this, "EquilibriumLine", false,
                startTime, equilibrium,
                endTime, equilibrium,
                Brushes.Silver, DashStyleHelper.Solid, 1);

            // Draw Discount Zone (Pro mode: line + filled box)
            // Discount line at bottom
            Draw.Line(this, "DiscountLine", false,
                startTime, discountBottom,
                endTime, discountBottom,
                DiscountColor, DashStyleHelper.Solid, 2);

            // Discount filled box (from equilibrium to bottom) - transparent border
            Draw.Rectangle(this, "DiscountBox", false,
                startTime, equilibrium,
                endTime, discountBottom,
                Brushes.Transparent, DiscountColor, 90);

            // Draw labels at the right side (using correct Draw.Text signature)
            Draw.Text(this, "PremiumLabel", false, "Premium",
                0, premiumTop, 20, PremiumColor,
                new SimpleFont("Arial", 10), TextAlignment.Left,
                Brushes.Transparent, Brushes.Transparent, 0);

            Draw.Text(this, "EquilibriumLabel", false, "Equilibrium",
                0, equilibrium, 20, Brushes.Silver,
                new SimpleFont("Arial", 10), TextAlignment.Left,
                Brushes.Transparent, Brushes.Transparent, 0);

            Draw.Text(this, "DiscountLabel", false, "Discount",
                0, discountBottom, 20, DiscountColor,
                new SimpleFont("Arial", 10), TextAlignment.Left,
                Brushes.Transparent, Brushes.Transparent, 0);
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Swing Length", Description = "Lookback period for swing detection", Order = 1, GroupName = "Parameters")]
        public int SwingLength
        {
            get { return swingLength; }
            set { swingLength = Math.Max(1, value); }
        }

        [XmlIgnore]
        [Display(Name = "Premium Color", Description = "Color of the premium zone", Order = 2, GroupName = "Parameters")]
        public Brush PremiumColor
        {
            get { return premiumColor; }
            set { premiumColor = value; }
        }

        [Browsable(false)]
        public string PremiumColorSerializable
        {
            get { return Serialize.BrushToString(PremiumColor); }
            set { PremiumColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Discount Color", Description = "Color of the discount zone", Order = 3, GroupName = "Parameters")]
        public Brush DiscountColor
        {
            get { return discountColor; }
            set { discountColor = value; }
        }

        [Browsable(false)]
        public string DiscountColorSerializable
        {
            get { return Serialize.BrushToString(DiscountColor); }
            set { DiscountColor = Serialize.StringToBrush(value); }
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PremiumDiscountZone[] cachePremiumDiscountZone;
		public PremiumDiscountZone PremiumDiscountZone(int swingLength)
		{
			return PremiumDiscountZone(Input, swingLength);
		}

		public PremiumDiscountZone PremiumDiscountZone(ISeries<double> input, int swingLength)
		{
			if (cachePremiumDiscountZone != null)
				for (int idx = 0; idx < cachePremiumDiscountZone.Length; idx++)
					if (cachePremiumDiscountZone[idx] != null && cachePremiumDiscountZone[idx].SwingLength == swingLength && cachePremiumDiscountZone[idx].EqualsInput(input))
						return cachePremiumDiscountZone[idx];
			return CacheIndicator<PremiumDiscountZone>(new PremiumDiscountZone(){ SwingLength = swingLength }, input, ref cachePremiumDiscountZone);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PremiumDiscountZone PremiumDiscountZone(int swingLength)
		{
			return indicator.PremiumDiscountZone(Input, swingLength);
		}

		public Indicators.PremiumDiscountZone PremiumDiscountZone(ISeries<double> input , int swingLength)
		{
			return indicator.PremiumDiscountZone(input, swingLength);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PremiumDiscountZone PremiumDiscountZone(int swingLength)
		{
			return indicator.PremiumDiscountZone(Input, swingLength);
		}

		public Indicators.PremiumDiscountZone PremiumDiscountZone(ISeries<double> input , int swingLength)
		{
			return indicator.PremiumDiscountZone(input, swingLength);
		}
	}
}

#endregion
