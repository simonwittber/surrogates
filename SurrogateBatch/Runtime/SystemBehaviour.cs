using System.Collections.Generic;
using UnityEngine;

namespace Surrogates
{
    public class SystemBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static List<T> Instances { get; private set; }
        // public static Vector3[] arrayS = new Vector3[5];
        BatchSystem system;

        protected virtual void OnEnable()
        {
            if (Instances == null)
                Instances = new List<T>();
            system = BatchSystem.Get<T>();
            Instances.Add(this as T);
        }

        protected virtual void OnDisable()
        {
            var end = Instances.Count - 1;
            var index = Instances.IndexOf(this as T);
            Instances[index] = Instances[end];
            Instances.RemoveAt(end);
        }
    }
}