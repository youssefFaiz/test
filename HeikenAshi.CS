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
    public class HeikenAshi8 : Indicator
    {
        private Series<double> haclose;
        private Series<double> haopen;
        private Series<int> Signal;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"HeikenAshi technique.";
                Name = "HeikenAshi8";
                Calculate = Calculate.OnEachTick;
                IsOverlay = true;
            }
            else if (State == State.DataLoaded)
            {
                haclose = new Series<double>(this);
                haopen = new Series<double>(this);
                Signal = new Series<int>(this);
            }
        }

        protected override void OnBarUpdate()
        {
            haclose[0] = (Open[0] + High[0] + Low[0] + Close[0]) / 4;

            if (CurrentBar == 0)
                haopen[0] = (Open[0] + Close[0]) / 2;
            else
                haopen[0] = (haopen[1] + haclose[1]) / 2;

            bool isHeikinAshiUp = haclose[0] > haopen[0];
            bool isHeikinAshiDown = haclose[0] <= haopen[0];

            Signal[0] = isHeikinAshiUp ? 1 : isHeikinAshiDown ? 2 : 0;

            // Sets the brush used for the bar color to its default color as defined in the chart properties dialog
            //BarBrush = Brushes.Yellow;//null;
            BarBrush = isHeikinAshiUp ? Brushes.Aqua : isHeikinAshiDown ? Brushes.Red : null;
        }

        #region Properties
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> barOpen
        {
            get { return haopen; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> barClose
        {
            get { return haclose; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<int> signal
        {
            get { return Signal; }
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private HeikenAshi8[] cacheHeikenAshi8;
		public HeikenAshi8 HeikenAshi8()
		{
			return HeikenAshi8(Input);
		}

		public HeikenAshi8 HeikenAshi8(ISeries<double> input)
		{
			if (cacheHeikenAshi8 != null)
				for (int idx = 0; idx < cacheHeikenAshi8.Length; idx++)
					if (cacheHeikenAshi8[idx] != null &&  cacheHeikenAshi8[idx].EqualsInput(input))
						return cacheHeikenAshi8[idx];
			return CacheIndicator<HeikenAshi8>(new HeikenAshi8(), input, ref cacheHeikenAshi8);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.HeikenAshi8 HeikenAshi8()
		{
			return indicator.HeikenAshi8(Input);
		}

		public Indicators.HeikenAshi8 HeikenAshi8(ISeries<double> input )
		{
			return indicator.HeikenAshi8(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.HeikenAshi8 HeikenAshi8()
		{
			return indicator.HeikenAshi8(Input);
		}

		public Indicators.HeikenAshi8 HeikenAshi8(ISeries<double> input )
		{
			return indicator.HeikenAshi8(input);
		}
	}
}

#endregion
