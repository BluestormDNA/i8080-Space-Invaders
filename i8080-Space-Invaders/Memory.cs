using System;
using System.IO;

namespace i8080_Space_Invaders {

    class Memory {

        public byte[] mem = new byte[0x8000];
        //public byte[] mem = new byte[0x16000];
        private string[] romFiles = { "invaders.h", "invaders.g", "invaders.f", "invaders.e" };

        public void loadRom() {
            for (int i = 0; i < romFiles.Length; i++) {
                byte[] rom = File.ReadAllBytes(romFiles[i]);
                Array.Copy(rom, 0, mem, 2048 * i, rom.Length);
            }
        }

        public void loadTest() {
            byte[] rom = File.ReadAllBytes("8080EX1.COM");
            Array.Copy(rom, 0, mem, 0x100, rom.Length);
            mem[5] = 0xC9;
            //Fix the stack pointer from 0x6ad to 0x7ad    
            // this 0x06 byte 112 in the code, which is    
            // byte 112 + 0x100 = 368 in memory    
            //mem[368] = 0x7;

            //Skip DAA test    
            //mem[0x59c] = 0xc3; //JMP    
            //mem[0x59d] = 0xc2;
            //mem[0x59e] = 0x05;
        }

        public void dumpMemory() {

            string path = "memHexDump.txt";
            if (!File.Exists(path)) {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path)) {
                    for (int i = 0x2400; i < 0x4000; i++) {
                        for (int b = 0; b < 8; b++) {
                            string s = ((mem[i] & 0x80 >> b) != 0) ? "1" : "0";
                            sw.Write(s);
                        }

                    }
                }
            }

        }

    }
}