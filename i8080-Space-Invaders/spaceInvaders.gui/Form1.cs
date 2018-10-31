using System;
using System.Windows.Forms;

namespace i8080_Space_Invaders {
    public partial class Form : System.Windows.Forms.Form {

        SpaceInvaders si;

        public Form() {
            InitializeComponent();
        }

        private void Form_Load(object sender, EventArgs e) {
            si = new SpaceInvaders(pictureBox);
        }

        private void Key_Down(object sender, KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.A:
                case Keys.Left:
                    si.handleInput(0x20, true);
                    break;

                case Keys.D:
                case Keys.Right:
                    si.handleInput(0x40, true);
                    break;

                case Keys.Space:
                    si.handleInput(0x10, true);
                    break;

                case Keys.Return:
                    si.handleInput(0x4, true);
                    break;

                case Keys.D1:
                    si.handleInput(0x1, true);
                    break;
            }
        }

        private void Key_Up(object sender, KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.A:
                case Keys.Left:
                    si.handleInput(0x20, false);
                    break;

                case Keys.D:
                case Keys.Right:
                    si.handleInput(0x40, false);
                    break;

                case Keys.Space:
                    si.handleInput(0x10, false);
                    break;

                case Keys.Return:
                    si.handleInput(0x4, false);
                    break;

                case Keys.D1:
                    si.handleInput(0x1, false);
                    break;
            }
        }
    }
}

