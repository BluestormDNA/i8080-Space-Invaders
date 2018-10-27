using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace i8080_Space_Invaders {
    class Display {

        private const int WIDTH = 224;
        private const int HEIGHT = 256;
        private const ushort videoRamStart = 0x2400;
        private const ushort videoRamEnd = 0x4000;
        //1C00x or 7168d

        private byte[] mem;

        //public DirectBitmap Bmp;

        public Display(Memory memory) {
            this.mem = memory.mem;
            //Bmp = new DirectBitmap(256, 224);
        }

        public Image generateFrame() {
            //overkill for bmp rotate pending rewrite
            DirectBitmap Bmp = new DirectBitmap(256, 224);

            for (int i = videoRamStart; i < videoRamEnd; i++) {
                int y = (i - videoRamStart) / 32;
                for (int b = 0; b < 8; b++) {
                    int x = ((i - videoRamStart) % 32) * 8 + b;
                    Color color = ((mem[i] & 0x1 << b) != 0) ? Color.White : Color.Black;
                    Bmp.SetPixel(x, y, color);
                }
            }
            Bmp.Bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
            return Bmp.Bitmap;
        }

    }
}