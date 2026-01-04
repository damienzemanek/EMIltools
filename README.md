### EMILtools: Signals & Timers System

Here is the requested documentation file. It provides a human-readable overview of the high-performance **Signals & Modifiers** and **Timers** systems, including their seamless "Bridge" integration.

---

### EMILtools: Signals & Timers System

A high-performance, type-safe architecture for Unity, designed to handle global timing and dynamic stat modification with zero-allocation math and fluent API design.

### Installation

Copy the `EMILtools-Private` folder into your Unity project's `Assets` directory.

### Signals & Modifiers System

This is an elite framework for modifying entity stats (Health, Speed, etc.) using reflection-backed discovery and a powerful decorator pattern. It’s built to be as fast as possible while staying completely flexible.

#### Key Features
- **Phantom Tags (Type-Safe Routing)**: We use "Tags" (empty structs like `Speed` or `Health`) to identify stats. This means `typeof(TTag)` is your unique key—no more magic strings or typo-related bugs.
- **Zero-Boxing Heterogeneity**: Thanks to some advanced JIT "Double Elision" tricks, you can have a list of different modifier types (Adders, Multipliers, etc.) without ever hitting the heap. It’s pure value-type performance.
- **Decorator Support**: You can wrap any modifier in timers, loggers, or custom logic seamlessly without touching the core math.

#### Usage Example

```csharp
public class Enemy : MonoBehaviour, IStatUser 
{
    // The system automatically finds and caches these at startup
    public Stat<float, Speed> speed = new(10f);
    public Dictionary<Type, IStat> Stats { get; set; }

    void Awake() => this.CacheStats();

    public void ApplyFreeze() 
    {
        // One-liner: Multiply speed by 0.5 for 3 seconds
        // The compiler enforces that you only apply Speed mods to Speed stats
        this.Modify<Speed>(new MathMod(x => x * 0.5f)).WithTimer(3f);
    }
}
```

### Timers System

A centralized ticking engine designed to handle thousands of concurrent timers with minimal GC pressure. It’s the backbone for anything that needs to happen over time.

#### Key Features
- **Global Ticker**: A persistent, hidden `MonoBehaviour` that handles all `Update` and `FixedUpdate` cycles in one place.
- **Leak-Safe**: Uses `ConditionalWeakTable` to prevent memory leaks. If your object gets destroyed, the timer system won't keep it alive.
- **Fast Removal**: We use custom $O(1)$ removal logic (swap-and-pop) so cleaning up expired timers is practically free.

#### Usage Example

```csharp
public class Player : MonoBehaviour, ITimerUser 
{
    private CountdownTimer sprintTimer = new(5f);

    void Awake() 
    {
        // Register with the global ticker (Update loop)
        this.InitializeTimers((sprintTimer, isFixed: false));
        
        // Easy event chaining
        this.Sub(sprintTimer.OnTimerStop, () => Debug.Log("Sprint Over!"));
    }

    void OnEnable() => sprintTimer.Start();
    
    void OnDestroy() => this.ShutdownTimers();
}
```

### The "Bridge" Integration

The magic happens where these two systems meet. When you call `.WithTimer(duration)` on a stat modification, the system handles the heavy lifting for you:

1.  **Injects** a `CountdownTimer` directly into the modifier slot.
2.  **Subscribes** to the timer's expiration event automatically.
3.  **Cleans Up**: When the timer finishes, it removes the modifier and restores the base stat value. You don't have to write a single line of cleanup code.
