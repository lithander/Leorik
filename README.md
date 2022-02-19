# Leorik
Unshackled from the constraints of minimalism and simplicity **Leorik** is the successor to my bare-bones chess engine [MinimalChess](https://github.com/lithander/MinimalChessEngine).

## Features

* Written from scratch in C#
* Board representation uses bitboards and Copy/Make
* Pseudo-legal move generator (with a somewhat novel slider generation approach)
* Simple evaluation based exclusively on tapered, tuned PSTs
* Incremental updates of the evaluation and Zobrist hash
* Two-bucket Transposition Table.
* Staged move generation
* MVV-LVA sorted captures
* Killers
* PVS search
* Quiescence search

## Version History
```
Version:   1.0
Size:      910 LOC
Strength:  2150 ELO 
```
[__Version 1.0__](https://github.com/lithander/Leorik/releases/tag/v1.0) is the first public release. It uses a pretty fast move generator, copy&make and incrementaly updates the zobrist key and the PST based evaluation. This allows it to visit several million nodes per second, but the search does not implement any unsafe pruning techniques or reductions and so the search suffers from a hig branching factor and remains quite shallow even at higher time controls. This lack of sophistication in both it's search and it's evaluation causes it to play rather weak at an estimated 2150 ELO.

## How to play

**Leorik** does not provide its own user interface. Instead it implements the [UCI](https://en.wikipedia.org/wiki/Universal_Chess_Interface) protocol to make it compatible with most popular Chess GUIs such as:
* [Arena Chess GUI](http://www.playwitharena.de/) (free)
* [BanksiaGUI](https://banksiagui.com/) (free)
* [Cutechess](https://cutechess.com/) (free)
* [Nibbler](https://github.com/fohristiwhirl/nibbler/releases) (free)
* [Chessbase](https://chessbase.com/) (paid).

Once you have a chess GUI installed you can download prebuild [binaries for Mac, Linux or Windows](https://github.com/lithander/Leorik/releases/tag/v1.0) and extract the contents of the zip file into a location of your choice.

As a final step you have to register the engine with the GUI. The details depend on the GUI you have chosen but there's usually something like "Add Engine..." in the settings.

Now you should be ready to select **Leorik** as a player!

## Help & Support

Please let me know of any bugs, compilation-problems or stability issues and features that you feel **Leorik** is lacking.
