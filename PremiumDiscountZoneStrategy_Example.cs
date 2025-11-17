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
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
    public class PremiumDiscountZoneStrategy_Example : Strategy
    {
        private PremiumDiscountZone pdZone;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Example strategy using Premium Discount Zone indicator";
                Name = "PremiumDiscountZoneStrategy_Example";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
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

                // Example parameters
                SwingLength = 20;
                ProfitTarget = 100;
                StopLoss = 50;
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                // Initialize the Premium Discount Zone indicator
                pdZone = PremiumDiscountZone(SwingLength);
                AddChartIndicator(pdZone);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < BarsRequiredToTrade)
                return;

            // Example entry logic using the Premium Discount Zone

            // PREMIUM ZONE LOGIC: Allow only SHORT trades
            if (pdZone.IsInPremiumZone)
            {
                // Example: Enter short when in premium zone and some condition is met
                // Here we use a simple example: price crosses below a moving average
                if (CrossBelow(Close, SMA(50), 1))
                {
                    if (Position.MarketPosition == MarketPosition.Flat)
                    {
                        EnterShort("Short_Premium");
                        SetProfitTarget("Short_Premium", CalculationMode.Ticks, ProfitTarget);
                        SetStopLoss("Short_Premium", CalculationMode.Ticks, StopLoss, false);
                    }
                }
            }

            // DISCOUNT ZONE LOGIC: Allow only LONG trades
            else if (pdZone.IsInDiscountZone)
            {
                // Example: Enter long when in discount zone and some condition is met
                // Here we use a simple example: price crosses above a moving average
                if (CrossAbove(Close, SMA(50), 1))
                {
                    if (Position.MarketPosition == MarketPosition.Flat)
                    {
                        EnterLong("Long_Discount");
                        SetProfitTarget("Long_Discount", CalculationMode.Ticks, ProfitTarget);
                        SetStopLoss("Long_Discount", CalculationMode.Ticks, StopLoss, false);
                    }
                }
            }

            // EQUILIBRIUM/NO ZONE: Do not allow any trades
            // No trading logic here - effectively blocking trades when not in premium or discount zones
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Swing Length", Description = "Swing detection period for Premium/Discount zones", Order = 1, GroupName = "Premium Discount Zone")]
        public int SwingLength { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Profit Target (Ticks)", Description = "Profit target in ticks", Order = 2, GroupName = "Strategy Parameters")]
        public int ProfitTarget { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Stop Loss (Ticks)", Description = "Stop loss in ticks", Order = 3, GroupName = "Strategy Parameters")]
        public int StopLoss { get; set; }
        #endregion
    }
}
