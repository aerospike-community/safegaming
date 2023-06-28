using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayerGeneration
{
    public sealed class Progression : IDisposable
    {
        private bool disposedValue;

        public Progression(ConsoleDisplay consoleDisplay, string tag, object task)
        {
            this.ConsoleDisplay = consoleDisplay;
            this.Tag = tag;
            this.Task = task;

            this.ConsoleDisplay?.Increment(Tag, Task);
        }

        public Progression(Progression progression, object task)
        {
            this.ConsoleDisplay = progression.ConsoleDisplay;
            this.Tag = progression.Tag;
            this.Task = task;

            this.ConsoleDisplay?.Increment(Tag, Task);
        }

        public string Tag { get; }
        public object Task { get; }
        public ConsoleDisplay ConsoleDisplay { get; }

        public int Incremental(object task = null)
        {
            return this.ConsoleDisplay?.Increment(Tag, task) ?? 0;
        }

        public int Decrement()
        {
            return this.ConsoleDisplay?.Decrement(Tag) ?? 0;
        }

        public void Write(string message)
        {
            this.ConsoleDisplay?.Write(message, this.Tag);
        }

        public void End()
        {
            this.ConsoleDisplay?.TaskEnd(Tag);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.ConsoleDisplay?.Decrement(Tag);
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }


}
