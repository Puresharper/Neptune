using System;
using System.Reflection;

namespace System.Runtime
{
    public sealed partial class Advice
    {
        public partial class Boundary
        {
            public interface IAfter
            {
                MethodBase Method { get; }
                T Argument<T>(int index);
                T Exception<T>()
                    where T : Exception;
                void Exception<T>(Exception exception)
                    where T : Exception;
                T Return<T>();
                void Return<T>(T value);
            }
        }
    }
}