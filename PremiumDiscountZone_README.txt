================================================================================
PREMIUM DISCOUNT ZONE INDICATOR - NINJTRADER 8
================================================================================

OVERVIEW
--------
This indicator identifies Premium and Discount zones based on swing highs
and lows, allowing strategies to filter trades based on price location
within the overall range.

CONVERSION FROM PINESCRIPT
---------------------------
This is a conversion of the PineScript "CandelaCharts - Premium & Discount"
indicator with the following modifications:
- Only AUTO mode (swing-based detection)
- Only PRO display mode (lines + filled boxes)
- Simplified inputs: Swing Length, Premium Color, Discount Color

FILES
-----
1. PremiumDiscountZone.cs - The main indicator
2. PremiumDiscountZoneStrategy_Example.cs - Example strategy showing usage
3. PremiumDiscountZone_README.txt - This file

INSTALLATION
------------
1. Copy PremiumDiscountZone.cs to your NinjaTrader indicators folder:
   Documents\NinjaTrader 8\bin\Custom\Indicators\

2. (Optional) Copy the example strategy to:
   Documents\NinjaTrader 8\bin\Custom\Strategies\

3. Open NinjaTrader and compile (Tools > Compile or F5)

INDICATOR FEATURES
------------------
- Automatically detects swing highs and lows based on lookback period
- Draws premium zone (green by default) in upper 50% of range
- Draws discount zone (red by default) in lower 50% of range
- Draws equilibrium line at 50% midpoint
- Extends zones 20 bars into the future
- Displays zone labels (Premium, Equilibrium, Discount)

PARAMETERS
----------
1. Swing Length (default: 20)
   - Lookback period for swing high/low detection
   - Higher values = longer-term swings
   - Lower values = shorter-term swings

2. Premium Color (default: Green)
   - Color for the premium zone

3. Discount Color (default: Red)
   - Color for the discount zone

HOW IT WORKS
------------
The indicator uses a swing detection algorithm:

1. SWING DETECTION
   - Looks back 'Swing Length' bars
   - Detects when a bar's high is the highest in the lookback period (swing high)
   - Detects when a bar's low is the lowest in the lookback period (swing low)
   - Continuously updates swing high/low as new extremes are made

2. ZONE CALCULATION
   - Premium Zone: From swing high to equilibrium (50% level)
   - Discount Zone: From equilibrium to swing low
   - Equilibrium: Midpoint between swing high and swing low

3. VISUAL DISPLAY (Pro Mode)
   - Draws filled rectangles for premium (top) and discount (bottom)
   - Draws lines at swing high, equilibrium, and swing low
   - Displays text labels for each zone
   - Extends zones 20 bars into the future

USING IN STRATEGIES
--------------------
The indicator exposes two public boolean properties:

1. IsInPremiumZone
   - Returns true when current price is in the premium zone
   - Use this to ALLOW ONLY SHORT TRADES

2. IsInDiscountZone
   - Returns true when current price is in the discount zone
   - Use this to ALLOW ONLY LONG TRADES

3. When neither is true (price in equilibrium)
   - DO NOT ALLOW ANY TRADES

EXAMPLE STRATEGY USAGE
----------------------
// In OnStateChange, State.DataLoaded:
private PremiumDiscountZone pdZone;
pdZone = PremiumDiscountZone(20); // 20 bar swing length

// In OnBarUpdate:
if (pdZone.IsInPremiumZone)
{
    // ONLY allow short trades here
    if (YourShortEntryCondition)
    {
        EnterShort();
    }
}
else if (pdZone.IsInDiscountZone)
{
    // ONLY allow long trades here
    if (YourLongEntryCondition)
    {
        EnterLong();
    }
}
// If neither condition is true, no trades are allowed

COMPLETE STRATEGY EXAMPLE
--------------------------
See PremiumDiscountZoneStrategy_Example.cs for a complete working example
that demonstrates:
- How to initialize the indicator
- How to check IsInPremiumZone for short trades
- How to check IsInDiscountZone for long trades
- How to prevent trades in equilibrium zone

TRADING LOGIC
-------------
The concept behind Premium/Discount zones:

PREMIUM ZONE (Upper range):
- Price is trading at a "premium" (expensive)
- Sellers may be more willing to sell
- Strategy should ONLY look for SHORT opportunities
- Reduces risk of shorting at discounted prices

DISCOUNT ZONE (Lower range):
- Price is trading at a "discount" (cheap)
- Buyers may be more willing to buy
- Strategy should ONLY look for LONG opportunities
- Reduces risk of buying at premium prices

EQUILIBRIUM (Middle range):
- Price is fairly valued
- No clear advantage for longs or shorts
- Best to stay out and wait for better opportunities

TIPS
----
1. Adjust Swing Length based on your trading timeframe:
   - Scalping: 10-20 bars
   - Day trading: 20-50 bars
   - Swing trading: 50-100 bars

2. The indicator works on any timeframe and instrument

3. Consider combining with other indicators for entry timing:
   - Use Premium/Discount zones for DIRECTION filtering
   - Use other indicators (RSI, MACD, etc.) for ENTRY timing

4. The zones update dynamically as new swing highs/lows are formed

5. Zones extend 20 bars into the future to show current zones clearly

TROUBLESHOOTING
---------------
- If zones don't appear: Increase the Swing Length parameter
- If zones update too frequently: Increase the Swing Length
- If zones are too static: Decrease the Swing Length

DIFFERENCES FROM PINESCRIPT VERSION
------------------------------------
REMOVED:
- Custom mode (time-based range selection)
- Solid, Outlined, and Flat display modes
- Line style options (solid/dashed/dotted)
- Line width options
- Text size and font options
- Equilibrium show/hide option (always shown)
- Labels show/hide option (always shown)

KEPT:
- Auto mode (swing-based detection)
- Pro display mode
- Premium/Discount color customization
- Swing length parameter

ADDED:
- Public properties for strategy integration (IsInPremiumZone, IsInDiscountZone)
- Optimized for NinjaTrader 8 platform
- Strategy-friendly API

================================================================================
© Converted to NinjaTrader 8
Original PineScript: © CandelaCharts
================================================================================
