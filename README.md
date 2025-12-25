# EMILtools Architectural System
### Included is:
### EMILtimers
#### a lightweight timer utility designed to replace contant update calls on dozens to hundreds of Monobehaviours all calling on their own timers, with a centralized high-performance tick-based system. 
### Signals
#### a reactive EventBus with configuration for Reactive events, Modifiers, and Validation (called Intercepts)




## EMILtimers Key Features
- Cache-Efficient Ticking: All timers are flattened into contiguous memory buffers reducing CPU overhead on potentially thousands of seperate Update() and FixedUpdate() calls, all on a single pass
- Leak-Proof Registry: Using ConditionalWeakTable to manage timer lifecycles ensuring background timers are cleaned up properly using the GC.
- Fluent API Calls: Initialize and chain event subscriptions into a single line of readable code
- Physics Seperation: Native support for Update and FixeUpdate buffers, ensureing physics-dependant timers run without unwanted variability
- Fast Shutdown of Timers: Uses the Swap-back pattern for constant-time removal of timers from the buffers during object destruction

## Usage: Transform messy timer logic into a clean declarative and developer oriented chain:
### Example: Implementatition of a fireball ability

### BEFORE
```csharp
public class FireballAbilityUser : MonoBehaviour
{
    public float chargeDuration = 1.5f;
    public float cooldownDuration = 3.0f;

    private float chargeTimer;
    private float cooldownTimer;
    private bool isCharging;
    private bool isOnCooldown;

    // ❌ Single-Instance update calls
    //    (with many objects, you'll have more and more individual update calls)
    void Update() => HandleCooldown();
    void FixedUpdate() => HandleCharge();
    
    void HandleCooldown()
    {
        if (!isOnCooldown) return;

         // ❌ Manualy subtract for all timers you create
        cooldownTimer -= Time.deltaTime;

        // ❌ Manually set flags for all timers you create
        if (cooldownTimer <= 0) 
            isOnCooldown = false; 
    }

    void HandleCharge()
    {
        if (!isCharging)
        {
            chargeTimer = chargeDuration; // ❌ Manual Guarding
            return;
        }
        
        chargeTimer -= Time.fixedDeltaTime; // ❌ Manual subtraction

        if (chargeTimer > 0) return;
        
        isCharging = false;
        UseAbility();
        
        // ❌ Manually reset flag values
        isOnCooldown = true;
        cooldownTimer = cooldownDuration;
    }


    public void UseAbility() 
    {
        if (isOnCooldown || isCharging) return;
        isCharging = true;
        chargeTimer = chargeDuration;
        
        /* ...Ability logic here...*/
    }
}
```

### AFTER
```csharp
public class FireballAbility : MonoBehaviour, TimerUtility.ITimerUser 
{
    [SerializeField] Reference<float> chargeDuration = new(1.5f);
    [SerializeField] Reference<float> cooldownDuration = new(3.0f);

    CountdownTimer chargeTimer;
    CountdownTimer cooldownTimer;
    
    void Awake() 
    {
        chargeTimer = new CountdownTimer(chargeDuration);
        cooldownTimer = new CountdownTimer(cooldownDuration);

        // 1. In awake, Register all your timers ONCE and subscribe initial callbacks
        this.InitializeTimers(
                (chargeTimer, true),
                (cooldownTimer, false))
            .Sub(chargeTimer.OnTimerStop, UseAbility);
    }

    // 2. Flag values are managed by the timers themselves, are automatically reset
    public void UseAbility()
    {
          /* ...Ability logic here...*/
          
          //Easily chain calls on other timers
          cooldownTimer.Start();
    }

    void OnDestroy() => this.ShutdownTimers();
}
```

# How?
Here are the steps to using this tool:

### 1. Utilize the interface
```csharp
public class PlayerController : MonoBehaviour, ITimerUser
```
### 2. Declare timers
```csharp
CountdownTimer timer_jumpCooldown;
CountdownTimer timer_levelTimeLimit;
```
### 3. Initialize and Subscribe
```csharp
void Awake() {
        this.InitializeTimers(
                (timer_jumpInput, true), 
                (timer_levelTimeLimit, false))
            .Sub(timer_jumpCooldown.OnTimerStop, CanNowJump)
            .Sub(timer_timeLimit.OnTimerStop, Lose));
}
```
### 4. Add your own custom functionality
```csharp
void TimeLimitUI {
  //Use the Progress variable to set any UI (Its already clamped 0 to 1)
  slider.value = timer_timeLimit.Progress; 
}
```
### 5. ShutdownTimers when the object is destroyed
```csharp
// This will automatically unsubscribe all events made in InitializeTimers()!
void OnDestroy() => this.ShutdownTimers();
```
  
