using System;
using System.Threading;
using System.Collections.Generic;

namespace ProtoBuf
{
    /// <summary>
    /// enhance buffer management
    /// minimize runtime memory allocation
    /// </summary>
    public sealed class BufferPoolEx
    {
        public const int alignment = 64; //< assert(alignment % 2 == 0)
        public const int shift = 6; //< pow(2, 6) == 64
        public const int maxSize = 64 * 128; //< 8192
        public const int incBlocks = 16;
        public static readonly List<byte[]>[] pool = new List<byte[]>[128];

        static BufferPoolEx()
        {
            for (int i = 0; i < pool.Length; ++i)
                pool[i] = new List<byte[]>();
        }

        private BufferPoolEx() { }

        private static int Align(int size, int align)
        {
            return (size + (align - 1)) & (~(align - 1));
        }

        private static int AvailableSlots(List<byte[]> list)
        {
            int slots = 0;
            for (; slots < list.Count && null != list[slots]; ++slots) ;
            return slots;
        }

        private static void Swap(List<byte[]> list, int i, int j)
        {
            byte[] tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }

        private static int EnsureBlocks(List<byte[]> list, int size, int blocks = 1)
        {
            int slots = AvailableSlots(list);
            if (slots < blocks)
            {
                int inc = Align(blocks, incBlocks);
                list.Capacity += inc;
                for (int i = 0; i < inc; ++i)
                {
                    list.Add(new byte[size]);
                    Swap(list, slots++, list.Count - 1);
                }
            }
            return slots - 1;
        }

        /// <summary>
        /// allow user to manually expand pool capacity in advance to avoid runtime allocation
        /// </summary>
        /// <param name="blocks">blocks per size list</param>
        public static void EnsureCapacity(int blocks)
        {
            for (int i = 0; i < pool.Length; ++i)
            {
                List<byte[]> list = pool[i];
                lock (list)
                {
                    if (list.Count < blocks)
                    {
                        int size = (i + 1) << shift;
                        int slots = AvailableSlots(list);
                        int inc = Align(blocks - list.Count, incBlocks);
                        list.Capacity += inc;
                        for (int j = 0; j < inc; ++j)
                        {
                            list.Add(new byte[size]);
                            Swap(list, slots++, list.Count - 1);
                        }

                    }
                }
            }
        }

        public static void Flush()
        {
            for (int i = 0; i < pool.Length; ++i)
            {
                List<byte[]> list = pool[i];
                lock (list)
                    list.Clear();
            }
        }

        public static byte[] GetBuffer(int size = 0)
        {
            if (0 == size)
            {
                size = alignment;
            }
            else
            {
                size = Align(size, alignment);
                Helpers.DebugAssert(size <= maxSize);
            }

            byte[] buffer;
            List<byte[]> list = pool[(size >> shift) - 1];
            lock (list)
            {
                int i = EnsureBlocks(list, size);
                buffer = list[i];
                list[i] = null;
            }
            return buffer;
        }

        public static void ResizeAndFlushLeft(ref byte[] buffer, int toFitAtLeastBytes, int copyFromIndex, int copyBytes)
        {
            Helpers.DebugAssert(buffer != null);
            Helpers.DebugAssert(toFitAtLeastBytes > buffer.Length);
            Helpers.DebugAssert(copyFromIndex >= 0);
            Helpers.DebugAssert(copyBytes >= 0);

            byte[] newBuffer = GetBuffer(toFitAtLeastBytes);
            if (copyBytes > 0)
                Helpers.BlockCopy(buffer, copyFromIndex, newBuffer, 0, copyBytes);
            ReleaseBufferToPool(ref buffer);
            buffer = newBuffer;
        }

        public static void ReleaseBufferToPool(ref byte[] buffer)
        {
            if (null == buffer ||
                (buffer.Length & (alignment - 1)) != 0
                || buffer.Length > maxSize)
            {
                return;
            }

            int index = (buffer.Length >> shift) - 1;
            List<byte[]> list = pool[index];
            lock (list)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    if (null == list[i])
                    {
                        list[i] = buffer;
                        buffer = null;
                        break;
                    }
                }
            }
        }
    }
}
