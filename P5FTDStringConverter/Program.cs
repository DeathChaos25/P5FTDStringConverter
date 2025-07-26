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
                Console.WriteLine("P5FTDStringConverter\nUsage: Drag and Drop a txt or ftd file into the program's exe\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            try
            {
                FileInfo arg0 = new FileInfo(args[0]);
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                if (arg0.Extension.ToLower() == ".ftd")
                {
                    ConvertFtdToTxt(arg0);
                }
                else if (arg0.Extension.ToLower() == ".txt")
                {
                    ConvertTxtToFtd(arg0);
                }
                else
                {
                    Console.WriteLine("https://youtu.be/Uuw6PdJvW88");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        static void ConvertFtdToTxt(FileInfo ftdFile)
        {
            try
            {
                Console.WriteLine($"Attempting to convert {ftdFile.Name}");
                List<UInt32> StringPointers = new List<UInt32>();

                using (BinaryObjectReader ftdfile = new BinaryObjectReader(ftdFile.FullName, Endianness.Big, AtlusEncoding.Persona5RoyalEFIGS))
                {
                    var temp1 = ftdfile.ReadUInt16(); // should be 00 01
                    var temp2 = ftdfile.ReadUInt16(); // should be 00 00
                    UInt32 Magic = ftdfile.ReadUInt32(); // FTD0
                    UInt32 Filesize = ftdfile.ReadUInt32(); // location is 0x8
                    var DataType = ftdfile.ReadUInt16(); // should be 00 01
                    var numOfPointers = ftdfile.ReadUInt16(); // location 0xE

                    if (DataType != 1)
                    {
                        Console.WriteLine("P5FTDStringConverter: FTD file is not a text type!");
                        Console.ReadKey();
                        return;
                    }

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

                    var savePath = Path.Combine(Path.GetDirectoryName(ftdFile.FullName), Path.GetFileNameWithoutExtension(ftdFile.FullName) + ".txt");
                    File.WriteAllLines(savePath, FTDStrings, Encoding.UTF8);
                    Console.WriteLine($"File saved to {savePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting FTD to TXT: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        static void ConvertTxtToFtd(FileInfo txtFile)
        {
            try
            {
                Console.WriteLine($"Attempting to convert {txtFile.Name}");

                var savePath = Path.Combine(Path.GetDirectoryName(txtFile.FullName), Path.GetFileNameWithoutExtension(txtFile.FullName) + ".ftd");

                using (BinaryObjectWriter ftdfile = new BinaryObjectWriter(savePath, Endianness.Big, AtlusEncoding.Persona5RoyalEFIGS))
                {
                    string[] readText = File.ReadAllLines(txtFile.FullName, AtlusEncoding.Persona5RoyalEFIGS);

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

                    int targetPadding = (int)((0x10 - ftdfile.Position % 0x10) % 0x10); // pad to end of line if not enough pointers
                    if (targetPadding > 0)
                    {
                        for (int j = 0; j < targetPadding; j++)
                        {
                            ftdfile.WriteByte((byte)0);
                        }
                    }

                    long NextPos = ftdfile.Position;
                    int i = 0;
                    foreach (string s in readText)
                    {
                        long targetPointerPos = 0x10 + 4 * i;
                        ftdfile.Seek(targetPointerPos, SeekOrigin.Begin);
                        ftdfile.WriteUInt32((UInt32)NextPos);

                        ftdfile.Seek(NextPos, SeekOrigin.Begin);

                        byte[] encodedBytes = AtlusEncoding.Persona5RoyalEFIGS.GetBytes(s);
                        var strLen = encodedBytes.Length + 1;

                        ftdfile.Seek(NextPos, SeekOrigin.Begin);
                        ftdfile.WriteByte((byte)strLen);
                        ftdfile.WriteByte(1);
                        ftdfile.WriteUInt16(0);
                        ftdfile.WriteBytes(encodedBytes);
                        ftdfile.WriteByte(0);

                        targetPadding = (int)((0x10 - ftdfile.Position % 0x10) % 0x10);
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
                    Console.WriteLine($"File saved to {savePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting file: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}