﻿using System;
using System.Linq;
using System.Linq.Expressions;
using Mono;

namespace Mono.Cecil
{
    static internal class __FieldDefinition
    {
        static public CustomAttribute Attribute<T>(this FieldDefinition field)
            where T : Attribute
        {
            var _attribute = new CustomAttribute(field.Module.Import(typeof(T).GetConstructor(System.Type.EmptyTypes)));
            field.CustomAttributes.Add(_attribute);
            return _attribute;
        }

        static public CustomAttribute Attribute<T>(this FieldDefinition field, Expression<Func<T>> expression)
            where T : Attribute
        {
            var _constructor = (expression.Body as NewExpression).Constructor;
            var _attribute = new CustomAttribute(field.Module.Import(_constructor));
            foreach (var _argument in (expression.Body as NewExpression).Arguments) { _attribute.ConstructorArguments.Add(new CustomAttributeArgument(field.Module.Import(_argument.Type), Expression.Lambda<Func<object>>(Expression.Convert(_argument, System.Reflection.Metadata<object>.Type)).Compile()())); }
            field.CustomAttributes.Add(_attribute);
            return _attribute;
        }
    }
}
