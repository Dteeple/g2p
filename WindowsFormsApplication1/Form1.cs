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
using CsvHelper;
using CsvHelper.Configuration;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        List<string> _items = new List<string>();
        List<string> _orthItems = new List<string>();


        public Form1()
        {
            InitializeComponent();


        }



        //Start guessing prons
        private void button1_Click(object sender, EventArgs e) // Start g2p
        {
            string orthin = textBox1.Text;  //Regex.Replace(textBox1.Text, @".*\}(.*)~.*", "{0}");
            Dictionary<string, double> nbestPronout = gToPGuesser.mainCode(orthin);
            //Prints nbest choices for pronout
            //(1) user can choose one pron and modify as needed; (2) user's selection is fed back into json file with a score for future use
            var sortedNbest = nbestPronout.OrderByDescending(pair => pair.Value).Take(10);
            foreach (var pronscore in sortedNbest)
            {
                string pron = pronscore.Key;
                //double score = pronscore.Value; //not actually printing score to Form
                _items.Add(pron);

            }


            listBox1.DataSource = _items;
        }





        private void button2_Click(object sender, EventArgs e) // Adds stress arbitrarily before first vowel, for user to move as needed
        {
            string selected = Regex.Replace(listBox1.SelectedItem.ToString(), @"^([^AaxO@YEeJIioyUu]*)([AaxO@YEeJIioyUu])", "$1ˈ$2");
            //{ "AA", "A" }, { "AE", "a" }, { "AH", "x" }, { "AX", "x" }, { "AO", "O" }, { "AW", "@" }, { "AY", "Y" }, 
            //{ "EH", "E" }, { "ER", "e" }, { "EY", "J" }, { "IH", "I" }, { "IY", "i" },
            //    { "OW", "o" }, { "OY", "y" }, { "UH", "U" }, { "UW", "u" },

            textBox2.Text = selected;
        }



        public List<string> loopThrough(IEnumerable<CustomClass> records)
        {
            //string[] missingTransLines;
            string path = @"EN/Data/Dictionary_edited.txt";
            List<string> missing = new List<string>();
            CsvConfiguration config = new CsvConfiguration();
            config.Delimiter = "\t";
            config.Encoding = Encoding.UTF8;
            config.HasHeaderRecord = false;
            config.QuoteNoFields = true;
            if (!File.Exists(path))
            {
                // Create a file to write to.
                File.CreateText(path);
            }
            using (StreamWriter sw = File.AppendText(path)) // adds new entry line to Dictionary_edited.txt
            {
                CsvWriter csvwrite = new CsvWriter(sw, config);
                
                foreach (var rec in records) //iterate through dictionary records
                {
                    // 2) identify transcription field
                    string pronLine = rec.formPresSpellTrans;

                    string[] entries = pronLine.Split('¦');

                    foreach (var entry in entries)
                    {
                        // button3WasClicked = false;
                        //textBox1.Clear();
                        string[] splitEntry = entry.Split('|');
                        string trans = splitEntry[2];
                        // 3) if no transcription, start guessing
                        if (trans.EndsWith("~"))
                        {

                            string formToGuess = splitEntry[0].Split('~')[1];
                            string formType = splitEntry[0].Split('~')[0];
                            textBox1.Text = formToGuess; // enters a new form to guess into first text box
                            label3.Text = rec.recordName; // adds key form to displayed info
                            label4.Text = formType; // adds form type to displayed info
                            csvwrite.WriteRecord(rec);
                            missing.Add(formToGuess);
                            //System.Threading.Thread.Sleep(500);
                            //continue;
                            //sw.WriteLine(trans);


                        }

                    }

                }
                //end foreach

            }
            return missing;

        }


        public void Form1_Load(object sender, EventArgs e)
        {
            //textBox1.Text = "Loading dictionary items...";
            CsvConfiguration config = new CsvConfiguration();
            config.Delimiter = "\t";
            config.Encoding = Encoding.UTF8;
            config.HasHeaderRecord = false;
            config.QuoteNoFields = true;
            string dictPath = "EN/Data/Dictionary.txt";
            //Step 1 of plan
            using (StreamReader SR = File.OpenText(dictPath))
            {
                CsvReader csvread = new CsvReader(SR, config);

                //string[] splitLine = SR.ReadLine().Split('\t');
                //textBox1.Text = splitLine[0];
                IEnumerable<CustomClass> records = csvread.GetRecords<CustomClass>(); // record is used to iterate over all dictionary records
                // put it outside, load once
                //Maybe should change following to for loops, so that you can change entries
                List<string> missing = loopThrough(records);
                
            }
            
        }


        
        
        // global scope
        public void button3_Click(object sender, EventArgs e) // "Submit transcription"
        {
            string orthin = textBox1.Text.ToLower();
            string pronout_w_stress = textBox2.Text; // This will be returned to the JSON file for future guessing, sans stress, and in ARPAbet
            string key = label3.Text;
            string form = label4.Text;
            string to_add = key + '\t' + form + '\t' + orthin + '\t' + pronout_w_stress + '\t' + string.Format("{0:yyyy-MM-dd hh-mm-ss-tt}", DateTime.Now) + '\n';

            string path = @"EN/Data/Dictionary_edited.txt";
            if (!File.Exists(path))
            {
                // Create a file to write to.
                File.CreateText(path);
            }
            using (StreamWriter sw = File.AppendText(path)) // adds new entry line to Dictionary_edited.txt
            {
                // sw.WriteLine(string.Format("{0:yyyy-MM-dd hh-mm-ss-tt}", DateTime.Now));
                sw.WriteLine(to_add);
            }


            
            string pronout = Regex.Replace(pronout_w_stress, @"[ˈˌ]", ""); // deletes stress marks (can't use them in LCS weights dictionary)
            textBox1.Clear();
            listBox1.DataSource = null;
            listBox1.Items.Clear();
            _items.Clear();
            textBox2.Clear();


            //add new pair to LCS wts, and augment shared g2p mapping wts
            gToPGuesser.newpair(orthin, pronout);



        }






        public class CustomClass
        {
            public string recordName { get; set; } //1
            public string recStatus { get; set; } //2
            public string timeStamp { get; set; } //3
            public string register { get; set; } //4
            public string acronym { get; set; } //5            
            public string style { get; set; } //6
            public string origin { get; set; } //7
            public string frequency { get; set; } //8
            public string definition { get; set; }//9
            public string etymology { get; set; } //10
            public string frames { get; set; } //11
            public string semGroups { get; set; } //12
            public string semFields { get; set; } //13
            public string semRels { get; set; } //14
            public string lexRels { get; set; } //15
            public string definiteness { get; set; } //definiteness //16
            public string declinability { get; set; } //declinability //17
            public string dependency { get; set; } //dependency //18
            public string unkOne { get; set; } //unk1 //19
            public string unkTwo { get; set; } //unk2 //20
            public string number { get; set; } //number //21
            public string gender { get; set; } //gender //22                                    
            public string formPresSpellTrans { get; set; }//23 Form¬Presentation|Spelling|Transcription separated by |. Each new entry separated by ¦




            //public string pronString { get; set; } //
            // have to split up fromPresSpellTrans, split('¦'), split('|'), find American only
        }


    }
}
