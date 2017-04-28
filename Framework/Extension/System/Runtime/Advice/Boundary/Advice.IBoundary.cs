using System;
using System.Reflection;

namespace System.Runtime
{
    public sealed partial class Advice
    {
        public interface IBoundary
        {
            void Initialize(MethodBase method, ParameterInfo[] signature);
            void Instance<T>(T instance);
            void Argument<T>(int index, ref T value);
            void Before();
            void Resume();
            void Yield();
            void Return();
            void Throw(ref Exception exception);
            void Return<T>(ref T value);
            void Throw<T>(ref Exception exception, ref T value);
            void Finally();
        }
    }
}