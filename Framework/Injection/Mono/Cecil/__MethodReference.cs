using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil.Rocks;

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

        public static MethodReference MakeHostInstanceGeneric(this MethodDefinition self, params TypeReference[] arguments)
        {
            var reference = new MethodReference(self.Name, self.ReturnType, self.DeclaringType.MakeGenericInstanceType(arguments))
            {
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention
            };

            foreach (var parameter in self.Parameters)
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            foreach (var generic_parameter in self.GenericParameters)
                reference.GenericParameters.Add(new GenericParameter(generic_parameter.Name, reference));

            return reference;
        }

        //static public GenericInstanceMethod MakeGenericMethod(this MethodReference method, IEnumerable<GenericParameter> arguments)
        //{
        //    var _method = new GenericInstanceMethod(method);
        //    foreach (var _argument in arguments) { _method.GenericArguments.Add(_argument); }
        //    return _method;
        //}
    }
}
