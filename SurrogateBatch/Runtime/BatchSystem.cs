using System;
using System.Collections.Generic;
using System.Linq;
using Surrogates;
using UnityEngine;

namespace Surrogates
{
    public class BatchSystem : MonoBehaviour
    {
        static GameObject container;
        static BatchSystem system;
        static bool isDirty;

        public string[] systems;
        HashSet<Type> types = new HashSet<Type>();
        List<ISurrogateAction> methods = new List<ISurrogateAction>();

        public static BatchSystem Get<T>()
        {
            if (container == null)
                container = GameObject.Find("SystemContainer");
            if (container == null) container = new GameObject("SystemContainer");

            if (system == null)
                system = container.GetComponent<BatchSystem>();
            if (system == null)
                system = container.AddComponent<BatchSystem>();
            if (!system.types.Contains(typeof(T)))
            {
                var action = SurrogateRegister.GetSurrogateAction(typeof(T), "UpdateBatch");
                system.types.Add(typeof(T));
                system.methods.Add(action);
                isDirty = true;
            }
            return system;
        }

        void Update()
        {
            if (Application.isEditor && isDirty)
            {
                systems = (from i in methods select i.GetType().Name).ToArray();
                isDirty = false;
            }
            foreach (var i in methods) i.Invoke();
        }
    }
}