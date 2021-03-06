﻿using BlueStorm.intel8080CpuCore;

namespace i8080_Space_Invaders {
    public class Bus : i8080IObus{

        byte shiftH;
        byte shiftL;
        byte offset;

        public byte input { get; set; }

        byte lower3bitMask = 0x07; //0000 0111 covers amount to shift from 0 to 7

        public byte Read(byte b) { //in
            switch (b) {
                case 0x01: //coin and buttons
                    return input;
                case 0x03:
                    ushort shift = (ushort)(shiftH << 8 | shiftL);
                    return (byte)(shift >> (8 - offset));
                default: //this covers case 0x02: only used by p2 and dipswitch...
                    return 0;
            }
        }

        public void Write(byte b, byte A) { //out
            switch (b) {
                case 0x02:
                    offset = (byte)(A & lower3bitMask);
                    break;
                case 0x04:
                    shiftL = shiftH;
                    shiftH = A;
                    break;
                default:
                    break;
            }
        }
    }
}