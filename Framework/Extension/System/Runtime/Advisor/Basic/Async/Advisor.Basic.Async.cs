using System;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Runtime
{
    static public partial class Advisor
    {
        public partial class Basic
        {
            /// <summary>
            /// Create advice for async methods.
            /// </summary>
            public interface IAsync
            {
                /// <summary>
                /// GetHashCode.
                /// </summary>
                /// <returns>HashCode</returns>
                [DebuggerHidden]
                [EditorBrowsable(EditorBrowsableState.Never)]
                int GetHashCode();

                /// <summary>
                /// ToString.
                /// </summary>
                /// <returns>String</returns>
                [DebuggerHidden]
                [EditorBrowsable(EditorBrowsableState.Never)]
                string ToString();

                /// <summary>
                /// GetType.
                /// </summary>
                /// <returns>Type</returns>
                [DebuggerHidden]
                [EditorBrowsable(EditorBrowsableState.Never)]
                Type GetType();
            }

            private sealed class Async : IAsync
            {
                [DebuggerHidden]
                [EditorBrowsable(EditorBrowsableState.Never)]
                int Advisor.Basic.IAsync.GetHashCode()
                {
                    return this.GetHashCode();
                }

                [DebuggerHidden]
                [EditorBrowsable(EditorBrowsableState.Never)]
                string Advisor.Basic.IAsync.ToString()
                {
                    return this.ToString();
                }

                [DebuggerHidden]
                [EditorBrowsable(EditorBrowsableState.Never)]
                Type Advisor.Basic.IAsync.GetType()
                {
                    return this.GetType();
                }
            }
        }
    }
}