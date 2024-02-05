# Leorik
<p align="center">
<img src="https://github.com/lithander/Leorik/blob/master/Leorik.Logo.png" alt="Leorik Logo" width="600"/>
</p>

Unshackled from the constraints of minimalism and simplicity **Leorik** is the successor to my bare-bones chess engine [MinimalChess](https://github.com/lithander/MinimalChessEngine).

## Features
* Pseudo-legal move generator, bitboards and copy/make.
* Negamax search with Alpha-Beta pruning
* Principal Variation Search (PVS)
* Iterative Deepening & Aspiration windows
* Simple NNUE evaluation: (768->256)x2->1
* Network trained on selfplay games using [Bullet](https://github.com/jw1912/bullet)
* Lockfree Transposition Table with two buckets and aging
* Static Exchange Evalution (SEE)
* Null-Move Pruning
* Staged move generation
	- MVV-LVA sorted captures
	- Killer, Counter, Followup
	- History sorted quiets with LMR
* Lazy SMP parallel search

## How to play

**Leorik** does not provide its own user interface. Instead it implements the [UCI](https://en.wikipedia.org/wiki/Universal_Chess_Interface) protocol to make it compatible with most popular Chess GUIs such as:
* [Arena Chess GUI](http://www.playwitharena.de/) (free)
* [BanksiaGUI](https://banksiagui.com/) (free)
* [Cutechess](https://cutechess.com/) (free)
* [Nibbler](https://github.com/fohristiwhirl/nibbler/releases) (free)
* [Chessbase](https://chessbase.com/) (paid).

Once you have a chess GUI installed you can download prebuild [binaries for Mac, Linux or Windows](https://github.com/lithander/Leorik/releases/) and extract the contents of the zip file into a location of your choice.

As a final step you have to register the engine with the GUI. The details depend on the GUI you have chosen but there's usually something like "Add Engine..." in the settings.

Now you should be ready to select **Leorik** as a player!

## Version History
### Leorik 3.0
[__Version 3.0__](https://github.com/lithander/Leorik/releases/tag/3.0) combines the feature set of Version 2.5.6 with a NNUE based evaluation that completely replaces the handcrafted one. The neural network is based on a relatively simple architecture with 768 inputs and one hidden layer of 256 neurons. It was trained using [Bullet](https://github.com/jw1912/bullet) on 622M labeled positions extracted from selfplay games. Despite the modest complexity of the NNUE architecture it has contributed most of the Elo gains of this version. The rest is from the adoption of Aspiration Windows and a revamped staged move generation that replaces the 2nd Killer move with a Counter and FollowUp move if available.

Version 3.0 is expected to play at a strength of ~3300 CCRL Elo.

### Leorik 2.5
[__Version 2.5__](https://github.com/lithander/Leorik/releases/tag/2.5) now employs a faster move generator based on PEXT by default. The evaluation replaces the standard PSQTs with linear functions that calculate the piece-square values using 18 parameters reflecting the positions of both kings and the game phase. To compute these values at a high speed the engine uses AVX2. Leorik now supports the 'Threads' UCI option. Each thread searches independentaly ("Lazy SMP") but can share information with each others through the lockless transposition table.

Version 2.5 is the first version that is published under the permissive **MIT open-source license**. It is listed at [2918 Elo](https://computerchess.org.uk/ccrl/404/cgi/engine_details.cgi?print=Details&each_game=1&eng=Leorik%202.5%2064-bit#Leorik_2_5_64-bit) on the CCRL Blitz and [2918 Elo](https://computerchess.org.uk/ccrl/4040/cgi/engine_details.cgi?match_length=30&each_game=0&print=Details&each_game=0&eng=Leorik%202.5%2064-bit#Leorik_2_5_64-bit) on the CCRL 40/15 rating lists.

### Leorik 2.4
[__Version 2.4__](https://github.com/lithander/Leorik/releases/tag/2.4) uses Static-Exchange-Evaluation (SEE) to skip bad captures in quiescence search and to search bad moves at a reduced depth in the main search. Futility Pruning was removed from the main search but the idea of basing a pruning-decision on the static evaluation of a position is now used in a much more radical Null Move Pruning implementation. Last but not least Leorik now recognizes certain drawn positions with a material advantage for one side (e.g. KvKNN) and evaluates them much closer to zero.

Version 2.4 is listed at [2831 Elo](http://computerchess.org.uk/ccrl/404/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%202.4%2064-bit#Leorik_2_4_64-bit) on the CCRL Blitz and [2799 Elo](http://computerchess.org.uk/ccrl/4040/cgi/engine_details.cgi?print=Details&each_game=0&eng=Leorik%202.4%2064-bit#Leorik_2_4_64-bit) on the CCRL 40/15 rating lists.

### Leorik 2.3
[__Version 2.3__](https://github.com/lithander/Leorik/releases/tag/2.3) replaces all previously handcrafted evaluation terms with tunable weights and all weights are tuned from scratch on selfplay games.
The first batch of games was played with a version that only knew basic piece material values. On these games a new set of weights was tuned and compiled into a stronger version. After half a dozen such iterations Leorik surpassed it's old playing strength.
There have also been a few bugfixes and tweaks to existing functionality like an improved Move-History. 

It is listed at [2677 Elo](http://computerchess.org.uk/ccrl/404/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%202.3%2064-bit#Leorik_2_3_64-bit) on the CCRL Blitz and [2736 Elo](http://computerchess.org.uk/ccrl/4040/cgi/engine_details.cgi?print=Details&each_game=0&eng=Leorik%202.3%2064-bit#Leorik_2_3_64-bit) on the CCRL 40/15 rating lists.

### Leorik 2.2
[__Version 2.2__](https://github.com/lithander/Leorik/releases/tag/2.2) adds a mobility term to the evaluation: Each  non-pawn piece receives bonus cp based on the number of non-capture moves it can make (up to a cap) multiplied with a small, constant cp value. Other changes address irregularities observed with the last version: Null-Move pruning in pawn endgames has been disabled because of the increased risk of missing Zugzwang. The replacement scheme of the Transposition Table has been rewritten to better protect deep nodes in matches using long time-control settings. The time-control logic has been completely rewritten to improve performance in matches without per-move increment. 

It is listed at [2690 Elo](https://computerchess.org.uk//ccrl/404/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%202.2%2064-bit#Leorik_2_2_64-bit) on the CCRL Blitz and [2681 Elo](https://computerchess.org.uk/ccrl/4040/cgi/engine_details.cgi?print=Details&each_game=1&eng=Leorik%202.2%2064-bit#Leorik_2_2_64-bit) on the CCRL 40/15 rating lists.

### Leorik 2.1
[__Version 2.1__](https://github.com/lithander/Leorik/releases/tag/2.1) adds a pawn structure term to the evaluation: A bonus is awarded to passed pawns and for pawns being connected with or protected by other friendly pawns. Isolated pawns receive a malus. The pawn structure term is only updated when a pawn moves or get's captured. A simple pawn hash table is used to avoid re-evaluating previously encountered pawn structures. 

It is listed at [2566 Elo](https://computerchess.org.uk/ccrl/404/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%202.1%2064-bit#Leorik_2_1_64-bit) on the CCRL Blitz and [2598 Elo](https://computerchess.org.uk/ccrl/4040/cgi/engine_details.cgi?print=Details&each_game=1&eng=Leorik%202.1%2064-bit#Leorik_2_1_64-bit) on the CCRL 40/15 rating lists.

### Leorik 2.0
[__Version 2.0__](https://github.com/lithander/Leorik/releases/tag/2.0) adds null move pruning and futility pruning. Quiet moves are now history sorted and late quiet moves are searched at reduced depth. These search improvements in combination with a significant increase of evaluated positions per second (nps) allow Leorik to look twice as deep as version 1.0 and are making it at least 400 Elo stronger. The evaluation is still minimal, using the same PSQTs as version 1.0 and nothing else. 

It is listed at [2537 Elo](https://computerchess.org.uk/ccrl/404/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%202.0.2%2064-bit#Leorik_2_0_2_64-bit) on the CCRL Blitz and [2529 Elo](https://computerchess.org.uk/ccrl/4040/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%202.0%2064-bit#Leorik_2_0_64-bit) on the CCRL 40/15 rating lists.

### Leorik 1.0
[__Version 1.0__](https://github.com/lithander/Leorik/releases/tag/1.0) combines a pretty fast move generator, copy&make and incremental updates of the Zobrist key and the PST based evaluation to search several million nodes per second. The search does not implement any unsafe pruning techniques or reductions and so it suffers from a high branching factor and remains quite shallow even at higher time controls. But it solves all mate puzzle with the shortest path.

It is listed at [2112 Elo](https://computerchess.org.uk/ccrl/404/cgi/engine_details.cgi?eng=Leorik%201.0%2064-bit#Leorik_1_0_64-bit) on the CCRL Blitz list.

## Help & Support

Please let me know of any bugs, compilation-problems or stability issues and features that you feel **Leorik** is lacking.
