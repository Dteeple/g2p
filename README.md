# g2p

C# program for guessing pronunciation from orthography ("grapheme-to-phoneme mapping")

g2p_forms_app/g2p_form.sln is the basic version of the program. A user can type in an orthography, generate g2p guesses, choose the best guess, modify it, and potentially save the results to a dictionary (this last element has not been added). 

The results *are* saved to the lcs weights dictionary (bin\Debug\wts_updated.json), which assigns weights to orthography-phonetic mappings, derived originally by comparing all longest common substrings (LCS) between two orthographies in the CMU dictionary, and corresponding LCS between their transcriptions.

Example:  orthographies "honey" and "honor" have an LCS "hon" and their transcriptions, "HH AH1 N IY0" and "AA1 N ER0" have LCS "N". So, in this case, the ortho-phonetic mapping "hon"-"N" is augmented by a weight of 1 in the weights dictionary.  The more common the mapping, the higher the weight.

The g2p program will add to those weights, as it interacts with the user.

g2p_loop_thru_dict.sln is meant to loop through the existing Dictionary.txt file and identify forms with missing transcriptions, to be guessed by the g2p program and applied to an edited version of the dictionary. Currently it does not successfully loop, but stops on the first identified form.
