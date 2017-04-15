using System;

namespace System.Runtime
{
    public sealed partial class Advice
    {
        public partial class Boundary
        {
            public interface IFactory
            {
                Advice.IBoundary Create();
            }
        }
    }
}