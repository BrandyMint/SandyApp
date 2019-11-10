using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Utilities {
    public static class UnityHelper {
        public static T[] GetComponentsOnlyInChildren<T>(this Component obj) where T : Component {
            var objList = new List<T>();
            for (int i = 0; i < obj.transform.childCount; ++i) {
                var child = obj.transform.GetChild(i).GetComponent<T>();
                if (child != null)
                    objList.Add(child);
            }
            return objList.ToArray();
        }
        
        public static bool RandomBool() {
            return UnityEngine.Random.value > 0.5f;
        }
    
        public static T Random<T>(this IEnumerable<T> enumerable) {
            if (!TryRandom(enumerable, out var ret))
                throw new ArgumentOutOfRangeException();
            return ret;
        }

        public static bool TryRandom<T>(this IEnumerable<T> enumerable, out T ret) {
            var array = PrepareToMultipleEnumerate(enumerable);
            var count = array.Count();
            if (count == 0) {
                ret = default;
                return false;
            }
            var rand = UnityEngine.Random.Range(0, count);
            ret = array.ElementAt(rand);
            return true;
        }

        private static IEnumerable<T> PrepareToMultipleEnumerate<T>(IEnumerable<T> enumerable) {
            return enumerable is T[] || enumerable is List<T> 
                ? enumerable 
                : enumerable.ToArray();
        }

        public static Vector2 RandomPoint(this Rect rect) {
            var x = UnityEngine.Random.Range(rect.xMin, rect.xMax);
            var y = UnityEngine.Random.Range(rect.yMin, rect.yMax);
            return new Vector2(x, y);
        }

        public static void SetLocalPosition(this Transform tr, float? x = null, float? y = null, float? z = null) {
            var pos = tr.localPosition;
            if (x == null) x = pos.x;
            if (y == null) y = pos.y;
            if (z == null) z = pos.z;
            tr.localPosition = new Vector3(x.Value, y.Value, z.Value);
        }
    
        public static Transform FindChildRecursively(this Transform parent, string name) {
            foreach (Transform child in parent) {
                if (child.name == name) {
                    return child;
                }
                var res = child.FindChildRecursively(name);
                if (res) return res;
            }
            return null;
        }
        
        public static IEnumerable<Transform> FindChildsRecursively(this Transform parent, string name) {
            return parent.GetComponentsInChildren<Transform>(true)
                .Where(child => child != parent && child.name == name);
        }

        public static IEnumerable<T> GetComponentsRecursively<T>(this Transform parent) {
            foreach (var component in parent.GetComponents<T>()) {
                yield return component;
            }
            foreach (Transform child in parent) {
                foreach (var component in child.GetComponentsRecursively<T>()) {
                    yield return component;
                }
            }
        }

        public static T GetComponentRecursively<T>(this Transform parent) where T : Component {
            var res = parent.GetComponent<T>();
            if (res != null) return res;
            foreach (Transform child in parent) {
                res = child.GetComponentRecursively<T>();
                if (res != null) return res;
            }
            return null;
        }
        
        public static void SetPropsByGameObjects(object obj, Transform root) {
            foreach (var propInfo in obj.GetType().GetProperties()) {
                var row = root.FindChildRecursively(propInfo.Name);
                Assert.IsNotNull(row, $"Not found {propInfo.Name} in {root.name}");
                object prop;
                if (typeof(GameObject).IsAssignableFrom(propInfo.PropertyType)) {
                    prop = row.gameObject;
                } else if (typeof(Component).IsAssignableFrom(propInfo.PropertyType)) {
                    prop = row.GetComponent(propInfo.PropertyType) ?? row.GetComponentInChildren(propInfo.PropertyType);
                } else {
                    prop = Activator.CreateInstance(propInfo.PropertyType);
                    SetPropsByGameObjects(prop, row);
                }
                propInfo.SetValue(obj, prop, null);
            }
        }
        
        public static void AddEventTrigger(this Component obj, EventTriggerType eventType, UnityAction<BaseEventData> action) {
            var et = obj.GetComponent<EventTrigger>() 
                     ?? obj.gameObject.AddComponent<EventTrigger>();

            var trigger = et.triggers.FirstOrDefault(t => t.eventID == eventType);
            if (trigger == null) {
                trigger = new EventTrigger.Entry { eventID = eventType };
                et.triggers.Add(trigger);
            }
            trigger.callback.AddListener(action);
        }
    }
}