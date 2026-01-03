using System;
using System.Collections;
using System.Collections.Generic;
using EMILtools.Signals;
using EMILtools.Timers;
using NUnit.Framework;
using static EMILtools.Signals.ModifierStrategies;
using UnityEngine;
using UnityEngine.TestTools;
using static EMILtools.Signals.ModiferRouting;
using static EMILtools.Signals.ModifierExtensions;

public class StatsSystemTests : MonoBehaviour
{
    
    private class TestStatUser : IStatUser
    {
        public Dictionary<Type, IStat> Stats { get; set; }
        public Stat<float, SpeedModifier> speed = new Stat<float, SpeedModifier>(10f);
    }

    [Test]
    public void Modify_Once()
    {
        var user = new TestStatUser();
        user.CacheStats();
        
        var speed = new SpeedModifier(x => x * 2);
        user.Modify(speed);
        Assert.AreEqual(20f, user.speed.Value, "The stat value should be doubled by the modifier");
    }
    
    
    [Test]
    public void Modify_Once_AndRemove_Once()
    {
        var user = new TestStatUser();
        user.CacheStats();
        
        var speed = new SpeedModifier(x => x * 2);
        user.Modify(speed);
        
        Assert.AreEqual(20f, user.speed.Value, "Initial modifier added, stat should be doubled");

        // Removing the same modifier by its hash should restore base value
        user.RemoveModifier(speed);
        
        Assert.AreEqual(10f, user.speed.Value, "After removing the modifier, stat should return to base value");
    }

    
    
    [Test]
    public void Modify_Twice_TheSameModifiers_DifferentFuncs_InSeries()
    {
        var user = new TestStatUser();
        user.CacheStats();
        
        var speed1 = new SpeedModifier(x => x + 5);
        var speed2 = new SpeedModifier(x => x * 2);
    
        user.Modify(speed1);
        user.Modify(speed2);
        
        // (10 + 5) * 2 = 30
        Assert.AreEqual(30f, user.speed.Value, "Multiple modifiers should apply in the order they are added");
    }
    
    [Test]
    public void Modify_Twice_TheSameModifier_DifferentFuncs_InSeries_RemoveOneModifier()
    {
        var user = new TestStatUser();
        user.CacheStats();
    
        var doubleMod = new SpeedModifier(x => x * 2);
        var addMod = new SpeedModifier(x => x + 100);
    
        user.Modify(doubleMod);
        user.Modify(addMod);
    
        // (10 * 2) + 100 = 120
        Assert.AreEqual(120f, user.speed.Value, "(10 * 2) + 100 = 120");
    
        // Remove only the 'double' modifier by its hash
        user.RemoveModifier(doubleMod);
    
        // 10 + 100 = 110
        Assert.AreEqual(110f, user.speed.Value, "After removing the doubler, only the +100 modifier should remain");
    }
    
    
    [Test]
    public void Modify_Twice_TheSame_InSeries()
    {
        var user = new TestStatUser();
        user.CacheStats();
        var speed = new SpeedModifier(x => x + 5);
    
        user.Modify(speed);
        user.Modify(speed);
        
        // (10 + 5) + 5 = 20
        Assert.AreEqual(20f, user.speed.Value, "Adding the same +5 modifier twice should increase the stat by 10 total");
    }
    
    
    [Test]
    public void ApplyModifier_Timed()
    {
        var user = new TestStatUser();
        user.CacheStats();
    
        var speed = new SpeedModifier(x => x * 2);
        user.Modify(speed).WithTimer(5, out _, out StatModDecTimed<float, SpeedModifier> dec);
        
        // While the timed decorator is active, the underlying modifier is applied
        Assert.AreEqual(20f, user.speed.Value, "The stat value should be doubled while the timed modifier is active");
        Assert.AreEqual(true, dec.timer.isRunning, "The timer associated with the timed modifier should be running");
    }
    
    
    
    [Test]
    public void Modify_Once_WithTwoDecors_IsRunning()
    {
        var user = new TestStatUser();
        user.CacheStats();
    
        var speed = new SpeedModifier(x => x + 10);
        // Two separate timed decorators controlling the same underlying modifier's lifecycle
        user.Modify(speed)
            .WithTimer(5, out _, out StatModDecTimed<float, SpeedModifier> dec1)
            .WithTimer(5, out _, out StatModDecTimed<float, SpeedModifier> dec2);
        
        // Only the SpeedModifier (+10) changes the math; decorators manage timing/callbacks only
        // 10 (base) + 10 (modifier) = 20
        Assert.AreEqual(20f, user.speed.Value, "The underlying modifier applies its +10 once; decorators do not change the math");
        
        Assert.IsTrue(dec1.timer.isRunning && dec2.timer.isRunning, "Both timers for both decorators should be running");
    }
    
    [UnityTest]
    public IEnumerator Modify_Once_WithTwoDecors_Concurrently_StopTimerImmedietely()
    {
        yield return null;
        print("[TEST] Starting test");
        var user = new TestStatUser();
        user.CacheStats();
    
        var speed = new SpeedModifier(x => x * 2);
        user.Modify(speed)
            .WithTimer(5, out _ ,out StatModDecTimed<float, SpeedModifier> dec1)
            .WithTimer(5, out _, out StatModDecTimed<float, SpeedModifier> dec2);
        
        print($"(Speed {user.speed.Value})," +
              $" (timer isRunning {dec1.timer.isRunning})" + 
              $" (Duration: {dec1.timer.Duration})");
    
        // While any timed decorator keeps the modifier alive, stat is doubled once: 10 * 2 = 20
        Assert.AreEqual(20f, user.speed.Value, "Initially, the stat is doubled once by the modifier");
    
        // Force-stopping the short decorator triggers removal of the entire modifier slot (modifier + all decorators)
        dec1.ForceStop(user.speed);
         
        print($"(Speed {user.speed.Value})," +
              $" (timer isRunning {dec1.timer.isRunning})" + 
              $" (Duration: {dec1.timer.Duration})");
    
        // After the forced stop, the whole modifier is gone and we revert to base value
        Assert.AreEqual(10f, user.speed.Value, "Force-stopping (1) timed decorator should remove the entire modifier and revert to base value");
    }
    
    
    [Test]
    public void Modify_Once_WithOneDecorTimer_OnAddCalled()
    {
        var user = new TestStatUser();
        user.CacheStats();
    
        bool onAddCalled = false;
        var speed = new SpeedModifier(x => x + 5);
    
        user.Modify(speed).WithTimer(5, out _, out _, new Action[1] { () => onAddCalled = true });
    
        Assert.IsTrue(onAddCalled, "The OnAdd callback on the decorator should be triggered when it is attached");
        Assert.AreEqual(15f, user.speed.Value, "Immediately after applying the +5 modifier, the stat should be 15");
    }
    
        
    [UnityTest]
    public IEnumerator Modify_Once_WithOneDecorTimer_Timer_OnStartCalled()
    {
        var user = new TestStatUser();
        user.CacheStats();
    
        bool onStartCalled = false;
        var speed = new SpeedModifier(x => x + 5);
        
        CountdownTimer customTimer = new CountdownTimer(5, 
            OnTimerStartCbs: new Action[1] { () => onStartCalled = true });
    
        user.Modify(speed).WithTimer(customTimer);

        yield return new WaitForSeconds(1f);
        print("IsRunning?: " + customTimer.isRunning);
        Assert.IsTrue(customTimer.isRunning, "Custom Timer is running");
        Assert.IsTrue(onStartCalled, "The OnAdd callback on the decorator should be triggered when it is attached");
        Assert.AreEqual(15f, user.speed.Value, "Immediately after applying the +5 modifier, the stat should be 15");
    }

    
    
    // Other tests
    
    [Test]
    public void RemoveDecorator_ByHash_RemovesOnlyThatDecorator()
    {
        var user = new TestStatUser();
        user.CacheStats();
    
        var speed = new SpeedModifier(x => x + 10);
    
        user.Modify(speed)
            .WithTimer(5, out _, out StatModDecTimed<float, SpeedModifier> dec1)
            .WithTimer(5, out _, out StatModDecTimed<float, SpeedModifier> dec2);
    
        // Underlying modifier applies once: 10 + 10 = 20
        Assert.AreEqual(20f, user.speed.Value, "Initial value should reflect a single +10 modifier with two attached decorators");
    
        // Explicit decorator removal by hash only runs its OnRemove callbacks; 
        // it does NOT drive timer expiry or remove the underlying modifier.

        user.RemoveDecorator(speed, dec1);
        //user.speed.RemoveDecorator(strat.hash, timedShort);
        
        // The math is still unchanged: modifier is still present, so value stays 20
        Assert.AreEqual(20f, user.speed.Value, 
            "Removing a decorator directly only triggers its callbacks; it does not change the underlying modifier's effect");
    
        // Slot still exists and still has one decorator attached
        Assert.AreEqual(1, user.speed.Modifiers.Count, "The modifier slot should still exist");
        Assert.AreEqual(1, user.speed.Modifiers[0].decorators.Count, "Exactly one decorator should remain on the slot");
    }
    
    [Test]
    public void RemoveModifier_RemovesEntireSlot_EvenWithDecorators()
    {
        var user = new TestStatUser();
        user.CacheStats();
    
        var speed = new SpeedModifier(x => x * 2);

        user.Modify(speed).WithTimer(100).WithTimer(100);
    
        // At this point the modifier is active and has decorators; stat must differ from base
        Assert.AreNotEqual(10f, user.speed.Value, "Modifiers should have changed the stat value from its base");
    
        // Removing the base modifier by hash clears the entire slot (modifier + decorators)
        user.RemoveModifier(speed);
    
        Assert.AreEqual(10f, user.speed.Value, "Removing the base modifier should revert the stat to its base value");
        Assert.IsTrue(user.speed.Modifiers == null || user.speed.Modifiers.Count == 0,
            "All modifier slots should be removed when the base modifier is removed");
    }
    
    [Test]
    public void RemoveModifier_NonExistingHash_NoChange()
    {
        var user = new TestStatUser();
        user.CacheStats();
    
        var speed = new SpeedModifier(x => x * 2);
        user.Modify(speed);
    
        Assert.AreEqual(20f, user.speed.Value, "Base modifier applied should double the stat");
    
        // Different func → different hash. Attempting to remove using this one should do nothing.
        var fake = new SpeedModifier(x => x + 999);
    
        user.RemoveModifier(fake);
    
        // Stat remains unchanged
        Assert.AreEqual(20f, user.speed.Value,
            "Removing a modifier using a non-matching hash should not change the stat");
    }
    
    
    [Test]
    public void TimedDecorators_SameHash_ForceStop_RemovesEntireModifier()
    {
        var user = new TestStatUser();
        user.CacheStats();
    
        var speed = new SpeedModifier(x => x + 5);
        user.Modify(speed)
            .WithTimer(5, out _, out StatModDecTimed<float, SpeedModifier> dec1);
    
        Assert.AreEqual(15f, user.speed.Value, "Initial value should be base +5 from the modifier");
    
        // Force-stopping any one of the timed decorators should cause the entire modifier slot to be removed
        dec1.ForceStop(user.speed);
    
        // With the entire modifier slot removed, stat returns to base value
        Assert.AreEqual(10f, user.speed.Value, "Force-stopping a timed decorator with this hash removes the whole modifier");
    
        Assert.AreEqual(0, user.speed.Modifiers.Count, "The modifier slot should be completely removed");
    }
    
    [UnityTest]
    public IEnumerator TimedDecorator_AutoExpire_RemovesEntireModifier()
    {
        var user = new TestStatUser();
        user.CacheStats();
    
        var speed = new SpeedModifier(x => x * 2);
        user.Modify(speed).WithTimer(0.5f, out CountdownTimer timer, out _);
    
        Assert.AreEqual(20f, user.speed.Value, "Base + timed modifier applied initially should double the stat");
    
        // Manually drive the timer system instead of relying on a scene-driven GlobalTicker
        float simulated = 0f;
        while (timer.isRunning && simulated < 1f)
        {
            TimerUtility.TickAllUpdates(0.02f); // advance all update timers by 0.02s
            simulated += 0.02f;
            yield return null;                   // yield to the test runner for a frame
        }
    
        // After the timer expires, the TimedModifier's stop callback should have removed the entire modifier
        Assert.AreEqual(10f, user.speed.Value,
            "Timed expiry should remove the entire modifier slot, returning the stat to its base value");
    }
    
    [UnityTest]
    public IEnumerator TimedModifier_Expires_DoesNotRemoveOtherModifiers()
    {
        var user = new TestStatUser();
        user.CacheStats();
    
        var speedTemp  = new SpeedModifier(x => x * 2);     // temporary buff
        var speedPerm = new SpeedModifier(x => x + 5);     // permanent buff

        user.Modify(speedTemp).WithTimer(0.5f, out CountdownTimer timer, out _);
        user.Modify(speedPerm);
    
        // While the timed modifier is active: (10 * 2) + 5 = 25
        Assert.AreEqual(25f, user.speed.Value, "Timed modifier and permanent modifier should both be active initially");
        
        // Manually tick the global timer system until this specific timer expires
        float simulated = 0f;
        while (timer.isRunning && simulated < 1f)
        {
            TimerUtility.TickAllUpdates(0.02f); // advances all update timers
            simulated += 0.02f;
            yield return null;                  // let the test runner advance a frame
        }
    
        // Once the timed modifier expires, only the permanent +5 modifier should remain: 10 + 5 = 15
        Assert.AreEqual(15f, user.speed.Value, "Expiring the timed modifier should leave other modifiers intact");
    }

    
}
