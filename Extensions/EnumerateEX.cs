using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static EMILtools.Extensions.NumEX;

namespace EMILtools.Extensions
{
    public static class EnumerateEX
    {
        public static int LAST<T>(this List<T> list) => list.Count - 1;
        
        
        [Serializable]
        public struct PairStringObj
        {
            public string name;
            public GameObject obj;
        }

        public static GameObject SetActive(this PairStringObj[] vals, string _name, bool _active)
        {
            for(int i = 0; i < vals.Length; i++)
                if (vals[i].name == _name) return vals[i].obj.SetActiveThen(_active);

            return null;
        }
        public static void SetAllActive(this PairStringObj[] vals, bool _active)
        {
            for (int i = 0; i < vals.Length; i++) vals[i].obj.SetActive(_active);
        }

        public static float Rand(this Vector2 v)
        {
            return UnityEngine.Random.Range(v.x, v.y);
        }


        #region Privates

        #endregion
        public static T Rand<T>(this T[] ts)
        {
            return ts[UnityEngine.Random.Range(0, ts.Length)];
        }
        public static T Rand<T>(this T[] ts, T[] exclude)
        {
            var include = ts.Where(t => !exclude.Contains(t)).ToArray();
            if (include.Length <= 0) return default;
            return Rand(include);
        }
        public static T Rand<T>(this T[] ts, int min, int max)
        {
            return ts[UnityEngine.Random.Range(min, max)];
        }


        public static T Rand<T>(this List<T> ts)
        {
            return ts[UnityEngine.Random.Range(0, ts.Count)];
        }
        
        public static T RandAndRemove<T>(this List<T> ts)
        {
            if (ts == null || ts.Count == 0) return default;

            int indx = UnityEngine.Random.Range(0, ts.Count);
            T rand = ts[indx];

            ts[indx] = ts[NumEX.LAST];
            ts.RemoveLast();
            return rand;
        }

        public static void RemoveLast<T>(this List<T> ts)
        {
            if (ts == null || ts.Count == 0) return;
            ts.RemoveAt(ts.Count -1);
        }
        


        public static T Rand<T>(this List<T> ts, List<T> exclude)
        {
            var include = ts.Where(t => !exclude.Contains(t)).ToList();
            if (include.Count <= 0) return default;
            return Rand(include);
        }

        public static T Rand<T>(this List<T> ts, int min, int max)
        {
            return ts[UnityEngine.Random.Range(min, max)];
        }

        public static List<T> Swap<T>(this List<T> list, int first, int second)
        {
            if (first == second) return list;
            if (first < 0 || second < 0 || first >= list.Count || second >= list.Count) throw new ArgumentOutOfRangeException();

            T buffer = list[first];
            list[first] = list[second];
            list[second] = buffer;

            return list;
        }

        public static List<GameObject> SetAllActive(this List<GameObject> list, bool val)
        {
            for(int i = 0; i < list.Count; i++) list[i].SetActive(val);
            return list;
        }

        public static GameObject[] SetAllActive(this GameObject[] list, bool val)
        {
            for (int i = 0; i < list.Length; i++) list[i].SetActive(val);
            return list;
        }

        public static List<GameObject> UnparentAll(this List<GameObject> list)
        {
            for (int i = 0; i < list.Count; i++) list[i].transform.parent = null;
            return list;
        }

        public static GameObject[] UnparentAll(this GameObject[] list)
        {
            for (int i = 0; i < list.Length; i++) list[i].transform.parent = null;
            return list;
        }


        public static List<T> AddOnce<T>(this List<T> list, T item)
        {
            if (item == null) throw new InvalidOperationException("Trying to add null item to list");
            if(list.Contains(item)) return list;
            else list.Add(item);
            return list;
        }
        
        public static T AddGet<T>(this List<T> list, T item)
        {
            if (item == null) throw new InvalidOperationException("Trying to add null item to list");
    
            if (list.Contains(item))  return list[list.IndexOf(item)];
    
            list.Add(item);
            return item;
        }
        
        public static T ReplaceItemAtItsSpotWith<T>(this List<T> list, T item, T replacement)
        {
            if (item == null) throw new InvalidOperationException("Trying to find a null item to replace");

            int index = list.IndexOf(item);
    
            if (index != -1)
            {
                list[index] = replacement;
                return replacement;
            }

            // If item not found, add replacement to the end of the list
            list.Add(replacement);
            return replacement;
        }

        public static bool IsEmpty<T>(this List<T> list) => list.Count == 0;

        // Swap back
        public static List<T> FastRemove<T>(this List<T> buffer, T t)
        {
            //O(n) search :(
            int index = buffer.IndexOf(t);
            if (index == -1) return buffer;
            
            buffer.Swap(index, buffer.LAST());
            buffer.RemoveLast();
            return buffer;
        }

        #region Methods

        #endregion

    }

}