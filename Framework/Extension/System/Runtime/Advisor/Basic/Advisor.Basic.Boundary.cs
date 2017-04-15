using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Runtime
{
    static public partial class Advisor
    {
        static public IAdvice Boundary<T>(this Advisor.IBasic basic)
            where T : Advice.IBoundary, new()
        {
            throw new NotImplementedException();

            // create a method to wrap entire call!
            // create a structure to store arguments, exception & return and implements IBefore/IAfter
            // (1) store as local the new structure
            // (2) on start instantiate Boundary with newobj if it is a class.
            // (3) call before first and give structure as argument
            // (4) try catch finally => call after in finally and check if exception != null => throw it or return value from structure!
            // voilà!
            
            // for async method : find AsyncMachineStructure
            // find field where IBoundary.Factory is stored.
            // replace factory! by Factory<T>

            //return new Advice((_Method, _Pointer) =>
            //{
            //    var _type = _Method.Type();
            //    var _signature = _Method.Signature();
            //    var _routine = new Closure.Routine(_Pointer, _signature, _type);
            //    var _method = new DynamicMethod(string.Empty, _type, _signature, _Method.DeclaringType, true);
            //    var _body = _method.GetILGenerator();
            //    if (_type == Metadata.Void)
            //    {
            //        if (advice.Target != null) { _body.Emit(OpCodes.Ldsfld, Advisor.Module.DefineField(advice.Target)); }
            //        _body.Emit(_signature, false);
            //        _body.Emit(OpCodes.Newobj, _routine.Constructor);
            //        _body.Emit(OpCodes.Ldftn, _routine.Method);
            //        _body.Emit(OpCodes.Newobj, Metadata<Action>.Type.GetConstructors().Single());
            //        _body.Emit(OpCodes.Call, advice.Method);
            //    }
            //    else
            //    {
            //        _body.DeclareLocal(_routine.Type);
            //        if (advice.Target != null) { _body.Emit(OpCodes.Ldsfld, Advisor.Module.DefineField(advice.Target)); }
            //        _body.Emit(_signature, false);
            //        _body.Emit(OpCodes.Newobj, _routine.Constructor);
            //        _body.Emit(OpCodes.Stloc_0);
            //        _body.Emit(OpCodes.Ldloc_0);
            //        _body.Emit(OpCodes.Ldftn, _routine.Method);
            //        _body.Emit(OpCodes.Newobj, Metadata<Action>.Type.GetConstructors().Single());
            //        _body.Emit(OpCodes.Call, advice.Method);
            //        _body.Emit(OpCodes.Ldloc_0);
            //        _body.Emit(OpCodes.Ldfld, _routine.Value);
            //    }
            //    _body.Emit(OpCodes.Ret);
            //    _method.Prepare();
            //    return _method;
            //});
        }
    }
}