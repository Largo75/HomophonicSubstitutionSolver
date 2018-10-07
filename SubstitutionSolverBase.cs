using System;
using System.Collections.Generic;


namespace HomophonicSubstitutionSolver
{
    public unsafe abstract class SubstitutionSolverBase
    {
        // ------------------------------------------------------------------------------------------------------------------
        #region Fields
        protected NGrams nGrams;
        private string cipherText = "";
        protected int cipherTextLength = 0;
        protected int uniqueSymbolCount = 0;
        private List<Int64> uniqueSymbols;
        public Int64[] unigramDistribution; // Unigram distribution in plaintext
        protected Int64[] symbolDistribution; // Unigram distribution in ciphertext
        protected Int64[] cipherTextAsNumbers;
        protected Int64[] key;
        protected float icWeight = 7.0f;
        protected float[] icTable; // A table for precalculated values (speeding up index of coincidence calculation)
        protected const int maxICTableSize = 500;
        #endregion

        // ------------------------------------------------------------------------------------------------------------------
        #region Methods
        public SubstitutionSolverBase(NGrams nGrams)
        {
            this.nGrams = nGrams;
        }

        // ------------------------------------------------------------------------------------------------------------------
        public void Init(string cipherText)
        {
            this.cipherText = cipherText;
            cipherTextLength = cipherText.Length;

            unigramDistribution = new Int64[nGrams.alphabetSize];
            
            DetermineUniqueCipherSymbols();

            InitializeRandomKey();            
            ConvertCipherToNumbers();

            DetermineSymbolDistribution();
            DetermineUnigramDistribution();

            // Create the table for precalculated ic values
            icTable = new float[maxICTableSize];

            for (int i=0; i< maxICTableSize; i++)
            {
                icTable[i] = i * (i - 1);
            }
            
        }

        // ------------------------------------------------------------------------------------------------------------------
        public void DetermineUnigramDistribution()
        {
            for (Int64 i = 0; i < nGrams.alphabetSize; i++)
            {
                unigramDistribution[i] = 0;
            }

            for (int i=0; i<uniqueSymbolCount; i++)
            {
                unigramDistribution[key[i]] += symbolDistribution[i];
            }
        }

        // ------------------------------------------------------------------------------------------------------------------
        private void DetermineSymbolDistribution()
        {
            symbolDistribution = new Int64[uniqueSymbols.Count];

            for (int i=0; i<uniqueSymbols.Count; i++)
            {
                symbolDistribution[i] = 0;
            }

            for (int i = 0; i < cipherTextAsNumbers.Length; i++)
            {
                symbolDistribution[cipherTextAsNumbers[i]]++;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------
        private void ConvertCipherToNumbers()
        {
            cipherTextAsNumbers = new Int64[cipherTextLength];
            Int64 index = 0;

            for (int i =0; i<cipherTextLength; i++)
            {
                for (int k =0; k<uniqueSymbolCount; k++)
                {
                    if (cipherText[i] == uniqueSymbols[k])
                    {
                        cipherTextAsNumbers[index] = k;
                        index++;
                    }
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------------
        public void DetermineUniqueCipherSymbols()
        {
            uniqueSymbols = new List<Int64>();
            
            for (int i =0; i<cipherTextLength; i++)
            {
                if (!uniqueSymbols.Contains(cipherText[i]))
                {
                    uniqueSymbols.Add(cipherText[i]);
                }
            }

            uniqueSymbolCount = uniqueSymbols.Count;
        }

        // ------------------------------------------------------------------------------------------------------------------
        public float GetIndexOfCoincidence()
        {
            // This method calculates the raw index of coincidence. The following calculation
            // has to be done separately:
            // ic /= (float)(cipherTextLength * (cipherTextLength - 1));


            // DO NOT FORGET TO UPDATE "unigramDistribution" BEFORE CALLING THIS METHOD!
            float ic = 0.0f;

            fixed (float* pi = icTable)
            {
                for (Int64 i = 0; i < nGrams.alphabetSize; i++)
                {
                    ic += *(pi + unigramDistribution[i]);
                }             
            }

            return ic;
        }

        // ------------------------------------------------------------------------------------------------------------------
        public abstract void InitializeRandomKey();
        public abstract void Solve(float targetIterations, float startTemperature, float icWeight, out float score, out string solution);
        #endregion
    }
}
