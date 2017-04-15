using System;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

using MethodBase = System.Reflection.MethodBase;
using MethodInfo = System.Reflection.MethodInfo;


namespace Virtuoze.Neptune.Injector
{
    static public class Program
    {
        private const string Neptune = "<Neptune>";
        public const string Module = "<Module>";
        public const string Pointer = "<Pointer>";

        static private readonly MethodInfo GetMethodHandle = System.Reflection.Metadata<MethodBase>.Property(_Method => _Method.MethodHandle).GetGetMethod();
        static private readonly MethodInfo GetFunctionPointer = System.Reflection.Metadata<RuntimeMethodHandle>.Method(_Method => _Method.GetFunctionPointer());
        static private readonly MethodInfo CreateDelegate = System.Reflection.Metadata.Method(() => Delegate.CreateDelegate(System.Reflection.Argument<Type>.Value, System.Reflection.Argument<MethodInfo>.Value));

        static Program()
        {
            //TODO load extension from assembly location (over?)
        }

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
            _assembly.Write(assembly, new WriterParameters { WriteSymbols = true  });
        }

        static private bool Bypass(TypeDefinition type)
        {
            return type.IsInterface || type.IsValueType || type.Name == Program.Module || (type.BaseType != null && type.BaseType.Resolve() == type.Module.Import(typeof(MulticastDelegate)).Resolve());
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
                    TryEnd = _copy[_exception.TryEnd]
                });
            }
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
            return _field;
        }

        static private FieldDefinition Machine(this MethodDefinition method)
        {
            var _machine = method.DeclaringType.Authority("<Machine>").Type($"<{method.Name}>", TypeAttributes.NestedPublic | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            var _tuple = _machine.Field<Tuple<IntPtr, IntPtr, IntPtr, IntPtr>>(FieldAttributes.Static | FieldAttributes.Private);
            var _field = _machine.Field<IntPtr>(Program.Pointer, FieldAttributes.Static | FieldAttributes.Public);
            var _activate = _machine.Method<Tuple<IntPtr, IntPtr, IntPtr, IntPtr>>("<Activate>", MethodAttributes.Static | MethodAttributes.Private);
            _activate.Body.Emit(OpCodes.Ldsfld, _tuple);
            _activate.Body.Emit(OpCodes.Ret);
            var _return = _machine.Method("<Return>", MethodAttributes.Static | MethodAttributes.Private);
            _return.Parameter<AsyncTaskMethodBuilder>("<AsyncTaskMethodBuilder>");
            //all original args + response if it has! <this> <return>
            _return.Parameter<int>("<Index>");
            _return.Parameter<int>("<Count>");
            _return.Body.Emit(OpCodes.Ldsfld, _tuple); // call setResult on first arg!
            _return.Body.Emit(OpCodes.Ret);
            var _exception = _machine.Method("<Exception>", MethodAttributes.Static | MethodAttributes.Private);
            _exception.Parameter<int>("<Index>");
            _exception.Parameter<int>("<Count>");
            _exception.Body.Emit(OpCodes.Ldsfld, _tuple); // call setexception!
            _exception.Body.Emit(OpCodes.Ret);
            var _resume = _machine.Method("<Resume>", MethodAttributes.Static | MethodAttributes.Private);
            _resume.Parameter<int>("<Index>");
            _resume.Parameter<int>("<Count>");
            _resume.Body.Emit(OpCodes.Ret);
            var _yield = _machine.Method("<Yield>", MethodAttributes.Static | MethodAttributes.Private);
            _yield.Parameter<int>("<Index>");
            _yield.Parameter<int>("<Count>");
            _yield.Body.Emit(OpCodes.Ret);
            var _initializer = _machine.Initializer();
            var _variable = _initializer.Body.Variable<RuntimeMethodHandle>();
            _initializer.Body.Variable<Func<IntPtr>>();

            //_initializer.Body.Emit(OpCodes.Stsfld, _tuple);
            //push 4 fantastics here!

            _initializer.Body.Emit(OpCodes.Newobj, System.Reflection.Metadata.Constructor(() => new Tuple<IntPtr, IntPtr, IntPtr, IntPtr>(System.Reflection.Argument<IntPtr>.Value, System.Reflection.Argument<IntPtr>.Value, System.Reflection.Argument<IntPtr>.Value, System.Reflection.Argument<IntPtr>.Value)));
            _initializer.Body.Emit(OpCodes.Stsfld, _tuple);
            _initializer.Body.Emit(_activate);
            _initializer.Body.Emit(OpCodes.Callvirt, Program.GetMethodHandle);
            _initializer.Body.Emit(OpCodes.Stloc_0);
            _initializer.Body.Emit(OpCodes.Ldloca_S, _variable);
            _initializer.Body.Emit(OpCodes.Callvirt, Program.GetFunctionPointer);
            _initializer.Body.Emit(OpCodes.Stsfld, _field);

            //emit 4 fantastics methods!<Return> <Exception> <Resume> <Yield>

            _initializer.Body.Emit(OpCodes.Ret);

            

            return _field;
        }

        static private void Manage(this MethodDefinition method)
        {
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
                    default: method.Body.Emit(OpCodes.Ldarg_S, method.Parameters[_index]); break;
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
            if (method.Module.Import(typeof(IAsyncStateMachine)).IsAssignableFrom(method.DeclaringType) && method.Name == "MoveNext") // TODO : change method name test by reliable IAsyncStateMachine.MoveNext resolution.
            {
                //add static field to intermediate as pointer to produce Tuple<IntPtr, IntPtr, IntPtr, IntPtr>!
                var _activation = _intermediate.DeclaringType.Field<IntPtr>("<Activation>", FieldAttributes.Public | FieldAttributes.Static);

                //add constructor to state machine to force initialize 4 fantastics by calling intermediate!


                //add instance field to state machine <Intermediate>
                //pointer to call resume:(AsyncTaskMethodBuilder, int state) / setresult(AsyncTaskMethodBuilder, int state) / setexception(AsyncTaskMethodBuilder, int state) / AwaitUnsafeOnCompleted(AsyncTaskMethodBuilder, int)
            }
        }
    }
}
