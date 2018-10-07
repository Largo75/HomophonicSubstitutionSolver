using System;
using System.Collections.Generic;
using System.Text;

namespace HomophonicSubstitutionSolver
{
    class HomophonicSolver : SubstitutionSolverBase
    {
        // ------------------------------------------------------------------------------------------------------------------
        private List<int> letterFrequenciesEnglish = new List<int>();
        private int letterFrequencyTableSize = 0;

        // ------------------------------------------------------------------------------------------------------------------
        public HomophonicSolver(NGrams nGrams) : base(nGrams)
        {
            
            Random rnd = new Random(Guid.NewGuid().GetHashCode());

            // Letter frequencies for common english plaintexts
            float[] letterFrequencies =
            {
                8.167f,     // a
                1.492f,     // b
                2.782f,     // c
                4.253f,     // d
                12.703f,    // e
                2.228f,     // f
                2.015f,     // g
                6.094f,     // h
                6.966f,     // i
                0.153f,     // j
                0.772f,     // k
                4.025f,     // l
                2.406f,     // m
                6.749f,     // n
                7.507f,     // o
                1.929f,     // p
                0.095f,     // q
                5.987f,     // r
                6.327f,     // s
                9.056f,     // t
                2.758f,     // u
                0.978f,     // v
                2.360f,     // w
                0.150f,     // x
                1.974f,     // y
                0.074f      // z
            };

            // Create a table which contains the numbers 0-25 (A-Z) according to
            // the letter frequencies.
            // e.g. 816 times "0" ("A"), 1270 times "4" ("E") and so on.
            for (int i = 0; i < letterFrequencies.Length; i++)
            {
                for (float k = 0; k < letterFrequencies[i] * 100.0f; k++)
                {
                    letterFrequenciesEnglish.Add(i);
                }
            }

            letterFrequencyTableSize = letterFrequenciesEnglish.Count;
        }

        // ------------------------------------------------------------------------------------------------------------------
        public override void InitializeRandomKey()
        {
            key = new Int64[uniqueSymbolCount];

            Random rnd = new Random(Guid.NewGuid().GetHashCode());

            for (int i=0; i<uniqueSymbolCount; i++)
            {
                key[i] = letterFrequenciesEnglish[rnd.Next(letterFrequencyTableSize)];
            }
        }

        // ------------------------------------------------------------------------------------------------------------------
        public override unsafe void Solve(float targetIterations, float startTemperature, float icWeight, out float score, out string solution)
        {
            float coolingRate = startTemperature / targetIterations;
            float temperature = startTemperature;
            this.icWeight = icWeight;

            float currentScore = 1.0f;

            // Calclulate the plaintext score
            fixed (Int64* pk = key)
            {
                fixed (Int64* pc = cipherTextAsNumbers)
                {
                    for (Int64 i = 0; i < cipherTextLength - nGrams.nGramLength; i++)
                    {
                        // The following is a bit hard to read because of optimization and dereferencing the fixed array (pointers)
                        currentScore += nGrams.nGrams[*(pk + (*(pc + i + 4))) + 26 * (*(pk + *(pc + i + 3))) + 676 * (*(pk + *(pc + i + 2))) + 17576 * (*(pk + *(pc + i + 1))) + 456976 * (*(pk + *(pc + i)))];

                        // Assume the array as a 5 dimensional array (5grams). Then it would be something like this:
                        //int letter1 = key[cipherTextAsNumbers[i + 4];
                        //int letter2 = key[cipherTextAsNumbers[i + 3];
                        //int letter3 = key[cipherTextAsNumbers[i + 2];
                        //int letter4 = key[cipherTextAsNumbers[i + 1];
                        //int letter5 = key[cipherTextAsNumbers[i];
                        //score += nGrams[letter1][letter2][letter3][letter4][letter5];
                        
                    }
                }
            }

            DetermineUnigramDistribution();

            float currentIC = GetIndexOfCoincidence();
            // Normalize the score
            currentScore *= 1.0f + (currentIC / (float)(cipherTextLength * (cipherTextLength - 1)) * icWeight);

            float bestScore = currentScore;
            float acceptanceProbability = 0.0f;

            Int64 iterations = 0;
            Int64 symbolIndex = 0;
            Int64 letterIndex = 0;
            Int64 oldLetter = 0;
            float newScore = 0.0f;
            float distOld1, distNew1, distOld2, distNew2 = 0.0f;
            float oldIC = 0.0f;

            Random rnd = new Random(Guid.NewGuid().GetHashCode());

            // Hill climber + simulated annealing
            while (temperature > 1)
            {
                iterations++;

                // Change one letter                
                letterIndex = letterFrequenciesEnglish[rnd.Next(letterFrequencyTableSize)];

                // Make sure we picked a different letter.
                // If the same letter is picked again we increase it. This improves the solve rate slightly.
                if (key[symbolIndex] == letterIndex)
                {
                    letterIndex++;
                    if (letterIndex >= nGrams.alphabetSize)
                        letterIndex = 0;
                }

                // Remember the old letter
                oldLetter = key[symbolIndex];
                key[symbolIndex] = letterIndex;

                newScore = 1.0f;

                // Duplicated code. I don't move this into a method because of performance reasons.
                // Calculate the plaintext score
                fixed (Int64* pk = key)
                {
                    fixed (Int64* pc = cipherTextAsNumbers)
                    {
                        for (Int64 i = 0; i < cipherTextLength - nGrams.nGramLength; i++)
                        {
                            newScore += nGrams.nGrams[*(pk + (*(pc + i + 4))) + 26 * (*(pk + *(pc + i + 3))) + 676 * (*(pk + *(pc + i + 2))) + 17576 * (*(pk + *(pc + i + 1))) + 456976 * (*(pk + *(pc + i)))];
                        }
                    }
                }

                Int64 symbolCount = symbolDistribution[symbolIndex];
                oldIC = currentIC;

                // We don't need to determine the unigram distribution again.
                // It is better to consider only the number of new and dropped letters.
                distOld1 = icTable[unigramDistribution[oldLetter]];
                distOld2 = icTable[unigramDistribution[letterIndex]];

                unigramDistribution[oldLetter] -= symbolCount;
                unigramDistribution[letterIndex] += symbolCount;

                distNew1 = icTable[unigramDistribution[oldLetter]];
                distNew2 = icTable[unigramDistribution[letterIndex]];
                                
                // Same for index of coincidence. We don't need to calculate the whole thing.
                currentIC -= Math.Abs(distOld1-distNew1);
                currentIC += Math.Abs(distOld2-distNew2);                

                // Normalize the score
                newScore *= 1.0f + (currentIC / (float)(cipherTextLength * (cipherTextLength - 1)) * icWeight);

                // Accept/Drop?
                if (newScore > currentScore)
                {
                    acceptanceProbability = 1.0f;
                }
                else
                {
                    acceptanceProbability = (float)Math.Exp((newScore-currentScore)/temperature);
                }

                if (acceptanceProbability > rnd.NextDouble() / 1.0)
                {
                    currentScore = newScore;
                }
                else
                {
                    // If we drop the current solution, we have to restore the old distribution and the old IC.
                    unigramDistribution[oldLetter] += symbolCount;
                    unigramDistribution[letterIndex] -= symbolCount;

                    currentIC = oldIC;

                    key[symbolIndex] = oldLetter;
                }

                // Keep track of the best solution found
                bestScore = Math.Max(currentScore, bestScore);
  
                symbolIndex++;
                if (symbolIndex >= uniqueSymbolCount)
                {
                    symbolIndex = 0;
                }

                // Cool down the system
                temperature -= coolingRate;
            }
            
            // TODO: Not the best idea. (Adding 20k to get positive scores)
            score = currentScore + 20000.0f;   
            StringBuilder builder = new StringBuilder();

            // Build the solution
            for (Int64 i =0; i<cipherTextLength; i++)
            {
                builder.Append((char)(key[cipherTextAsNumbers[i]] + 65));
            }

            solution = builder.ToString();
        }
    }
}
