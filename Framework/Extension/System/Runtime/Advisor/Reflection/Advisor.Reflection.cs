﻿using System;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Runtime
{
    static public partial class Advisor
    {
        /// <summary>
        /// Reflection.
        /// </summary>
        public interface IReflection
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
    }
}