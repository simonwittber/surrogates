
The Surrogates Project provides tools to eliminate the need for .Net reflection in your Unity code.


Usage: 

You can use the following code, wherever you would normally use reflection and PropertyInfo instances.

    ISurrogateProperty<float> floatProperty;
    floatProperty = SurrogateRegister.GetSurrogate<float>(target, propertyName);

The target parameter is some Unity Component, and name is the property name which will be wrapped. Then, it's as simple as:

    floatProperty.Set(someFloatValue);
    someFloatValue = floatProperty.Get();

You can also access Action methods:

    var myAction = SurrogateRegister.GetSurrogateAction(target, methodName);
    myAction.Invoke();

The first time this code runs, it will use reflection, but will register the property in the Surrogate Register. After you exit play mode, code will be generated in Xyzzy.Surrogates.dll which eliminates the reflection code, leaving you with an object that uses native code to access the property.

You can also use Surrogates to create batched Update methods, instead of per instance Update methods.

    public class AnUpdateBatchedComponent : SystemBehaviour<AnUpdateBatchedComponent>
    {
        //This gets called once per frame, regardless of number of components in the scene.
        public static void UpdateBatch()
        {
            foreach (var i in Instances)
            {
                //Do stuff.
            }
        }
    }

No more boilerplate required, it just works.

