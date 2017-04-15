using System;

namespace System.Runtime
{
    public sealed partial class Advice
    {
        public partial class Boundary : Advice.IBoundary
        {
            virtual public void Before(Advice.Boundary.IBefore before)
            {
            }

            virtual public void Resume(Advice.Boundary.IResume resume)
            {
            }

            virtual public void Yield(Advice.Boundary.IYield yield)
            {
            }

            virtual public void After(Advice.Boundary.IAfter after)
            {
            }
        }
    }
}