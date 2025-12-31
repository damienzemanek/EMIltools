using System;
using System.Collections.Generic;
using EMILtools.Signals;
using EMILtools.Timers;
using NUnit.Framework;
using static EMILtools.Signals.ModifierStrategies;
using UnityEngine;
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
        Assert.AreEqual(true, timer.isRunning);
    }
    
    
}
