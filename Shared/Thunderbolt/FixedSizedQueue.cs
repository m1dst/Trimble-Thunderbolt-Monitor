using System;
using Microsoft.SPOT;
using System.Collections;

namespace TrimbleMonitor.Thunderbolt
{
    public class FixedSizedQueue : Queue
    {
        public int Size { get; private set; }

        public FixedSizedQueue(int size)
        {
            Size = size;
        }

        public new void Enqueue(object obj)
        {
            base.Enqueue(obj);
            lock (this)
            {
                while (base.Count > Size)
                {
                    base.Dequeue();
                }
            }
        }

    }
}
