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

            // Calculate zone boundaries
            if (swingHighPrice > 0 && swingLowPrice > 0)
            {
                CalculateZones();

                // Check if current price is in premium or discount zone
                CheckPriceInZone();

                // Draw zones on last bar
                if (CurrentBar == Count - 1)
                {
                    DrawZones();
                }
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
            double range = swingHighPrice - swingLowPrice;

            // Premium zone: top 5% to 50% of range
            premiumTop = swingHighPrice;
            premiumBottom = swingHighPrice - (range * 0.05);

            // Equilibrium: 50% level
            equilibrium = (swingHighPrice + swingLowPrice) / 2.0;

            // Discount zone: bottom 5% to 50% of range
            discountTop = swingLowPrice + (range * 0.05);
            discountBottom = swingLowPrice;
        }

        private void CheckPriceInZone()
        {
            double currentPrice = Close[0];

            // Check if in premium zone (above equilibrium, closer to swing high)
            IsInPremiumZone = currentPrice >= equilibrium && currentPrice <= premiumTop;

            // Check if in discount zone (below equilibrium, closer to swing low)
            IsInDiscountZone = currentPrice <= equilibrium && currentPrice >= discountBottom;
        }

        private void DrawZones()
        {
            // Clear previous drawings
            RemoveDrawObjects();

            int leftBar = Math.Min(swingHighBar, swingLowBar);
            int rightBar = CurrentBar + 20; // Extend 20 bars to the right

            // Draw Premium Zone (Pro mode: line + filled box)
            // Premium line at top
            Draw.Line(this, "PremiumLine", false,
                leftBar, premiumTop,
                rightBar, premiumTop,
                PremiumColor, DashStyleHelper.Solid, 2);

            // Premium filled box (from top to equilibrium)
            Draw.Rectangle(this, "PremiumBox", false,
                leftBar, premiumTop,
                rightBar, equilibrium,
                PremiumColor, PremiumColor, 20);

            // Draw Equilibrium line
            Draw.Line(this, "EquilibriumLine", false,
                leftBar, equilibrium,
                rightBar, equilibrium,
                Brushes.Gray, DashStyleHelper.Solid, 1);

            // Draw Discount Zone (Pro mode: line + filled box)
            // Discount line at bottom
            Draw.Line(this, "DiscountLine", false,
                leftBar, discountBottom,
                rightBar, discountBottom,
                DiscountColor, DashStyleHelper.Solid, 2);

            // Discount filled box (from equilibrium to bottom)
            Draw.Rectangle(this, "DiscountBox", false,
                leftBar, equilibrium,
                rightBar, discountBottom,
                DiscountColor, DiscountColor, 20);

            // Draw labels
            Draw.Text(this, "PremiumLabel", false, "Premium",
                rightBar, premiumTop, 0, PremiumColor,
                new SimpleFont("Arial", 10), TextAlignment.Left,
                Brushes.Transparent, Brushes.Transparent, 0);

            Draw.Text(this, "EquilibriumLabel", false, "Equilibrium",
                rightBar, equilibrium, 0, Brushes.Gray,
                new SimpleFont("Arial", 10), TextAlignment.Left,
                Brushes.Transparent, Brushes.Transparent, 0);

            Draw.Text(this, "DiscountLabel", false, "Discount",
                rightBar, discountBottom, 0, DiscountColor,
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
