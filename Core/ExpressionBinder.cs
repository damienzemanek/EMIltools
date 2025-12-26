using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace EMILtools.Core
{
    public static class ExpressionBinder
    {
        public static Action<TArg> BindAction<TArg>(object inputInstance, MethodInfo methodInfo)
        {
            if(inputInstance == null || methodInfo == null) return null;
            
            // Setup Instance and Arguements
            var instance = Expression.Constant(inputInstance);
            var arguement = Expression.Parameter(typeof(TArg), "arg");
            
            // This Action is used for methods expecting an interface parameter
            // Must convert the interface parameter to its concrete type using Convert()
            var castParamToConcreteInterface = Expression.Convert(arguement,
                methodInfo.GetParameters()[0].ParameterType);

            // Combines the instance and Converted parameter into 
            // instance.Method((IInterfaceConcrete)arguement)
            var call = Expression.Call(instance, methodInfo, castParamToConcreteInterface);
            
            // Turns the instructions into the function. with the call and the given arugement
            var lambda = Expression.Lambda<Action<TArg>>(call, arguement);
            
            var compiled = lambda.Compile();
            
            return arg =>
            {
                Debug.Log($"[BindAction] Invoking {methodInfo.Name} on {inputInstance} with arg={arg}");
                compiled(arg);
            };
        }
        
    }

}
