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

public class StatsSystemTests : MonoBehaviour
{
    
    private class TestStatUser : IStatUser
    {
        public ModifierRouter router { get; set; } = new();
        public Stat<float, SpeedModifier> speed = new Stat<float, SpeedModifier>(10f);
    }

    [Test]
    public void Modify_Once()
    {
        var user = new TestStatUser();
        user.CacheStatFields();
        
        var strat = new SpeedModifier(x => x * 2);

        // Explicitly specifying T is required so the router can bind the correct stat
        user.ModifyStatUser<float, SpeedModifier>(ref strat);
        Assert.AreEqual(20f, user.speed.Value, "The stat value should be doubled by the modifier");
    }
    
    [Test]
    public void Modify_Once_AndRemove_Once()
    {
        var user = new TestStatUser();
        user.CacheStatFields();
        var strat1 = new SpeedModifier(x => x * 2);

        user.ModifyStatUser(ref strat1);
        
        Assert.AreEqual(20f, user.speed.Value, "Initial modifier added, stat should be doubled");

        // Removing the same modifier by its hash should restore base value
        user.RemoveModifier(ref strat1);
        
        Assert.AreEqual(10f, user.speed.Value, "After removing the modifier, stat should return to base value");
    }

    [Test]
    public void Modify_Twice_TheSameModifiers_DifferentFuncs_InSeries()
    {
        var user = new TestStatUser();
        user.CacheStatFields();
        var strat1 = new SpeedModifier(x => x + 5);
        var strat2 = new SpeedModifier(x => x * 2);

        user.ModifyStatUser(ref strat1);
        user.ModifyStatUser(ref strat2);
        
        // (10 + 5) * 2 = 30
        Assert.AreEqual(30f, user.speed.Value, "Multiple modifiers should apply in the order they are added");
    }
    
    [Test]
    public void Modify_Twice_TheSameModifier_DifferentFuncs_InSeries_RemoveOneModifier()
    {
        var user = new TestStatUser();
        user.CacheStatFields();

        var doubleMod = new SpeedModifier(x => x * 2);
        var addMod = new SpeedModifier(x => x + 100);

        user.ModifyStatUser(ref doubleMod);
        user.ModifyStatUser(ref addMod);

        // (10 * 2) + 100 = 120
        Assert.AreEqual(120f, user.speed.Value, "(10 * 2) + 100 = 120");

        // Remove only the 'double' modifier by its hash
        user.RemoveModifier(ref doubleMod);

        // 10 + 100 = 110
        Assert.AreEqual(110f, user.speed.Value, "After removing the doubler, only the +100 modifier should remain");
    }

    
    [Test]
    public void Modify_Twice_TheSame_InSeries()
    {
        var user = new TestStatUser();
        user.CacheStatFields();
        var strat1 = new SpeedModifier(x => x + 5);

        user.ModifyStatUser(ref strat1);
        user.ModifyStatUser(ref strat1);
        
        // (10 + 5) + 5 = 20
        Assert.AreEqual(20f, user.speed.Value, "Adding the same +5 modifier twice should increase the stat by 10 total");
    }
    
    
    [Test]
    public void ApplyModifier_Timed()
    {
        var user = new TestStatUser();
        user.CacheStatFields();

        var strat = new SpeedModifier(x => x * 2);
        var timed = strat.WithTimed(5);
        user.ModifyStatUser(ref strat, timed);

        var tm = (TimedModifier<float, SpeedModifier>)timed;
        CountdownTimer timer = tm.timer;
        
        // While the timed decorator is active, the underlying modifier is applied
        Assert.AreEqual(20f, user.speed.Value, "The stat value should be doubled while the timed modifier is active");
        Assert.AreEqual(true, timer.isRunning, "The timer associated with the timed modifier should be running");
    }
    
    [Test]
    public void Modify_Once_WithTwoDecors_IsRunning()
    {
        var user = new TestStatUser();
        user.CacheStatFields();

        var strat = new SpeedModifier(x => x + 10);
        // Two separate timed decorators controlling the same underlying modifier's lifecycle
        var timed1 = strat.WithTimed(5);
        var timed2 = strat.WithTimed(10);
        
        // Both decorators attach to the same modifier, but the modifier's math itself is applied once
        user.ModifyStatUser(ref strat, timed1, timed2);

        // Only the SpeedModifier (+10) changes the math; decorators manage timing/callbacks only
        // 10 (base) + 10 (modifier) = 20
        Assert.AreEqual(20f, user.speed.Value, "The underlying modifier applies its +10 once; decorators do not change the math");

        var tm1 = (TimedModifier<float, SpeedModifier>)timed1;
        var tm2 = (TimedModifier<float, SpeedModifier>)timed2;
        Assert.IsTrue(tm1.timer.isRunning && tm2.timer.isRunning, "Both timers for both decorators should be running");
    }

    [UnityTest]
    public IEnumerator Modify_Once_WithTwoDecors_Concurrently_StopTimerImmedietely()
    {
        yield return null;
        print("[TEST] Starting test");
        var user = new TestStatUser();
        user.CacheStatFields();

        var strat = new SpeedModifier(x => x * 2);
        var timedShort = strat.WithTimed(0.1f); // short-lived decorator
        var timedLong = strat.WithTimed(100f);  // long-lived decorator
        user.ModifyStatUser(ref strat, timedShort, timedLong);
        
        print($"(Speed {user.speed.Value})," +
              $" (timer isRunning {(timedShort as TimedModifier<float, SpeedModifier>).timer.isRunning})" + 
              $" (Duration: {(timedShort as TimedModifier<float, SpeedModifier>).timer.Duration})");

        // While any timed decorator keeps the modifier alive, stat is doubled once: 10 * 2 = 20
        Assert.AreEqual(20f, user.speed.Value, "Initially, the stat is doubled once by the modifier");

        // Force-stopping the short decorator triggers removal of the entire modifier slot (modifier + all decorators)
        (timedShort as TimedModifier<float, SpeedModifier>).ForceStop(user.speed);
         
        print($"(Speed {user.speed.Value})," +
              $" (timer isRunning {(timedShort as TimedModifier<float, SpeedModifier>).timer.isRunning})" + 
              $" (Duration: {(timedShort as TimedModifier<float, SpeedModifier>).timer.Duration})");

        // After the forced stop, the whole modifier is gone and we revert to base value
        Assert.AreEqual(10f, user.speed.Value, "Force-stopping the timed decorator should remove the entire modifier and revert to base value");
    }


    [Test]
    public void Modify_Once_WithOneDecorTimer_OnAddCalled()
    {
        var user = new TestStatUser();
        user.CacheStatFields();

        bool onAddCalled = false;
        var strat = new SpeedModifier(x => x + 5);
        var customDec = new TimedModifier<float, SpeedModifier>(
            strat.func, 
            strat.hash,
            new CountdownTimer(5), 
            add: () => onAddCalled = true);

        user.ModifyStatUser(ref strat, customDec);

        Assert.IsTrue(onAddCalled, "The OnAdd callback on the decorator should be triggered when it is attached");
        Assert.AreEqual(15f, user.speed.Value, "Immediately after applying the +5 modifier, the stat should be 15");
    }
    
    
    // Other tests
    
    [Test]
    public void RemoveDecorator_ByHash_RemovesOnlyThatDecorator()
    {
        var user = new TestStatUser();
        user.CacheStatFields();

        var strat = new SpeedModifier(x => x + 10);
        var timedShort = strat.WithTimed(5f);
        var timedLong  = strat.WithTimed(5f);

        user.ModifyStatUser(ref strat, timedShort, timedLong);

        // Underlying modifier applies once: 10 + 10 = 20
        Assert.AreEqual(20f, user.speed.Value, "Initial value should reflect a single +10 modifier with two attached decorators");

        var tmShort = (TimedModifier<float, SpeedModifier>)timedShort;

        // Explicit decorator removal by hash only runs its OnRemove callbacks; 
        // it does NOT drive timer expiry or remove the underlying modifier.
        user.speed.RemoveDecorator(strat.hash, timedShort);

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
        user.CacheStatFields();

        var strat = new SpeedModifier(x => x * 2);
        var timed1 = strat.WithTimed(100f);
        var timed2 = strat.WithTimed(100f);

        user.ModifyStatUser(ref strat, timed1, timed2);

        // At this point the modifier is active and has decorators; stat must differ from base
        Assert.AreNotEqual(10f, user.speed.Value, "Modifiers should have changed the stat value from its base");

        // Removing the base modifier by hash clears the entire slot (modifier + decorators)
        user.RemoveModifier(ref strat);

        Assert.AreEqual(10f, user.speed.Value, "Removing the base modifier should revert the stat to its base value");
        Assert.IsTrue(user.speed.Modifiers == null || user.speed.Modifiers.Count == 0,
            "All modifier slots should be removed when the base modifier is removed");
    }
    
    [Test]
    public void RemoveModifier_NonExistingHash_NoChange()
    {
        var user = new TestStatUser();
        user.CacheStatFields();

        var strat = new SpeedModifier(x => x * 2);
        user.ModifyStatUser(ref strat);

        Assert.AreEqual(20f, user.speed.Value, "Base modifier applied should double the stat");

        // Different func → different hash. Attempting to remove using this one should do nothing.
        var fake = new SpeedModifier(x => x + 999);

        user.RemoveModifier(ref fake);

        // Stat remains unchanged
        Assert.AreEqual(20f, user.speed.Value,
            "Removing a modifier using a non-matching hash should not change the stat");
    }

    
    [Test]
    public void TimedDecorators_SameHash_ForceStop_RemovesEntireModifier()
    {
        var user = new TestStatUser();
        user.CacheStatFields();

        var strat = new SpeedModifier(x => x + 5);
        var timed1 = strat.WithTimed(100f);
        var timed2 = strat.WithTimed(100f);

        user.ModifyStatUser(ref strat, timed1, timed2);

        // Underlying modifier applies once: 10 + 5 = 15
        Assert.AreEqual(15f, user.speed.Value, "Initial value should be base +5 from the modifier");

        var tm1 = (TimedModifier<float, SpeedModifier>)timed1;

        // Force-stopping any one of the timed decorators should cause the entire modifier slot to be removed
        tm1.ForceStop(user.speed);

        // With the entire modifier slot removed, stat returns to base value
        Assert.AreEqual(10f, user.speed.Value, "Force-stopping a timed decorator with this hash removes the whole modifier");

        Assert.AreEqual(0, user.speed.Modifiers.Count, "The modifier slot should be completely removed");
    }

    [UnityTest]
    public IEnumerator TimedDecorator_AutoExpire_RemovesEntireModifier()
    {
        var user = new TestStatUser();
        user.CacheStatFields();

        var strat = new SpeedModifier(x => x * 2);
        var timed = strat.WithTimed(0.05f);
        user.ModifyStatUser(ref strat, timed);

        Assert.AreEqual(20f, user.speed.Value, "Base + timed modifier applied initially should double the stat");

        var tm = (TimedModifier<float, SpeedModifier>)timed;
        var timer = tm.timer;

        float simulated = 0f;
        // Manually drive the timer system instead of relying on a scene-driven GlobalTicker
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
        user.CacheStatFields();

        var timedMod  = new SpeedModifier(x => x * 2);     // temporary buff
        var staticMod = new SpeedModifier(x => x + 5);     // permanent buff

        var timed = timedMod.WithTimed(0.05f);

        user.ModifyStatUser(ref timedMod, timed);
        user.ModifyStatUser(ref staticMod);

        // While the timed modifier is active: (10 * 2) + 5 = 25
        Assert.AreEqual(25f, user.speed.Value, "Timed modifier and permanent modifier should both be active initially");

        var tm    = (TimedModifier<float, SpeedModifier>)timed;
        var timer = tm.timer;

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
