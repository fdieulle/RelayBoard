﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RelayBoard.Core;

namespace RelayBoard.Net.Tests
{
    public class RelayInputMock : IRelayInput
    {
        private readonly List<Action<DateTime>> _subscriptions = new List<Action<DateTime>>();
        #region Implementation of IRelayInput

        public string Name { get; }
        public IDisposable Subscribe(Action<DateTime> onTick)
        {
            _subscriptions.Add(onTick);
            return new AnonymousDisposable(() => { _subscriptions.Remove(onTick); });
        }

        #endregion

        public RelayInputMock(string name)
        {
            Name = name;
        }

        public void Notify(DateTime timestamp)
        {
            foreach (var subscription in _subscriptions)
                subscription(timestamp);
        }

        #region Overrides of Object

        public override string ToString()
        {
            return Name;
        }

        #endregion
    }

    public unsafe class RelayOutputMock : IRelayOutput
    {
        private readonly IRelayOutput[] _dependencies;
        private PulseProbe* _pulseProbe;


        public RelayOutputMock(string name, params IRelayOutput[] dependencies)
        {
            _dependencies = dependencies;
            Name = name;
        }

        #region Implementation of IRelayOutput

        public string Name { get; }

        public IEnumerable<IRelayOutput> GetDependencies()
        {
            return _dependencies;
        }

        public void Inject(PulseProbe* state)
        {
            _pulseProbe = state;
        }

        #endregion

        public bool IsInvalidated => _pulseProbe->IsFlaged;

        public DateTime LastUpdateTimestamp => _pulseProbe->LastTimestamp;

        public void Reset() => _pulseProbe->Reset();

        #region Base implem

        private bool _isInvalidated;
        private DateTime _lastTimestamp;

        public bool IsInvalidatedBase => _isInvalidated;

        public DateTime LastUpdateTimestampBase => _lastTimestamp;

        public void Invalidate(DateTime timestamp)
        {
            _isInvalidated = true;
            _lastTimestamp = timestamp;
        }

        public void ResetBase()
        {
            _isInvalidated = false;
        }

        #endregion

        #region Overrides of Object

        public override string ToString()
        {
            return Name;
        }

        #endregion
    }

    [TestFixture]
    public class RelayBoardTests
    {
        [Test]
        public void SingleInputSingleOutput()
        {
            var board = new RelayBoard();

            var input = new RelayInputMock("I");
            var output = new RelayOutputMock("O");

            board.Connect(input, output);
            board.Initialize();

            output.Check(false, DateTime.MinValue);

            var now = DateTime.Now;

            input.Notify(now);
            output.Check(true, now);

            output.Reset();
            output.Check(false, now);

            now = now.AddSeconds(1);
            input.Notify(now);
            output.Check(true, now);

            now = now.AddSeconds(1);
            input.Notify(now);
            output.Check(true, now);

            output.Reset();
            output.Check(false, now);

            output.Reset();
            output.Check(false, now);

            board.Dispose();
        }

        [Test]
        public void SingleInputManyOutputs()
        {
            const int nbSusbscriber = 10;

            var board = new RelayBoard();

            var input = new RelayInputMock("I");
            var outputs = new RelayOutputMock[nbSusbscriber];
            for (var i = 0; i < nbSusbscriber; i++)
                outputs[i] = new RelayOutputMock("O" + (i + 1));

            for (var i = 0; i < nbSusbscriber; i++)
                board.Connect(input, outputs[i]);
            board.Initialize();

            for (var i = 0; i < nbSusbscriber; i++)
                outputs[i].Check(false, DateTime.MinValue);

            var now = DateTime.Now;

            input.Notify(now);
            for (var i = 0; i < nbSusbscriber; i++)
                outputs[i].Check(true, now);

            for (var i = 0; i < nbSusbscriber; i++)
            {
                outputs[i].Reset();
                outputs[i].Check(false, now);
            }

            now = now.AddSeconds(1);
            input.Notify(now);
            for (var i = 0; i < nbSusbscriber; i++)
                outputs[i].Check(true, now);

            now = now.AddSeconds(1);
            input.Notify(now);
            for (var i = 0; i < nbSusbscriber; i++)
                outputs[i].Check(true, now);

            for (var i = 0; i < nbSusbscriber; i++)
            {
                outputs[i].Reset();
                outputs[i].Check(false, now);
            }

            board.Dispose();
        }

        [Test]
        public void ManyInputsSingleOutput()
        {
            const int nbInputs = 10;

            var board = new RelayBoard();
            var output = new RelayOutputMock("O");

            var inputs = new RelayInputMock[nbInputs];
            for (var i = 0; i < nbInputs; i++)
            {
                inputs[i] = new RelayInputMock("I" + (i + 1));
                board.Connect(inputs[i], output);
            }

            board.Initialize();

            output.Check(false, DateTime.MinValue);

            var now = DateTime.Now;

            for (var i = 0; i < inputs.Length; i++)
            {
                inputs[i].Notify(now);
                output.Check(true, now);

                output.Reset();
                output.Check(false, now);
            }

            for (var i = 0; i < inputs.Length; i++)
                inputs[i].Notify(now);
            output.Check(true, now);

            output.Reset();
            output.Check(false, now);

            for (var i = 0; i < inputs.Length; i++)
            {
                inputs[i].Notify(now);
                output.Check(true, now);

                output.Reset();
                output.Check(false, now);
            }
        }

        [Test]
        public void ManyInputsManyOutputs()
        {
            const int nbInputs = 10;
            const int nbOutputs = 10;

            var board = new RelayBoard();

            var inputs = new RelayInputMock[nbInputs];
            var outputs = new RelayOutputMock[nbOutputs];

            for (var i = 0; i < outputs.Length; i++)
                outputs[i] = new RelayOutputMock("S" + (i + 1));

            for (var i = 0; i < inputs.Length; i++)
                inputs[i] = new RelayInputMock("F" + (i + 1));

            for (var i = 0; i < outputs.Length; i++)
                for (var j = 0; j <= i && j < inputs.Length; j++)
                    board.Connect(inputs[j], outputs[i]);

            board.Initialize();

            // Check if all subscribers are invalid first
            for (var i = 0; i < outputs.Length; i++)
                outputs[i].Check(false, DateTime.MinValue);

            var now = DateTime.Now;
            for (var j = 0; j < inputs.Length; j++)
            {
                inputs[j].Notify(now.AddSeconds(j));
                for (var i = 0; i < outputs.Length; i++)
                {
                    if (i >= j) outputs[i].Check(true, now.AddSeconds(j));
                    else outputs[i].Check(false, now.AddSeconds(i));

                    outputs[i].Reset();
                }
            }
        }

        [TestCase(750, 5000, 0.3)]
        [TestCase(750, 5000, 0.7)]
        [TestCase(750, 1000, 0.99)]
        [TestCase(50, 5000, 0.5)]
        public void RandomSetup(int nbInputs, int nbOutputs, double percentOfObyI)
        {
            var random = new Random(42);

            var inputs = new RelayInputMock[nbInputs];
            for (var i = 0; i < inputs.Length; i++)
                inputs[i] = new RelayInputMock("Feed" + (i + 1));

            var outputs = new RelayOutputMock[nbOutputs];
            for (var i = 0; i < outputs.Length; i++)
                outputs[i] = new RelayOutputMock("S" + (i + 1));

            var dependencies = new Dictionary<string, List<string>>();
            var manager = new RelayBoard();
            for (var i = 0; i < inputs.Length; i++)
            {
                var list = new List<string>();
                dependencies[inputs[i].Name] = list;
                for (var j = 0; j < outputs.Length; j++)
                {
                    if (random.NextDouble() <= percentOfObyI)
                    {
                        manager.Connect(inputs[i], outputs[j]);
                        list.Add(outputs[j].Name);
                    }
                }
            }

            manager.Initialize();
            Console.WriteLine(manager.Report());

            var now = DateTime.Now;
            var iterations = 150;
            var percentOfNotif = 0.77;
            var outByName = outputs.ToDictionary(p => p.Name, p => p);

            var hash = new HashSet<string>();
            var stack = new Stack<string>();
            var timestamps = outputs.ToDictionary(p => p.Name, p => DateTime.MinValue);

            for (var i = 0; i < iterations; i++)
            {
                now = now.AddSeconds(i);
                for (var j = 0; j < inputs.Length; j++)
                {
                    if (random.NextDouble() <= percentOfNotif)
                    {
                        inputs[j].Notify(now);
                        stack.Push(inputs[j].Name);
                    }
                }

                while (stack.Count > 0)
                {
                    var deps = dependencies[stack.Pop()];
                    foreach (var dep in deps)
                    {
                        if (hash.Contains(dep)) continue;
                        outByName[dep].Check(true, now);
                        timestamps[dep] = now;
                        hash.Add(dep);
                    }
                }

                for (var j = 0; j < outputs.Length; j++)
                {
                    if (!hash.Contains(outputs[i].Name))
                        outputs[i].Check(false, timestamps[outputs[i].Name]);
                    outputs[j].Reset();
                }

                hash.Clear();
            }
        }
    }
}