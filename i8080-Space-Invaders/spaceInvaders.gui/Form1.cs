using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace i8080_Space_Invaders {
    public partial class Form : System.Windows.Forms.Form {
        public Form() {
            InitializeComponent();
        }

        private void Form_Load(object sender, EventArgs e) {
            SpaceInvaders spaceInvaders = new SpaceInvaders(pictureBox);
        }
    }
}
