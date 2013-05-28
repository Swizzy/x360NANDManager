namespace x360NANDManager {
    using System;

    public sealed class EventArg<T> : EventArgs {
        private readonly T _data;

        internal EventArg(T data) {
            _data = data;
        }

        public T Data {
            get { return _data; }
        }
    }

    public sealed class ProgressData {
        public double Percentage;
        public long Current;
        public long Maximum;
    }
}