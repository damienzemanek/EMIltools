using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EMILtools;

namespace EMILtools.Extensions
{
    //Did you know that delegates are immutable? crazy, that means when you do
    // +=, its just doing a Combine(delgate, delegate)?
    // and that creates a whole ahh new list.
    public static class DelegateEX
    {
        
        public static bool Contains(this Delegate _delegate, Delegate _this)
        {
            if (_delegate == null) return false;

            foreach (Delegate existing in _delegate.GetInvocationList())
            {
                //.Method is the function being called, its a MethodInfo that identifies :
                // which method
                // on which class
                // the signature etc
                // .Target is the object instance the method belongs to
                // the target matters because the method runs on a specific instance
                if (existing.Method == _this.Method &&
                    existing.Target == _this.Target) return true;

            }

            return false;
        }
        
        public static bool Contains(this Delegate _delegate, Action _this)
        {
            if (_delegate == null) return false;

            foreach (Delegate existing in _delegate.GetInvocationList())
            {
                //.Method is the function being called, its a MethodInfo that identifies :
                // which method
                // on which class
                // the signature etc
                // .Target is the object instance the method belongs to
                // the target matters because the method runs on a specific instance
                if (existing.Method == _this.Method &&
                    existing.Target == _this.Target) return true;

            }

            return false;
        }
        
        
    }
}