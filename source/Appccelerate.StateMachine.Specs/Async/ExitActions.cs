// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExitActions.cs" company="Appccelerate">
//   Copyright (c) 2008-2017 Appccelerate
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Appccelerate.StateMachine.Async
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using FluentAssertions;

    using Xbehave;

    public class ExitActions
    {
        private const int State = 1;
        private const int AnotherState = 2;
        private const int Event = 2;

        [Scenario]
        public void ExitAction(
            AsyncPassiveStateMachine<int, int> machine,
            bool exitActionExecuted,
            bool asyncExitActionExecuted)
        {
            "establish a state machine with exit action on a state"._(() =>
                {
                    machine = new AsyncPassiveStateMachine<int, int>();

                    machine.In(State)
                        .ExecuteOnExit(() => exitActionExecuted = true)
                        .ExecuteOnExit(async () =>
                        {
                            asyncExitActionExecuted = true;
                            await Task.Yield();
                        })
                        .On(Event).Goto(AnotherState);
                });

            "when leaving the state"._(async () =>
                {
                    machine.Initialize(State);
                    await machine.Start();
                    await machine.Fire(Event);
                });

            "it should execute the synchronous exit action"._(()
                => exitActionExecuted.Should().BeTrue());

            "it should execute the asynchronous exit action"._(()
                => asyncExitActionExecuted.Should().BeTrue());
        }

        [Scenario]
        public void ExitActionWithParameter(
            AsyncPassiveStateMachine<int, int> machine,
            string receivedParameter,
            string asyncReceivedParameter)
        {
            const string parameter = "parameter";

            "establish a state machine with exit action with parameter on a state"._(() =>
                {
                    machine = new AsyncPassiveStateMachine<int, int>();

                    machine.In(State)
                        .ExecuteOnExitParametrized(p => receivedParameter = p, parameter)
                        .ExecuteOnExitParametrized(
                            async p =>
                            {
                                asyncReceivedParameter = p;
                                await Task.Yield();
                            },
                            parameter)
                        .On(Event).Goto(AnotherState);
                });

            "when leaving the state"._(async () =>
                {
                    machine.Initialize(State);
                    await machine.Start();
                    await machine.Fire(Event);
                });

            "it should execute the synchronous exit action"._(() =>
                receivedParameter.Should().NotBeNull());

            "it should pass parameter to the synchronous exit action"._(() =>
                receivedParameter.Should().Be(parameter));

            "it should execute the asynchronous exit action"._(() =>
                asyncReceivedParameter.Should().NotBeNull());

            "it should pass parameter to the asynchronous exit action"._(() =>
                asyncReceivedParameter.Should().Be(parameter));
        }

        [Scenario]
        public void MultipleExitActions(
            AsyncPassiveStateMachine<int, int> machine,
            bool exitAction1Executed,
            bool exitAction2Executed,
            bool asyncExitAction1Executed,
            bool asyncExitAction2Executed)
        {
            "establish a state machine with several exit actions on a state"._(() =>
                {
                    machine = new AsyncPassiveStateMachine<int, int>();

                    machine.In(State)
                        .ExecuteOnExit(() => exitAction1Executed = true)
                        .ExecuteOnExit(() => exitAction2Executed = true)
                        .ExecuteOnExit(async () =>
                        {
                            asyncExitAction1Executed = true;
                            await Task.Yield();
                        })
                        .ExecuteOnExit(async () =>
                        {
                            asyncExitAction2Executed = true;
                            await Task.Yield();
                        })
                        .On(Event).Goto(AnotherState);
                });

            "when leaving the state"._(async () =>
                {
                    machine.Initialize(State);
                    await machine.Start();
                    await machine.Fire(Event);
                });

            "it should execute all exit actions"._(()
                => new[]
                {
                    exitAction1Executed,
                    exitAction2Executed,
                    asyncExitAction1Executed,
                    asyncExitAction2Executed
                }.Should().Equal(true, true, true, true));
        }

        [Scenario]
        public void ExceptionHandling(
            AsyncPassiveStateMachine<int, int> machine,
            bool exitAction1Executed,
            bool exitAction2Executed,
            bool exitAction3Executed,
            bool exitAction4Executed)
        {
            var exception2 = new Exception();
            var exception3 = new Exception();
            var exception4 = new Exception();
            var receivedException = new List<Exception>();

            "establish a state machine with several exit actions on a state and some of them throw an exception"._(() =>
                {
                    machine = new AsyncPassiveStateMachine<int, int>();

                    machine.In(State)
                        .ExecuteOnExit(() => exitAction1Executed = true)
                        .ExecuteOnExit(() =>
                            {
                                exitAction2Executed = true;
                                throw exception2;
                            })
                        .ExecuteOnExit(() =>
                            {
                                exitAction3Executed = true;
                                throw exception3;
                            })
                        .ExecuteOnExit(async () =>
                        {
                            exitAction4Executed = true;
                            await Task.Yield();
                            throw exception4;
                        })
                        .On(Event).Goto(AnotherState);

                    machine.TransitionExceptionThrown += (s, e) => receivedException.Add(e.Exception);
                });

            "when entering the state"._(async () =>
                {
                    machine.Initialize(State);
                    await machine.Start();
                    await machine.Fire(Event);
                });

            "it should execute all entry actions on entry"._(()
                => new[]
                {
                    exitAction1Executed,
                    exitAction2Executed,
                    exitAction3Executed,
                    exitAction4Executed
                }.Should().Equal(true, true, true, true));

            "it should handle all exceptions of all throwing entry actions by firing the TransitionExceptionThrown event"._(() =>
                receivedException
                    .Should().BeEquivalentTo(exception2, exception3, exception4));
        }

        [Scenario]
        public void EventArgument(
            AsyncPassiveStateMachine<int, int> machine,
            int passedArgument)
        {
            const int argument = 17;

            "establish a state machine with an exit action taking an event argument"._(() =>
                {
                    machine = new AsyncPassiveStateMachine<int, int>();

                    machine.In(State)
                        .ExecuteOnExit((int a) => passedArgument = a)
                        .On(Event).Goto(AnotherState);

                    machine.In(AnotherState)
                        .ExecuteOnEntry((int a) => passedArgument = a);
                });

            "when leaving the state"._(async () =>
                {
                    machine.Initialize(State);
                    await machine.Start();
                    await machine.Fire(Event, argument);
                });

            "it should pass event argument to exit action"._(() =>
                passedArgument.Should().Be(argument));
        }
    }
}