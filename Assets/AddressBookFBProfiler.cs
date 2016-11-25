using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using FlatBuffers;
using FlatBuffers_Profile;

public class AddressBookFBProfiler : MonoBehaviour
{
    #region inspector
    public int count = 1000;
    public int bufSize = 4096;
    #endregion inspector

    public static AddressBookFBProfiler instance;

    List<FlatBufferBuilder> mBuilders;
    System.Diagnostics.Stopwatch mStopwatch = new System.Diagnostics.Stopwatch();
    Offset<PhoneNumber>[] mPhoneArray = new Offset<PhoneNumber>[10];
    Offset<Person>[] mPersonArray = new Offset<Person>[10];

    bool mDoFBSerialization;
    bool mDoFBDeserialization;

    const string phoneNumber = "15210173278";
    const string personName = "llisperzhang";
    const string email = "llisperzhang@gmail.com";

    static PhoneNumber phoneNumberInst = new PhoneNumber();
    static Person personInst = new Person();
    static AddressBook addressBookInst = new AddressBook();

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (mDoFBSerialization)
        {
            FBSerialization();
            mDoFBSerialization = false;
        }

        if (mDoFBDeserialization)
        {
            FBDeserialization();
            mDoFBDeserialization = false;
        }
    }

    [ContextMenu("FB_SerializationTest")]
    void FB_SerializationTest()
    {
        FlatBufferBuilder fbb = new FlatBufferBuilder(4096);
        FB_Serialize(fbb);
        StringBuilder sb = new StringBuilder();
        FB_Deserialize(fbb.DataBuffer, sb);
        Debug.Log("result:\n" + sb.ToString());
    }

    void FB_Serialize(FlatBufferBuilder fbb)
    {
        fbb.Clear();
        for (int p = 0; p < 10; ++p)
        {
            for (int n = 0; n < 10; ++n)
            {
                StringOffset phoneNumberOffset = fbb.CreateString(phoneNumber);
                PhoneNumber.StartPhoneNumber(fbb);
                PhoneNumber.AddNumber(fbb, phoneNumberOffset);
                PhoneNumber.AddType(fbb, (PhoneType)(n % 3));
                mPhoneArray[n] = PhoneNumber.EndPhoneNumber(fbb);
            }

            StringOffset nameOffset = fbb.CreateString(personName);
            StringOffset emailOffset = fbb.CreateString(email);
            VectorOffset phoneArrayOffset = Person.CreatePhonesVector(fbb, mPhoneArray);

            Person.StartPerson(fbb);
            Person.AddName(fbb, nameOffset);
            Person.AddId(fbb, p);
            Person.AddEmail(fbb, emailOffset);
            Person.AddPhones(fbb, phoneArrayOffset);
            mPersonArray[p] = Person.EndPerson(fbb);
        }

        VectorOffset peopleArrayOffset = AddressBook.CreatePeopleVector(fbb, mPersonArray);
        AddressBook.StartAddressBook(fbb);
        AddressBook.AddPeople(fbb, peopleArrayOffset);
        Offset<AddressBook> offset = AddressBook.EndAddressBook(fbb);
        fbb.Finish(offset.Value);
    }

    void FB_Deserialize(ByteBuffer byteBuffer, StringBuilder sb)
    {
        AddressBook addressBook = AddressBook.GetRootAsAddressBook(byteBuffer, addressBookInst);
        int plen = addressBook.PeopleLength;
        for (int i = 0; i < plen; ++i)
        {
            Person person = addressBook.GetPeople(personInst, i);
            string n = person.Name;
            int id = person.Id;
            string email = person.Email;
            sb.AppendFormat("{0},{1},{2}", n, id, email);
            int len = person.PhonesLength;
            for (int j = 0; j < len; ++j)
            {
                PhoneNumber pn = person.GetPhones(phoneNumberInst, j);
                string number = pn.Number;
                PhoneType pt = pn.Type;
                sb.AppendFormat("-[{0}({1})]", number, pt);
            }
            sb.Append('\n');
        }
    }

    void FB_Deserialize(ByteBuffer byteBuffer)
    {
        AddressBook addressBook = AddressBook.GetRootAsAddressBook(byteBuffer, addressBookInst);
        int plen = addressBook.PeopleLength;
        for (int i = 0; i < plen; ++i)
        {
            Person person = addressBook.GetPeople(personInst, i);
            string n = person.Name;
            int id = person.Id;
            string email = person.Email;
            int len = person.PhonesLength;
            for (int j = 0; j < len; ++j)
            {
                PhoneNumber pn = person.GetPhones(phoneNumberInst, j);
                string number = pn.Number;
                PhoneType pt = pn.Type;
            }
        }
    }

    [CUDLR.Command("fb_prep_buf", "flatbuffers prepare buffers")]
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
        mBuilders = new List<FlatBufferBuilder>();
        for (int i = 0; i < count; ++i)
            mBuilders.Add(new FlatBufferBuilder(bufSize));
        GC.Collect();
        Debug.Log("Buffers Prepared");
    }

    [CUDLR.Command("fbs", "flatbuffers serialization")]
    public static void Cmd_DoFBSerialization()
    {
        instance.mDoFBSerialization = true;
    }

    [ContextMenu("DoFBSerialization")]
    public void Ctx_DoFBSerialization()
    {
        instance.mDoFBSerialization = true;
    }

    void FBSerialization()
    {
        mStopwatch.Reset();
        mStopwatch.Start();
        for (int i = 0; i < instance.mBuilders.Count; ++i)
        {
            FlatBufferBuilder fbb = instance.mBuilders[i];
            instance.FB_Serialize(fbb);
        }
        mStopwatch.Stop();
        Debug.LogFormat(
            "FBSerialization, count:{0}, time:{1}(ms)",
            instance.mBuilders.Count,
            mStopwatch.ElapsedMilliseconds);
    }

    [CUDLR.Command("fbdes", "flatbuffers deserialization")]
    public static void Cmd_DoFBDeserialization()
    {
        instance.mDoFBDeserialization = true;
    }

    [ContextMenu("DoFBDeserialization")]
    public void Ctx_DoFBDeserialization()
    {
        instance.mDoFBDeserialization = true;
    }

    void FBDeserialization()
    {
        mStopwatch.Reset();
        mStopwatch.Start();
        for (int i = 0; i < mBuilders.Count; ++i)
            FB_Deserialize(mBuilders[i].DataBuffer);
        mStopwatch.Stop();
        Debug.LogFormat(
            "FBDeserialization, count:{0}, time:{1}(ms)",
            instance.mBuilders.Count,
            mStopwatch.ElapsedMilliseconds);
    }
}
