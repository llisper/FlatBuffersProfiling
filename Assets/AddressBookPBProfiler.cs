using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using ProtoBuf;
using ProtoBuf_Profile;

public class AddressBookPBProfiler : MonoBehaviour
{
    #region inspector
    public int count;
    public int bufSize;
    #endregion inspector

    public static AddressBookPBProfiler instance;

    AddressBookSerializer mSerializer = new AddressBookSerializer();
    System.Diagnostics.Stopwatch mStopwatch = new System.Diagnostics.Stopwatch();
    List<AddressBook> mBooks;
    List<MemoryStream> mStreams;
    List<ProtoWriter> mWriters;

    bool mDoPBSerialization;
    bool mDoPBDeserialization;

    const string phoneNumber = "15210173278";
    const string personName = "llisperzhang";
    const string email = "llisperzhang@gmail.com";

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (mDoPBSerialization)
        {
            PBSerialization();
            mDoPBSerialization = false;
        }

        if (mDoPBDeserialization)
        {
            PBDeserialization();
            mDoPBDeserialization = false;
        }
    }

    void PB_Serialize(int i)
    {
        AddressBook ab = mBooks[i];
        for (int p = 0; p < 10; ++p)
        {
            Person person = ab.people[p];
            person.name = personName;
            person.id = p;
            person.email = email;
            for (int n = 0; n < 10; ++n)
            {
                PhoneNumber pn = person.phones[n];
                pn.number = phoneNumber;
                pn.type = (PhoneType)(n % 3);
            }
        }
        MemoryStream stream = mStreams[i];
        stream.Position = 0;
        ProtoWriter writer = mWriters[i];
        mSerializer.Serialize(writer, ab);
    }

    void PB_Deserialize(int i)
    {
        MemoryStream stream = mStreams[i];
        int len = (int)stream.Position;
        stream.Position = 0;
        AddressBook ab = (AddressBook)mSerializer.Deserialize(stream, mBooks[i], typeof(AddressBook), len);
        for (int p = 0; p < ab.people.Count; ++p)
        {
            Person person = ab.people[p];
            string n = person.name;
            int id = person.id;
            string email = person.email;
            for (int j = 0; j < person.phones.Count; ++j)
            {
                PhoneNumber pn = person.phones[j];
                string number = pn.number;
                PhoneType pt = pn.type;
            }
        }
    }

    [CUDLR.Command("pb_prep_buf", "protobuf prepare buffers")]
    public static void Cmd_PrepareBuffers(string[] args)
    {
        if (null != args && args.Length > 1)
        {
            instance.count = int.Parse(args[0]);
            instance.bufSize = int.Parse(args[1]);
            instance.Ctx_PrepareBuffers();
        }
    }

    [ContextMenu("PrepareBuffers")]
    void Ctx_PrepareBuffers()
    {
        mBooks = new List<AddressBook>();

        if (null != mStreams)
        {
            for (int i = 0; i < mStreams.Count; ++i)
                mStreams[i].Close();
            mStreams.Clear();
        }
        else
        {
            mStreams = new List<MemoryStream>();
        }

        if (null != mWriters)
        {
            for (int i = 0; i < mWriters.Count; ++i)
                mWriters[i].Close();
            mWriters.Clear();
        }
        else
        {
            mWriters = new List<ProtoWriter>();
        }

        for (int i = 0; i < count; ++i)
        {
            AddressBook ab = new AddressBook();
            for (int p = 0; p < 10; ++p)
            {
                Person person = new Person();
                for (int n = 0; n < 10; ++n)
                    person.phones.Add(new PhoneNumber());
                ab.people.Add(person);
            }
            mBooks.Add(ab);
            MemoryStream stream = new MemoryStream(new byte[bufSize]);
            mStreams.Add(stream);
            mWriters.Add(new ProtoWriter(stream, mSerializer, null));
        }
        GC.Collect();
        Debug.Log("Buffers Prepared");
    }

    [CUDLR.Command("pbs", "protobuf serialization")]
    public static void Cmd_DopBSerialization()
    {
        instance.mDoPBSerialization = true;
    }

    [ContextMenu("DoPBSerialization")]
    public void Ctx_DoPBSerialization()
    {
        instance.mDoPBSerialization = true;
    }

    void PBSerialization()
    {
        mStopwatch.Reset();
        mStopwatch.Start();
        for (int i = 0; i < instance.mBooks.Count; ++i)
            instance.PB_Serialize(i);
        mStopwatch.Stop();
        /*
        Debug.LogFormat(
            "PBSerialization, count:{0}, time:{1}(ms)",
            instance.mBooks.Count,
            mStopwatch.ElapsedMilliseconds);
            */
    }

    [CUDLR.Command("pbdes", "protobuf deserialization")]
    public static void Cmd_DoPBDeserialization()
    {
        instance.mDoPBDeserialization = true;
    }

    [ContextMenu("DoPBDeserialization")]
    public void Ctx_DoPBDeserialization()
    {
        instance.mDoPBDeserialization = true;
    }

    void PBDeserialization()
    {
        mStopwatch.Reset();
        mStopwatch.Start();
        for (int i = 0; i < mBooks.Count; ++i)
            PB_Deserialize(i);
        mStopwatch.Stop();
        /*
        Debug.LogFormat(
            "PBDeserialization, count:{0}, time:{1}(ms)",
            instance.mBooks.Count,
            mStopwatch.ElapsedMilliseconds);
            */
    }
}
