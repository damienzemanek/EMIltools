### EMILtools: Signals & Timers System

A high-performance, type-safe architecture for Unity, designed to handle global timing and dynamic stat modification with zero-allocation math and fluent API design.

### Installation

Copy the `EMILtools-Private` folder into your Unity project's `Assets` directory.

### Signals & Modifiers System

A framework for modifying entity stats (Health, Speed, etc.).
- Uses reflection discovery and caching only in Awake() when an IStatUser is initialized.
- struct based Modifiers to be memory-efficient that use the Decorator pattern to create special Modifiers like Timed Modifiers.

#### Key Features
- **Type-Safe Routing**: "Tags" (empty structs like `Speed` or `Health`) are used to identify stats. This means `typeof(TTag)` is your unique key—no more magic strings or typo-related bugs.
- **SoC of Math and Tags**: Tags are completely seperate from modifiers, meaning you can tag your stats, once and don't have to deal with moving a container around to reference your stat.
- **Zero-Boxing Heterogeneous Storage of Modifiers**: Using a JIT "Double Elision" resolve method, you can have a list of different modifier types (Adders, Multipliers, etc.) without ever hitting the heap. It’s pure value-type performance.
- **Decorator Support**: You can wrap any modifier in timers, loggers, or custom logic seamlessly without touching the core math.

#### Usage Example

```csharp
public class Enemy : MonoBehaviour, IStatUser 
{
    // The only variable that is implemented via IStatUser
    public Dictionary<Type, IStat> Stats { get; set; }

    // Create your own custom stats using your own Stat Identifier Tags. For instace: Speed
    public Stat<float, Speed> speed = new(10f);


    // Cache your stats in awake so you don't have any manual work, Its just plug and play to modify them
    void Awake() => this.CacheStats();


    public void GetFrozen() 
    {
        // Initialize any modifiers
        var halfspeed = new Mathmod(x => x * 0.5f); //[ Multiply speed by 0.5 for 3 seconds ]

        // FluentAPI: Easily call decorators to add additional functionality to Modifiers
        this.Modify<Speed>(halfspeed).WithTimer(3f);
    }
}
```

### Timers System

A centralized ticking engine designed to handle thousands of concurrent timers with minimal GC pressure. It’s the backbone for anything that needs to happen over time.

#### Key Features
- **Global Ticker**: A persistent, hidden `MonoBehaviour` that handles all `Update` and `FixedUpdate` cycles in one place.
- **Leak-Safe**: Uses `ConditionalWeakTable` to prevent memory leaks. If your object gets destroyed, the timer system won't keep it alive.
- **Fast Removal**: Using a $O(1)$ removal logic (swap-and-pop) so cleaning up expired timers is practically free.

#### Usage Example

```csharp
public class Player : MonoBehaviour, ITimerUser 
{
    // Declare any timers
    private CountdownTimer sprintTimer = new(5f);

    void Awake() 
    {
        // Register with the global ticker (Update loop)
        this.InitializeTimers((sprintTimer, isFixed: false));
        
        // Easy event chaining
        this.Sub(sprintTimer.OnTimerStop, () => Debug.Log("Sprint Over!"));
    }

    // Customize your own callbacks
    void OnEnable() => sprintTimer.Start();

    // Easily unsubscribe everything at once with ShutdownTimers
    void OnDestroy() => this.ShutdownTimers();
}
```

