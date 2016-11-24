using UnityEngine;
using System;
using System.IO;
using FlatBuffers;
using CompanyNamespaceWhatever;

public class Profiler : MonoBehaviour
{
    #region inspector
    public enum Ptype
    {
        Save,
        Load,
    };

    public Ptype profileType = Ptype.Save;
    public bool profiling;
    #endregion inspector

    byte[] mBuffer = new byte[4096];
    FlatBufferBuilder mBuilder = new FlatBufferBuilder(4096);
    MemoryStream mStream;
    ByteBuffer mByteBuffer;
    string mTime = "Test String ! time : " + DateTime.Now;
    GameDataWhatever mLoadGameData = new GameDataWhatever();

    void Awake()
    {
        mStream = new MemoryStream(mBuffer);
        mByteBuffer = new ByteBuffer(mBuffer);
    }

    void Update()
    {
        if (profiling)
        {
            if (profileType == Ptype.Save)
                Save();
            else
                Load();
        }
    }

    [ContextMenu("Save")]
    void Save()
    {
        mBuilder.Clear();
        mStream.SetLength(0);

        FlatBufferBuilder fbb = mBuilder;
        // FlatBufferBuilder fbb = new FlatBufferBuilder(1);

        // Create our sword for GameDataWhatever
        //------------------------------------------------------

        WeaponClassesOrWhatever weaponType = WeaponClassesOrWhatever.Sword;
        Sword.StartSword(fbb);
        Sword.AddDamage(fbb, 123);
        Sword.AddDistance(fbb, 999);
        Offset<Sword> offsetWeapon = Sword.EndSword(fbb);

        /*
        // For gun uncomment this one and remove the sword one
        WeaponClassesOrWhatever weaponType = WeaponClassesOrWhatever.Gun;
        Gun.StartGun(fbb);
        Gun.AddDamage(fbb, 123);
        Gun.AddReloadspeed(fbb, 999);
        Offset<Gun> offsetWeapon = Gun.EndGun(fbb);
        */
        //------------------------------------------------------

        // Create strings for GameDataWhatever
        //------------------------------------------------------
        StringOffset cname = fbb.CreateString(mTime);
        //------------------------------------------------------

        // Create GameDataWhatever object we will store string and weapon in
        //------------------------------------------------------
        GameDataWhatever.StartGameDataWhatever(fbb);

        GameDataWhatever.AddName(fbb, cname);
        GameDataWhatever.AddPos(fbb, Vec3.CreateVec3(fbb, 1, 2, 1)); // structs can be inserted directly, no need to be defined earlier
        GameDataWhatever.AddColor(fbb, CompanyNamespaceWhatever.Color.Red);

        //Store weapon
        GameDataWhatever.AddWeaponType(fbb, weaponType);
        GameDataWhatever.AddWeapon(fbb, offsetWeapon.Value);

        var offset = GameDataWhatever.EndGameDataWhatever(fbb);
        //------------------------------------------------------

        // GameDataWhatever.FinishGameDataWhateverBuffer(fbb, offset);
        mStream.Write(fbb.DataBuffer.Data, fbb.DataBuffer.Position, fbb.Offset);
        // Debug.Log("size: " + mStream.Length);
    }

    [ContextMenu("Load")]
    void Load()
    {
        mByteBuffer.Reset();
        ByteBuffer bb = mByteBuffer;

        /*
        if (!GameDataWhatever.GameDataWhateverBufferHasIdentifier(bb))
        {
            throw new Exception("Identifier test failed, you sure the identifier is identical to the generated schema's one?");
        }
        */

        GameDataWhatever data = GameDataWhatever.GetRootAsGameDataWhatever(bb, mLoadGameData);
        // string n = data.Name;
        // Vector3 vec3 = new Vector3(data.Pos.X, data.Pos.Y, data.Pos.Z);
        CompanyNamespaceWhatever.Color color = data.Color;
        WeaponClassesOrWhatever weaponType = data.WeaponType;

        switch (data.WeaponType)
        {
            case WeaponClassesOrWhatever.Sword:
                Sword sword = new Sword();
                data.GetWeapon<Sword>(sword);
                int damage = sword.Damage;
                break;
            case WeaponClassesOrWhatever.Gun:
                Gun gun = new Gun();
                data.GetWeapon<Gun>(gun);
                short reloadSpeed = gun.Reloadspeed;
                break;
            default:
                break;
        }
    }
}
