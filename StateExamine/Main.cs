using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

// JSON Serialization
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Utilities;
using Newtonsoft.Json;

// 

namespace StateExamine
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        public void RenderImage()
        {
            

        }

        /// <summary>
        /// Display the open file dialog and load the game state into the form
        /// </summary>
        /// <param name="sender">The dialog that is activating this event</param>
        /// <param name="e">The arguments that are required for this event.</param>
        private void loadStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Load in the JSON file in question
            OpenFileDialog _filedialog = new OpenFileDialog();
            _filedialog.Filter = "Text files (*.txt)|JSON files (*.json)";
            _filedialog.FileOk += delegate(object senderobject, CancelEventArgs eventargs)
            {
                // Load in the file and deserialize into a game state that we are after.
            };
            
            _filedialog.ShowDialog();

        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("About", "Developed by Luc Shelton", MessageBoxButtons.OK);
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }
    }
}
