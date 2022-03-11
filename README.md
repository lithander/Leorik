# Leorik
Unshackled from the constraints of minimalism and simplicity **Leorik** is the successor to my bare-bones chess engine [MinimalChess](https://github.com/lithander/MinimalChessEngine).

## Features

* Pseudo-legal move generator, bitboards and copy/make
* Negamax search with Alpha-Beta Pruning and Iterative Deepening
* Minimal evaluation based on tapered, tuned PSTs only
* Incrementally updated evaluation and Zobrist hash
* Transposition Table with two buckets and aging
* PVS search (null windows)
* Futility pruning
* Null-Move pruning
* Staged move generation
* MVV-LVA sorted captures
* Killers
* History sorted quiets with LMR

## Version History
```
Version:   2.0
Size:      1009 LOC
Strength:  2600 Elo 
```
[__Version 2.0__](https://github.com/lithander/Leorik/releases/tag/2.0) adds null move pruning and futility pruning. Quiet moves are now history sorted and late quiet moves are searched at reduced depth. These search improvements in combination with a significant increase of evaluated positions per second (nps) allow Leorik to look twice as deep as version 1.0 and are making it at least 400 Elo stronger. The evaluation is still minimal, using the same PSTs as version 1.0 and nothing else.

```
Version:   1.0
Size:      910 LOC
Strength:  2150 Elo 
```
[__Version 1.0__](https://github.com/lithander/Leorik/releases/tag/1.0) combines a pretty fast move generator, copy&make and incremental updates of the Zobrist key and the PST based evaluation to search several million nodes per second. The search does not implement any unsafe pruning techniques or reductions and so it suffers from a high branching factor and remains quite shallow even at higher time controls. This lack of sophistication in both it's search and it's evaluation causes this version to play rather weak at an estimated 2150 Elo. It's a pretty decent mate-finder though!

## How to play

**Leorik** does not provide its own user interface. Instead it implements the [UCI](https://en.wikipedia.org/wiki/Universal_Chess_Interface) protocol to make it compatible with most popular Chess GUIs such as:
* [Arena Chess GUI](http://www.playwitharena.de/) (free)
* [BanksiaGUI](https://banksiagui.com/) (free)
* [Cutechess](https://cutechess.com/) (free)
* [Nibbler](https://github.com/fohristiwhirl/nibbler/releases) (free)
* [Chessbase](https://chessbase.com/) (paid).

Once you have a chess GUI installed you can download prebuild [binaries for Mac, Linux or Windows](https://github.com/lithander/Leorik/releases/tag/1.0) and extract the contents of the zip file into a location of your choice.

As a final step you have to register the engine with the GUI. The details depend on the GUI you have chosen but there's usually something like "Add Engine..." in the settings.

Now you should be ready to select **Leorik** as a player!

## Help & Support

Please let me know of any bugs, compilation-problems or stability issues and features that you feel **Leorik** is lacking.
