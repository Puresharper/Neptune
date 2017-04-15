using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime
{
    static public partial class Advisor
    {
        static private readonly ConditionalWeakTable<MethodBase, DynamicMethod> m_Dictionary = new ConditionalWeakTable<MethodBase, DynamicMethod>();

        static private DynamicMethod Closure(MethodBase method)
        {
            //Generate class with field corresponding to arguments and a pointer
            //Create method to invoke 

            //Func<Task> dynamicMethod(IntPtr, T1, T2, T3...);


            //create in generated field : a func<Task> creator matching with this signature!

            return null;
        }

        /// <summary>
        /// Create an advice that runs before the advised method.
        /// </summary>
        /// <param name="basic">Basic</param>
        /// <param name="advice">Delegate to be invoked before the advised method</param>
        /// <returns>Advice</returns>
        static public IAdvice Before(this Advisor.Basic.IAsync basic, Action advice)
        {
            return new Advice((_Method, _Pointer) =>
            {
                var _signature = _Method.Signature();
                var _type = _Method.Type();
                var _method = new DynamicMethod(string.Empty, _type, _signature, _Method.DeclaringType, true);
                var _body = _method.GetILGenerator();
                var _closure = Advisor.Closure(_Method);
                switch (IntPtr.Size)
                {
                    case 4: _body.Emit(OpCodes.Ldc_I4, _Pointer.ToInt32()); break;
                    case 8: _body.Emit(OpCodes.Ldc_I8, _Pointer.ToInt64()); break;
                    default: throw new NotSupportedException();
                }
                //push all arguments

                _body.Emit(OpCodes.Ldsfld, _closure);
                //invoke closure to create Func<Task<?>>; 
                if (advice.Target != null) { _body.Emit(OpCodes.Ldsfld, Advisor.Module.DefineField(advice.Target)); }
                _body.Emit(OpCodes.Call, advice.Method);
                _body.Emit(OpCodes.Call, Metadata.Method(() => Advisor.Async.State.Machine.Before.Begin(Argument<Func<Task>>.Value, Argument<Action>.Value)));
                _body.Emit(OpCodes.Ret);
                _method.Prepare();
                return _method;
            });
        }

        ///// <summary>
        ///// Create an advice that runs before the advised method.
        ///// </summary>
        ///// <param name="basic">Basic</param>
        ///// <param name="advice">Delegate to be invoked before the advised method : Action(object = [target instance of advised method call], object[] = [boxed arguments used to call advised method])</param>
        ///// <returns>Advice</returns>
        //static public IAdvice Before(this Advisor.Basic.IAsync basic, Action<object, object[]> advice)
        //{
        //    return new Advice((_Method, _Pointer) =>
        //    {
        //        var _signature = _Method.Signature();
        //        var _type = _Method.Type();
        //        var _method = new DynamicMethod(string.Empty, _type, _signature, _Method.DeclaringType, true);
        //        var _body = _method.GetILGenerator();
        //        if (advice.Target != null) { _body.Emit(OpCodes.Ldsfld, Advisor.Module.DefineField(advice.Target)); }
        //        _body.Emit(_signature, true);
        //        _body.Emit(OpCodes.Call, advice.Method);
        //        _body.Emit(_signature, false);
        //        _body.Emit(_Pointer, _type, _signature);
        //        _body.Emit(OpCodes.Ret);
        //        _method.Prepare();
        //        return _method;
        //    });
        //}
    }
}