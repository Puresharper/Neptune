using System;

namespace System.Runtime
{
    public sealed partial class Advice
    {
        public partial class Boundary
        {
            public class Factory : Advice.Boundary.IFactory
            {
                static private Advice.Boundary m_Boundary = new Advice.Boundary();

                public IBoundary Create()
                {
                    return Advice.Boundary.Factory.m_Boundary;
                }
            }

            public class Factory<T> : Advice.Boundary.IFactory
                where T : Advice.IBoundary, new()
            {
                public IBoundary Create()
                {
                    return new T();
                }
            }
        }
    }
}