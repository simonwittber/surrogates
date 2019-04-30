
Surrogate provides tools to eliminate the need for .Net reflection in your Unity code.


Usage: 

First, Choose [Assets->Create->Surrogate Assembly]
This will create an Xyzzy.Surrogate.dll file in your Assets folder.

Then, you can use the following code, wherever you would normally use reflection and PropertyInfo instances.

    ISurrogateProperty<float> floatProperty;
    floatProperty = SurrogateRegister.GetSurrogate<float>(target, propertyName);

The target parameter is some Unity Component, and name is the property name which will be wrapped. Then, it's as simple as:

    floatProperty.Set(someFloatValue);
    someFloatValue = floatProperty.Get();

You can also access Action methods:
    var myAction = SurrogateRegister.GetSurrogateAction(target, methodName);
    myAction.Invoke();

You can also use Surrogates to magically create batched Update methods.

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

