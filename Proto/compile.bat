@echo off
protogen\protogen.exe -i:AddressBook.proto -o:AddressBook\AddressBook\AddressBook.cs -ns:ProtoBuf_Profile
pause
