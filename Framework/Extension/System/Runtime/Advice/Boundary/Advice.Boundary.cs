using System;
using System.Reflection;

namespace System.Runtime
{
    public sealed partial class Advice
    {
        public partial class Boundary : Advice.IBoundary
        {
            virtual public void Initialize(MethodBase method, ParameterInfo[] signature)
            {
            }

            virtual public void Instance<T>(T instance)
            {
            }

            virtual public void Argument<T>(int index, ref T value)
            {
            }

            virtual public void Before()
            {
            }

            virtual public void Resume()
            {
            }

            virtual public void Yield()
            {
            }

            public void Return()
            {
            }

            public void Throw(ref Exception exception)
            {
            }

            public void Return<T>(ref T value)
            {
            }

            public void Throw<T>(ref Exception exception, ref T value)
            {
            }

            public void Finally()
            {
            }
        }
    }
}