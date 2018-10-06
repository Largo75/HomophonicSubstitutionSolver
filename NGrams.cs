using System;
using System.IO;
using System.Diagnostics;

namespace HomophonicSubstitutionSolver
{
    public struct NGrams
    {
        // ------------------------------------------------------------------------------------------------------------------
        #region Fields
        public float[] nGrams;      // 1D-Array for nGrams (faster than multidimensional array)
        public int alphabetSize;    // Usualla 26 (A-Z)
        public int nGramLength;     // Currently only 5-grams are supported
        #endregion

        // ------------------------------------------------------------------------------------------------------------------
        #region Methods
        public NGrams(string filename)
        {
            alphabetSize = 26;
            nGramLength = 5;

            var watch = Stopwatch.StartNew();

            // Initialize an empty array for nGrams
            int nGramMaxCount = alphabetSize * alphabetSize * alphabetSize * alphabetSize * alphabetSize;
            nGrams = new float[nGramMaxCount];

            for (int i = 0; i < nGramMaxCount; i++)
            {
                nGrams[i] = 0.0f;
            }

            // In case of a crash: Did you forget to set the working directory in the project settings?
            var lines = File.ReadLines(filename);
            int nGramCountInCorpus = 0;
            int nGramCountInFile = 0;
            int uniqueNGramCount = 0;

            String nGram = "";
            int nGramCount = 0;

            // Read the nGram file line by line
            foreach (var line in lines)
            {
                if (nGramCountInCorpus == 0)
                {
                    // The first line of the file contains the total count of ngrams in the plaintext corpus
                    nGramCountInCorpus = Convert.ToInt32(line);
                }
                else
                {
                    // Read the ngram and its count
                    nGram = line.Substring(0, nGramLength);
                    nGramCount = Convert.ToInt32(line.Substring(nGramLength));
                    nGramCountInFile++;

                    // Since we use an 1D-Array we need to calculate the index (26^n)
                    int index = (int)nGram[4] - 65; // "A" = ASCII 65
                    index += 26 * ((int)nGram[3] - 65);
                    index += 676 * ((int)nGram[2] - 65);
                    index += 17576 * ((int)nGram[1] - 65);
                    index += 456976 * ((int)nGram[0] - 65);

                    nGrams[index] = (float)nGramCount;
                }
            }

            // Calculate lowest log score for nGrams that do not appear in the corpus
            float minLogFrequency = (float)Math.Log(0.01f / (float)nGramCountInCorpus);
            float logScore = 0.0f;

            // Now the array contains the number of the particular nGrams.
            // But for the plaintext scoring we need the log score. Let's calculate that...
            for (int i = 0; i < alphabetSize; i++)
            {
                for (int j = 0; j < alphabetSize; j++)
                {
                    for (int k = 0; k < alphabetSize; k++)
                    {
                        for (int l = 0; l < alphabetSize; l++)
                        {
                            for (int m = 0; m < alphabetSize; m++)
                            {
                                int index = m + 26 * l + 676 * k + 17576 * j + 456976 * i;

                                if (nGrams[index] == 0)
                                {
                                    nGrams[index] = minLogFrequency;
                                }
                                else
                                {
                                    logScore = (float)Math.Log(nGrams[index] / (float)nGramCountInCorpus);
                                    nGrams[index] = logScore;

                                    uniqueNGramCount++;
                                }
                            }
                        }
                    }
                }
            }

            var elapsedMs = watch.ElapsedMilliseconds;

            Console.WriteLine("nGrams loaded from file: " + filename);
            Console.WriteLine("Loaded in " + elapsedMs + " milliseconds");
            Console.WriteLine("Number of nGrams in file: " + nGramCountInFile);
            Console.WriteLine("Total nGram count in source corpus: " + nGramCountInCorpus);
        }
        #endregion
    }
}
