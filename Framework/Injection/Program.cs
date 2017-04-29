using System;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

using MethodBase = System.Reflection.MethodBase;
using MethodInfo = System.Reflection.MethodInfo;

namespace Virtuoze.Neptune.Injection
{
    static public class Program
    {
        private const string Neptune = "<Neptune>";
        public const string Module = "<Module>";
        public const string Pointer = "<Pointer>";

        static private readonly MethodInfo GetMethodHandle = System.Reflection.Metadata<MethodBase>.Property(_Method => _Method.MethodHandle).GetGetMethod();
        static private readonly MethodInfo GetFunctionPointer = System.Reflection.Metadata<RuntimeMethodHandle>.Method(_Method => _Method.GetFunctionPointer());
        static private readonly MethodInfo CreateDelegate = System.Reflection.Metadata.Method(() => Delegate.CreateDelegate(System.Reflection.Argument<Type>.Value, System.Reflection.Argument<MethodInfo>.Value));

        static public void Main(string[] arguments)
        {
            if (arguments == null) { throw new ArgumentNullException(); }
            switch (arguments.Length)
            {
                case 1:
                    Program.Manage(arguments[0]);
                    break;
                case 2:
                    var _directory = string.Concat(Path.GetDirectoryName(arguments[0]), @"\");
                    var _document = XDocument.Load(arguments[0]);
                    var _namespace = _document.Root.Name.Namespace;
                    var _name = _document.Descendants(_namespace.GetName("AssemblyName")).Single().Value;
                    var _type = _document.Descendants(_namespace.GetName("OutputType")).SingleOrDefault();
                    foreach (var _element in _document.Descendants(_namespace.GetName("OutputPath")))
                    {
                        foreach (var _attribute in _element.Parent.Attributes())
                        {
                            if (_attribute.Value.Contains(arguments[1]))
                            {
                                switch (_type == null ? "Library" : _type.Value)
                                {
                                    case "Library": Program.Manage(string.Concat(_directory, _element.Value, _name, ".dll")); return;
                                    case "WinExe":
                                    case "Exe": Program.Manage(string.Concat(_directory, _element.Value, _name, ".exe")); return;
                                    default: throw new NotSupportedException($"Unknown OutputType: {_type.Value}");
                                }
                            }
                        }
                    }
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        static private void Manage(string assembly)
        {
            var _resolver = new DefaultAssemblyResolver();
            _resolver.AddSearchDirectory(Path.GetDirectoryName(assembly));
            var _assembly = AssemblyDefinition.ReadAssembly(assembly, new ReaderParameters() { AssemblyResolver = _resolver, ReadSymbols = true, ReadingMode = ReadingMode.Immediate });
            var _module = _assembly.MainModule;
            foreach (var _type in _module.GetTypes().ToArray()) { Program.Manage(_type); }
            _assembly.Write(assembly, new WriterParameters { WriteSymbols = true });
        }

        static private bool Bypass(TypeDefinition type)
        {
            return type.IsInterface || type.IsValueType || type.Name == Program.Module || (type.BaseType != null && type.BaseType.Resolve() == type.Module.Import(typeof(MulticastDelegate)).Resolve() || type.Interfaces.Any(_Interface => _Interface.Resolve() == type.Module.Import(typeof(IAsyncStateMachine)).Resolve()));
        }

        static private bool Bypass(MethodDefinition method)
        {
            return method.Body == null || (method.IsConstructor && method.IsStatic);
        }

        static private void Manage(TypeDefinition type)
        {
            if (Program.Bypass(type)) { return; }
            foreach (var _method in type.Methods.ToArray())
            {
                if (Program.Bypass(_method)) { continue; }
                Program.Manage(_method);
            }
        }

        static private TypeDefinition Authority(this TypeDefinition type)
        {
            foreach (var _type in type.NestedTypes) { if (_type.Name == Program.Neptune) { return _type; } }
            return type.Type(Program.Neptune, TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.NestedAssembly | TypeAttributes.BeforeFieldInit | TypeAttributes.SpecialName);
        }

        static private TypeDefinition Authority(this TypeDefinition type, string name)
        {
            var _authority = type.Authority();
            foreach (var _type in _authority.NestedTypes) { if (_type.Name == name) { return _type; } }
            return _authority.Type(name, TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.NestedPublic | TypeAttributes.BeforeFieldInit | TypeAttributes.SpecialName);
        }

        static private MethodDefinition Authentic(this MethodDefinition method)
        {
            var _type = method.DeclaringType.Authority("<Authentic>");
            var _copy = new Copy(method);
            var _method = _type.Method(method.IsConstructor ? "<Constructor>" : method.Name, MethodAttributes.Static | MethodAttributes.Public);
            foreach (var _attribute in method.CustomAttributes) { _method.CustomAttributes.Add(_attribute); }
            foreach (var _parameter in method.GenericParameters) { _method.GenericParameters.Add(new GenericParameter(_parameter.Name, _method)); }
            _copy.Genericity = _method.GenericParameters.ToArray();
            _method.ReturnType = _copy[method.ReturnType];
            if (!method.IsStatic) { _method.Parameters.Add(new ParameterDefinition("this", ParameterAttributes.None, method.DeclaringType)); }
            foreach (var _parameter in method.Parameters) { _method.Add(new ParameterDefinition(_parameter.Name, _parameter.Attributes, _copy[_parameter.ParameterType])); }
            _copy.Signature = _method.Parameters.ToArray();
            var _body = _method.Body;
            _body.InitLocals = method.Body.InitLocals;
            foreach (var _variable in method.Body.Variables) { _body.Add(new VariableDefinition(_variable.Name, _copy[_variable.VariableType])); }
            _copy.Variation = _body.Variables.ToArray();
            foreach (var _instruction in method.Body.Instructions) { _body.Instructions.Add(_copy[_instruction]); }

            //TODO : for virtual method => replace base call to "pure path"!
            if (method.IsVirtual)
            {
                //lookup base call to same method definition and swap it to direct base authentic call!
                //it will allow to wrap the entire virtual call.
            }

            foreach (var _exception in method.Body.ExceptionHandlers)
            {
                _body.ExceptionHandlers.Add(new ExceptionHandler(_exception.HandlerType)
                {
                    CatchType = _exception.CatchType,
                    TryStart = _copy[_exception.TryStart],
                    TryEnd = _copy[_exception.TryEnd],
                    HandlerType = _exception.HandlerType,
                    HandlerStart = _copy[_exception.HandlerStart],
                    HandlerEnd = _copy[_exception.HandlerEnd]
                });
            }
            method.Body.OptimizeMacros();
            _body.OptimizeMacros();
            return _method;
        }

        static private FieldDefinition Intermediate(this MethodDefinition method, MethodDefinition authentic)
        {
            var _intermediate = method.DeclaringType.Authority("<Intermediate>").Type(method.IsConstructor ? $"<<Constructor>>" : $"<{method.Name}>", TypeAttributes.NestedPublic | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            foreach (var _parameter in method.GenericParameters) { _intermediate.GenericParameters.Add(new GenericParameter(_parameter.Name, _intermediate)); }
            var _field = _intermediate.Field<IntPtr>(Program.Pointer, FieldAttributes.Static | FieldAttributes.Public);
            var _initializer = _intermediate.Initializer();
            var _variable = _initializer.Body.Variable<RuntimeMethodHandle>();
            _initializer.Body.Variable<Func<IntPtr>>();
            if (_intermediate.GenericParameters.Count == 0)
            {
                _initializer.Body.Emit(authentic);
                _initializer.Body.Emit(OpCodes.Callvirt, Program.GetMethodHandle);
                _initializer.Body.Emit(OpCodes.Stloc_0);
                _initializer.Body.Emit(OpCodes.Ldloca_S, _variable);
                _initializer.Body.Emit(OpCodes.Callvirt, Program.GetFunctionPointer);
                _initializer.Body.Emit(OpCodes.Stsfld, _field);
            }
            else
            {
                _initializer.Body.Emit(authentic.MakeGenericMethod(_intermediate.GenericParameters));
                _initializer.Body.Emit(OpCodes.Callvirt, Program.GetMethodHandle);
                _initializer.Body.Emit(OpCodes.Stloc_0);
                _initializer.Body.Emit(OpCodes.Ldloca_S, _variable);
                _initializer.Body.Emit(OpCodes.Callvirt, Program.GetFunctionPointer);
                _initializer.Body.Emit(OpCodes.Stsfld, new FieldReference(_field.Name, _field.FieldType, _intermediate.MakeGenericType(_intermediate.GenericParameters)));

                //TODO : IOC of AOP !? What the? in fact it will be used to be able to inject on method on demand but a late as possible.
                //Action<MethodBase> _update;
                //lock (AppDomain.CurrentDomain.Evidence.SyncRoot) { _update = AppDomain.CurrentDomain.GetData("<Neptune<Update>>") as Action<MethodBase>; }
                //if (_update != null) { _update(...); }
            }
            _initializer.Body.Emit(OpCodes.Ret);
            _initializer.Body.OptimizeMacros();
            return _field;
        }

        static private FieldDefinition Metadata(this MethodDefinition method)
        {
            var _metadata = method.DeclaringType.Authority("<Metadata>").Type(method.IsConstructor ? $"<<Constructor>>" : $"<{method.Name}>", TypeAttributes.NestedPublic | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            foreach (var _parameter in method.GenericParameters) { _metadata.GenericParameters.Add(new GenericParameter(_parameter.Name, _metadata)); }
            var _field = _metadata.Field<MethodBase>("<Method>", FieldAttributes.Static | FieldAttributes.Public);
            var _initializer = _metadata.Initializer();
            if (_metadata.HasGenericParameters)
            {
                _initializer.Body.Emit(method.MakeGenericMethod(_metadata.GenericParameters));
                _initializer.Body.Emit(OpCodes.Stsfld, new FieldReference(_field.Name, _field.FieldType, _metadata.MakeGenericType(_metadata.GenericParameters)));
            }
            else
            {
                _initializer.Body.Emit(method);
                _initializer.Body.Emit(OpCodes.Stsfld, _field);
            }
            _initializer.Body.Emit(OpCodes.Ret);
            _initializer.Body.OptimizeMacros();
            return _field;
        }

        static private void Manage(this MethodDefinition method)
        {
            var _metadata = method.Metadata();
            var _machine = method.CustomAttributes.SingleOrDefault(_Attribute => _Attribute.AttributeType.Resolve() == method.Module.Import(typeof(AsyncStateMachineAttribute)).Resolve());
            var _authentic = method.Authentic();
            var _intermediate = method.Intermediate(_authentic);
            method.Body = new MethodBody(method);
            for (var _index = 0; _index < _authentic.Parameters.Count; _index++)
            {
                switch (_index)
                {
                    case 0: method.Body.Emit(OpCodes.Ldarg_0); break;
                    case 1: method.Body.Emit(OpCodes.Ldarg_1); break;
                    case 2: method.Body.Emit(OpCodes.Ldarg_2); break;
                    case 3: method.Body.Emit(OpCodes.Ldarg_3); break;
                    default: method.Body.Emit(OpCodes.Ldarg_S, method.Parameters[method.IsStatic ? _index : _index - 1]); break;
                }
            }
            if (method.GenericParameters.Count == 0)
            {
                method.Body.Emit(OpCodes.Ldsfld, _intermediate);
                method.Body.Emit(OpCodes.Calli, method.ReturnType, _authentic.Parameters);
            }
            else
            {
                var _type = new GenericInstanceType(_intermediate.DeclaringType);
                foreach (var _parameter in method.GenericParameters) { _type.GenericArguments.Add(_parameter); }
                method.Body.Emit(OpCodes.Ldsfld, new FieldReference(_intermediate.Name, _intermediate.FieldType, _type));
                var _method = new GenericInstanceMethod(_authentic);
                foreach (var _parameter in method.GenericParameters) { _method.GenericArguments.Add(_parameter); }
                method.Body.Emit(OpCodes.Calli, _method.ReturnType, _method.Parameters);
            }
            method.Body.Emit(OpCodes.Ret);
            method.Body.OptimizeMacros();
            if (_machine != null)
            {
                var _type = _machine.ConstructorArguments[0].Value as TypeDefinition;
                var _factory = _type.Field<Advice.Boundary.IFactory>("<Factory>", FieldAttributes.Public | FieldAttributes.Static);
                var _boundary = _type.Field<Advice.IBoundary>("<Boundary>", FieldAttributes.Public);
                _type.IsBeforeFieldInit = true;
                var _intializer = _type.Initializer();
                _intializer.Body.Emit(OpCodes.Newobj, System.Reflection.Metadata.Constructor(() => new Advice.Boundary.Factory()));
                _intializer.Body.Emit(OpCodes.Stsfld, _factory.Relative());
                _intializer.Body.Emit(OpCodes.Ret);
                var _constructor = _type.Methods.Single(m => m.IsConstructor && !m.IsStatic);
                _constructor.Body = new MethodBody(_constructor);
                _constructor.Body.Emit(OpCodes.Ldarg_0);
                _constructor.Body.Emit(OpCodes.Call, System.Reflection.Metadata.Constructor(() => new object()));
                _constructor.Body.Emit(OpCodes.Ldarg_0);
                _constructor.Body.Emit(OpCodes.Ldsfld, _constructor.Module.Import(_factory.Relative()));
                _constructor.Body.Emit(OpCodes.Callvirt, System.Reflection.Metadata<Advice.Boundary.IFactory>.Method(_Factory => _Factory.Create()));
                _constructor.Body.Emit(OpCodes.Stfld, _boundary.Relative());
                _constructor.Body.Emit(OpCodes.Ret);
                var _move = _type.Methods.Single(_Method => _Method.Name == "MoveNext");
                var _state = _type.Fields.Single(_Field => _Field.Name == "<>1__state").Relative();
                var _builder = _type.Fields.Single(_Field => _Field.Name == "<>t__builder").Relative();
                var _offset = 0;
                var _begin = _move.Body.Instructions[_offset];
                var _resume = Instruction.Create(OpCodes.Ldarg_0);
                _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Ldarg_0));
                _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Ldfld, _state));
                _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Ldc_I4_0));
                _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Bge, _resume));
                foreach (var _parameter in method.Parameters)
                {
                    _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Ldarg_0));
                    _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Ldfld, _boundary.Relative()));
                    _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Ldc_I4, _parameter.Index));
                    _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Ldarg_0));
                    _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Ldflda, _type.Fields.First(_Field => _Field.Name == _parameter.Name).Relative()));
                    _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Callvirt, _move.Module.Import(_move.Module.Import(System.Reflection.Metadata<Advice.IBoundary>.Method(_Boundary => _Boundary.Argument<object>(System.Reflection.Argument<int>.Value, ref System.Reflection.Argument<object>.Value)).GetGenericMethodDefinition()).MakeGenericMethod(_parameter.ParameterType.IsGenericParameter ? _type.GenericParameters.First(_Type => _Type.Name == _parameter.ParameterType.Name) : _parameter.ParameterType))));
                }
                _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Ldarg_0));
                _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Ldfld, _boundary.Relative()));
                _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Callvirt, _move.Module.Import(System.Reflection.Metadata<Advice.IBoundary>.Method(_Boundary => _Boundary.Invoke()))));
                _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Br_S, _begin));
                _move.Body.Instructions.Insert(_offset++, _resume);
                _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Ldfld, _boundary.Relative()));
                _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Callvirt, _move.Module.Import(System.Reflection.Metadata<Advice.IBoundary>.Method(_Boundary => _Boundary.Resume()))));
                while (_offset < _move.Body.Instructions.Count)
                {
                    var _instruction = _move.Body.Instructions[_offset];
                    if (_instruction.OpCode == OpCodes.Call)
                    {
                        if (_instruction.Operand is MethodReference)
                        {
                            var _operand = _instruction.Operand as MethodReference;
                            if (_operand.Name == "AwaitUnsafeOnCompleted")
                            {
                                _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Ldarg_0));
                                _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Ldfld, _boundary.Relative()));
                                _move.Body.Instructions.Insert(_offset++, Instruction.Create(OpCodes.Callvirt, _move.Module.Import(System.Reflection.Metadata<Advice.IBoundary>.Method(_Boundary => _Boundary.Yield()))));
                            }
                            else if (_operand.Name == "SetResult")
                            {
                                var _return = _type.Method("<Return>", MethodAttributes.Public);
                                if (_operand.HasParameters)
                                {
                                    var _parameter = new ParameterDefinition("<Value>", ParameterAttributes.None, (_builder.FieldType as GenericInstanceType).GenericArguments[0]);
                                    _return.Parameters.Add(_parameter);
                                    var _exception = _return.Body.Variable<Exception>("<Exception>");
                                    var _disposed = _return.Body.Variable<bool>("<Invoked>");
                                    var _end = Instruction.Create(OpCodes.Ret);
                                    _return.Body.Emit(OpCodes.Ldarg_0);
                                    _return.Body.Emit(OpCodes.Ldfld, _boundary.Relative());
                                    _return.Body.Emit(OpCodes.Ldarga_S, _parameter);
                                    _return.Body.Emit(OpCodes.Callvirt, _move.Module.Import(_move.Module.Import(System.Reflection.Metadata<Advice.IBoundary>.Method(_Boundary => _Boundary.Return<object>(ref System.Reflection.Argument<object>.Value)).GetGenericMethodDefinition()).MakeGenericMethod(_parameter.ParameterType)));
                                    _return.Body.Emit(OpCodes.Ldc_I4_1);
                                    _return.Body.Emit(OpCodes.Stloc_1);
                                    _return.Body.Emit(OpCodes.Ldarg_0);
                                    _return.Body.Emit(OpCodes.Ldfld, _boundary.Relative());
                                    _return.Body.Emit(OpCodes.Callvirt, _move.Module.Import(_move.Module.Import(System.Reflection.Metadata<Advice.IBoundary>.Method(_Boundary => _Boundary.Dispose()))));
                                    _return.Body.Emit(OpCodes.Ldarg_0);
                                    _return.Body.Emit(OpCodes.Ldflda, _builder);
                                    _return.Body.Emit(OpCodes.Ldarg_1);
                                    _return.Body.Emit(OpCodes.Call, _operand);
                                    _return.Body.Emit(OpCodes.Ret);
                                    var _catch = _return.Body.Emit(OpCodes.Stloc_0);
                                    _return.Body.Emit(OpCodes.Ldloc_1);
                                    using (_return.Body.False())
                                    {
                                        _return.Body.Emit(OpCodes.Ldarg_0);
                                        _return.Body.Emit(OpCodes.Ldfld, _boundary.Relative());
                                        _return.Body.Emit(OpCodes.Callvirt, _move.Module.Import(_move.Module.Import(System.Reflection.Metadata<Advice.IBoundary>.Method(_Boundary => _Boundary.Dispose()))));
                                    }
                                    _return.Body.Emit(OpCodes.Ldarg_0);
                                    _return.Body.Emit(OpCodes.Ldflda, _builder);
                                    _return.Body.Emit(OpCodes.Ldloc_0);
                                    var _method = _move.Module.Import(_builder.FieldType.Resolve().Methods.Single(_Method => _Method.Name == "SetException"));
                                    _method.DeclaringType = _builder.FieldType;
                                    _return.Body.Emit(OpCodes.Call, _method);
                                    _return.Body.Emit(OpCodes.Ret);
                                    _return.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
                                    {
                                        TryStart = _return.Body.Instructions[0],
                                        TryEnd = _return.Body.Instructions[_catch],
                                        HandlerStart = _return.Body.Instructions[_catch],
                                        HandlerEnd = _return.Body.Instructions[_return.Body.Instructions.Count - 1],
                                        CatchType = _exception.VariableType
                                    });
                                    _return.Body.OptimizeMacros();
                                    _instruction.Operand = _type.HasGenericParameters ? _return.MakeHostInstanceGeneric(_type.GenericParameters.ToArray()) : _return;
                                    _move.Body.Instructions[_offset - 2].OpCode = OpCodes.Nop;
                                }
                                else
                                {
                                    var _exception = _return.Body.Variable<Exception>("<Exception>");
                                    var _disposed = _return.Body.Variable<bool>("<Invoked>");
                                    var _end = Instruction.Create(OpCodes.Ret);
                                    _return.Body.Emit(OpCodes.Ldarg_0);
                                    _return.Body.Emit(OpCodes.Ldfld, _boundary.Relative());
                                    _return.Body.Emit(OpCodes.Callvirt, _move.Module.Import(System.Reflection.Metadata<Advice.IBoundary>.Method(_Boundary => _Boundary.Return())));
                                    _return.Body.Emit(OpCodes.Ldc_I4_1);
                                    _return.Body.Emit(OpCodes.Stloc_1);
                                    _return.Body.Emit(OpCodes.Ldarg_0);
                                    _return.Body.Emit(OpCodes.Ldfld, _boundary.Relative());
                                    _return.Body.Emit(OpCodes.Callvirt, _move.Module.Import(_move.Module.Import(System.Reflection.Metadata<Advice.IBoundary>.Method(_Boundary => _Boundary.Dispose()))));
                                    _return.Body.Emit(OpCodes.Ldarg_0);
                                    _return.Body.Emit(OpCodes.Ldflda, _builder);
                                    _return.Body.Emit(OpCodes.Call, _operand);
                                    _return.Body.Emit(OpCodes.Ret);
                                    var _catch = _return.Body.Emit(OpCodes.Stloc_0);
                                    _return.Body.Emit(OpCodes.Ldloc_1);
                                    using (_return.Body.False())
                                    {
                                        _return.Body.Emit(OpCodes.Ldarg_0);
                                        _return.Body.Emit(OpCodes.Ldfld, _boundary.Relative());
                                        _return.Body.Emit(OpCodes.Callvirt, _move.Module.Import(_move.Module.Import(System.Reflection.Metadata<Advice.IBoundary>.Method(_Boundary => _Boundary.Dispose()))));
                                    }
                                    _return.Body.Emit(OpCodes.Ldarg_0);
                                    _return.Body.Emit(OpCodes.Ldflda, _builder);
                                    _return.Body.Emit(OpCodes.Ldloc_0);
                                    var _method = _move.Module.Import(_builder.FieldType.Resolve().Methods.Single(_Method => _Method.Name == "SetException"));
                                    _method.DeclaringType = _builder.FieldType;
                                    _return.Body.Emit(OpCodes.Call, _method);
                                    _return.Body.Emit(OpCodes.Ret);
                                    _return.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
                                    {
                                        TryStart = _return.Body.Instructions[0],
                                        TryEnd = _return.Body.Instructions[_catch],
                                        HandlerStart = _return.Body.Instructions[_catch],
                                        HandlerEnd = _return.Body.Instructions[_return.Body.Instructions.Count - 1],
                                        CatchType = _exception.VariableType,
                                    });
                                    _return.Body.OptimizeMacros();
                                    _instruction.Operand = _type.HasGenericParameters ? _return.MakeHostInstanceGeneric(_type.GenericParameters.ToArray()) : _return;
                                    _move.Body.Instructions[_offset - 1].OpCode = OpCodes.Nop;
                                }
                            }
                            else if (_operand.Name == "SetException")
                            {
                                var _throw = _type.Method("<Throw>", MethodAttributes.Public);
                                var _parameter = new ParameterDefinition("<Exception>", ParameterAttributes.None, _throw.Module.Import(typeof(Exception)));
                                _throw.Parameters.Add(_parameter);
                                if (_builder.FieldType.IsGenericInstance)
                                {
                                    var _value = new VariableDefinition("<Value>", (_builder.FieldType as GenericInstanceType).GenericArguments[0]);
                                    _throw.Body.Variables.Add(_value);
                                    var _disposed = _throw.Body.Variable<bool>("<Invoked>");
                                    _throw.Body.Emit(OpCodes.Ldarg_0);
                                    _throw.Body.Emit(OpCodes.Ldfld, _boundary.Relative());
                                    _throw.Body.Emit(OpCodes.Ldarg_S, _parameter);
                                    _throw.Body.Emit(OpCodes.Ldloca_S, _value);
                                    _throw.Body.Emit(OpCodes.Callvirt, _move.Module.Import(System.Reflection.Metadata<Advice.IBoundary>.Method(_Boundary => _Boundary.Throw<object>(ref System.Reflection.Argument<Exception>.Value, ref System.Reflection.Argument<object>.Value)).GetGenericMethodDefinition()).MakeGenericMethod(_value.VariableType));
                                    _throw.Body.Emit(OpCodes.Ldc_I4_1);
                                    _throw.Body.Emit(OpCodes.Stloc_1);
                                    _throw.Body.Emit(OpCodes.Ldarg_0);
                                    _throw.Body.Emit(OpCodes.Ldfld, _boundary.Relative());
                                    _throw.Body.Emit(OpCodes.Callvirt, _move.Module.Import(_move.Module.Import(System.Reflection.Metadata<Advice.IBoundary>.Method(_Boundary => _Boundary.Dispose()))));
                                    _throw.Body.Emit(OpCodes.Ldarg_1);
                                    using (_throw.Body.True())
                                    {
                                        _throw.Body.Emit(OpCodes.Ldarg_0);
                                        _throw.Body.Emit(OpCodes.Ldflda, _builder);
                                        _throw.Body.Emit(OpCodes.Ldarg_1);
                                        _throw.Body.Emit(OpCodes.Call, _operand);
                                        _throw.Body.Emit(OpCodes.Ret);
                                    }
                                    _throw.Body.Emit(OpCodes.Ldarg_0);
                                    _throw.Body.Emit(OpCodes.Ldflda, _builder);
                                    _throw.Body.Emit(OpCodes.Ldloc_0);
                                    var _method = _move.Module.Import(_move.Module.Import(_builder.FieldType.Resolve().Methods.Single(_Method => _Method.Name == "SetResult" && _Method.Parameters[0].ParameterType.IsGenericParameter)));
                                    _method.DeclaringType = _builder.FieldType;
                                    _throw.Body.Emit(OpCodes.Call, _method);
                                    _throw.Body.Emit(OpCodes.Ret);
                                    var _catch = _throw.Body.Emit(OpCodes.Starg, _parameter);
                                    _throw.Body.Emit(OpCodes.Ldloc_1);
                                    using (_throw.Body.False())
                                    {
                                        _throw.Body.Emit(OpCodes.Ldarg_0);
                                        _throw.Body.Emit(OpCodes.Ldfld, _boundary.Relative());
                                        _throw.Body.Emit(OpCodes.Callvirt, _move.Module.Import(_move.Module.Import(System.Reflection.Metadata<Advice.IBoundary>.Method(_Boundary => _Boundary.Dispose()))));
                                    }
                                    _throw.Body.Emit(OpCodes.Ldarg_0);
                                    _throw.Body.Emit(OpCodes.Ldflda, _builder);
                                    _throw.Body.Emit(OpCodes.Ldarg_1);
                                    _throw.Body.Emit(OpCodes.Call, _operand);
                                    _throw.Body.Emit(OpCodes.Ret);
                                    _throw.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
                                    {
                                        TryStart = _throw.Body.Instructions[0],
                                        TryEnd = _throw.Body.Instructions[_catch],
                                        HandlerStart = _throw.Body.Instructions[_catch],
                                        HandlerEnd = _throw.Body.Instructions[_throw.Body.Instructions.Count - 1],
                                        CatchType = _parameter.ParameterType,
                                    });
                                }
                                else
                                {
                                    var _disposed = _throw.Body.Variable<bool>("<Invoked>");
                                    _throw.Body.Emit(OpCodes.Ldarg_0);
                                    _throw.Body.Emit(OpCodes.Ldfld, _boundary.Relative());
                                    _throw.Body.Emit(OpCodes.Ldarga_S, _parameter);
                                    _throw.Body.Emit(OpCodes.Callvirt, _move.Module.Import(System.Reflection.Metadata<Advice.IBoundary>.Method(_Boundary => _Boundary.Throw(ref System.Reflection.Argument<Exception>.Value))));
                                    _throw.Body.Emit(OpCodes.Ldc_I4_1);
                                    _throw.Body.Emit(OpCodes.Stloc_0);
                                    _throw.Body.Emit(OpCodes.Ldarg_0);
                                    _throw.Body.Emit(OpCodes.Ldfld, _boundary.Relative());
                                    _throw.Body.Emit(OpCodes.Callvirt, _move.Module.Import(_move.Module.Import(System.Reflection.Metadata<Advice.IBoundary>.Method(_Boundary => _Boundary.Dispose()))));
                                    _throw.Body.Emit(OpCodes.Ldarg_1);
                                    using (_throw.Body.True())
                                    {
                                        _throw.Body.Emit(OpCodes.Ldarg_0);
                                        _throw.Body.Emit(OpCodes.Ldflda, _builder);
                                        _throw.Body.Emit(OpCodes.Ldarg_1);
                                        _throw.Body.Emit(OpCodes.Call, _operand);
                                        _throw.Body.Emit(OpCodes.Ret);
                                    }
                                    _throw.Body.Emit(OpCodes.Ldarg_0);
                                    _throw.Body.Emit(OpCodes.Ldflda, _builder);
                                    var _method = _move.Module.Import(_builder.FieldType.Resolve().Methods.Single(_Method => _Method.Name == "SetResult" && _Method.Parameters.Count == 0));
                                    _method.DeclaringType = _builder.FieldType;
                                    _throw.Body.Emit(OpCodes.Call, _method);
                                    _throw.Body.Emit(OpCodes.Ret);
                                    var _catch = _throw.Body.Emit(OpCodes.Starg, _parameter);
                                    _throw.Body.Emit(OpCodes.Ldloc_0);
                                    using (_throw.Body.False())
                                    {
                                        _throw.Body.Emit(OpCodes.Ldarg_0);
                                        _throw.Body.Emit(OpCodes.Ldfld, _boundary.Relative());
                                        _throw.Body.Emit(OpCodes.Callvirt, _move.Module.Import(_move.Module.Import(System.Reflection.Metadata<Advice.IBoundary>.Method(_Boundary => _Boundary.Dispose()))));
                                    }
                                    _throw.Body.Emit(OpCodes.Ldarg_0);
                                    _throw.Body.Emit(OpCodes.Ldflda, _builder);
                                    _throw.Body.Emit(OpCodes.Ldarg_1);
                                    _throw.Body.Emit(OpCodes.Call, _operand);
                                    _throw.Body.Emit(OpCodes.Ret);
                                    _throw.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
                                    {
                                        TryStart = _throw.Body.Instructions[0],
                                        TryEnd = _throw.Body.Instructions[_catch],
                                        HandlerStart = _throw.Body.Instructions[_catch],
                                        HandlerEnd = _throw.Body.Instructions[_throw.Body.Instructions.Count - 1],
                                        CatchType = _parameter.ParameterType,
                                    });
                                }
                                _throw.Body.OptimizeMacros();
                                _instruction.Operand = _type.HasGenericParameters ? _throw.MakeHostInstanceGeneric(_type.GenericParameters.ToArray()) : _throw;
                                _move.Body.Instructions[_offset - 2].OpCode = OpCodes.Nop;
                            }
                        }
                    }
                    _offset++;
                }
                _move.Body.OptimizeMacros();
            }
        }
    }
}
