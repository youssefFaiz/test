========================================
FVG INDICATOR FOR NINJATRADER 8
========================================

INSTALLATION INSTRUCTIONS:
--------------------------

1. Locate your NinjaTrader 8 documents folder:
   - Default location: C:\Users\[YourUsername]\Documents\NinjaTrader 8\

2. Copy the FVGIndicator.cs file to:
   C:\Users\[YourUsername]\Documents\NinjaTrader 8\bin\Custom\Indicators\

3. Open NinjaTrader 8

4. Compile the indicator:
   - Press F5 or go to Tools > Edit NinjaScript > Indicator
   - Find "FVGIndicator" in the list
   - Click "Compile" or close the NinjaScript Editor (it will auto-compile)
   - Check for any compilation errors in the output window

5. Add to chart:
   - Right-click on your chart
   - Select "Indicators"
   - Find "FVG Indicator" in the list
   - Click "Add" and configure settings


KEY FEATURES IMPLEMENTED:
-------------------------

✓ Fair Value Gap Detection
  - Detects both bullish and bearish FVGs
  - Uses 3-candle pattern (gap between bar 0 and bar 2)

✓ Box Extension Logic
  - Boxes extend to the right continuously
  - Stops extending when FVG is mitigated
  - Freezes at exact mitigation bar

✓ Mitigation Tracking
  - Bullish FVG: Mitigated when close goes below FVG open
  - Bearish FVG: Mitigated when close goes above FVG open
  - Optional auto-deletion of filled FVGs

✓ Visual Customization
  - Customizable box colors with opacity control
  - Customizable border colors with opacity control
  - Adjustable border width (1-5)
  - Label text display with font size control
  - Center Equilibrium (CE) dotted lines

✓ Multi-Timeframe Support
  - Works with any timeframe (Minute, Hour, Day, etc.)
  - Timeframe validation (hides lower timeframes if enabled)
  - Maximum 100 FVGs displayed (configurable)


SETTINGS OVERVIEW:
------------------

1. GENERAL SETTINGS
   - Show Indicator: Enable/disable the indicator
   - Remove Filled FVGs: Auto-delete mitigated FVGs
   - Hide Lower Timeframes: Only show FVGs from selected timeframe or higher

2. TIMEFRAME SETTINGS
   - Timeframe Value: Number (e.g., 1, 5, 15)
   - Timeframe Type: Type (e.g., "Minute", "Day", "Hour")
   - Label Text: Text displayed on FVG boxes
   - Max FVG Count: Maximum number of FVGs to display (1-500)

3. BOX VISUALS
   - Bullish Box Color: Color for bullish FVG boxes (default: Green)
   - Bullish Box Opacity: Transparency 0-100 (default: 10)
   - Bearish Box Color: Color for bearish FVG boxes (default: Red)
   - Bearish Box Opacity: Transparency 0-100 (default: 10)

4. BORDER VISUALS
   - Border Width: Thickness of box borders (1-5)
   - Bullish Border Color: Border color for bullish FVGs
   - Bullish Border Opacity: Border transparency 0-100
   - Bearish Border Color: Border color for bearish FVGs
   - Bearish Border Opacity: Border transparency 0-100

5. LABEL VISUALS
   - Label Bull Color: Text color for bullish labels (default: Black)
   - Label Bear Color: Text color for bearish labels (default: Black)
   - Label Font Size: Font size for labels (6-24)

6. CE LINE VISUALS
   - Bullish CE Line Color: Color for bullish center equilibrium line
   - Bearish CE Line Color: Color for bearish center equilibrium line
   - CE Line Padding: Space between line and box edge (1-50 bars)


HOW IT WORKS:
-------------

FVG DETECTION:
- Bullish FVG: When the high of the current bar is less than the low of 2 bars ago
- Bearish FVG: When the low of the current bar is greater than the high of 2 bars ago

BOX BEHAVIOR:
- Created at the start time of the FVG
- Extends continuously to the right on each new bar
- When mitigated, stops at the exact bar where mitigation occurred
- Optionally deleted if "Remove Filled FVGs" is enabled

MITIGATION LOGIC:
- Bullish FVG: Mitigated when price closes below the FVG opening level
- Bearish FVG: Mitigated when price closes above the FVG opening level


USAGE TIPS:
-----------

1. Start with default settings to see how the indicator works
2. Adjust opacity for better visibility on your chart background
3. Use transparent border colors if you don't want borders
4. Set "Remove Filled FVGs" to false if you want to keep historical FVGs
5. Increase "Max FVG Count" if you need to see more FVGs on the chart
6. The indicator works best on clean charts with minimal clutter


TROUBLESHOOTING:
----------------

If the indicator doesn't appear:
1. Check NinjaScript Editor for compilation errors (press F5)
2. Ensure the file is in the correct folder
3. Try recompiling (Tools > Compile All)
4. Restart NinjaTrader 8

If FVGs don't appear:
1. Check "Show Indicator" is enabled
2. Verify your timeframe settings match your chart
3. Ensure "Hide Lower Timeframes" is disabled initially
4. Check that there are actual FVGs in the price action


DIFFERENCES FROM PINE SCRIPT VERSION:
--------------------------------------

- NinjaTrader uses bars ago indexing (0 = current bar)
- Drawing objects use tags for identification
- Colors use Brush objects with opacity percentages
- Time-based calculations use DateTime instead of Unix timestamps
- Settings are organized in property groups


TECHNICAL NOTES:
----------------

- Uses Calculate.OnBarClose for consistent FVG detection
- Implements multi-timeframe data via AddDataSeries()
- FVGs stored in List<FVGData> for efficient management
- Drawing objects updated on each bar to extend boxes
- Unique tags ensure proper object tracking and deletion


For questions or issues, refer to NinjaTrader documentation:
https://ninjatrader.com/support/helpGuides/nt8/

========================================
