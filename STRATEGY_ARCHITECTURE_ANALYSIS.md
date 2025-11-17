# AllinOneStrategy Architecture Analysis

## OVERVIEW

The AllinOneStrategy is a comprehensive multi-entry trading system with:
- **6 Entry Types**: BOS, CISD, FVG, IFVG, Sweep (LTF/HTF), HeikenAshi (LTF/HTF)
- **3 Entry Modes per type**: Standalone, Combined, Display-Only
- **4 Trade Quantities** with independent R:R ratios
- **Unified Signal Management System**
- **Risk-Reward (R:R) Based Stops and Targets**

---

## CORE ARCHITECTURE

### 1. SIGNAL MANAGEMENT SYSTEM

All entry types use the same `SignalInfo` class and flow through a unified pipeline:

```csharp
// SignalInfo Class (lines 275-301)
public class SignalInfo
{
    public string SignalType { get; set; }  // "BOS", "FVG", "LTF_Sweep", etc.
    public int OrderPosition { get; set; }  // 0=Any, 1=1st, 2=2nd, etc.
    public bool IsLong { get; set; }
    public int SignalBar { get; set; }
    public double EntryPrice { get; set; }
    public double StopLoss { get; set; }    // CRITICAL: Stop loss price
    public bool UseStopLossRR { get; set; } // Enable R:R for this entry type?
    public int StopLossPlusTicks { get; set; } // Plus ticks adjustment
    public int MaxBarsBetween { get; set; }
    public bool IsCombinedEntry { get; set; }
    public bool IsStandalone { get; set; }
    public bool HasGeneratedTrade { get; set; }
}
```

**Key Fields:**
- `SignalType`: Identifies entry source (BOS, FVG, Sweep, etc.)
- `StopLoss`: Pre-calculated stop loss PRICE (not ticks!)
- `UseStopLossRR`: If true, uses `StopLoss` to calculate R:R targets
- `StopLossPlusTicks`: Additional buffer added to stop loss
- `OrderPosition`: For combined entries (0=Any, 1=1st, 2=2nd, etc.)

---

## 2. ENTRY SIGNAL FLOW

### A. Detection Phase (OnBarUpdate)

Each entry type has its own detection method:

```
OnBarUpdate()
├── CheckForBOSSignal()           // Detects swing breaks
├── CheckForCISDSignal()          // Detects pullback reversals
├── CheckForFVGSignal()           // Detects fair value gaps
├── CheckForIFVGSignal()          // Detects inverse FVGs
├── CheckForLTFSweepSignal()      // Detects LTF sweeps
├── CheckForHTFSweepSignal()      // Detects HTF sweeps
├── CheckForLTFHeikenAshiSignal() // Detects LTF HA color changes
└── CheckForHTFHeikenAshiSignal() // Detects HTF HA color changes
```

### B. Signal Creation Pattern

Each detection method creates a `SignalInfo` with:
1. **Entry Price**: Close[0] ± EntryPlusTicks
2. **Stop Loss PRICE**: Calculated based on entry type logic
3. **Flags**: Standalone/Combined/DisplayOnly

**Example: BOS Bullish Signal (lines 1183-1206)**
```csharp
SignalInfo signal = new SignalInfo
{
    SignalType = "BOS",
    IsLong = true,
    EntryPrice = Close[0] + (bosEntryPlusTicks * TickSize),
    StopLoss = GetLowestLowBetween(swingBarsAgo, 0),  // PRICE!
    UseStopLossRR = useBOSStopLossRR,                 // Enable R:R?
    StopLossPlusTicks = bosStopLossPlusTicks,         // Plus 2 ticks
    IsCombinedEntry = useBOSCombinedEntry,
    IsStandalone = useBOSEntry && !useBOSCombinedEntry
};
AddSignal(signal);
```

---

## 3. STOP LOSS CALCULATION (BY ENTRY TYPE)

### BOS (Break of Structure) - lines 1180, 1224

**Bullish BOS:**
```csharp
// Stop = Lowest low between swing high and break point
double stopLoss = GetLowestLowBetween(swingBarsAgo, 0);
```

**Bearish BOS:**
```csharp
// Stop = Highest high between swing low and break point
double stopLoss = GetHighestHighBetween(swingBarsAgo, 0);
```

### CISD (Change in State of Delivery) - lines 1518-1540

**Bullish CISD:**
```csharp
// Stop = Lowest low between start candle and break candle
double stopLoss = GetLowestLowBetween(startCandle, breakCandle);
```

**Bearish CISD:**
```csharp
// Stop = Highest high between start candle and break candle
double stopLoss = GetHighestHighBetween(startCandle, breakCandle);
```

### FVG (Fair Value Gap) - lines 1823-1877

**Bullish FVG:**
```csharp
// Stop = Bottom of FVG gap (bar[3].High)
double stopLoss = fvg.Bottom - (fvgStopLossPlusTicks * TickSize);
```

**Bearish FVG:**
```csharp
// Stop = Top of FVG gap (bar[3].Low)
double stopLoss = fvg.Top + (fvgStopLossPlusTicks * TickSize);
```

### SWEEP (Liquidity Sweep) - lines 2408-2510

**Bullish Sweep:**
```csharp
// Stop = Last swing LOW using Swing(5) indicator
double? lastSwingLow = GetLastSwingLow(SweepSwingLength);
double stopLoss = lastSwingLow.Value - (sweepSwingStopLossPlusTicks * TickSize);
// Fallback: pivot.Price - plusTicks
```

**Bearish Sweep:**
```csharp
// Stop = Last swing HIGH using Swing(5) indicator
double? lastSwingHigh = GetLastSwingHigh(SweepSwingLength);
double stopLoss = lastSwingHigh.Value + (sweepSwingStopLossPlusTicks * TickSize);
// Fallback: pivot.Price + plusTicks
```

### HeikenAshi - lines 2778

```csharp
// NO integrated stop loss
double stopLoss = 0;
bool useStopLossRR = false;
```
**Note**: HeikenAshi uses custom stops or swing-based stops in ExecuteEntry.

### IFVG (Inverse FVG) - lines 2072-2123

**Bullish IFVG:**
```csharp
// Stop = Bottom of original FVG (was bearish, now mitigated upward)
double stopLoss = ifvg.Bottom - (ifvgStopLossPlusTicks * TickSize);
```

**Bearish IFVG:**
```csharp
// Stop = Top of original FVG (was bullish, now mitigated downward)
double stopLoss = ifvg.Top + (ifvgStopLossPlusTicks * TickSize);
```

---

## 4. COMBINED ENTRY LOGIC

### A. Combined Entry Validation (lines 568-658)

Two modes for combining signals:

#### Mode 1: "Any" Order (OrderPosition = 0)
```csharp
// Need at least 2 DIFFERENT signal types with same direction
var matchingSignals = activeSignals
    .Where(s => s.IsCombinedEntry && s.IsLong == newSignal.IsLong && !s.HasGeneratedTrade)
    .ToList();

var uniqueSignalTypes = matchingSignals.Select(s => s.SignalType).Distinct().Count();

if (uniqueSignalTypes >= 2)
    return true;  // Valid combined entry
```

**Example**: BOS + FVG (both bullish) → LONG

#### Mode 2: Sequential Order (OrderPosition = 1, 2, 3, etc.)
```csharp
// Check all previous positions exist in chronological order
for (int i = 1; i < newSignal.OrderPosition; i++)
{
    var previousSignal = activeSignals.FirstOrDefault(s =>
        s.OrderPosition == i &&
        s.IsCombinedEntry &&
        s.IsLong == newSignal.IsLong &&
        !s.HasGeneratedTrade
    );

    if (previousSignal == null || previousSignal.SignalBar >= newSignal.SignalBar)
        return false;  // Missing or out of order
}
```

**Example**:
- BOS (Order=1) at bar 100
- FVG (Order=2) at bar 105
- Sweep (Order=3) at bar 108 → TRIGGER ENTRY

### B. Combined Entry Stop Loss (lines 660-675)

```csharp
// Find the LAST signal (chronologically) with R:R enabled
var signalsByTime = combinedSignals.OrderByDescending(s => s.SignalBar).ToList();

foreach (var signal in signalsByTime)
{
    if (signal.UseStopLossRR)
        return signal.StopLoss + (signal.StopLossPlusTicks * TickSize);
}
```

**Priority**: Most recent signal's stop loss (if R:R enabled)

---

## 5. TRADE EXECUTION & R:R SYSTEM

### A. ExecuteEntry Method (lines 750-1031)

Processes up to 4 separate trades per entry signal:

```csharp
ExecuteEntry(SignalInfo triggerSignal, List<SignalInfo> allSignals)
{
    // 1. Get stop loss (combined or standalone)
    bool isCombinedEntry = allSignals.Count > 1;
    double stopLoss = isCombinedEntry
        ? GetCombinedEntryStopLoss(allSignals)
        : triggerSignal.StopLoss + (triggerSignal.StopLossPlusTicks * TickSize);

    // 2. Execute Trade 1, Trade 2, Trade 3, Trade 4 (if quantity > 0)
    //    Each trade can have:
    //    - Custom stop OR shared stop
    //    - Custom target OR R:R calculated target
}
```

### B. Trade 1 Logic (lines 787-831)

```csharp
// === TRADE 1 ===
if (trade1Quantity > 0)
{
    // 1. Determine stop loss
    double finalStopLoss1 = useCustomStopLoss1
        ? (isLong ? entryPrice - customStopLoss1Ticks * TickSize
                  : entryPrice + customStopLoss1Ticks * TickSize)
        : stopLoss;  // Use signal's stop loss

    // 2. Calculate risk
    double risk1 = Math.Abs(entryPrice - finalStopLoss1);

    // 3. Determine profit target
    double profitTarget1 = 0;

    if (useCustomTarget1)
        profitTarget1 = isLong ? entryPrice + customTarget1Ticks * TickSize
                               : entryPrice - customTarget1Ticks * TickSize;
    else if (useRR_1_1_Trade1)
        profitTarget1 = isLong ? entryPrice + risk1 : entryPrice - risk1;
    else if (useRR_1_2_Trade1)
        profitTarget1 = isLong ? entryPrice + (risk1 * 2) : entryPrice - (risk1 * 2);
    else if (useRR_1_3_Trade1)
        profitTarget1 = isLong ? entryPrice + (risk1 * 3) : entryPrice - (risk1 * 3);
    // ... up to 1:4

    // 4. Set stop and target BEFORE entry
    SetStopLoss("BOS_1", CalculationMode.Price, finalStopLoss1, false);
    SetProfitTarget("BOS_1", CalculationMode.Price, profitTarget1);

    // 5. Enter
    if (isLong)
        EnterLong(trade1Quantity, "BOS_1");
    else
        EnterShort(trade1Quantity, "BOS_1");
}
```

**Same pattern repeats for Trade 2, Trade 3, Trade 4**

### C. R:R Calculation Formula

```
Risk = |EntryPrice - StopLoss|

Target = EntryPrice ± (Risk × Ratio)

Ratios available:
- 1:1   → Risk × 1
- 1:1.5 → Risk × 1.5
- 1:2   → Risk × 2
- 1:2.5 → Risk × 2.5
- 1:3   → Risk × 3
- 1:3.5 → Risk × 3.5
- 1:4   → Risk × 4
```

**Example:**
```
Entry: 4500
Stop:  4475 (25 ticks below)
Risk:  25 ticks

1:1 Target   = 4500 + 25 = 4525
1:2 Target   = 4500 + 50 = 4550
1:3 Target   = 4500 + 75 = 4575
```

---

## 6. MULTIPLE TRADE QUANTITIES

### Configuration
```csharp
// Line 34-37
private int trade1Quantity = 1;  // First contract
private int trade2Quantity = 0;  // Second contract (off by default)
private int trade3Quantity = 0;  // Third contract
private int trade4Quantity = 0;  // Fourth contract
```

### Use Cases

**Example 1: Scalping (1 contract)**
```
trade1Quantity = 1
useRR_1_2_Trade1 = true
→ 1 contract with 1:2 R:R
```

**Example 2: Scaling Out (3 contracts)**
```
trade1Quantity = 1  → 1:1 R:R (quick profit)
trade2Quantity = 1  → 1:2 R:R
trade3Quantity = 1  → 1:3 R:R (runner)
```

**Example 3: Mixed Stops**
```
trade1Quantity = 1
useCustomStopLoss1 = true, customStopLoss1Ticks = 10
useRR_1_2_Trade1 = true

trade2Quantity = 1
useCustomStopLoss2 = false  → Uses signal's stop loss
useRR_1_3_Trade2 = true
```

---

## 7. ENTRY TYPE COMPARISON

| Entry Type | Stop Loss Calculation | R:R Enabled? | Special Notes |
|-----------|----------------------|--------------|---------------|
| **BOS** | Lowest/Highest between swing & break | ✅ Yes | Uses swing pivot detection |
| **CISD** | Lowest/Highest between start & break | ✅ Yes | Two-phase: level creation → cross detection |
| **FVG** | FVG gap boundary (top/bottom) | ✅ Yes | Requires retest + close beyond |
| **IFVG** | Original FVG boundary | ✅ Yes | FVG that got mitigated (inverse) |
| **Sweep LTF** | Last swing high/low (chart TF) | ✅ Yes | Uses Swing(5) indicator |
| **Sweep HTF** | Last swing high/low (15min TF) | ✅ Yes | HTF pivots checked on every LTF bar |
| **HeikenAshi LTF** | None (custom only) | ❌ No | Color change detection (cyan ↔ red) |
| **HeikenAshi HTF** | None (custom only) | ❌ No | Color change detection (aqua ↔ purple) |

---

## 8. ADDING NEW ENTRY TYPES (TEMPLATE)

To add a new entry type (e.g., your HeikenAshi improvements), follow this pattern:

### Step 1: Add Settings (Variables section)
```csharp
// ===== MyNewEntry Settings =====
private bool useMyNewEntry = false;
private bool useMyNewCombinedEntry = false;
private bool useMyNewDisplayOnly = false;
private int myNewSignalOrder = 0;
private int myNewMaxBarsBetween = 10;
private int myNewEntryPlusTicks = 0;
private bool useMyNewStopLossRR = true;
private int myNewStopLossPlusTicks = 2;
```

### Step 2: Add Detection Method
```csharp
private void CheckForMyNewEntry()
{
    // Your detection logic here

    // When signal detected:
    SignalInfo signal = new SignalInfo
    {
        SignalType = "MyNewEntry",
        OrderPosition = myNewSignalOrder,
        IsLong = true/false,
        SignalBar = CurrentBar,
        SignalTime = Time[0],
        EntryPrice = Close[0] + (myNewEntryPlusTicks * TickSize),
        StopLoss = CalculateStopLoss(),  // YOUR LOGIC HERE
        UseStopLossRR = useMyNewStopLossRR,
        StopLossPlusTicks = myNewStopLossPlusTicks,
        MaxBarsBetween = myNewMaxBarsBetween,
        IsCombinedEntry = useMyNewCombinedEntry,
        IsStandalone = useMyNewEntry && !useMyNewCombinedEntry,
        HasGeneratedTrade = false
    };

    AddSignal(signal);
}
```

### Step 3: Call in OnBarUpdate
```csharp
protected override void OnBarUpdate()
{
    // ... existing code ...

    // Add your entry check
    if (useMyNewEntry || useMyNewCombinedEntry || useMyNewDisplayOnly)
    {
        CheckForMyNewEntry();
    }

    // ... existing code ...
}
```

### Step 4: Add Properties (for user inputs)
```csharp
[NinjaScriptProperty]
[Display(Name = "Use My New Entry", Order = 1, GroupName = "MyNewEntry")]
public bool UseMyNewEntry
{
    get { return useMyNewEntry; }
    set { useMyNewEntry = value; }
}
```

---

## 9. KEY INSIGHTS FOR DEVELOPMENT

### A. Signal Lifecycle
```
1. DETECTION → Create SignalInfo with StopLoss PRICE
2. STORAGE → Add to activeSignals list
3. VALIDATION → Check standalone vs combined
4. EXECUTION → Create 1-4 trades with R:R targets
5. CLEANUP → Mark as HasGeneratedTrade = true
```

### B. Stop Loss is ALWAYS a Price (Not Ticks!)
```csharp
// ❌ WRONG
StopLoss = 25;  // This is wrong - not ticks!

// ✅ CORRECT
StopLoss = Close[0] - (25 * TickSize);  // Price!
```

### C. R:R Calculation Happens AFTER Signal Creation
```
Signal stores:  StopLoss = 4475 (price)
ExecuteEntry calculates:
  Risk = |4500 - 4475| = 25 ticks
  Target = 4500 + (25 * 2) = 4550 (for 1:2 R:R)
```

### D. Combined Entries Don't Replace Standalone
You can enable BOTH:
- `useBOSEntry = true` → Standalone BOS trades
- `useBOSCombinedEntry = true` → BOS participates in combined entries

They generate SEPARATE signals!

---

## 10. CRITICAL CODE LOCATIONS

| Feature | Line Numbers | Method Name |
|---------|-------------|-------------|
| **Signal Validation** | 568-658 | ValidateCombinedEntry() |
| **Trade Execution** | 750-1031 | ExecuteEntry() |
| **R:R Calculation** | 794-812 | Inside ExecuteEntry (Trade 1) |
| **BOS Detection** | 1034-1252 | CheckForBOSSignal() |
| **BOS Stop Loss** | 1180, 1224 | GetLowestLowBetween() / GetHighestHighBetween() |
| **CISD Detection** | 1355-1540 | CheckForCISDSignal() |
| **FVG Detection** | 1650-1877 | CheckForFVGSignal() |
| **Sweep Detection** | 2381-2564 | ProcessSweeps() |
| **Sweep Stop Loss** | 2409, 2499 | GetLastSwingHigh() / GetLastSwingLow() |
| **HeikenAshi LTF** | 2636-2681 | CheckForLTFHeikenAshiSignal() |
| **HeikenAshi HTF** | 2683-2744 | CheckForHTFHeikenAshiSignal() |
| **HA Entry Generation** | 2746-2815 | GenerateHeikenAshiEntry() |

---

## 11. EXAMPLE: COMPLETE ENTRY FLOW

Let's trace a **BOS + FVG Combined Entry**:

### Bar 100: BOS Bullish Detected
```csharp
// CheckForBOSSignal() creates:
SignalInfo bosSignal = new SignalInfo {
    SignalType = "BOS",
    OrderPosition = 1,  // 1st signal
    IsLong = true,
    StopLoss = 4475,  // Lowest low between swing and break
    UseStopLossRR = true,
    StopLossPlusTicks = 2,
    IsCombinedEntry = true,  // Will wait for 2nd signal
    IsStandalone = false
};
activeSignals.Add(bosSignal);  // Stored, waiting
```

### Bar 105: FVG Bullish Retest
```csharp
// CheckForFVGSignal() creates:
SignalInfo fvgSignal = new SignalInfo {
    SignalType = "FVG",
    OrderPosition = 2,  // 2nd signal
    IsLong = true,
    StopLoss = 4470,  // FVG bottom
    UseStopLossRR = true,
    StopLossPlusTicks = 2,
    IsCombinedEntry = true,
    IsStandalone = false
};
activeSignals.Add(fvgSignal);  // Now we have 2 signals!
```

### Validation (ValidateCombinedEntry)
```csharp
// Check sequential order
bosSignal.OrderPosition = 1, bar 100 ✅
fvgSignal.OrderPosition = 2, bar 105 ✅
bosSignal.SignalBar < fvgSignal.SignalBar ✅ (chronological)

// Check different types
uniqueTypes = ["BOS", "FVG"] → 2 types ✅

→ VALID COMBINED ENTRY!
```

### Execution (ExecuteEntry)
```csharp
// Get combined stop loss (most recent with R:R)
stopLoss = fvgSignal.StopLoss + (fvgSignal.StopLossPlusTicks * TickSize)
         = 4470 + 2 = 4472

// Trade 1 with 1:2 R:R
entryPrice = 4500
risk = |4500 - 4472| = 28 ticks
target = 4500 + (28 * 2) = 4556

SetStopLoss("CombinedEntry_1", Price, 4472)
SetProfitTarget("CombinedEntry_1", Price, 4556)
EnterLong(1, "CombinedEntry_1")
```

---

## SUMMARY

The AllinOneStrategy uses a **modular signal-based architecture** where:

1. Each entry type detects patterns and creates `SignalInfo` with pre-calculated stop loss **PRICE**
2. Signals are stored in `activeSignals` list with combined/standalone flags
3. `ValidateCombinedEntry()` checks if multiple signals align (chronologically + different types)
4. `ExecuteEntry()` creates 1-4 trades, each with custom stops or R:R-calculated targets
5. R:R calculation uses `Risk = |Entry - Stop|` then `Target = Entry ± (Risk × Ratio)`

**To add HeikenAshi improvements**: Follow the template in Section 8, ensuring you calculate a proper `StopLoss` PRICE (not ticks) in your detection method.

All entry types flow through the same execution pipeline, making the system highly extensible!
