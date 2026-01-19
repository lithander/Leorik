# Leorik
<p align="center">
<img src="https://github.com/lithander/Leorik/blob/master/Resources/Leorik.Logo.png" alt="Leorik Logo" width="600"/>
</p>

Unshackled from the constraints of minimalism and simplicity **Leorik** is the successor to my bare-bones chess engine [MinimalChess](https://github.com/lithander/MinimalChessEngine).
It plays Standard and Chess960/Fischer Random at superhuman strength.

## Features
* Pseudo-legal move generator, bitboards and copy/make.
* Fail-hard Negamax with Alpha-Beta pruning
* Iterative Deepening & Aspiration windows
* History corrected NNUE evaluation
* NNUE architecture: (768x5 -> 640)x2 -> 1x8
	- horizontally mirrored
	- 5 input buckets (king pos)
	- 8 output buckets (piececount)
* Network trained on selfplay games using [Bullet](https://github.com/jw1912/bullet)
* Lockfree Transposition Table with two buckets and aging
* Principal Variation Search (PVS)
  - search first move at full depth with a full window,
  - others moves need to beat alpha in a reduced null-window probe first
* Pruning
  - Null move pruning
    - reduction based on remaining depth
    - RFP-like early return if fail-high is expected
  - Adaptive Razoring
    - drop to quiescence when staticEval is too far below alpha
    - adaptive margin from quiet improvement stats
  - Late Move Reductions (LMR)
    - reduce late quiet moves
    - extra reductions based on static eval and SEE
  - Mate distance pruning
* Staged Move Ordering
  - Best Hashmove
  - MVV-LVA sorted captures
  - Killer, Continuations
  - History sorted Quiets
  - Remaining Quiets
* Lazy SMP parallel search
* MultiPV - search multiple lines in parallel
* Pondering - think on opponent's time

## Version History

### Leorik 3.2

[__Version 3.2__](https://github.com/lithander/Leorik/releases/tag/3.2) now also plays Fisher Random chess. It supports UCI features like MultiPV and Pondering and gains strength from a more sophisticated NNUE architecture and a few search improvements. Built with .Net 10.
The new NNUE architecture uses horizontal mirroring, 5 input and 8 output buckets. The same network supports both FRC and standard chess and was trained on over 6 billion labeled positions.
Search improvements include dynamic NMP reductions, better time control that takes the stability of the search into account and Razoring with adaptive margins based on live statistics.

### Leorik 3.1

[__Version 3.1__](https://github.com/lithander/Leorik/releases/tag/3.1) improves Leorik's NNUE evaluation by increasing the hidden layer size to 640 neurons and adopting SCReLU activation. The network was trained from zero over 19 generations and the final network 640HL-S-5288M-Tmix-Q5-v19 was trained on 5.2B positions. Search improvements include the addition of Correction History, increased reduction of late quiet moves, a dynamic threshold for identifying such moves, and the introduction of RFP with dynamic margins derived from NMP statistics.

[3432 Elo](https://computerchess.org.uk/ccrl/404/cgi/engine_details.cgi?eng=Leorik%203.1%2064-bit) CCRL Blitz and [3370 Elo](https://computerchess.org.uk/ccrl/4040/cgi/engine_details.cgi?eng=Leorik%203.1%2064-bit) CCRL 40/15 rating.

### Leorik 3.0
[__Version 3.0__](https://github.com/lithander/Leorik/releases/tag/3.0) introduces NNUE based evaluation that completely replaces the handcrafted one. The network architecture uses 768 inputs and one hidden layer of 256 neurons. It was trained using [Bullet](https://github.com/jw1912/bullet) on 622M labeled positions extracted from selfplay games. Despite the modest complexity of the NNUE architecture it has contributed most of the Elo gains over Version 2.5. The rest (~50 Elo) is from the adoption of Aspiration Windows and a revamped Staged Move Generation that replaces the 2nd Killer with Counter and FollowUp moves if available.

[3276 Elo](https://computerchess.org.uk/ccrl/404/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%203.0%2064-bit#Leorik_3_0_64-bit) CCRL Blitz and [3227 Elo](https://computerchess.org.uk/ccrl/4040/cgi/engine_details.cgi?print=Details&each_game=0&eng=Leorik%203.0%2064-bit#Leorik_3_0_64-bit) CCRL 40/15 rating.

### Leorik 2.5
[__Version 2.5__](https://github.com/lithander/Leorik/releases/tag/2.5) now employs a faster move generator based on PEXT by default. PSQTs were replaced with linear functions that calculate the piece-square values using 18 parameters reflecting the positions of both kings and the game phase. To compute these values at a high speed the engine uses AVX2. Leorik now supports the 'Threads' UCI option. Each thread searches independently ("Lazy SMP") but can share information with other threads through the lockless transposition table.
This is also the first version that is published under the permissive **MIT open-source license**.

[2921 Elo](https://computerchess.org.uk/ccrl/404/cgi/engine_details.cgi?print=Details&each_game=1&eng=Leorik%202.5%2064-bit#Leorik_2_5_64-bit) CCRL Blitz and [2922 Elo](https://computerchess.org.uk/ccrl/4040/cgi/engine_details.cgi?match_length=30&each_game=0&print=Details&each_game=0&eng=Leorik%202.5%2064-bit#Leorik_2_5_64-bit)  CCRL 40/15 rating.

### Leorik 2.4
[__Version 2.4__](https://github.com/lithander/Leorik/releases/tag/2.4) uses Static-Exchange-Evaluation (SEE) to skip bad captures in quiescence search and to search bad moves at a reduced depth in the main search. Futility Pruning was removed from the main search but the idea of basing a pruning-decision on the static evaluation of a position is now used in a much more radical Null Move Pruning implementation. Last but not least Leorik now recognizes certain drawn positions with a material advantage for one side (e.g. KvKNN) and evaluates them much closer to zero.

[2831 Elo](http://computerchess.org.uk/ccrl/404/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%202.4%2064-bit#Leorik_2_4_64-bit) CCRL Blitz and [2799 Elo](http://computerchess.org.uk/ccrl/4040/cgi/engine_details.cgi?print=Details&each_game=0&eng=Leorik%202.4%2064-bit#Leorik_2_4_64-bit) CCRL 40/15 rating.

### Leorik 2.3
[__Version 2.3__](https://github.com/lithander/Leorik/releases/tag/2.3) replaces all previously handcrafted evaluation terms with tunable weights and all weights are tuned from scratch on selfplay games.
The first batch of games was played with a version that only knew basic piece material values. On these games a new set of weights was tuned making the selfplay stronger. After half a dozen generations Leorik surpassed its old playing strength.
There have also been a few bugfixes and tweaks to existing functionality like an improved Move-History. 

[2677 Elo](http://computerchess.org.uk/ccrl/404/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%202.3%2064-bit#Leorik_2_3_64-bit) CCRL Blitz and [2736 Elo](http://computerchess.org.uk/ccrl/4040/cgi/engine_details.cgi?print=Details&each_game=0&eng=Leorik%202.3%2064-bit#Leorik_2_3_64-bit) CCRL 40/15 rating.

### Leorik 2.2
[__Version 2.2__](https://github.com/lithander/Leorik/releases/tag/2.2) adds a mobility term to the evaluation: Each non-pawn piece receives bonus cp based on the number of non-capture moves it can make (up to a cap) multiplied with a small, constant cp value. Null-Move pruning in pawn endgames has been disabled because of the increased risk of missing Zugzwang. The replacement scheme of the Transposition Table has been rewritten to better protect deep nodes in matches using long time-control settings. The time-control logic has been completely rewritten to improve performance in matches without per-move increment. 

[2690 Elo](https://computerchess.org.uk//ccrl/404/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%202.2%2064-bit#Leorik_2_2_64-bit) CCRL Blitz and [2681 Elo](https://computerchess.org.uk/ccrl/4040/cgi/engine_details.cgi?print=Details&each_game=1&eng=Leorik%202.2%2064-bit#Leorik_2_2_64-bit) CCRL 40/15 rating.

### Leorik 2.1
[__Version 2.1__](https://github.com/lithander/Leorik/releases/tag/2.1) adds a pawn structure term to the evaluation: A bonus is awarded to passed pawns and for pawns being connected with or protected by other friendly pawns. Isolated pawns receive a malus. The pawn structure term is only updated when a pawn moves or get's captured. A simple pawn hash table is used to avoid re-evaluating previously encountered pawn structures. 
[2566 Elo](https://computerchess.org.uk/ccrl/404/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%202.1%2064-bit#Leorik_2_1_64-bit) CCRL Blitz and [2598 Elo](https://computerchess.org.uk/ccrl/4040/cgi/engine_details.cgi?print=Details&each_game=1&eng=Leorik%202.1%2064-bit#Leorik_2_1_64-bit) CCRL 40/15 rating.

### Leorik 2.0
[__Version 2.0__](https://github.com/lithander/Leorik/releases/tag/2.0) adds null move pruning and futility pruning. Quiet moves are now history sorted and late quiet moves are searched at reduced depth. These search improvements in combination with a significant speed increase (nps) allow Leorik to look twice as deep as version 1.0 and are making it at least 400 Elo stronger. The evaluation is still minimal, using the same PSQTs as version 1.0 and nothing else. 
[2537 Elo](https://computerchess.org.uk/ccrl/404/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%202.0.2%2064-bit#Leorik_2_0_2_64-bit) CCRL Blitz and [2529 Elo](https://computerchess.org.uk/ccrl/4040/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%202.0%2064-bit#Leorik_2_0_64-bit) CCRL 40/15 rating.

### Leorik 1.0
[__Version 1.0__](https://github.com/lithander/Leorik/releases/tag/1.0) combines a pretty fast move generator, copy&make and incremental updates of the Zobrist key and the PST based evaluation to search several million nodes per second. The search does not implement any unsafe pruning techniques or reductions and so it suffers from a high branching factor and remains quite shallow even at higher time controls. But it solves all mate puzzles with the shortest path.
[2112 Elo](https://computerchess.org.uk/ccrl/404/cgi/engine_details.cgi?eng=Leorik%201.0%2064-bit#Leorik_1_0_64-bit) on the CCRL Blitz list.

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

## Help & Support

Please let me know of any bugs, compilation-problems or stability issues and features that you feel **Leorik** is lacking.
