### EMILtools: Timers & Signals System

A high-performance, type-safe architecture for Unity, designed to handle global timing and dynamic stat modification with zero-allocation math and fluent API design.

### Installation

Copy the `EMILtools-Private` folder into your Unity project's `Assets` directory.

### Timers System

A centralized ticking engine designed to handle thousands of concurrent timers with minimal GC pressure.

#### Key Features
- **Global Ticker**: A persistent, hidden `MonoBehaviour` that centralizes all `Update` and `FixedUpdate` cycles.
- **Leak-Safe**: Uses `ConditionalWeakTable` to prevent memory leaks if objects are destroyed without manual cleanup.
- **Fast Removal**: Custom $O(1)$ removal logic for global buffers.

#### Usage Example

```csharp
public class Player : MonoBehaviour, ITimerUser 
{
    private CountdownTimer sprintTimer = new(5f);

    void Awake() 
    {
        // Bind to global ticker
        this.InitializeTimers((sprintTimer, isFixed: false));
        
        // Subscription chain
        this.Sub(sprintTimer.OnTimerStop, () => Debug.Log("Sprint Over!"));
    }

    void OnEnable() => sprintTimer.Start();
    
    void OnDestroy() => this.ShutdownTimers();
}
```

### Signals & Modifiers System

An elite framework for modifying entity stats (Health, Speed, etc.) using reflection-backed discovery and the decorator pattern.

#### Key Features
- **Type-Safe Routing**: Uses `typeof(TMod)` as a unique key for $O(1)$ lookups—no magic strings.
- **Zero-Boxing**: Leverages generic struct constraints to perform calculations without heap allocations.
- **Decorator Support**: Wrap modifiers in timers, loggers, or custom logic seamlessly.

#### Usage Example

```csharp
public class Enemy : MonoBehaviour, IStatUser 
{
    // Auto-cached by the system scan
    public Stat<float, SpeedModifier> speed = new(10f);
    public Dictionary<Type, IStat> Stats { get; set; }

    void Awake() => this.CacheStats();

    public void ApplyFreeze() 
    {
        // Multiply speed by 0.5 for 3 seconds
        this.Modify(new SpeedModifier(x => x * 0.5f)).WithTimer(3f);
    }
}
```

### The "Bridge" Integration

The systems are designed to interact fluently. Calling `.WithTimer(duration)` on a modification automatically handles the subscription and cleanup:

1.  **Injects** a `CountdownTimer` into the modifier slot.
2.  **Subscribes** to the timer's expiration event.
3.  **Removes** the modifier and restores the base stat value automatically when the timer finishes.

### Advanced Patterns Included

- **Delegate.CreateDelegate**: High-speed reflection by caching method pointers.
- **EqualityComparer<T>.Default**: Boxing-free generic comparisons.
- **Expression Tree Hashing**: Identifies anonymous logic (lambdas) using FNV-1a hashing for precise removal.
- **Lazy Initialization**: Delays list creation to save memory on inactive entities.
