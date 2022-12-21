# Leorik
<p align="center">
<img src="https://github.com/lithander/Leorik/blob/master/Leorik.Logo.png" alt="Leorik Logo" width="600"/>
</p>

Unshackled from the constraints of minimalism and simplicity **Leorik** is the successor to my bare-bones chess engine [MinimalChess](https://github.com/lithander/MinimalChessEngine).

## Features
* Pseudo-legal move generator, bitboards and copy/make
* Negamax search with Alpha-Beta pruning and Iterative Deepening
* Transposition Table with two buckets and aging
* PVS search (null windows)
* Staged move generation
* MVV-LVA sorted captures
* History & Killer Heuristic for sorting quiets
* Late Move reductions
* Null-Move pruning
* Futility pruning
* A fast, multi-threaded Tuner using gradient descent
#### Evaluation
* Tapered PSQTs, tuned from scratch on selfplay games
* Mobility: counting non-captures per piece
* Pawn Structure: considering isolated, connected, protected and passed pawns 
* Pawn Hash Table

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
### Leorik 2.3
[__Version 2.3__](https://github.com/lithander/Leorik/releases/tag/2.3) replaces all previously handcrafted evaluation terms with tunable weights and all weights are tuned from scratch on selfplay games.
The first batch of games was played with a version that only knew basic piece material values. On these games a new set of weights was tuned and compiled into a stronger version. After half a dozen such iterations Leorik surpassed it's old playing strength.
There have also been a few bugfixes and tweaks to existing functionality like an improved Move-History. 

It's estimated playing strength is at 2750 Elo.

### Leorik 2.2
[__Version 2.2__](https://github.com/lithander/Leorik/releases/tag/2.2) adds a mobility term to the evaluation: Each  non-pawn piece receives bonus cp based on the number of non-capture moves it can make (up to a cap) multiplied with a small, constant cp value. Other changes address irregularities observed with the last version: Null-Move pruning in pawn endgames has been disabled because of the increased risk of missing Zugzwang. The replacement scheme of the Transposition Table has been rewritten to better protect deep nodes in matches using long time-control settings. The time-control logic has been completely rewritten to improve performance in matches without per-move increment. 

It is listed at [2698 Elo](https://ccrl.chessdom.com/ccrl/404/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%202.2%2064-bit#Leorik_2_2_64-bit) on the CCRL Blitz and [2684 Elo](https://ccrl.chessdom.com/ccrl/4040/cgi/engine_details.cgi?print=Details&each_game=1&eng=Leorik%202.2%2064-bit#Leorik_2_2_64-bit) on the CCRL 40/15 rating lists.

### Leorik 2.1
[__Version 2.1__](https://github.com/lithander/Leorik/releases/tag/2.1) adds a pawn structure term to the evaluation: A bonus is awarded to passed pawns and for pawns being connected with or protected by other friendly pawns. Isolated pawns receive a malus. The pawn structure term is only updated when a pawn moves or get's captured. A simple pawn hash table is used to avoid re-evaluating previously encountered pawn structures. 

It is listed at [2583 Elo](https://ccrl.chessdom.com/ccrl/404/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%202.1%2064-bit#Leorik_2_1_64-bit) on the CCRL Blitz and [2607 Elo](https://ccrl.chessdom.com/ccrl/4040/cgi/engine_details.cgi?print=Details&each_game=1&eng=Leorik%202.1%2064-bit#Leorik_2_1_64-bit) on the CCRL 40/15 rating lists.

### Leorik 2.0
[__Version 2.0__](https://github.com/lithander/Leorik/releases/tag/2.0) adds null move pruning and futility pruning. Quiet moves are now history sorted and late quiet moves are searched at reduced depth. These search improvements in combination with a significant increase of evaluated positions per second (nps) allow Leorik to look twice as deep as version 1.0 and are making it at least 400 Elo stronger. The evaluation is still minimal, using the same PSQTs as version 1.0 and nothing else. 

It is listed at [2555 Elo](https://ccrl.chessdom.com/ccrl/404/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%202.0.2%2064-bit#Leorik_2_0_2_64-bit) on the CCRL Blitz and [2544 Elo](https://ccrl.chessdom.com/ccrl/4040/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=Leorik%202.0%2064-bit#Leorik_2_0_64-bit) on the CCRL 40/15 rating lists.

### Leorik 1.0
[__Version 1.0__](https://github.com/lithander/Leorik/releases/tag/1.0) combines a pretty fast move generator, copy&make and incremental updates of the Zobrist key and the PST based evaluation to search several million nodes per second. The search does not implement any unsafe pruning techniques or reductions and so it suffers from a high branching factor and remains quite shallow even at higher time controls. But it solves all mate puzzle with the shortest path.

It is listed at [2149 Elo](https://ccrl.chessdom.com/ccrl/404/cgi/engine_details.cgi?eng=Leorik%201.0%2064-bit#Leorik_1_0_64-bit) on the CCRL Blitz list.

## Help & Support

Please let me know of any bugs, compilation-problems or stability issues and features that you feel **Leorik** is lacking.
