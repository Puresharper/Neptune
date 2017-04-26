using System;

namespace System.Runtime
{
    public sealed partial class Advice
    {
        public partial class Boundary : Advice.IBoundary
        {
            virtual public void Argument(IntPtr pointer)
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

            public void Return<T>(ref T value, ref Exception exception)
            {
            }

            public void Throw<T>(ref T value, ref Exception exception)
            {
            }
        }
    }
}