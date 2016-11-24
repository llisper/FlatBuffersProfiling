using UnityEngine;
using System.Text;
using System.Collections.Generic;
using FlatBuffers;
using Profile;

public class AddressBookProfiler : MonoBehaviour
{
    #region inspector
    public int count = 1000;
    public int bufSize = 4096;
    public bool runSerialization;
    public long elapsedTime;
    #endregion inspector

    List<FlatBufferBuilder> mBuilders;
    System.Diagnostics.Stopwatch mStopwatch = new System.Diagnostics.Stopwatch();
    Offset<PhoneNumber>[] mPhoneArray = new Offset<PhoneNumber>[10];
    Offset<Person>[] mPersonArray = new Offset<Person>[10];

    const string phoneNumber = "15210173278";
    const string personName = "llisperzhang";
    const string email = "llisperzhang@gmail.com";

    static PhoneNumber phoneNumberInst = new PhoneNumber();
    static Person personInst = new Person();
    static AddressBook addressBookInst = new AddressBook();

    [ContextMenu("PrepareBuffers")]
    void PrepareBuffers()
    {
        mBuilders = new List<FlatBufferBuilder>();
        for (int i = 0; i < count; ++i)
            mBuilders.Add(new FlatBufferBuilder(bufSize));
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

    void FB_Deserialize(ByteBuffer byteBuffer, StringBuilder sb = null)
    {
        AddressBook addressBook = AddressBook.GetRootAsAddressBook(byteBuffer, addressBookInst);
        int plen = addressBook.PeopleLength;
        for (int i = 0; i < plen; ++i)
        {
            Person person = addressBook.GetPeople(personInst, i);
            string n = person.Name;
            int id = person.Id;
            string email = person.Email;
            if (null != sb)
                sb.AppendFormat("{0},{1},{2}", n, id, email);
            int len = person.PhonesLength;
            for (int j = 0; j < len; ++j)
            {
                PhoneNumber pn = person.GetPhones(phoneNumberInst, j);
                string number = pn.Number;
                PhoneType pt = pn.Type;
                if (null != sb)
                    sb.AppendFormat("-[{0}({1})]", number, pt);
            }
            if (null != sb)
                sb.Append('\n');
        }
    }

    void Update()
    {
        if (runSerialization)
        {
            elapsedTime = 0;
            mStopwatch.Reset();
            mStopwatch.Start();
            FB_Serialization();
            mStopwatch.Stop();
            elapsedTime = mStopwatch.ElapsedMilliseconds;
            runSerialization = false;
        }
    }

    [ContextMenu("FB_Serialization")]
    void FB_Serialization()
    {
        for (int i = 0; i < mBuilders.Count; ++i)
        {
            FlatBufferBuilder fbb = mBuilders[i];
            // FB_Serialize(fbb);
        }
        Debug.Break();
    }

    [ContextMenu("FB_Deserialization")]
    void FB_Deserialization()
    {
        for (int i = 0; i < mBuilders.Count; ++i)
            FB_Deserialize(mBuilders[i].DataBuffer);
    }
}
