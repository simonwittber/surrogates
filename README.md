
Usage: 

First, Choose [Assets->Create->Surrogate Assembly]
This will create an Xyzzy.Surrogate.dll file in your Assets folder.

Then, you can use the following code, wherever you would normally use reflection and PropertyInfo instances.

    ISurrogateProperty<float> floatProperty;
    floatProperty = SurrogateRegister.GetSurrogate<float>(target, propertyName);

The target parameter is some Unity Component, and name is the property name which will be wrapped. Then, it's as simple as:

    floatProperty.Set(someFloatValue);
    someFloatValue = floatProperty.Get();

