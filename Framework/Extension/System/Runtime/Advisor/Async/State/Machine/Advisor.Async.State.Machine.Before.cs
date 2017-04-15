using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime
{
    static public partial class Advisor
    {
        static internal partial class Async
        {
            static public partial class State
            {
                static public partial class Machine
                {
                    internal class Before : IAsyncStateMachine
                    {
                        static public Task Begin(Func<Task> method, Action action)
                        {
                            var _state = new Before(method, action);
                            _state.m_Builder.Start(ref _state);
                            return _state.m_Builder.Task;
                        }

                        private int m_State;
                        private AsyncTaskMethodBuilder m_Builder;
                        private TaskAwaiter m_Awaiter;
                        private Func<Task> m_Method;
                        private Action m_Action;

                        public Before(Func<Task> method, Action action)
                        {
                            this.m_Method = method;
                            this.m_Action = action;
                            this.m_Builder = AsyncTaskMethodBuilder.Create();
                            this.m_State = -1;
                        }

                        void IAsyncStateMachine.MoveNext()
                        {
                            var _state = this.m_State;
                            try
                            {
                                TaskAwaiter _awaiter;
                                if (_state != 0)
                                {
                                    this.m_Action();
                                    _awaiter = this.m_Method().GetAwaiter();
                                    if (!_awaiter.IsCompleted)
                                    {
                                        this.m_State = _state = 0;
                                        this.m_Awaiter = _awaiter;
                                        var stateMachine = this;
                                        this.m_Builder.AwaitUnsafeOnCompleted(ref _awaiter, ref stateMachine);
                                        return;
                                    }
                                }
                                else
                                {
                                    _awaiter = this.m_Awaiter;
                                    this.m_Awaiter = new TaskAwaiter();
                                    this.m_State = _state = -1;
                                }
                                _awaiter.GetResult();
                                _awaiter = new TaskAwaiter();
                            }
                            catch (Exception exception)
                            {
                                this.m_State = -2;
                                this.m_Builder.SetException(exception);
                                return;
                            }
                            this.m_State = -2;
                        }

                        void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
                        {
                        }
                    }
                }

                static public partial class Machine<T>
                {
                    internal class Before : IAsyncStateMachine
                    {
                        static public Task<T> Begin(Func<Task<T>> method, Action action)
                        {
                            var _state = new Before(method, action);
                            _state.m_Builder.Start(ref _state);
                            return _state.m_Builder.Task;
                        }

                        private int m_State;
                        private AsyncTaskMethodBuilder<T> m_Builder;
                        private TaskAwaiter<T> m_Awaiter;
                        private T m_Return;
                        private Func<Task<T>> m_Method;
                        private Action m_Action;

                        public Before(Func<Task<T>> method, Action action)
                        {
                            this.m_Method = method;
                            this.m_Action = action;
                            this.m_Builder = AsyncTaskMethodBuilder<T>.Create();
                            this.m_State = -1;
                        }

                        void IAsyncStateMachine.MoveNext()
                        {
                            T _return;
                            var _state = this.m_State;
                            try
                            {
                                TaskAwaiter<T> _awaiter;
                                if (_state != 0)
                                {
                                    this.m_Action();
                                    _awaiter = this.m_Method().GetAwaiter();
                                    if (!_awaiter.IsCompleted)
                                    {
                                        this.m_State = _state = 0;
                                        this.m_Awaiter = _awaiter;
                                        var stateMachine = this;
                                        this.m_Builder.AwaitUnsafeOnCompleted(ref _awaiter, ref stateMachine);
                                        return;
                                    }
                                }
                                else
                                {
                                    _awaiter = this.m_Awaiter;
                                    this.m_Awaiter = new TaskAwaiter<T>();
                                    this.m_State = _state = -1;
                                }
                                this.m_Return = _awaiter.GetResult();
                                _awaiter = new TaskAwaiter<T>();
                                _return = this.m_Return;
                            }
                            catch (Exception exception)
                            {
                                this.m_State = -2;
                                this.m_Builder.SetException(exception);
                                return;
                            }
                            this.m_State = -2;
                            this.m_Builder.SetResult(_return);
                        }

                        void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
                        {
                        }
                    }
                }
            }
        }
    }
}
