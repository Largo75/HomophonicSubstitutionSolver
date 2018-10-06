using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/*
This project aims to crack ciphers based on homophonic substitution. It uses a combination of hillclimbing and simulated annealing. Many thanks to Jarlve for the support and the many valuable tips.
The goal is to crack the yet unsolved 340 cipher of the Zodiac killer. Three solvable ciphers are included for testing the solver. Among them is the already cracked z408. If you're not familiar with the case: the plaintext of this cipher is quite intriguing and disturbing.
https://en.wikipedia.org/wiki/Zodiac_Killer

Requirements:
- Visual Studio 2017 Community Edition or similiar.
- The Microsoft .net Framework 4.7.2 Developer Pack. You can find it here: https://www.microsoft.com/net/download/visual-studio-sdks


In order for the nGram corpus to be loaded correctly, the working directory must be set to the projects root folder. (Right click "HomophonicSubstitutionSolver" in Project view. Then "Debug" -> Working Directory. Make sure to choose "All configurations" in the upper dropdown field).
In release mode, the solver on an i7 8700 requires round about 450 milliseconds. In debug mode about 1.5 seconds.
Since the solver must be as performant as possible, abstraction was largely avoided. Assertions, exception handling and all the other conveniences were also not used. Even comfortable containers were not used in favor of simple arrays. The source code should therefore be treated like a raw egg: very careful!
For performance reasons, arrays are not accessed through the index operator. Instead, the "fixed" keyword is used. If you do not know this and find the syntax confusing, you first have to read in here.



If this solver should lead to the cracking of the previously unsolved z340 of the Zodiac Killer, then please make sure to refer to the following forum:
http://zodiackillersite.com/viewforum.php?f=81
Some of the members there have put years of work into this topic.

If you have any questions, suggestions or ideas, please also use the forum you just linked to.

 * */
namespace HomophonicSubstitutionSolver
{
    class Program
    {
        static NGrams nGramsEnglish = new NGrams(filename: "Corpora/5grams_english.txt");

        private static int targetIterations = 500000;
        private static float startTemperature = 27.0f;
        private static float icWeight = 7.0f;

        private static string cipherText_z340 = "HERabcdVPeIfLTGghNb+BjkOlDWYmnoKpqBrstM+UZGWjqLkuHJSbbvdcwoVxbO++RKgyzM+u12hI7FP+34e5bwRdFcO-ohCeFagDjk7+KQl8gUtXGVmuLIjGgJp2kO+yNYu+9LzhnM+0+ZRgFBtrA#4K-ucUV+dJ+ObvnFBr-U+R571EIDYBb0TMKOgntcRJIo7T4Mm+3BFu#zSrk+NI7FBtj8wRcGFNdp7g40mtV41++rBXfos4zCEaVUZ7-+ItmxuBKjObdmpMQGgRtT+Lf#Cn+FcWBIqL++qWCuWtPOSHT5jqbIFehWnv1ByYOBo-CtaMDHNbeSuZOwAIK8+";
        private static string cipherText_z408First340Letters = "abPcZcUBbdORefXeBWV+gGYFhaHPiKjkYgMJYlUIdmkTnNQYDopS1carBPORAUbsRtkEdlLMZJvyzfFHVWgwYi+kGDaKIphkXwoxS1RNnjYEtOwkGBTQSrBLvcPrBiXkEHMUlRRdCZKkfIpWkjwoLMyarBPDR+uehzN1gEZHdFZCfOVWIo+nLptlRhHIaDRqTYyzvgciXJQAPoMwRUnbLpNVEKHeGyIjJdoawLMtNApZ1PxUfdAarBVWz+VTnOPleSytsUghmDxGbbIMNdpSCEcabbZsAPrBVfgXkWkqFrwC+iaAaBbOToRUC+qvYkqlSkWVZgGYKEqTYAabrLnq";
        private static string cipherText_testcase1 = "hEXw;TIcnowGadU1C-1sV5AJ9=rEFNhLdws2+IcQ3ZqkBC;yYuZT+B5LLcU;TIcAJ0tM4UPkldowpg1v;;anXIKC-sTR5Ax;3DqBM=;yESToInX;obp=Xu2iHZ9d7kDRBl+Lnh0HtPiUpZnkmh;;TOrhLm=BTJXw;dVV5gHhoFXv:kQajAXlqlxB73QyGE+wIDt=2l20AcuX=j4iejAXlNFZLMS;;Pnoga;xHs=:d1sIXV3QytDRkCI52dBNp+bp=hb0TPZFckd:lDTKli7RZ9gLpanxN2XM3lyGys204PoNkdT1=I;TUCIgD5ALqXrKah7b=;YUicqcAlpxqTXS";
        private static string cipherText_testcase2 = "dohlD1EXcA=QanLFxbXGjiqkj3VtT;=r7wZCRz5KdleDY8ihAIZnGy:udqB2cM;X7+Tp0lDob=vRsCA5IhV1NYtXHSePk;TYczL=UiqBwFf7YnXHrbgk;TGZYCaV5KdhBM8DRHiAkBx:cQ3t=2XnspyH+QCuF0;ZTNdjPTD5L=XjiU:=I2ho;XqfTZ7kcwld1DYGg8sTYaRATCN=XjiuU=Ip5z;XqxTT3ZFd7Bh9V=vJ2LcwpX;y8=;X:uTKF+Ml=t0s2DNopPHUXQf;gR=AaQxnlXS:k1F3uyiQ+;04rLw2PBfQCU8ZjgspdNl=;DqTaiFIXGT=vj7Xlz2xLV5e";

        // ------------------------------------------------------------------------------------------------------------------
        static void Main(string[] args)
        {
            Console.WriteLine("Solver started");
            HomophonicSolver solver = new HomophonicSolver(nGrams: nGramsEnglish);

            int numRestarts = 20;
            float bestScore = 0.0f;

            // Simple test: Just restart the solver 20 times and track the best solution
            // You may also try cipherText_z408First340Letters or cipherText_testcase1.
            for (int i = 1; i < numRestarts; i++)
            {
                Console.WriteLine("Restarts: " + i);
                solver.Init(cipherText: cipherText_testcase2);
                solver.Solve(targetIterations: targetIterations, startTemperature: startTemperature, icWeight: icWeight, score: out float score, solution: out string solution);

                if (score > bestScore)
                {
                    bestScore = score;
                    Console.WriteLine("Score: " + score);
                    Console.WriteLine(solution);
                    Console.WriteLine("-----------------");
                }
            }


            Console.WriteLine("Solver finished");
            Console.ReadKey();
        }

    }
}
