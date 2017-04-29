using System;
using Mono;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections;
using Mono.Collections.Generic;

using ConstructorInfo = System.Reflection.ConstructorInfo;
using MethodInfo = System.Reflection.MethodInfo;

namespace Mono.Cecil
{
    static internal class __MethodBody
    {
        static private readonly MethodInfo GetTypeFromHandle = System.Reflection.Metadata.Method(() => Type.GetTypeFromHandle(System.Reflection.Argument<RuntimeTypeHandle>.Value));
        static private readonly MethodInfo GetMethodFromHandle = System.Reflection.Metadata.Method(() => MethodInfo.GetMethodFromHandle(System.Reflection.Argument<RuntimeMethodHandle>.Value, System.Reflection.Argument<RuntimeTypeHandle>.Value));

        static public int Add(this MethodBody body, Instruction instruction)
        {
            body.Instructions.Add(instruction);
            var _branch = Branch.Query(body);
            if (_branch == null) { return body.Instructions.Count - 1; }
            _branch.Finialize(instruction);
            return body.Instructions.Count - 1;
        }

        static public IDisposable True(this MethodBody body)
        {
            return new Branch(body, OpCodes.Brfalse).Begin();
        }

        static public IDisposable False(this MethodBody body)
        {
            return new Branch(body, OpCodes.Brtrue).Begin();
        }

        static public int Emit(this MethodBody body, OpCode instruction)
        {
            return body.Add(Instruction.Create(instruction));
        }

        static public int Emit(this MethodBody body, OpCode instruction, Instruction label)
        {
            return body.Add(Instruction.Create(instruction, label));
        }

        static public int Emit(this MethodBody body, OpCode instruction, VariableDefinition variable)
        {
            return body.Add(Instruction.Create(instruction, variable));
        }

        static public int Emit(this MethodBody body, OpCode instruction, MethodInfo method)
        {
            return body.Add(Instruction.Create(instruction, body.Method.DeclaringType.Module.Import(method)));
        }

        static public int Emit(this MethodBody body, OpCode instruction, TypeReference type, Collection<ParameterDefinition> parameters)
        {
            if (instruction == OpCodes.Calli)
            {
                var _signature = new CallSite(type);
                foreach (var _parameter in parameters) { _signature.Parameters.Add(_parameter); }
                _signature.CallingConvention = MethodCallingConvention.Default;
                return body.Add(Instruction.Create(instruction, _signature));
            }
            throw new InvalidOperationException();
        }

        static public int Emit(this MethodBody body, OpCode instruction, TypeReference type)
        {
            return body.Add(Instruction.Create(instruction, type));
        }

        static public int Emit(this MethodBody body, OpCode instruction, Type type)
        {
            return body.Add(Instruction.Create(instruction, body.Method.Module.Import(type)));
        }

        static public int Emit(this MethodBody body, OpCode instruction, MethodReference method)
        {
            return body.Add(Instruction.Create(instruction, method));
        }

        static public int Emit(this MethodBody body, OpCode instruction, FieldReference field)
        {
            return body.Add(Instruction.Create(instruction, field));
        }

        static public int Emit(this MethodBody body, OpCode instruction, ParameterDefinition parameter)
        {
            return body.Add(Instruction.Create(instruction, parameter));
        }

        static public int Emit(this MethodBody body, OpCode instruction, int operand)
        {
            return body.Add(Instruction.Create(instruction, operand));
        }

        static public int Emit(this MethodBody body, OpCode instruction, string operand)
        {
            return body.Add(Instruction.Create(instruction, operand));
        }

        static public int Emit(this MethodBody body, OpCode instruction, ConstructorInfo constructor)
        {
            return body.Add(Instruction.Create(instruction, body.Method.DeclaringType.Module.Import(constructor)));
        }

        static public int Emit(this MethodBody body, TypeReference type)
        {
            body.Emit(OpCodes.Ldtoken, type);
            return body.Emit(OpCodes.Call, __MethodBody.GetTypeFromHandle);
        }

        static public int Emit(this MethodBody body, MethodReference method)
        {
            body.Emit(OpCodes.Ldtoken, method);
            body.Emit(OpCodes.Ldtoken, method.DeclaringType);
            return body.Emit(OpCodes.Call, __MethodBody.GetMethodFromHandle);
        }

        static public VariableDefinition Variable<T>(this MethodBody body)
        {
            var _variable = new VariableDefinition(string.Concat("<", System.Reflection.Metadata<T>.Type, ">"), body.Method.DeclaringType.Module.Import(System.Reflection.Metadata<T>.Type));
            body.Variables.Add(_variable);
            return _variable;
        }

        static public VariableDefinition Variable<T>(this MethodBody body, string name)
        {
            var _variable = new VariableDefinition(name, body.Method.DeclaringType.Module.Import(System.Reflection.Metadata<T>.Type));
            body.Variables.Add(_variable);
            return _variable;
        }

        static public VariableDefinition Add(this MethodBody body, VariableDefinition variable)
        {
            body.Variables.Add(variable);
            return variable;
        }
    }
}
