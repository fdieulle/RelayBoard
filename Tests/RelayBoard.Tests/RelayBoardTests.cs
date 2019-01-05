using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using RelayBoard.Core;

namespace RelayBoard.Tests
{
    public class RelayInputMock : IRelayInput
    {
        private readonly List<Action> _subscriptions = new List<Action>();

        #region Implementation of IRelayInput

        public string Name { get; }
        public IDisposable Subscribe(Action onTick)
        {
            _subscriptions.Add(onTick);
            return new AnonymousDisposable(() => { _subscriptions.Remove(onTick); });
        }

        #endregion

        public RelayInputMock(string name)
        {
            Name = name;
        }

        public void Notify()
        {
            foreach (var subscription in _subscriptions)
                subscription();
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

        public bool IsInvalidated => _pulseProbe->IsOn;

        public void Reset() => _pulseProbe->SetOff();

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

            output.Check(false);

            input.Notify();
            output.Check(true);

            output.Reset();
            output.Check(false);

            input.Notify();
            output.Check(true);

            input.Notify();
            output.Check(true);

            output.Reset();
            output.Check(false);

            output.Reset();
            output.Check(false);

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
                outputs[i].Check(false);

            input.Notify();
            for (var i = 0; i < nbSusbscriber; i++)
                outputs[i].Check(true);

            for (var i = 0; i < nbSusbscriber; i++)
            {
                outputs[i].Reset();
                outputs[i].Check(false);
            }

            input.Notify();
            for (var i = 0; i < nbSusbscriber; i++)
                outputs[i].Check(true);

            input.Notify();
            for (var i = 0; i < nbSusbscriber; i++)
                outputs[i].Check(true);

            for (var i = 0; i < nbSusbscriber; i++)
            {
                outputs[i].Reset();
                outputs[i].Check(false);
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

            output.Check(false);

            for (var i = 0; i < inputs.Length; i++)
            {
                inputs[i].Notify();
                output.Check(true);

                output.Reset();
                output.Check(false);
            }

            for (var i = 0; i < inputs.Length; i++)
                inputs[i].Notify();
            output.Check(true);

            output.Reset();
            output.Check(false);

            for (var i = 0; i < inputs.Length; i++)
            {
                inputs[i].Notify();
                output.Check(true);

                output.Reset();
                output.Check(false);
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
                outputs[i].Check(false);

            for (var j = 0; j < inputs.Length; j++)
            {
                inputs[j].Notify();
                for (var i = 0; i < outputs.Length; i++)
                {
                    outputs[i].Check(i >= j);
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
                        inputs[j].Notify();
                        stack.Push(inputs[j].Name);
                    }
                }

                while (stack.Count > 0)
                {
                    var deps = dependencies[stack.Pop()];
                    foreach (var dep in deps)
                    {
                        if (hash.Contains(dep)) continue;
                        outByName[dep].Check(true);
                        timestamps[dep] = now;
                        hash.Add(dep);
                    }
                }

                for (var j = 0; j < outputs.Length; j++)
                {
                    if (!hash.Contains(outputs[i].Name))
                        outputs[i].Check(false);
                    outputs[j].Reset();
                }

                hash.Clear();
            }
        }

        [TestCase(750, 5000, 0.3)]
        [TestCase(750, 5000, 0.7)]
        [TestCase(750, 1000, 0.99)]
        [TestCase(50, 5000, 0.5)]
        public void Benchmark(int nbInputs, int nbOutputs, double percentOfObyI)
        {
            var random = new Random(42);

            var inputs = new RelayInputMock[nbInputs];
            for (var i = 0; i < inputs.Length; i++)
                inputs[i] = new RelayInputMock("Feed" + (i + 1));

            var outputs = new RelayOutputMock[nbOutputs];
            for (var i = 0; i < outputs.Length; i++)
                outputs[i] = new RelayOutputMock("S" + (i + 1));

            var manager = new RelayBoard();
            for (var i = 0; i < inputs.Length; i++)
            {
                var list = new List<string>();
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

            var iterations = 1500;
            var percentOfNotif = 0.77;

            var stack = new Stack<string>();

            var randoms = new double[iterations * inputs.Length];
            for (var i = 0; i < iterations; i++)
                randoms[i] = random.NextDouble();

            var jitterIterations = (int)(iterations * 0.1);
            for (int i = 0, k = 0; i < jitterIterations; i++)
            {
                for (var j = 0; j < inputs.Length; j++)
                {
                    if (randoms[k++] <= percentOfNotif)
                    {
                        inputs[j].Notify();
                        stack.Push(inputs[j].Name);
                    }
                }

                for (var j = 0; j < outputs.Length; j++)
                {
                    if(outputs[j].IsInvalidated)
                        outputs[j].Reset();
                }
            }

            var sw = Stopwatch.StartNew();
            for (int i = 0, k = 0; i < iterations; i++)
            {
                for (var j = 0; j < inputs.Length; j++)
                {
                    if (randoms[k++] <= percentOfNotif)
                    {
                        inputs[j].Notify();
                        stack.Push(inputs[j].Name);
                    }
                }

                for (var j = 0; j < outputs.Length; j++)
                {
                    if (outputs[j].IsInvalidated)
                        outputs[j].Reset();
                }
            }
            sw.Stop();
            Console.WriteLine("Elapsed: {0} ms", sw.Elapsed.TotalMilliseconds);
        }

        [Test]
        public void UnsubcribeCallbackSingleInputManyOutput()
        {
            var board = new RelayBoard();

            var i1 = new RelayInputMock("I1");
            var o1 = new RelayOutputMock("O1");
            var o2 = new RelayOutputMock("O2");

            var c1 = board.Connect(i1, o1)
                .Subscribe(p => { });
            var c2 = board.Connect(i1, o2)
                .Subscribe(p => { });
            board.Initialize();
            Console.WriteLine(board.Report());

            o1.Check(false);
            o2.Check(false);

            i1.Notify();
            o1.Check(true);
            o2.Check(true);

            o1.Reset();
            o2.Reset();
            o1.Check(false);
            o2.Check(false);

            c2.Dispose();
            Console.WriteLine(board.Report());

            i1.Notify();
            o1.Check(true);
            o2.Check(true);

            board.Dispose();
        }

        [Test]
        public void UnsubcribeConnectorSingleInputManyOutput()
        {
            var board = new RelayBoard();

            var i1 = new RelayInputMock("I1");
            var o1 = new RelayOutputMock("O1");
            var o2 = new RelayOutputMock("O2");

            var c1 = board.Connect(i1, o1);
            c1.Subscribe(p => { });
            var c2 = board.Connect(i1, o2);
            c2.Subscribe(p => { });

            board.Initialize();
            Console.WriteLine(board.Report());

            o1.Check(false);
            o2.Check(false);

            i1.Notify();
            o1.Check(true);
            o2.Check(true);

            o1.Reset();
            o2.Reset();
            o1.Check(false);
            o2.Check(false);

            c2.Dispose();
            Console.WriteLine(board.Report());

            i1.Notify();
            o1.Check(true);
            o2.Check(false);

            board.Dispose();
        }

        [Test]
        public void UnsubcribeCallbackManyInputManyOutput()
        {
            var board = new RelayBoard();

            var i1 = new RelayInputMock("I1");
            var i2 = new RelayInputMock("I2");
            var o1 = new RelayOutputMock("O1");
            var o2 = new RelayOutputMock("O2");

            var c1 = board.Connect(i1, o1)
                .Subscribe(p => { });
            var c2 = board.Connect(i2, o2)
                .Subscribe(p => { });
            board.Initialize();

            o1.Check(false);
            o2.Check(false);

            i1.Notify();
            o1.Check(true);
            i2.Notify();
            o2.Check(true);

            o1.Reset();
            o2.Reset();
            o1.Check(false);
            o2.Check(false);

            c2.Dispose();

            i1.Notify();
            o1.Check(true);
            i2.Notify();
            o2.Check(true);

            board.Dispose();
        }

        [Test]
        public void UnsubcribeConnectorManyInputManyOutput()
        {
            var board = new RelayBoard();

            var i1 = new RelayInputMock("I1");
            var i2 = new RelayInputMock("I2");
            var o1 = new RelayOutputMock("O1");
            var o2 = new RelayOutputMock("O2");

            var c1 = board.Connect(i1, o1)
                .Subscribe(p => { });
            var c2 = board.Connect(i2, o2);
            c2.Subscribe(p => { });
            board.Initialize();

            o1.Check(false);
            o2.Check(false);

            i1.Notify();
            o1.Check(true);
            i2.Notify();
            o2.Check(true);

            o1.Reset();
            o2.Reset();
            o1.Check(false);
            o2.Check(false);

            c2.Dispose();

            i1.Notify();
            o1.Check(true);
            i2.Notify();
            o2.Check(false);

            board.Dispose();
        }

        [Test]
        public void UnsubcribeConnectorManyInputSingleOutput()
        {
            var board = new RelayBoard();

            var i1 = new RelayInputMock("I1");
            var i2 = new RelayInputMock("I2");
            var o1 = new RelayOutputMock("O1");

            var c1 = board.Connect(i1, o1)
                .Subscribe(p => { });
            var c2 = board.Connect(i2, o1);
            c2.Subscribe(p => { });
            board.Initialize();

            o1.Check(false);

            i1.Notify();
            o1.Check(true);

            o1.Reset();
            o1.Check(false);

            i2.Notify();
            o1.Check(true);

            o1.Reset();
            o1.Check(false);

            c2.Dispose();

            i1.Notify();
            o1.Check(true);

            o1.Reset();
            o1.Check(false);

            i2.Notify();
            o1.Check(false);

            board.Dispose();
        }

        [Test]
        public void UnsubcribeCallbackManyInputSingleOutput()
        {
            var board = new RelayBoard();

            var i1 = new RelayInputMock("I1");
            var i2 = new RelayInputMock("I2");
            var o1 = new RelayOutputMock("O1");

            var c1 = board.Connect(i1, o1)
                .Subscribe(p => { });
            var c2 = board.Connect(i2, o1)
                .Subscribe(p => { });
            board.Initialize();

            o1.Check(false);

            i1.Notify();
            o1.Check(true);

            o1.Reset();
            o1.Check(false);

            i2.Notify();
            o1.Check(true);

            o1.Reset();
            o1.Check(false);

            c2.Dispose();

            i1.Notify();
            o1.Check(true);

            o1.Reset();
            o1.Check(false);

            i2.Notify();
            o1.Check(true);

            board.Dispose();
        }

        [Test]
        public void UnsubcribeUseCaseFailed()
        {
            var board = new RelayBoard();

            var i1 = new RelayInputMock("I1");
            var i2 = new RelayInputMock("I2");
            var i3 = new RelayInputMock("I3");
            var i4 = new RelayInputMock("I4");

            var o1 = new RelayOutputMock("O1");

            var c1 = board.Connect(i1, o1);
            var s1 = c1.Subscribe(p => { });
            var c2 = board.Connect(i2, o1);
            var s2 = c2.Subscribe(p => { });
            var c3 = board.Connect(i3, o1);
            var s3 = c3.Subscribe(p => { });
            var c4 = board.Connect(i4, o1);
            var s4 = c4.Subscribe(p => { });
            
            board.Initialize();

            o1.Check(false);

            i1.Notify();
            board.Poll(DateTime.Now);
            o1.CheckAndReset(true);

            i1.Notify();
            board.Poll(DateTime.Now);
            o1.CheckAndReset(true);

            i1.Notify();
            i2.Notify();
            i4.Notify();
            board.Poll(DateTime.Now);
            o1.CheckAndReset(true);

            i4.Notify();
            board.Poll(DateTime.Now);
            o1.CheckAndReset(true);

            c2.Dispose();
            s2.Dispose();

            i4.Notify();
            i1.Notify();
            board.Poll(DateTime.Now);
            o1.CheckAndReset(true);

            board.Dispose();
        }
    }
}
