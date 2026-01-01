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
    public void ModifyStatUser_ApplyModifier()
    {
        var user = new TestStatUser();
        user.CacheStatFields();
        
        
        var strat = new SpeedModifier(x => x * 2);

        // trailing comma here works because generic constraint is inferred from param insert,
        // although T is never inferr-able. so it mest be set explicitly
        user.ModifyStatUser<float, SpeedModifier>(ref strat);
        Assert.AreEqual(20f, user.speed.Value, "The state value should be doubled by the modifier");
    }
    
    [Test]
    public void ModifyStatUser_ApplyAndRemoveModifier()
    {
        var user = new TestStatUser();
        user.CacheStatFields();
        var strat1 = new SpeedModifier(x => x * 2);

        user.ModifyStatUser(ref strat1);
        
        Assert.AreEqual(20f, user.speed.Value, "Initial Modifier Added, should be double");

        
        user.RemoveModifier(ref strat1);
        
        Assert.AreEqual(10f, user.speed.Value, "Initial Modifier Removed, should be back to initial value");
    }

    [Test]
    public void ModifyStatUser_ApplyMultipleModifiers()
    {
        var user = new TestStatUser();
        user.CacheStatFields();
        var strat1 = new SpeedModifier(x => x + 5);
        var strat2 = new SpeedModifier(x => x * 2);

        user.ModifyStatUser(ref strat1);
        user.ModifyStatUser(ref strat2);
        
        Assert.AreEqual(30f, user.speed.Value, "Multiple modifiers should stack in order");
    }
    
    
    
    
    [Test]
    public void ModifyStatUser_ApplyModifier_Timed()
    {
        var user = new TestStatUser();
        user.CacheStatFields();

        var strat = new SpeedModifier(x => x * 2);
        var timed = strat.WithTimed(5);
        user.ModifyStatUser(ref strat, timed);

        var tm = (TimedModifier<float, SpeedModifier>)timed;
        CountdownTimer timer = tm.timer;
        
        
        Assert.AreEqual(20f, user.speed.Value, "The state value should be doubled by the IStatModStrategy interface modifier");
        Assert.AreEqual(true, timer.isRunning, "Timer on stat should be running");
    }
    
    [Test]
    public void ModifyStatUser_StackingMultipleDecorators()
    {
        var user = new TestStatUser();
        user.CacheStatFields();

        var strat = new SpeedModifier(x => x + 10);
        // Stacking two different decorators on the same modifier
        var timed1 = strat.WithTimed(5);
        var timed2 = strat.WithTimed(10);
        
        // Applying both decorators to the same modifier instance
        user.ModifyStatUser(ref strat, timed1, timed2);

        // Since it's x + 10, if both apply, it should be 10 + 10 + 10 = 30
        // (Wait, ApplyDecorators chain: val = dec1.Apply(val) -> dec2.Apply(val))
        // dec1.Apply calls strat.func(val) -> 10 + 10 = 20
        // dec2.Apply calls strat.func(20) -> 20 + 10 = 30
        Assert.AreEqual(30f, user.speed.Value, "Both decorators should apply their logic in sequence");
        
        var tm1 = (TimedModifier<float, SpeedModifier>)timed1;
        var tm2 = (TimedModifier<float, SpeedModifier>)timed2;
        Assert.IsTrue(tm1.timer.isRunning && tm2.timer.isRunning, "Both timers should be running");
    }

    [UnityTest]
    public IEnumerator ModifyStatUser_TimedRemoval_TargetedInstance()
    {
        yield return null;
        print("[TEST] Starting test");
        var user = new TestStatUser();
        user.CacheStatFields();

        var strat = new SpeedModifier(x => x * 2);
        var timedShort = strat.WithTimed(0.1f); // Expires quickly
        var timedLong = strat.WithTimed(100f);  // Lasts long
        user.ModifyStatUser(ref strat, timedShort, timedLong);
        
        print($"Speed {user.speed.Value}," +
              $" timer is running {(timedShort as TimedModifier<float, SpeedModifier>).timer.isRunning}" + $" timer length: {(timedShort as TimedModifier<float, SpeedModifier>).timer.Duration}");
        Assert.AreEqual(40f, user.speed.Value, "Doubled x2, val is 40");

         // // Manually trigger the short timer stop (simulating expiration)
         (timedShort as TimedModifier<float, SpeedModifier>).removable = true;
         (timedShort as TimedModifier<float, SpeedModifier>).stat = user.speed;
         (timedShort as TimedModifier<float, SpeedModifier>).timer.OnTimerStop?.Invoke();
         print($"Speed {user.speed.Value}," +
               $" timer is running {(timedShort as TimedModifier<float, SpeedModifier>).timer.isRunning}" + $" timer length: {(timedShort as TimedModifier<float, SpeedModifier>).timer.Duration}");

         print($"Speed {user.speed.Value}," +
               $" timer is running {(timedShort as TimedModifier<float, SpeedModifier>).timer.isRunning}" + $" timer length: {(timedShort as TimedModifier<float, SpeedModifier>).timer.Duration}");

        // After tmShort is removed, only tmLong remains
        // Base 10 * 2 = 20
        Assert.AreEqual(20f, user.speed.Value, "Only the expired modifier should be removed; the other should persist");
    }

    [Test]
    public void ModifyStatUser_NakedModifierTargeting()
    {
        var user = new TestStatUser();
        user.CacheStatFields();

        var doubleMod = new SpeedModifier(x => x * 2);
        var addMod = new SpeedModifier(x => x + 100);

        user.ModifyStatUser(ref doubleMod);
        user.ModifyStatUser(ref addMod);

        Assert.AreEqual(120f, user.speed.Value, "(10 * 2) + 100 = 120");

        // Remove the 'double' modifier specifically by its func
        user.RemoveModifier(ref doubleMod);

        Assert.AreEqual(110f, user.speed.Value, "Should be 10 + 100 = 110 after removing the doubler");
    }

    [Test]
    public void ModifyStatUser_DecoratorOnAddOrder()
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

        Assert.IsTrue(onAddCalled, "The OnAdd callback should be triggered when the decorator is linked");
        Assert.AreEqual(15f, user.speed.Value, "Stat should be updated immediately upon adding decorator");
    }
    
    
}
