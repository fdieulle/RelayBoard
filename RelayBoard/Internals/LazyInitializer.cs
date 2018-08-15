using System;
using RelayBoard.Core;

namespace RelayBoard.Internals
{
    public class LazyInitializer
    {
        private readonly Action _initialize;
        private int _deep;

        public LazyInitializer(Action initialize)
        {
            _initialize = initialize;
        }

        public IDisposable Initialize()
        {
            _deep++;
            return new AnonymousDisposable(() =>
            {
                if (--_deep > 0) return;
                _deep = 0;
                _initialize();
            });
        }

        public void ForceInitialize()
        {
            _initialize();
        }
    }
}