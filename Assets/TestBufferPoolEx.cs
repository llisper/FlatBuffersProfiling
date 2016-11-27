using UnityEngine;
using System.Collections.Generic;
using ProtoBuf;

public class TestBufferPoolEx : MonoBehaviour
{
    void Start()
    {
        Debug.Assert(BufferPoolEx.pool.Length == 128);
        EnsureCapacityTest(1, 16);
        EnsureCapacityTest(16, 16);
        EnsureCapacityTest(17, 32);
        BufferPoolEx.Flush();
        EnsureCapacityTest(1, 16);
        GetBufferTest(0);
        GetBufferTest(1);
        GetBufferTest(64);
        GetBufferTest(65);
        BufferPoolEx.Flush();
        EnsureCapacityTest(1, 16);
        for (int i = 0; i < BufferPoolEx.pool.Length; ++i)
            OverGetBufferTest((i + 1) << BufferPoolEx.shift);
        ReleaseTest();
        BufferPoolEx.Flush();
        EnsureCapacityTest(1, 16);
        ResizeTest();
        Debug.Log("<color=green>ALL GOOD!</color>");
    }

    void EnsureCapacityTest(int size, int expect)
    {
        BufferPoolEx.EnsureCapacity(size);
        for (int i = 0; i < BufferPoolEx.pool.Length; ++i)
        {
            var pool = BufferPoolEx.pool[i];
            Debug.Assert(pool.Count == expect);
            for (int j = 0; j < pool.Count; ++j)
            {
                byte[] bytes = pool[j];
                Debug.Assert(bytes != null);
                Debug.Assert(bytes.Length == ((i + 1) << BufferPoolEx.shift));
            }
        }
    }

    void GetBufferTest(int size)
    {
        int alignSize = BufferPoolEx.alignment;
        if (size > 0)
            alignSize = (size + (BufferPoolEx.alignment - 1)) & (~(BufferPoolEx.alignment - 1));
        var pool = BufferPoolEx.pool[(alignSize >> BufferPoolEx.shift) - 1];
        int availSlots = AvailableSlots(pool);
        byte[] buffer = BufferPoolEx.GetBuffer(size);
        Debug.Assert(buffer.Length == alignSize);
        AvailableAssert(pool, availSlots - 1);

        BufferPoolEx.ReleaseBufferToPool(ref buffer);
        AvailableAssert(pool, availSlots);
        Debug.Assert(buffer == null);

    }

    void OverGetBufferTest(int size)
    {
        int alignSize = BufferPoolEx.alignment;
        if (size > 0)
            alignSize = (size + (BufferPoolEx.alignment - 1)) & (~(BufferPoolEx.alignment - 1));
        var pool = BufferPoolEx.pool[(alignSize >> BufferPoolEx.shift) - 1];
        int availSlots = AvailableSlots(pool);
        List<byte[]> buffers = new List<byte[]>(availSlots + 1);
        for (int i = 0; i < availSlots + 1; ++i)
            buffers.Add(BufferPoolEx.GetBuffer(size));
        Debug.Assert(pool.Count == availSlots + BufferPoolEx.incBlocks);
        AvailableAssert(pool, availSlots - 1);

        for (int i = 0; i < buffers.Count; ++i)
        {
            byte[] buffer = buffers[i];
            BufferPoolEx.ReleaseBufferToPool(ref buffer);
        }
        buffers.Clear();
        AvailableAssert(pool, availSlots + BufferPoolEx.incBlocks);
    }

    void ReleaseTest()
    {
        int[] avails = new int[BufferPoolEx.pool.Length];
        int[] counts = new int[BufferPoolEx.pool.Length];
        for (int i = 0; i < BufferPoolEx.pool.Length; ++i)
        {
            var pool = BufferPoolEx.pool[i];
            avails[i] = AvailableSlots(pool);
            counts[i] = BufferPoolEx.pool[i].Count;
        }

        byte[] none = new byte[64];
        BufferPoolEx.ReleaseBufferToPool(ref none);
        Debug.Assert(null != none);
        for (int i = 0; i < BufferPoolEx.pool.Length; ++i)
        {
            var pool = BufferPoolEx.pool[i];
            Debug.Assert(pool.Count == counts[i]);
            AvailableAssert(pool, avails[i]);
        }

        byte[] overflow = new byte[BufferPoolEx.maxSize + 64];
        BufferPoolEx.ReleaseBufferToPool(ref overflow);
        Debug.Assert(null != overflow);
        for (int i = 0; i < BufferPoolEx.pool.Length; ++i)
        {
            var pool = BufferPoolEx.pool[i];
            Debug.Assert(pool.Count == counts[i]);
            AvailableAssert(pool, avails[i]);
        }

        byte[] wrongsize = new byte[32];
        BufferPoolEx.ReleaseBufferToPool(ref wrongsize);
        Debug.Assert(null != wrongsize);
        for (int i = 0; i < BufferPoolEx.pool.Length; ++i)
        {
            var pool = BufferPoolEx.pool[i];
            Debug.Assert(pool.Count == counts[i]);
            AvailableAssert(pool, avails[i]);
        }
    }

    void ResizeTest()
    {
        byte[] buffer = BufferPoolEx.GetBuffer();
        for (int i = 1; i < BufferPoolEx.pool.Length; ++i)
        {
            var prev = BufferPoolEx.pool[i - 1];
            AvailableAssert(prev, 15);
            var pool = BufferPoolEx.pool[i];
            BufferPoolEx.ResizeAndFlushLeft(ref buffer, buffer.Length + 1, 0, buffer.Length);
            Debug.Assert(null != buffer);
            Debug.Assert(buffer.Length == ((i + 1) << BufferPoolEx.shift));
            AvailableAssert(prev, 16);
            AvailableAssert(pool, 15);
        }
        BufferPoolEx.ReleaseBufferToPool(ref buffer);
        Debug.Assert(null == buffer);
        AvailableAssert(BufferPoolEx.pool[BufferPoolEx.pool.Length - 1], 16);
    }

    void AvailableAssert(List<byte[]> pool, int expect)
    {
        int avail = AvailableSlots(pool);
        Debug.Assert(avail == expect);
    }

    int AvailableSlots(List<byte[]> list)
    {
        int slots = 0;
        for (; slots < list.Count && null != list[slots]; ++slots) ;
        return slots;
    }
}
