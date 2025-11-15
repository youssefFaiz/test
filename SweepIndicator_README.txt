========================================
SWEEP INDICATOR FOR NINJATRADER 8
========================================

INSTALLATION INSTRUCTIONS:
--------------------------

1. Locate your NinjaTrader 8 documents folder:
   - Default location: C:\Users\[YourUsername]\Documents\NinjaTrader 8\

2. Copy the SweepIndicator.cs file to:
   C:\Users\[YourUsername]\Documents\NinjaTrader 8\bin\Custom\Indicators\

3. Open NinjaTrader 8

4. Compile the indicator:
   - Press F5 or go to Tools > Edit NinjaScript > Indicator
   - Find "SweepIndicator" in the list
   - Click "Compile" or close the NinjaScript Editor (it will auto-compile)
   - Check for any compilation errors in the output window

5. Add to chart:
   - Right-click on your chart
   - Select "Indicators"
   - Find "Sweep Indicator" in the list
   - Click "Add" and configure settings


WHAT IS A SWEEP?
----------------

A liquidity sweep occurs when price temporarily moves beyond a swing high or swing low to "hunt" stop losses,
then reverses direction. This indicator detects these sweeps automatically.

Types of Sweeps:
- Bullish Sweep: Price wicks below a swing low but closes above it
- Bearish Sweep: Price wicks above a swing high but closes below it


KEY FEATURES:
-------------

✓ Liquidity Sweep Detection
  - Identifies when price "sweeps" swing highs and lows
  - Multiple detection modes for different trading styles

✓ Multi-Timeframe Support (MTF)
  - Detects sweeps on current timeframe
  - Optional higher timeframe (HTF) sweep detection
  - Different colors for current TF vs HTF sweeps

✓ Arrow Signals
  - Up arrows for bullish sweeps (below price)
  - Down arrows for bearish sweeps (above price)
  - Color-coded based on timeframe and direction

✓ Three Detection Modes
  1. Only Wicks - Pure wick sweeps only
  2. Only Outbreaks & Retest - Break and retest patterns
  3. Wicks + Outbreaks & Retest - Both combined


SETTINGS:
---------

1. SETTINGS GROUP

   Swing Length (default: 5)
   - Number of bars used to identify swing highs and lows
   - Higher values = major swings only
   - Lower values = more frequent, minor swings

   Detection Mode (default: "Only Wicks")
   - "Only Wicks" - Detects when price wicks beyond swing but closes back
   - "Only Outbreaks & Retest" - Detects when price breaks level then retests
   - "Wicks + Outbreaks & Retest" - Both detection methods combined

   Enable Higher Timeframe (default: true)
   - Turn on/off HTF sweep detection
   - Requires valid HTF timeframe setting

   Higher Timeframe (default: "15")
   - Timeframe to use for HTF detection
   - Examples: "15" (15min), "60" (1hour), "D" (daily)
   - Must be higher than chart timeframe

   Show Current TF (default: true)
   - Enable/disable current timeframe sweep detection
   - Can show only HTF sweeps if disabled


2. COLORS GROUP

   Current TF Bull (default: Lime Green)
   - Arrow color for bullish sweeps on current timeframe

   Current TF Bear (default: Red)
   - Arrow color for bearish sweeps on current timeframe

   HTF Bull (default: Lime)
   - Arrow color for bullish sweeps on higher timeframe

   HTF Bear (default: Orange Red)
   - Arrow color for bearish sweeps on higher timeframe


HOW IT WORKS:
-------------

DETECTION LOGIC:

1. SWING IDENTIFICATION
   - Indicator identifies swing highs and swing lows
   - Based on Swing Length parameter (left and right bars)

2. WICK SWEEPS (Mode: Only Wicks or Combined)
   - Bearish: High wicks above swing high, but closes below it
   - Bullish: Low wicks below swing low, but closes above it

3. OUTBREAK & RETEST (Mode: Only Outbreaks or Combined)
   - Price breaks through swing level (close beyond it)
   - Later, price retests the level from the opposite side
   - Bearish retest: Break above, then retest from below
   - Bullish retest: Break below, then retest from above

4. MULTI-TIMEFRAME
   - Current TF: Processes every bar on your chart timeframe
   - HTF: Processes higher timeframe bars separately
   - HTF sweeps shown with different colors


VISUAL SIGNALS:
---------------

ARROWS:
- Up Arrow (below bar) = Bullish sweep detected
- Down Arrow (above bar) = Bearish sweep detected

COLORS:
- Current TF arrows: User-defined colors (default: Lime/Red)
- HTF arrows: User-defined colors (default: Lime/OrangeRed)


TRADING STRATEGIES:
-------------------

1. REVERSAL TRADING
   - Sweep indicates potential reversal
   - Wait for confirmation before entry
   - Bullish sweep at support = potential long
   - Bearish sweep at resistance = potential short

2. MULTI-TIMEFRAME CONFLUENCE
   - Enable both current TF and HTF
   - Look for sweeps aligning on both timeframes
   - HTF sweeps = stronger signals

3. LIQUIDITY GRAB
   - Sweeps represent stop-loss hunting
   - Price often reverses after sweep
   - Enter after sweep in reversal direction


RECOMMENDED SETTINGS:
---------------------

SCALPING (1-5 min charts):
- Swing Length: 3-5
- Detection Mode: Only Wicks
- HTF: 15 or 30 minutes

DAY TRADING (5-15 min charts):
- Swing Length: 5-10
- Detection Mode: Wicks + Outbreaks & Retest
- HTF: 1 hour

SWING TRADING (1H-4H charts):
- Swing Length: 10-20
- Detection Mode: Wicks + Outbreaks & Retest
- HTF: Daily


TROUBLESHOOTING:
----------------

No arrows appearing:
1. Check "Show Current TF" is enabled
2. Verify Swing Length is appropriate for your chart
3. Ensure enough bars loaded (needs 2x Swing Length minimum)
4. Try different Detection Mode

Too many arrows:
1. Increase Swing Length (filters to major swings only)
2. Use "Only Wicks" mode (more selective)
3. Disable current TF, show only HTF sweeps

HTF not working:
1. Verify HTF timeframe is higher than chart timeframe
2. Check timeframe format (e.g., "15", "60", "D")
3. Disable and re-enable if needed


DIFFERENCES FROM PINE SCRIPT:
------------------------------

1. Arrows instead of Labels
   - Pine Script uses text labels
   - NinjaTrader uses arrow symbols (cleaner look)

2. Detection Mode Setting
   - Pine Script: dropdown in settings
   - NinjaTrader: string property (type exact text)
   - Valid values: "Only Wicks", "Only Outbreaks & Retest", "Wicks + Outbreaks & Retest"

3. Timeframe Format
   - Pine Script: uses TradingView format
   - NinjaTrader: uses simplified format
   - Examples: "15" = 15min, "60" = 1hour, "D" = daily


TECHNICAL NOTES:
----------------

- Calculate.OnBarClose ensures sweep detection on completed bars
- Pivots tracked in separate lists (highs/lows, current/HTF)
- Old pivots automatically removed after 2000 bars
- Multi-timeframe via AddDataSeries()
- Arrow placement: +/- 5 ticks from high/low


SUPPORT:
--------

For questions or issues, refer to NinjaTrader documentation:
https://ninjatrader.com/support/helpGuides/nt8/

========================================
