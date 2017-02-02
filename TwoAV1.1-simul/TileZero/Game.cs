/* 
Copyright 2015 Enkhbold Nyamsuren (http://www.bcogs.net , http://www.bcogs.info/)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

Namespace: TileZero
Filename: Game.cs
Description:
    Implements the main logic of the TileZero game.
    This is an abstract class to facilitate different variations of HAT-based adaptation.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Diagnostics; // [SC] for debugging messages

namespace TileZero 
{
    public class Game // [SC]making it an abstract class so that it cannot be instantiated
    {
        protected List<TileZeroTile> tileBag;

        protected VirtualBoard virtualBoard;
        protected List<Player> players;

        protected int activePlayerIndex;

        protected int playabelTileCount;
        protected int playedTileCount; // [SC] increments whenever a starting tile is put on a board or player receives a tile without dropping;

        protected int selectedRowIndex;
        protected int selectedColIndex;

        protected double correctAnswer;

        protected bool newGameInitFlag;
        protected bool activeGameFlag;

        protected bool startTurnFlag;
        protected bool endTurnFlag;

        private Random rnd = null;

        public Game() {
            virtualBoard = new VirtualBoard(this);
            rnd = new Random();
        }

        // [2016.12.01]
        public void initNewGame(string aiOneID, string aiTwoID, int playableTileCount, int startPlayerIndex) {
            int aiPlayerCount = 1;

            createTileBag();

            if (verifyPlayableTileCount(playableTileCount, aiPlayerCount)) this.playabelTileCount = playableTileCount;
            else this.playabelTileCount = tileBag.Count;

            playedTileCount = 0;

            virtualBoard.resetBoard();

            createPlayers(aiOneID, aiTwoID); // [SC] should be called after 3 tiles were put on a board

            activePlayerIndex = startPlayerIndex;

            correctAnswer = 0;

            newGameInitFlag = true;
        }

        // [2016.12.01]
        public void startNewGame() {
            if (!newGameInitFlag) return;

            if (activeGameFlag) return;

            putStartingTiles();

            newGameInitFlag = false;
            activeGameFlag = true;

            startTurnFlag = true;
            endTurnFlag = false;

            while (activeGameFlag) {
                bool endGameFlag = false;

                if (startTurnFlag) {
                    startTurnFlag = false;
                    startTurn();
                }

                if (endTurnFlag) {
                    endTurnFlag = false;
                    endGameFlag = endTurn();
                }

                if (endGameFlag) {
                    endGame();
                    break;
                }
            }
        }

        // [2016.12.01]
        public void endGame() {
            if (!activeGameFlag) return;

            Player activePlayer = players[activePlayerIndex];
            activePlayer.increaseScore(Cfg.LAST_PLAYER_REWARD);

            int maxScore = Cfg.NONE;
            List<Player> maxScorePlayers = new List<Player>();
            foreach (Player player in players) {
                int playerScore = player.getPlayerScore();
                if (maxScore == Cfg.NONE || maxScore == playerScore) {
                    maxScorePlayers.Add(player);
                    maxScore = playerScore;
                }
                else if (maxScore < playerScore) {
                    maxScorePlayers.Clear();
                    maxScorePlayers.Add(player);
                    maxScore = playerScore;
                }
            }

            if (maxScorePlayers.Count > 1) {
                Cfg.log("It is a draw!");
            }
            else {
                Cfg.log(String.Format("Player {0} won the game!", maxScorePlayers[0].getPlayerName()));
                maxScorePlayers[0].WinFlag = true;
            }

            activeGameFlag = false;
        }

        public void forceEndGame() {
            activeGameFlag = false;
        }

        // [2016.12.01]
        protected void startTurn() {
            if (!activeGameFlag) return;

            Player activePlayer = players[activePlayerIndex];

            Cfg.log(String.Format("{0}'s turn. Score: {1}. Tiles: {2}"
                                        , activePlayer.getPlayerName()
                                        , activePlayer.getPlayerScore()
                                        , activePlayer.PlayerTilesToString()));

            activePlayer.invokeAI();
            endTurn(activePlayerIndex);
        }

        // [2016.12.01]
        public void endTurn(int playerIndex) {
            if (!activeGameFlag) Cfg.log("No active game.");
            else if (playerIndex != activePlayerIndex) Cfg.log("Not your turn!" + playerIndex);
            else endTurnFlag = true;
        }

        // [2016.12.01]
        protected bool endTurn() {
            // [SC] reset board position
            resetSelected();

            Player activePlayer = players[activePlayerIndex];

            // [SC] reset player's variables that are persistent only for a turn
            activePlayer.resetTurnVars();

            // [SC] refill player's tile array with new tiles from bag
            fillPlayerTiles(activePlayer);

            // [SC] if player has no tiles then end the game
            if (activePlayer.getPlayerTileCount() == 0) return true;

            // [SC] make the next player in a queue as a current player
            if (++activePlayerIndex >= players.Count) activePlayerIndex = 0;

            // [SC] set a flag to start a next turn
            startTurnFlag = true;

            Cfg.log(getVirtualBoard().ToString());

            return false;
        }

        // [2016.12.01]
        protected void createTileBag() {
            tileBag = new List<TileZeroTile>();

            for (int colorIndex = 0; colorIndex < Cfg.MAX_VAL_INDEX; colorIndex++) {
                for (int shapeIndex = 0; shapeIndex < Cfg.MAX_VAL_INDEX; shapeIndex++) {
                    for (int tileID = 0; tileID < Cfg.MAX_TILE_ID; tileID++) {
                        tileBag.Add(new TileZeroTile(colorIndex, shapeIndex, tileID));
                    }
                }
            }

            tileBag.Shuffle();
        }

        // [2016.12.01]
        protected void putStartingTiles() {
            int startCol = virtualBoard.getColCount() / 2 - Cfg.START_TILE_COUNT / 2;
            int startRow = virtualBoard.getRowCount() / 2;

            for (int counter = 0; counter < Cfg.START_TILE_COUNT; counter++) {
                int currCol = startCol + counter;
                TileZeroTile tile = (TileZeroTile)tileBag.ElementAt(0);

                int result = putTileOnBoard(startRow, currCol, tile, false);

                // [TODO] need to terminate the game
                if (result == Cfg.NONE) {
                    Cfg.log("Error putting starting tiles");
                    break;
                }

                tileBag.Remove(tile);
                ++playedTileCount;
            }
        }

        // [2016.12.01]
        // [SC] a function for dropping a tile
        public void dropPlayerTile(int playerIndex) {
            if (!activeGameFlag) return;

            if (playerIndex != activePlayerIndex) {
                Cfg.log("It is not your turn!");
                return;
            }

            Player activePlayer = players[activePlayerIndex];

            // [SC] check if player drop tiles
            if (!activePlayer.getCanDrop()) {
                Cfg.log("Cannot drop a tile after putting a tile on a board!"); // [TODO]
                return;
            }

            // [SC] check if bag has tiles
            if (tileBag.Count == 0) {
                Cfg.log("Cannot drop a tile! The bag is empty."); // [TODO]
                return;
            }

            // [SC] check if player tile is selected
            if (!activePlayer.isTileSelected()) {
                Cfg.log("Select a tile at first!"); // [TODO]
                return;
            }

            TileZeroTile tile = activePlayer.getSelectedTile();

            // [SC] make sure that the tile being dropped is not a replacement tile of previously dropped tile
            if (!tile.getCanDrop()) {
                Cfg.log("Cannot drop a replacement tile!");
                return;
            }

            foreach (TileZeroTile newTile in tileBag) {
                // [SC] make sure that the new tile does not have the same features as the dropped tile
                if (newTile.getColorIndex() == tile.getColorIndex() && newTile.getShapeIndex() == tile.getShapeIndex()) continue;

                Cfg.log(String.Format("    Dropped tile {0}. Replaced with tile {1}.", tile.ToString(), newTile.ToString()));

                // [SC] remove the dropped tile from player's stack
                activePlayer.removeTile(tile);
                // [SC] add the dropped tile into the bag
                tileBag.Add(tile);

                // [SC] remove the new tile from the bag 
                tileBag.Remove(newTile);
                // [SC] add the new tile to player's stack
                activePlayer.addTile(newTile);
                // [SC] make sure that the new tile cannot be dropped in the same turn
                newTile.setCanDrop(false);

                // [SC] shuffle the bag
                tileBag.Shuffle();

                // [SC] prevent the player from moving tiles into the board
                activePlayer.setCanMove(false);

                break;
            }
        }

        // [2016.12.01]
        // [SC] place active player's tile on a board
        public void placePlayerTileOnBoard(int playerIndex) {
            if (!activeGameFlag) return;

            if (playerIndex != activePlayerIndex) {
                Cfg.log("It is not your turn!");
                return;
            }

            Player activePlayer = players[activePlayerIndex];

            // [SC] check if player can put tiles on a board
            if (!activePlayer.getCanMove()) {
                Cfg.log("Cannot move a tile after dropping a tile!"); // [TODO]
                return;
            }

            // [SC] check if board position is selected
            if (!isSelected()) {
                Cfg.log("Select a board position at first!"); // [TODO]
                return;
            }

            // [SC] check if player tile is selected
            if (!activePlayer.isTileSelected()) {
                Cfg.log("Select a tile at first!"); // [TODO]
                return;
            }

            TileZeroTile tile = activePlayer.getSelectedTile();
            int result = putTileOnBoard(selectedRowIndex, selectedColIndex, tile, true);
            if (result != Cfg.NONE) {
                Cfg.log(String.Format("    Put tile {0} at position {1}-{2} for {3} points.", tile.ToString(), selectedRowIndex, selectedColIndex, result));

                // [SC] increase player's score
                activePlayer.increaseScore(result);

                // [SC] remove the tile from the player and reset player selection
                activePlayer.removeSelectedTile();

                // [SC] disable mismatching tiles
                activePlayer.disableMismatchedTiles(tile.getColorIndex(), tile.getShapeIndex());

                // [SC] prevent the player from dropping tiles in the same turn
                activePlayer.setCanDrop(false);

                // [SC] reset board selection
                resetSelected();
            }
        }

        // [2016.12.01]
        // [SC] put a given tile on a specified board position; validCheck is true then verify if the move conforms to game rules
        protected int putTileOnBoard(int rowIndex, int colIndex, TileZeroTile tile, bool validCheck) {
            return virtualBoard.addTile(rowIndex, colIndex, tile, validCheck, null);
        }

        // [SC][2016.12.01]
        protected void createPlayers(string aiOneID, string aiTwoID) {
            // [SC] the list of all players
            players = new List<Player>();

            // [SC] creating a subject AI player
            Player subjectPlayer = new Player(0, aiOneID, this);
            fillPlayerTiles(subjectPlayer);
            players.Add(subjectPlayer);

            // [SC] creating an opponent AI player
            Player opponentPlayer = new Player(1, aiTwoID, this);
            fillPlayerTiles(opponentPlayer);
            players.Add(opponentPlayer);
        }

        // [2016.12.01]
        protected void fillPlayerTiles(Player player) {
            // [SC] make sure the tile bag is not empty and not all playable tiles are used
            while (tileBag.Count > 0 && playedTileCount < playabelTileCount) {
                TileZeroTile tile = tileBag.ElementAt(0);
                if (player.addTile(tile)) {
                    tileBag.Remove(tile);
                    ++playedTileCount;
                }
                else {
                    break;
                }
            }
        }

        // [2016.12.01]
        // [TODO] end the game
        protected bool verifyPlayableTileCount(int tileCount, int aiPlayerCount) {
            int minTileCount = Cfg.START_TILE_COUNT + (aiPlayerCount + 1) * Cfg.MAX_PLAYER_TILE_COUNT;
            if (tileCount < minTileCount) {
                Cfg.log(String.Format("The minimum umber of playable tiles should be {0}. Using default bag size.", minTileCount));
                return false;
            }
            return true;
        }

        /////////////////////////////////////////////////////////////////
        ////// START: board cell selection

        // [2016.12.01]
        public bool setSelectedCell(int rowIndex, int colIndex, int playerIndex) {
            if (playerIndex >= players.Count) {
                Cfg.log(String.Format("Unknown player with an index {0}.", playerIndex));
            }
            else if (activePlayerIndex != playerIndex) {
                Cfg.log(String.Format("It is not your turn, {0}!", players[playerIndex].getPlayerName()));
            }
            else {
                selectedRowIndex = rowIndex;
                selectedColIndex = colIndex;
                return true;
            }

            return false;
        }

        // [2016.12.01]
        public void resetSelected() {
            selectedRowIndex = Cfg.NONE;
            selectedColIndex = Cfg.NONE;
        }

        // [2016.12.01]
        // [SC] returns true if any board cell is selected
        public bool isSelected() {
            if (selectedRowIndex != Cfg.NONE && selectedColIndex != Cfg.NONE) return true;
            return false;
        }

        ////// END: board cell selection
        /////////////////////////////////////////////////////////////////

        // [SC] called by Player
        public VirtualBoard getVirtualBoard() {
            return virtualBoard;
        }

        public Player getPlayerByIndex(int index) {
            if (index >= 0 && index < players.Count) {
                return players[index];
            }
            else {
                return null;
            }
        }
    }
}
