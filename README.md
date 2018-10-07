# HomophonicSubstitutionSolver
A solver for homophonic substitution ciphers

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

If you have any questions, suggestions or ideas, please also use the forum linked above.
