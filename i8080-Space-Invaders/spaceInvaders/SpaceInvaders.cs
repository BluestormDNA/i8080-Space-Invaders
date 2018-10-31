using BlueStorm.intel8080CpuCore;
using System;
using System.Threading;
using System.Windows.Forms;

namespace i8080_Space_Invaders {
    public class SpaceInvaders {

        Cpu cpu;
        Memory memory;
        Display display;
        Bus iobus;
        PictureBox pictureBox;

        bool tock;

        public SpaceInvaders(PictureBox pictureBox) {
            this.pictureBox = pictureBox;

            memory = new Memory();
            memory.LoadRom();

            iobus = new Bus();
            cpu = new Cpu(memory, iobus);

            display = new Display(memory);
            //this.pictureBox.Image = display.Bmp.Bitmap;

            Thread cpuThread = new Thread(new ThreadStart(exe));
            cpuThread.IsBackground = true;
            cpuThread.Start();
        }

        public void exe() {
            DateTime time = DateTime.Now;
            DateTime elapsed = DateTime.Now;
            while (true) {

                while ((elapsed - time).TotalMilliseconds >= 8) {

                    while (cpu.cycles < 16666) {
                        cpu.exe();
                    }
                    if (tock) {
                        cpu.handleInterrupt(2);
                        renderFrame();
                        tock = false;
                    } else {
                        cpu.handleInterrupt(1);
                        tock = true;
                    }
                    cpu.cycles = 0;
                    time = DateTime.Now;
                }

                elapsed = DateTime.Now;

            }
        }

        public void renderFrame() {
            if (pictureBox.InvokeRequired) {
                pictureBox.Invoke(new MethodInvoker(
                delegate () {
                    //display.generateFrame();
                    pictureBox.Image = display.generateFrame();
                    pictureBox.Refresh();
                }));
            } else {
                //display.generateFrame;
                pictureBox.Image = display.generateFrame();
                pictureBox.Refresh();
            }
        }

        public void handleInput(byte b , Boolean pushed) {
            if (pushed) {
                switch (b) {
                    case 0x1:
                        iobus.input |= 0x1;
                        break;
                    case 0x4:
                        iobus.input |= 0x4;
                        break;
                    case 0x10:
                        iobus.input |= 0x10;
                        break;
                    case 0x20:
                        iobus.input |= 0x20;
                        break;
                    case 0x40:
                        iobus.input |= 0x40;
                        break;
                }
            } else {
                switch (b) {
                    case 0x1:
                        iobus.input &= 0xFE;
                        break;
                    case 0x4:
                        iobus.input &= 0xFB;
                        break;
                    case 0x10:
                        iobus.input &= 0xEF;
                        break;
                    case 0x20:
                        iobus.input &= 0xDF;
                        break;
                    case 0x40:
                        iobus.input &= 0xBF;
                        break;
                }
            }
        }

    }
}
