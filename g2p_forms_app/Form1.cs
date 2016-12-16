using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        List<string> _items = new List<string>(); 

        public Form1()
        {
            InitializeComponent();

            
        }

        //Start guessing prons
        private void button1_Click(object sender, EventArgs e)
        {
            string orthin = textBox1.Text;
            Dictionary<string, double> nbestPronout = gToPGuesser.mainCode(orthin);
            //Prints nbest choices for pronout
            //Need to add: (1) ability for user to choose one pron and modify as needed; (2) user's selection needs to be fed back into json file with a score for future use
            var sortedNbest = nbestPronout.OrderByDescending(pair => pair.Value).Take(10);
            foreach (var pronscore in sortedNbest)
            {
                string pron = pronscore.Key;
                //double score = pronscore.Value; //not actually printing score to Form
                _items.Add(pron);

            }


            listBox1.DataSource = _items;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string selected = Regex.Replace(listBox1.SelectedItem.ToString(), @"^([^AaxO@YEeJIioyUu]*)([AaxO@YEeJIioyUu])", "$1ˈ$2");
            //{ "AA", "A" }, { "AE", "a" }, { "AH", "x" }, { "AX", "x" }, { "AO", "O" }, { "AW", "@" }, { "AY", "Y" }, 
            //{ "EH", "E" }, { "ER", "e" }, { "EY", "J" }, { "IH", "I" }, { "IY", "i" },
            //    { "OW", "o" }, { "OY", "y" }, { "UH", "U" }, { "UW", "u" },

            textBox2.Text = selected;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            string orthin = textBox1.Text.ToLower();
            string pronout_w_stress = textBox2.Text; // This should be returned to the JSON file for future guessing, sans stress, and in ARPAbet
            
            string pronout = Regex.Replace(pronout_w_stress, @"[ˈˌ]", "");
            textBox1.Clear();
            listBox1.DataSource = null;
            listBox1.Items.Clear();
            _items.Clear();
            textBox2.Clear();

            
            //add new pair to LCS wts, and augment shared g2p mapping wts
            gToPGuesser.newpair(orthin, pronout);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
