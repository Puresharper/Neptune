using System;
using System.Reflection;

namespace System.Runtime
{
    public sealed partial class Advice
    {
        public partial class Boundary
        {
            public interface IYield
            {
                MethodBase Method { get; }
                T Argument<T>(int index);
                int Index { get; }
            }
        }
    }
}