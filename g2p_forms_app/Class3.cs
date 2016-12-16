using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.IO;

namespace WindowsFormsApplication1
{
    public class gToPGuesser
    {
        //to predict orthography-phonetic mappings by rewarding largest overlaps, and highest weighted substrings

        // maybe use cmu_to_ipa from Text_Utilities, instead? Not necessarily monocharacter, but at least using an existing system, and the one SMorFET uses
        public static Dictionary<string, string> cmu_to_monochar = new Dictionary<string, string>()
        { { "B", "b" } , { "D", "d" }, { "F", "f" }, { "G", "g" }, { "K", "k" }, { "L", "l" }, { "M", "m" }, { "N", "n" }, { "P", "p" },
            { "R", "r" }, { "S", "s" },{ "T","t" },{ "V", "v" },{ "W" ,"w" },{ "Y", "j" }, { "Z" ,"z" },
         { "AA", "A" }, { "AE", "a" }, { "AH", "x" }, { "AX", "x" }, { "AO", "O" }, { "AW", "@" }, { "AY", "Y" }, { "CH", "C" }, { "DH", "D" },
            { "EH", "E" }, { "ER", "e" }, { "EY", "J" }, { "HH", "h" }, { "IH", "I" }, { "IY", "i" }, { "JH", "G" }, { "NG", "N" },
                { "OW", "o" }, { "OY", "y" }, { "SH", "S" }, { "TH", "T" }, { "UH", "U" }, { "UW", "u" }, { "ZH", "Z"},
            {"alef", "ʔ" } };//TODO check alef

        //Loads wts_plus_cmu.json as the weights dictionary; should put this in the project folder, not local machine-specific location. EVENTUALLY: \dictionary_manager\Dictionary_Manager\bin\Debug\EN\Input_Corpus\wts_plus_cmu.json
        public static Dictionary<string, Dictionary<string, int>> mappingScores = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(File.ReadAllText(@"C:/Users/david/Desktop/corpus_addition/dicts/g2p_wts/wts_plus_cmu.json", Encoding.UTF8));





        public static void Main(string[] args)
        {
            Console.Write("Please enter orthography to guess: ");
            string orthin = Console.ReadLine();

            while (orthin != "Q")
            {
                Dictionary<string, double> nbestPronout = mainCode(orthin);
                //Prints nbest choices for pronout
                //Need to add: (1) ability for user to choose one pron and modify as needed; (2) user's selection needs to be fed back into json file with a score for future use
                var sortedNbest = nbestPronout.OrderByDescending(pair => pair.Value).Take(10);
                foreach (var pronscore in sortedNbest)
                {
                    string pron = pronscore.Key;
                    double score = pronscore.Value;
                    Console.Write("{0}\t{1}\n", score, pron);
                }
                Console.Write("Please enter orthography to guess: ");
                orthin = Console.ReadLine();
            }

        }

        //Main code block, takes an othography as input, returns list of matching prons
        public static Dictionary<string, double> mainCode(string orthin)
        {
            //// Main code block starts here
            //make orthin lowercase
            orthin = orthin.ToLower();
            //Create substringScores dict
            Dictionary<string, Dictionary<string, double>> substringScores = new Dictionary<string, Dictionary<string, double>> { };
            // Create dict to store results of substring overlap and scoring
            Dictionary<string, Dictionary<string, double>> overlapScores = new Dictionary<string, Dictionary<string, double>> { };
            // create dict to list (at least) 5 guesses at pronunciation for orthin
            Dictionary<string, double> nbestPronout = new Dictionary<string, double> { };

            int mult = 1; // reward for finding the exact string in the wts dict
                          //foreach (KeyValuePair<string, KeyValuePair<string, int>> entry in mappingScores)


            foreach (var item in mappingScores)
            {
                var orth = item.Key;
                if (orth == orthin)
                {
                    mult = 2;
                }
                if (orthin.Contains(orth))
                {
                    var pronscore = item.Value; //pair of <ARPAbet, frequency score> from CMU dict LCS comparisons
                    foreach (KeyValuePair<string, int> entry in pronscore)
                    {
                        double len = (double)orth.Length; //Length of matching substring, converted to double
                        double val = (double)entry.Value; //Frequency score from CMU dict LCS comparisons, converted to double
                        double score = (len - (len / val)) * mult; // Frequency score for each substring is normalized, so that max score approaches length of substring. Longer substrings always beat shorter. Then, higher frequency always beats lower.
                        if (score >= (len - 1))
                        {
                            //var pron = entry.Key; //ARPAbet transcription of matching substring, from CMU dict LCS comparison; want to convert this to one-character transliteration system, for better matching. cmu_to_monochar dictionary
                            // Console.Write("\t{0}, {1}, {2}\n", orth, pron, score);
                            //Console.Write("{0}", entry.Key);
                            string monoPron = convertArpaMonochar(entry.Key);
                            List<string> equivPronList = equivProns(monoPron);
                            foreach (string ePron in equivPronList)
                            {

                                if (substringScores.ContainsKey(orth)) // Add pron-score pair to substring dictionary, for later composition of orthin.
                                {
                                    if (substringScores[orth].ContainsKey(ePron))
                                    {
                                        //Do nothing
                                        //substringScores[orth][ePron] += score;
                                    }
                                    else
                                    {
                                        //Adds the value {pron, score} to the entry for orth
                                        substringScores[orth].Add(ePron, score);
                                    }

                                }
                                else
                                {
                                    //Adds whole new entry
                                    substringScores.Add(orth, new Dictionary<string, double> { { ePron, score } });

                                }
                            }

                        }

                    }

                }

            }




            //Finds normalized score for each orth in substringScores, finds overlap score for each pair of substrings, adds to overlapScores dictionary
            foreach (var orthx in substringScores.Keys)// sort pron - score pairs, descending by score; takes only highest 5
            {
                //Console.Write("\nOrthx:\t{0}\n", orthx);
                double prevHigh = 0.0;
                var sortedSubstringsX = substringScores[orthx].OrderByDescending(pair => pair.Value).Take(5);
                foreach (var pronscorex in sortedSubstringsX)
                {
                    //var x = Regex.Replace(pronscorex.Key, @"\s", ""); // Would be better to transliterate into one-character system, so that overlap is identified correctly (not H, for example). cmu_to_monochar dictionary
                    var x = pronscorex.Key; //convertArpaMonochar(pronscorex.Key);

                    var scorex = pronscorex.Value;
                    if (orthx == orthin)
                        if (nbestPronout.ContainsKey(x))
                        {
                            nbestPronout[x] += (scorex * 2);
                        }
                        else
                        {
                            nbestPronout.Add(x, (scorex * 2));
                        }
                    foreach (var orthz in substringScores.Keys)
                    {

                        if (orthz != orthx)
                        {

                            //Console.Write("\n\t{0}\n", orthz);
                            string orthy = overlap(orthx, orthz); //finds overlap string
                            string orthxz = combineStrings(orthx, orthy, orthz);
                            // Check to see that orthxz is contained in orthin, otherwise don't bother finding prons to overlap
                            if (orthin.Contains(orthxz))
                            {
                                var sortedSubstringsZ = substringScores[orthz].OrderByDescending(pair => pair.Value).Take(5);
                                foreach (var pronscorez in sortedSubstringsZ)
                                {
                                    //var z = Regex.Replace(pronscorez.Key, @"\s", "");
                                    var z = pronscorez.Key; //convertArpaMonochar(pronscorez.Key);

                                    var scorez = pronscorez.Value;
                                    if (orthz == orthin)
                                        if (nbestPronout.ContainsKey(z))
                                        {
                                            nbestPronout[z] += (scorez * 2);
                                        }
                                        else
                                        {
                                            nbestPronout.Add(z, (scorez * 2));
                                        }
                                    string y = overlap(x, z); //finds overlap string
                                    string pronxz = combineStrings(x, y, z);
                                    int scorey = y.Length - 1; //i.e., len(overlap(xz))
                                    double scorexz = overlapScore(x, scorex, z, scorez, y, scorey);
                                    if (scorexz > prevHigh)
                                    {
                                        if (orthxz == orthin)
                                            if (nbestPronout.ContainsKey(pronxz))
                                            {
                                                nbestPronout[pronxz] += scorexz;
                                            }
                                            else
                                            {
                                                nbestPronout.Add(pronxz, scorexz);
                                            }

                                        if (overlapScores.ContainsKey(orthxz)) // Add pron-score pair to substring dictionary
                                        {
                                            //Adds only the value {pron, score} to the entry for orth
                                            if (overlapScores[orthxz].ContainsKey(pronxz))
                                            {
                                                overlapScores[orthxz][pronxz] += scorexz;
                                            }
                                            else
                                            {
                                                overlapScores[orthxz].Add(pronxz, scorexz);
                                            }

                                        }
                                        else
                                        {
                                            //Adds whole new entry
                                            overlapScores.Add(orthxz, new Dictionary<string, double> { { pronxz, scorexz } });

                                        }
                                        //Console.Write("\t{0}\t+\t{1} => {2}\n", orthx, orthz, orthxz);
                                        //Console.Write("\t{0}\t+\t{1} => {2}\n", x, z, pronxz);
                                        Console.Write("{0}\t{1}\t{2}\n", pronxz, orthxz, scorexz);
                                        prevHigh = scorexz;
                                    }


                                }


                            }

                        }

                    }

                }


            }

            var oldOverlaps = overlapScores;

            //loops until it either finds 10 possible prons, or finishes fifth loop (don't want it looking forever)
            int ct = 1;
            while ((nbestPronout.Count < 5) && ct < 5)
            {
                Dictionary<string, Dictionary<string, double>> latestOverlaps = new Dictionary<string, Dictionary<string, double>> { };
                //var latestOverlaps = latestOverlapScores(ct);

                // Code to find overlaps and scores, add to new dictionary
                // take from previous dictionary of overlap scores, add overlaps to latest
                //Finds normalized score for each orth in substringScores, finds overlap score for each pair of substrings, adds to overlapScores dictionary
                foreach (var orthx in oldOverlaps.Keys)// sort pron - score pairs, descending by score; takes only highest 5
                {
                    //Console.Write("\nOrthx:\t{0}\n", orthx);
                    double prevHigh = 0.0;
                    var sortedSubstringsX = oldOverlaps[orthx].OrderByDescending(pair => pair.Value).Take(5);
                    foreach (var pronscorex in sortedSubstringsX)
                    {
                        //var x = Regex.Replace(pronscorex.Key, @"\s", ""); // Would be better to transliterate into one-character system, so that overlap is identified correctly (not H, for example). cmu_to_monochar dictionary
                        var x = pronscorex.Key; //convertArpaMonochar(pronscorex.Key);

                        var scorex = pronscorex.Value;
                        foreach (var orthz in oldOverlaps.Keys)
                        {

                            if (orthz != orthx)
                            {
                                //Console.Write("\n\t{0}\n", orthz);
                                string orthy = overlap(orthx, orthz); //finds overlap string
                                string orthxz = combineStrings(orthx, orthy, orthz);
                                // Check to see that orthxz is contained in orthin, otherwise don't bother finding prons to overlap
                                if (orthin.Contains(orthxz))
                                {
                                    var sortedSubstringsZ = oldOverlaps[orthz].OrderByDescending(pair => pair.Value).Take(5);
                                    foreach (var pronscorez in sortedSubstringsZ)
                                    {
                                        //var z = Regex.Replace(pronscorez.Key, @"\s", "");
                                        var z = pronscorez.Key; //convertArpaMonochar(pronscorez.Key);

                                        var scorez = pronscorez.Value;
                                        string y = overlap(x, z); //finds overlap string
                                        string pronxz = combineStrings(x, y, z);
                                        int scorey = y.Length; //i.e., len(overlap(xz))
                                        double scorexz = overlapScore(x, scorex, z, scorez, y, scorey);
                                        if (scorexz > prevHigh)
                                        {
                                            if (orthxz == orthin)
                                                if (nbestPronout.ContainsKey(pronxz))
                                                {
                                                    nbestPronout[pronxz] += scorexz;
                                                }
                                                else
                                                {
                                                    nbestPronout.Add(pronxz, scorexz);
                                                }

                                            if (latestOverlaps.ContainsKey(orthxz)) // Add pron-score pair to substring dictionary
                                            {
                                                //Adds only the value {pron, score} to the entry for orth
                                                if (latestOverlaps[orthxz].ContainsKey(pronxz))
                                                {
                                                    latestOverlaps[orthxz][pronxz] += scorexz;
                                                }
                                                else
                                                {
                                                    latestOverlaps[orthxz].Add(pronxz, scorexz);
                                                }

                                            }
                                            else
                                            {
                                                //Adds whole new entry
                                                latestOverlaps.Add(orthxz, new Dictionary<string, double> { { pronxz, scorexz } });

                                            }
                                            //Console.Write("\t{0}\t+\t{1} => {2}\n", orthx, orthz, orthxz);
                                            //Console.Write("\t{0}\t+\t{1} => {2}\n", x, z, pronxz);
                                            Console.Write("{0}\t{1}\t{2}\n", pronxz, orthxz, scorexz);
                                            prevHigh = scorexz;
                                        }


                                    }


                                }

                            }

                        }

                    }


                }
                Console.Write("\n--------\nFinished PASS no. {0}\n--------\n", ct);
                ct++;

                oldOverlaps = latestOverlaps;

            }




            return nbestPronout;
        }

        //Create pron variants
        //Equivalences: O and A, I and x, x and nothing...
        public static List<string> equivProns(string orig)
        {
            string equiv;
            Regex twoCons = new Regex(@"([bdfgklmnprstvwjzCDhGNSTZ])([bdfgklmnprstvwjzCDhGNSTZ$]{2})", RegexOptions.Compiled);
            List<string> equivPronList = new List<string>();

            if (orig.Contains("x"))
            {
                equiv = Regex.Replace(orig, @"x", "");
                equivPronList.Add(equiv);
                //equiv = Regex.Replace(orig, @"x", "I");
                //equivPronList.Add(equiv);
            }
            else
            {
                Match match = twoCons.Match(orig);
                if (match.Success)
                {
                    equiv = twoCons.Replace(orig, "$1x$2");
                    equivPronList.Add(equiv);
                }
            }
            if (orig.Contains("I"))
            {
                equiv = Regex.Replace(orig, @"I", "x");
                equivPronList.Add(equiv);
            }
            if (orig.Contains("O"))
            {
                equiv = Regex.Replace(orig, @"O([^r]*)", "A$1");
                equivPronList.Add(equiv);
            }
            else
            {
                equivPronList.Add(orig);
            }

            return equivPronList;
        }


        // Newly selected pron should be fed into the JSON file with a score
        // But it should also be compared against existing wts entries for LCS, to increase those wts, as well


        //Combines two strings x and z, removing overlap y
        public static string combineStrings(string x, string y, string z)
        {
            Regex yEnd = new Regex(@y + "$", RegexOptions.Compiled);
            string xz = yEnd.Replace(x, "") + z; //cuts off overlap y from end of x, pastes z on
            return xz;
        }

        //Calculate overlap score: score(xz) = (score(x) * score(z)) * 2^len(overlap(xz))
        public static double overlapScore(string x, double scorex, string z, double scorez, string y, int scorey)
        {
            double scorexz = (scorex + scorez) * (2 ^ scorey); //combined score, including overlap

            return scorexz;
        }



        //Find string overlap y
        public static string overlap(string x, string z)
        {

            string y; // overlap of x and y
            string lcs; // LCS of x and z, potentially the overlap
            int maxlen = LongestCommonSubstring(x, z, out lcs);
            if (x.EndsWith(lcs) && z.StartsWith(lcs))
            {
                y = lcs;
            }
            else
            {
                y = "";
            }

            return y;
        }



        //Converts ARPAbet transcription to Monocharacter transcription, for better string matching. Would use IPA, except that not all phonemes are one character.
        public static string convertArpaMonochar(string arpa)
        {
            string monochar;
            List<string> arpaList;
            arpaList = Regex.Split(arpa, @"\s+").ToList();
            //Console.Write("{0}\n", arpa);
            try
            {
                monochar = String.Join("", (from string ap in arpaList select cmu_to_monochar[ap]).ToList());
            }
            catch
            {
                monochar = "";
            }

            //foreach (string ap in arpaList)

            return monochar;
        }

        //Simplifies geminates


        // Copied from Wikibooks. Calculates longest common substring, outputs as sequence using out keyword.
        public static int LongestCommonSubstring(string str1, string str2, out string sequence)
        {
            sequence = string.Empty;
            if (String.IsNullOrEmpty(str1) || String.IsNullOrEmpty(str2))
                return 0;

            int[,] num = new int[str1.Length, str2.Length];
            int maxlen = 0;
            int lastSubsBegin = 0;
            StringBuilder sequenceBuilder = new StringBuilder();

            for (int i = 0; i < str1.Length; i++)
            {
                for (int j = 0; j < str2.Length; j++)
                {
                    if (str1[i] != str2[j])
                        num[i, j] = 0;
                    else
                    {
                        if ((i == 0) || (j == 0))
                            num[i, j] = 1;
                        else
                            num[i, j] = 1 + num[i - 1, j - 1];

                        if (num[i, j] > maxlen)
                        {
                            maxlen = num[i, j];
                            int thisSubsBegin = i - num[i, j] + 1;
                            if (lastSubsBegin == thisSubsBegin)
                            {//if the current LCS is the same as the last time this block ran
                                sequenceBuilder.Append(str1[i]);
                            }
                            else //this block resets the string builder if a different LCS is found
                            {
                                lastSubsBegin = thisSubsBegin;
                                sequenceBuilder.Length = 0; //clear it
                                sequenceBuilder.Append(str1.Substring(lastSubsBegin, (i + 1) - lastSubsBegin));
                            }
                        }
                    }
                }
            }
            sequence = sequenceBuilder.ToString();
            return maxlen;
        }







    }
}




