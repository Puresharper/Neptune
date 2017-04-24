using System;
using System.Collections;
using System.Collections.Generic;

namespace Mono.Cecil
{
    static internal class __MethodReference
    {
        static public GenericInstanceMethod MakeGenericMethod(this MethodReference method, params TypeReference[] arguments)
        {
            var _method = new GenericInstanceMethod(method);
            foreach (var _argument in arguments) { _method.GenericArguments.Add(_argument); }
            return _method;
        }

        //static public GenericInstanceMethod MakeGenericMethod(this MethodReference method, IEnumerable<GenericParameter> arguments)
        //{
        //    var _method = new GenericInstanceMethod(method);
        //    foreach (var _argument in arguments) { _method.GenericArguments.Add(_argument); }
        //    return _method;
        //}
    }
}
