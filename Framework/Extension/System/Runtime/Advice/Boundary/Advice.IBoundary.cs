using System;

namespace System.Runtime
{
    public sealed partial class Advice
    {
        public interface IBoundary
        {
            void Before(Advice.Boundary.IBefore before);
            void Resume(Advice.Boundary.IResume resume);
            void Yield(Advice.Boundary.IYield yield);
            void After(Advice.Boundary.IAfter after);
        }
    }
}