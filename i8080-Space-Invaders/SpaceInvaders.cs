using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace i8080_Space_Invaders {
    public class SpaceInvaders {

        Cpu cpu;
        Memory memory;
        Display display;
        IObus iobus;
        PictureBox pictureBox;

        bool tock;

        public SpaceInvaders(PictureBox pictureBox) {
            this.pictureBox = pictureBox;

            memory = new Memory();
            memory.loadRom();
            //memory.loadTest();

            iobus = new IObus();
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
    }
}
