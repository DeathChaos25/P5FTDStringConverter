using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Amicitia.IO.Binary;
using AtlusScriptLibrary.Common.Text.Encodings;

namespace P5FTDStringConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage:\nP5FTDStringConverter [path to ftd or path to txt]");
            }
            else
            {
                FileInfo arg0 = new FileInfo(args[0]);
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                if (arg0.Extension == ".ftd")
                {
                    Console.WriteLine($"Attempting to convert { arg0.Name }");
                    List<UInt32> StringPointers = new List<UInt32>();

                    using (BinaryObjectReader ftdfile = new BinaryObjectReader(args[0], Endianness.Big, AtlusEncoding.Persona5RoyalEFIGS))
                    {
                        var temp1 = ftdfile.ReadUInt16(); // should be 00 01
                        var temp2 = ftdfile.ReadUInt16(); // should be 00 00
                        UInt32 Magic = ftdfile.ReadUInt32(); // FTD0
                        UInt32 Filesize = ftdfile.ReadUInt32(); // location is 0x8
                        var temp3 = ftdfile.ReadUInt16(); // should be 00 01
                        var numOfPointers = ftdfile.ReadUInt16(); // location 0xE

                        List<String> FTDStrings = new List<String>();

                        for (int i = 0; i < numOfPointers; i++)
                        {
                            StringPointers.Add(ftdfile.ReadUInt32());
                        }

                        for (int i = 0; i < numOfPointers; i++)
                        {
                            ftdfile.Seek(StringPointers[i], SeekOrigin.Begin);
                            var stringLength = ftdfile.ReadByte();
                            var unk = ftdfile.ReadByte(); // should be 0x1
                            var nullVar = ftdfile.ReadUInt16(); // should be 00 00

                            FTDStrings.Add(ftdfile.ReadString(StringBinaryFormat.NullTerminated));
                        }

                        var savePath = Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(arg0.FullName) + ".txt");
                        File.WriteAllLines(savePath, FTDStrings);
                        Console.WriteLine($"File saved to {savePath}");
                    }
                }
                else if (arg0.Extension == ".txt")
                {
                    Console.WriteLine($"Attempting to convert { arg0.Name }");
                    string[] readText = File.ReadAllLines(arg0.FullName, AtlusEncoding.Persona5RoyalEFIGS);
                    var savePath = Path.Combine(Path.GetDirectoryName(args[0]), Path.GetFileNameWithoutExtension(arg0.FullName) + ".ftd");

                    using (BinaryObjectWriter ftdfile = new BinaryObjectWriter(savePath, Endianness.Big, AtlusEncoding.Persona5RoyalEFIGS))
                    {
                        ftdfile.WriteUInt16(0x0001);
                        ftdfile.WriteUInt16(0x0000);
                        ftdfile.WriteUInt32(0x46544430); // FTD0
                        ftdfile.WriteUInt32(0x0); // Filesize, come back and fix later
                        ftdfile.WriteUInt16(0x0001);
                        ftdfile.WriteUInt16((UInt16)readText.Length);

                        foreach (string s in readText)
                        {
                            ftdfile.WriteUInt32(0x0); //Write dummy pointers
                        }

                        long NextPos = ftdfile.Position;
                        int i = 0;
                        foreach (string s in readText)
                        {
                            long targetPointerPos = 0x10 + 4 * i;
                            ftdfile.Seek(targetPointerPos, SeekOrigin.Begin);
                            ftdfile.WriteUInt32((UInt32)NextPos);

                            ftdfile.Seek(NextPos, SeekOrigin.Begin);

                            var strLen = s.Length + 1;
                            ftdfile.WriteByte((byte)strLen);
                            ftdfile.WriteByte(1);
                            ftdfile.WriteUInt16(0);
                            ftdfile.WriteString(StringBinaryFormat.FixedLength, s, s.Length);

                            int targetPadding = (int)((0x10 - ftdfile.Position % 0x10) % 0x10);
                            if (targetPadding > 0)
                            {
                                for (int j = 0; j < targetPadding; j++)
                                {
                                    ftdfile.WriteByte((byte)0);
                                }
                            }

                            NextPos = ftdfile.Position;
                            i++;
                        }
                        ftdfile.Seek(8, SeekOrigin.Begin);
                        ftdfile.WriteUInt32((UInt32)ftdfile.Length); // fix filesize
                        ftdfile.Dispose();
                        Console.WriteLine($"File saved to {savePath}");
                    }
                }
                else Console.WriteLine("https://youtu.be/Uuw6PdJvW88");
            }
        }
    }
}
