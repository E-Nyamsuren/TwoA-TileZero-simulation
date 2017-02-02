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
Filename: Player.cs
Description:
    Defines a class that implements a player that can be either a contruct for human player or purely AI player.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;

namespace TileZero 
{
    public class Player 
    {
        private Game game;

        private int playerIndex;
        private string playerName;
        private string playerType;

        private List<TileZeroTile> playerTiles;
        private TileZeroTile selectedTile;

        private int colorReq;
        private int shapeReq;

        private bool canDropFlag; // [SC] if true, the player can drop a tile
        private bool canMoveFlag; // [SC] if true, the player can move a tile

        private int playerScore;

        public bool WinFlag { get; set; }

        public Player(int playerIndex, string playerType, Game game) {
            this.game = game;
            this.playerType = playerType;

            this.playerIndex = playerIndex;

            setPlayerName(createPlayerName());

            playerTiles = new List<TileZeroTile>();

            resetGameVars();
        }

        private string createPlayerName() {
            return playerType + " " + playerIndex;
        }

        public void resetGameVars() {
            resetTurnVars();

            playerScore = 0;

            WinFlag = false;
        }

        public void resetTurnVars() {
            resetSelected();

            resetColorReq();
            resetShapeReq();
            resetTiles();

            canDropFlag = true;
            canMoveFlag = true;
        }

        /////////////////////////////////////////////////////////////////
        ////// START: generic functions for manipulating tiles

        public bool addTile(TileZeroTile tile) {
            if (getPlayerTileCount() < Cfg.MAX_PLAYER_TILE_COUNT) {
                playerTiles.Add(tile);
                return true;
            }
            return false;
        }

        public void removeTile(TileZeroTile tile) {
            playerTiles.Remove(tile);
        }

        public TileZeroTile getTileAt(int index) {
            if (index < playerTiles.Count) return playerTiles[index];
            else return null;
        }

        public int getPlayerTileCount() {
            if (playerTiles != null) return playerTiles.Count;
            else return 0;
        }

        ////// END: generic functions for manipulating tiles
        /////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////
        ////// START: functions for manipulating a selected tile

        private bool setSelectedTile(TileZeroTile tile) {
            // [TODO] make sure the tile is one of the player's tiles
            if (!tile.getPlayable()) {
                Cfg.log(String.Format("The tile {0}{1} is not playable.", tile.getColorIndex(), tile.getShapeIndex()));
            }
            else {
                selectedTile = tile;
                return true;
            }

            return false;
        }

        public bool setSelectedTile(int colorIndex, int shapeIndex, int tileID) {
            resetSelected();
            foreach (TileZeroTile tile in playerTiles) {
                if (tile.sameTile(colorIndex, shapeIndex, tileID) && tile.getPlayable()) {
                    return setSelectedTile(tile);
                }
            }
            return false;
        }

        public void resetSelected() {
            selectedTile = null;
        }

        public bool isTileSelected() {
            if (selectedTile == null) return false;
            else return true;
        }

        public TileZeroTile getSelectedTile() {
            return selectedTile;
        }

        public void removeSelectedTile() {
            removeTile(selectedTile);
            resetSelected();
        }

        ////// END: functions for manipulating a selected tile
        /////////////////////////////////////////////////////////////////

        public void disableMismatchedTiles(int colorIndex, int shapeIndex) {
            if (!hasColorReq() && !hasShapeReq()) {
                setColorReq(colorIndex);
                setShapeReq(shapeIndex);
            }
            else if (hasColorReq() && hasShapeReq()) {
                if (sameColorReq(colorIndex) && !sameShapeReq(shapeIndex))
                    resetShapeReq();
                else if (!sameColorReq(colorIndex) && sameShapeReq(shapeIndex))
                    resetColorReq();
            }

            if (hasColorReq() && hasShapeReq()) {
                for (int currTileIndex = 0; currTileIndex < playerTiles.Count; currTileIndex++) {
                    TileZeroTile tile = playerTiles[currTileIndex];
                    if (!sameColorReq(tile.getColorIndex()) && !sameShapeReq(tile.getShapeIndex()))
                        tile.setPlayable(false);
                }
            }
            else if (hasColorReq() && !hasShapeReq()) {
                for (int currTileIndex = 0; currTileIndex < playerTiles.Count; currTileIndex++) {
                    TileZeroTile tile = playerTiles[currTileIndex];
                    if (!sameColorReq(tile.getColorIndex()))
                        tile.setPlayable(false);
                }
            }
            else if (!hasColorReq() && hasShapeReq()) {
                for (int currTileIndex = 0; currTileIndex < playerTiles.Count; currTileIndex++) {
                    TileZeroTile tile = playerTiles[currTileIndex];
                    if (!sameShapeReq(tile.getShapeIndex()))
                        tile.setPlayable(false);
                }
            }
        }

        public void resetTiles() {
            for (int currTileIndex = 0; currTileIndex < playerTiles.Count; currTileIndex++)
                playerTiles[currTileIndex].resetTile();
        }

        public void setPlayerName(string playerName) {
            this.playerName = playerName;
        }

        public string getPlayerName() {
            return playerName;
        }

        public int getPlayerScore() {
            return playerScore;
        }

        public void increaseScore(int score) {
            playerScore += score;
        }

        public int getPlayerIndex() {
            return playerIndex;
        }

        public string getPlayerType() {
            return playerType;
        }

        // [SC][2015.09.02][DAI]
        public void setPlayerType(string playerType) {
            this.playerType = playerType;
            setPlayerName(createPlayerName());
        }

        public string PlayerTilesToString() {
            string tilesStr = "{";
            foreach (TileZeroTile tile in playerTiles) {
                tilesStr += tile.ToString() + ",";
            }
            return tilesStr + "}";
        }

        /////////////////////////////////////////////////////////////////
        ////// START: can move and can drop flags

        public void setCanDrop(bool canDropFlag) {
            this.canDropFlag = canDropFlag;
        }

        public bool getCanDrop() {
            return canDropFlag;
        }

        public void setCanMove(bool canMoveFlag) {
            this.canMoveFlag = canMoveFlag;
        }

        public bool getCanMove() {
            return canMoveFlag;
        }

        ////// END: can move and can drop flags
        /////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////
        ////// START: functions for color requirements

        public int getColorReq() {
            return colorReq;
        }

        public void setColorReq(int colorIndex) {
            this.colorReq = colorIndex;
        }

        public bool hasColorReq() {
            if (colorReq != Cfg.NONE) return true;
            else return false;
        }

        public bool sameColorReq(int colorIndex) {
            if (this.colorReq == colorIndex) return true;
            else return false;
        }

        public void resetColorReq() {
            colorReq = Cfg.NONE;
        }

        ////// END: functions for color requirements
        /////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////
        ////// START: functions for shape requirements

        public int getShapeReq() {
            return shapeReq;
        }

        public void setShapeReq(int shapeIndex) {
            this.shapeReq = shapeIndex;
        }

        public bool hasShapeReq() {
            if (shapeReq != Cfg.NONE) return true;
            else return false;
        }

        public bool sameShapeReq(int shapeIndex) {
            if (this.shapeReq == shapeIndex) return true;
            else return false;
        }

        public void resetShapeReq() {
            shapeReq = Cfg.NONE;
        }

        ////// END: functions for shape requirements
        /////////////////////////////////////////////////////////////////

        public void invokeAI() {
            if (playerType.Equals(Cfg.VERY_EASY_AI)) invokeVeryEasyAI();
            else if (playerType.Equals(Cfg.EASY_AI)) invokeEasyAI();
            else if (playerType.Equals(Cfg.MEDIUM_COLOR_AI)) invokeColorOnlyMediumAI();
            else if (playerType.Equals(Cfg.MEDIUM_SHAPE_AI)) invokeShapeOnlyMediumAI();
            else if (playerType.Equals(Cfg.HARD_AI)) invokeHardAI();
            else if (playerType.Equals(Cfg.VERY_HARD_AI)) invokeVeryHardAI();
        }

        /////////////////////////////////////////////////////////////////
        ////// START: very easy ai functionality
        #region very easy AI
        // [SC] puts only a single tile on a board
        public void invokeVeryEasyAI() {
            VirtualBoard virtualBoard = game.getVirtualBoard();
            int rowCount = virtualBoard.getRowCount();
            int colCount = virtualBoard.getColCount();

            // [SC] using copy since indices in playerTiles may change due to removed tiles
            List<TileZeroTile> tempPlayerTiles = playerTiles.listShallowClone();

            bool tilePlacedFlag = false;
            bool shouldDropFlag = true;

            foreach (TileZeroTile tile in tempPlayerTiles) {
                // [SC] check if the tile is playable
                if (!tile.getPlayable()) continue;

                for (int currRowIndex = 0; currRowIndex < rowCount && !tilePlacedFlag; currRowIndex++) {
                    for (int currColIndex = 0; currColIndex < colCount; currColIndex++) {

                        int resultScore = virtualBoard.isValidMove(currRowIndex, currColIndex, tile, true, null, false);

                        if (resultScore != Cfg.NONE) {
                            setSelectedTile(tile);
                            game.setSelectedCell(currRowIndex, currColIndex, playerIndex);
                            game.placePlayerTileOnBoard(playerIndex);
                            tilePlacedFlag = true;
                            shouldDropFlag = false;
                            break;
                        }
                    }
                }

                if (tilePlacedFlag) break;
            }

            if (shouldDropFlag) {
                // [SC] dropping a random tile
                setSelectedTile(playerTiles.getRandomElement());
                game.dropPlayerTile(playerIndex);
                //Cfg.showMsg(getPlayerName() + " dropped a tile.");
            }
        }
        #endregion
        ////// END: very easy ai functionality
        /////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////
        ////// START: easy ai functionality
        #region easy AI
        public void invokeEasyAI() {
            VirtualBoard virtualBoard = game.getVirtualBoard();
            int rowCount = virtualBoard.getRowCount();
            int colCount = virtualBoard.getColCount();

            // [SC] using copy since indices in playerTiles may change due to removed tiles
            List<TileZeroTile> tempPlayerTiles = playerTiles.listShallowClone();

            bool tilePlacedFlag = false;
            bool shouldDropFlag = true;

            foreach (TileZeroTile tile in tempPlayerTiles) {
                // [SC] check if the tile is playable
                if (!tile.getPlayable()) continue;

                // [SC] add some daly before making next move
                if (tilePlacedFlag) {
                    tilePlacedFlag = false;
                }

                for (int currRowIndex = 0; currRowIndex < rowCount && !tilePlacedFlag; currRowIndex++) {
                    for (int currColIndex = 0; currColIndex < colCount; currColIndex++) {
                        int resultScore = virtualBoard.isValidMove(currRowIndex, currColIndex, tile, true, null, false);

                        if (resultScore != Cfg.NONE) {
                            setSelectedTile(tile);
                            game.setSelectedCell(currRowIndex, currColIndex, playerIndex);
                            game.placePlayerTileOnBoard(playerIndex);
                            tilePlacedFlag = true;
                            shouldDropFlag = false;
                            break;
                        }
                    }
                }
            }

            if (shouldDropFlag) {
                // [SC] dropping a random tile
                setSelectedTile(playerTiles.getRandomElement());
                game.dropPlayerTile(playerIndex);
                //Cfg.showMsg(getPlayerName() + " dropped a tile.");
            }
        }
        #endregion
        ////// END: easy ai functionality
        /////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////
        ////// START: medium ai functionality - color only
        #region medium AI with color dimension only
        public void invokeColorOnlyMediumAI() {
            CandidateTilePos selectedPosCombo = calculateMoves(true, false, false);

            if (selectedPosCombo == null) { // [SC] no tiles to put on a board
                // [SC] dropping a random tile
                setSelectedTile(playerTiles.getRandomElement());
                game.dropPlayerTile(playerIndex);
                //Cfg.showMsg(getPlayerName() + " dropped a tile.");
            }
            else if (getCanMove()) {
                CandidateTileSeq tileSeq = selectedPosCombo.getCandidateTileSeq();
                int totalMoveCount = selectedPosCombo.getComboLength();
                int currMoveCount = 0;

                while (currMoveCount < totalMoveCount) {
                    AbstractPos abstrPos = selectedPosCombo.getAbstrPosAt(currMoveCount);
                    int rowIndex = abstrPos.getRowIndex();
                    int colIndex = abstrPos.getColIndex();
                    int tileIndex = abstrPos.getTileIndex();

                    TileZeroTile tile = tileSeq.getTileAt(tileIndex);

                    setSelectedTile(tile);

                    game.setSelectedCell(rowIndex, colIndex, playerIndex);

                    game.placePlayerTileOnBoard(playerIndex);

                    currMoveCount++;
                }
            }
        }
        #endregion
        ////// END: medium ai functionality - color only
        /////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////
        ////// START: medium ai functionality - shape only
        #region medium AI with shape dimension only
        public void invokeShapeOnlyMediumAI() {
            CandidateTilePos selectedPosCombo = calculateMoves(false, true, false);

            if (selectedPosCombo == null) { // [SC] no tiles to put on a board
                // [SC] dropping a random tile
                setSelectedTile(playerTiles.getRandomElement());
                game.dropPlayerTile(playerIndex);
                //Cfg.showMsg(getPlayerName() + " dropped a tile.");
            }
            else if (getCanMove()) {
                CandidateTileSeq tileSeq = selectedPosCombo.getCandidateTileSeq();
                int totalMoveCount = selectedPosCombo.getComboLength();
                int currMoveCount = 0;

                while (currMoveCount < totalMoveCount) {
                    AbstractPos abstrPos = selectedPosCombo.getAbstrPosAt(currMoveCount);
                    int rowIndex = abstrPos.getRowIndex();
                    int colIndex = abstrPos.getColIndex();
                    int tileIndex = abstrPos.getTileIndex();

                    TileZeroTile tile = tileSeq.getTileAt(tileIndex);

                    setSelectedTile(tile);

                    game.setSelectedCell(rowIndex, colIndex, playerIndex);

                    game.placePlayerTileOnBoard(playerIndex);

                    currMoveCount++;
                }
            }
        }
        #endregion
        ////// END: medium ai functionality - shape only
        /////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////
        ////// START: hard ai functionality
        #region hard AI
        public void invokeHardAI() {
            CandidateTilePos selectedPosCombo = calculateMoves(true, true, true);

            if (selectedPosCombo == null) { // [SC] no tiles to put on a board
                // [SC] dropping a random tile
                setSelectedTile(playerTiles.getRandomElement());
                game.dropPlayerTile(playerIndex);
                //Cfg.showMsg(getPlayerName() + " dropped a tile.");
            }
            else if (getCanMove()) {
                CandidateTileSeq tileSeq = selectedPosCombo.getCandidateTileSeq();
                int totalMoveCount = selectedPosCombo.getComboLength();
                int currMoveCount = 0;

                while (currMoveCount < totalMoveCount) {
                    AbstractPos abstrPos = selectedPosCombo.getAbstrPosAt(currMoveCount);
                    int rowIndex = abstrPos.getRowIndex();
                    int colIndex = abstrPos.getColIndex();
                    int tileIndex = abstrPos.getTileIndex();

                    TileZeroTile tile = tileSeq.getTileAt(tileIndex);

                    setSelectedTile(tile);

                    game.setSelectedCell(rowIndex, colIndex, playerIndex);

                    game.placePlayerTileOnBoard(playerIndex);

                    currMoveCount++;
                }
            }
        }

        // [SC] gets the combo with the second highest score
        private void boardPosPermTraverseTreePaths(TreeNode rootNode, List<AbstractPos> currPath, CandidateTileSeq candTileSeq, int currScore
                                                    , List<CandidateTilePos> chosenPosComboList, List<CandidateTilePos> maxScorePosCombo) {

            if (rootNode.hasChildNodes()) {
                List<TreeNode> childNodes = rootNode.getChildNodes();
                foreach (TreeNode childNode in childNodes) {
                    List<AbstractPos> newPath = currPath.listShallowClone();
                    AbstractPos pos = (AbstractPos)childNode.getValue();
                    newPath.Add(pos);
                    boardPosPermTraverseTreePaths(childNode, newPath, candTileSeq, currScore + pos.getScore(), chosenPosComboList, maxScorePosCombo);
                }
            }
            else { // [SC] reached the final leaf; no more moves in the combo
                if (currScore > 0) {
                    CandidateTilePos newCandTilePos = new CandidateTilePos(candTileSeq, currPath, currScore);

                    if (maxScorePosCombo.Count == 0) {
                        maxScorePosCombo.Add(newCandTilePos);

                        chosenPosComboList.Clear();
                        chosenPosComboList.Add(newCandTilePos);
                    }
                    else if (maxScorePosCombo[0].getTotalScore() == currScore) {
                        if (chosenPosComboList.Count != 0 && chosenPosComboList[0].getTotalScore() == currScore) {
                            chosenPosComboList.Add(newCandTilePos);
                        }
                    }
                    else if (maxScorePosCombo[0].getTotalScore() < currScore) {
                        chosenPosComboList.Clear();
                        chosenPosComboList.Add(maxScorePosCombo[0]);

                        maxScorePosCombo.Clear();
                        maxScorePosCombo.Add(newCandTilePos);
                    }
                    else if (maxScorePosCombo[0].getTotalScore() > currScore) {
                        if (chosenPosComboList.Count == 0) {
                            chosenPosComboList.Add(newCandTilePos);
                        }
                        else if (chosenPosComboList[0].getTotalScore() == maxScorePosCombo[0].getTotalScore()) {
                            chosenPosComboList.Clear();
                            chosenPosComboList.Add(newCandTilePos);
                        }
                        else if (chosenPosComboList[0].getTotalScore() < currScore) {
                            chosenPosComboList.Clear();
                            chosenPosComboList.Add(newCandTilePos);
                        }
                        else if (chosenPosComboList[0].getTotalScore() == currScore) {
                            chosenPosComboList.Add(newCandTilePos);
                        }
                    }
                }
            }
        }
        #endregion hard AI
        ////// END: hard ai functionality
        /////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////
        ////// START: very hard ai functionality
        #region very hard AI
        public void invokeVeryHardAI() {
            CandidateTilePos selectedPosCombo = calculateMoves(true, true, false);

            if (selectedPosCombo == null) { // [SC] no tiles to put on a board
                // [SC] dropping a random tile
                setSelectedTile(playerTiles.getRandomElement());
                game.dropPlayerTile(playerIndex);
            }
            else if (getCanMove()) {
                CandidateTileSeq tileSeq = selectedPosCombo.getCandidateTileSeq();
                int totalMoveCount = selectedPosCombo.getComboLength();
                int currMoveCount = 0;

                while (currMoveCount < totalMoveCount) {
                    AbstractPos abstrPos = selectedPosCombo.getAbstrPosAt(currMoveCount);
                    int rowIndex = abstrPos.getRowIndex();
                    int colIndex = abstrPos.getColIndex();
                    int tileIndex = abstrPos.getTileIndex();

                    TileZeroTile tile = tileSeq.getTileAt(tileIndex);

                    setSelectedTile(tile);

                    game.setSelectedCell(rowIndex, colIndex, playerIndex);

                    game.placePlayerTileOnBoard(playerIndex);

                    currMoveCount++;
                }
            }
        }

        // [SC] 1. Create lists of tiles where each list is a group of tiles with the same color or shape.
        // [SC] 2. For each group of tiles, create lists of tile sequences where each list contains a sequence of tiles in a unique order.
        // [SC] 3. For each tile sequence, create a combination of unique board positions.
        public CandidateTilePos calculateMoves(bool considerColor, bool considerShape, bool suboptiomal) {
            List<CandidateTileSeq> candTileSeqList = new List<CandidateTileSeq>(); // [TODO]

            //////////////////////////////////////////////////////////////////////////////
            // [2016.12.08] new code
            List<List<TileZeroTile>> colorTileLists = new List<List<TileZeroTile>>(); // [SC] each list contains player's tiles of the same color
            List<List<TileZeroTile>> shapeTileLists = new List<List<TileZeroTile>>(); // [SC] each list contains player's tiles of the same shape
            for (int index = 0; index < Cfg.MAX_VAL_INDEX; index++) {
                colorTileLists.Add(new List<TileZeroTile>());
                shapeTileLists.Add(new List<TileZeroTile>());
            }

            foreach (TileZeroTile tile in playerTiles) {
                colorTileLists[tile.getColorIndex()].Add(tile);
                shapeTileLists[tile.getShapeIndex()].Add(tile);
            }

            colorTileLists.RemoveAll(p => p.Count == 0); // [SC] remove empty tile combos
            shapeTileLists.RemoveAll(p => p.Count == 0); // [SC] remove empty tile combos

            colorTileLists = colorTileLists.OrderBy(p => p.Count).ToList(); // [SC] order by size of tile combos
            shapeTileLists = shapeTileLists.OrderBy(p => p.Count).ToList(); // [SC] order by size of tile combos

            if (considerColor) {
                // [SC] remove all color tile combos that are subsets of another color or shape combo
                for (int indexOne = 0; indexOne < colorTileLists.Count; indexOne++) {
                    List<TileZeroTile> tileComboOne = colorTileLists[indexOne];

                    for (int indexTwo = indexOne + 1; indexTwo < colorTileLists.Count; indexTwo++) {
                        List<TileZeroTile> tileComboTwo = colorTileLists[indexTwo];

                        if (tileComboOne.All(p => tileComboTwo.Contains(p))) {
                            tileComboOne.Clear();
                            continue;
                        }
                    }

                    if (tileComboOne.Count == 0) { continue; }

                    if (considerShape) {
                        for (int indexTwo = 0; indexTwo < shapeTileLists.Count; indexTwo++) {
                            List<TileZeroTile> tileComboTwo = shapeTileLists[indexTwo];

                            // [SC] true if combo one is a subset of combo two
                            if (tileComboOne.Count <= tileComboTwo.Count && tileComboOne.All(p => tileComboTwo.Contains(p))) {
                                tileComboOne.Clear();
                                continue;
                            }
                        }
                    }
                }
                colorTileLists.RemoveAll(p => p.Count == 0); // [SC] remove empty tile combos
            }

            if (considerShape) {
                // [SC] remove all shape tile combos that are subsets of another color or shape combo
                for (int indexOne = 0; indexOne < shapeTileLists.Count; indexOne++) {
                    List<TileZeroTile> tileComboOne = shapeTileLists[indexOne];

                    for (int indexTwo = indexOne + 1; indexTwo < shapeTileLists.Count; indexTwo++) {
                        List<TileZeroTile> tileComboTwo = shapeTileLists[indexTwo];

                        if (tileComboOne.All(p => tileComboTwo.Contains(p))) {
                            tileComboOne.Clear();
                            continue;
                        }
                    }

                    if (tileComboOne.Count == 0) { continue; }

                    if (considerColor) {
                        for (int indexTwo = 0; indexTwo < colorTileLists.Count; indexTwo++) {
                            List<TileZeroTile> tileComboTwo = colorTileLists[indexTwo];

                            // [SC] true if combo one is a subset of combo two
                            if (tileComboOne.Count <= tileComboTwo.Count && tileComboOne.All(p => tileComboTwo.Contains(p))) {
                                tileComboOne.Clear();
                                continue;
                            }
                        }
                    }
                }
                shapeTileLists.RemoveAll(p => p.Count == 0); // [SC] remove empty tile combos
            }
            //
            //////////////////////////////////////////////////////////////////////////////

            //////////////////////////////////////////////////////////////////////////////
            // [2016.12.08] new code
            if (considerColor) {
                // [SC] iterate through list of tiles with one particular color
                foreach (List<TileZeroTile> tileList in colorTileLists) {
                    int colorIndex = tileList[0].getColorIndex();

                    TreeNode rootNode = new TreeNode(null);
                    for (int tileIndex = 0; tileIndex < tileList.Count; tileIndex++) {
                        tileListPermAddChildNodes(tileList, tileIndex, rootNode);
                    }

                    tileListPermTraverseTreePaths(rootNode, new List<TileZeroTile>(), colorIndex, Cfg.COLOR_ATTR, candTileSeqList);
                }
            }

            if (considerShape) {
                // [SC] iterate through list of tiles with one particular shape
                foreach(List<TileZeroTile> tileList in shapeTileLists) {
                    int shapeIndex = tileList[0].getShapeIndex();

                    TreeNode rootNode = new TreeNode(null);
                    for (int tileIndex = 0; tileIndex < tileList.Count; tileIndex++) {
                        tileListPermAddChildNodes(tileList, tileIndex, rootNode);
                    }

                    tileListPermTraverseTreePaths(rootNode, new List<TileZeroTile>(), shapeIndex, Cfg.SHAPE_ATTR, candTileSeqList);
                }
            }
            //
            //////////////////////////////////////////////////////////////////////////////

            //////////////////////////////////////////////////////////////////////////////
            // [2016.12.08] new code
            int maxCTS = 10;
            if (candTileSeqList.Count > maxCTS) {
                candTileSeqList = candTileSeqList.OrderByDescending(p => p.getTileCount()).ToList();
                candTileSeqList.RemoveRange(maxCTS, candTileSeqList.Count - maxCTS);
            }
            //
            //////////////////////////////////////////////////////////////////////////////

            VirtualBoard virtualBoard = game.getVirtualBoard();
            TileZeroTile[,] tileArray = virtualBoard.getBoardCopy();

            List<CandidateTilePos> chosenPosComboList = new List<CandidateTilePos>();
            List<CandidateTilePos> maxScorePosComboList = new List<CandidateTilePos>();

            foreach (CandidateTileSeq candTileSeq in candTileSeqList) {
                TreeNode rootNode = new TreeNode(null);

                boardPosPermAddChildNodes(candTileSeq, 0, rootNode, tileArray, virtualBoard);
                if (suboptiomal) {
                    boardPosPermTraverseTreePaths(rootNode, new List<AbstractPos>(), candTileSeq, 0, chosenPosComboList, maxScorePosComboList);
                }
                else {
                    boardPosPermTraverseTreePaths(rootNode, new List<AbstractPos>(), candTileSeq, 0, chosenPosComboList);
                }
            }

            return chosenPosComboList.getRandomElement();
        }

        /////////////////////////////////////////////////////////////////
        ////// START: A code for creating all possible permutations of board positions 
        ////// of tiles in given list, starting from left-most tile in the array

        private void boardPosPermAddChildNodes(CandidateTileSeq candTileSeq, int currTileIndex, TreeNode parentNode, TileZeroTile[,] tileArray, VirtualBoard virtualBoard) {
           if (currTileIndex >= candTileSeq.getTileCount()) return;

            TileZeroTile tile = candTileSeq.getTileAt(currTileIndex);

            for (int currRowIndex = 0; currRowIndex < tileArray.GetLength(0); currRowIndex++) {
                for (int currColIndex = 0; currColIndex < tileArray.GetLength(1); currColIndex++) {
                    int resultScore = virtualBoard.isValidMove(currRowIndex, currColIndex, tile, true, tileArray, false);

                    if (resultScore != Cfg.NONE) {
                        TileZeroTile[,] newTileArray = Cfg.createBoardCopy(tileArray);
                        virtualBoard.addTile(currRowIndex, currColIndex, tile, false, newTileArray);
                        TreeNode childNode = parentNode.addChildNodeValue(new AbstractPos(currTileIndex, currRowIndex, currColIndex, resultScore));
                        boardPosPermAddChildNodes(candTileSeq, currTileIndex + 1, childNode, newTileArray, virtualBoard);
                    }
                }
            }

            boardPosPermAddChildNodes(candTileSeq, currTileIndex + 1, parentNode, tileArray, virtualBoard); // [SC][2016.12.08] new code
        }

        private void boardPosPermTraverseTreePaths(TreeNode rootNode, List<AbstractPos> currPath, CandidateTileSeq candTileSeq, int currScore, List<CandidateTilePos> maxScorePosComboList) {
           if (rootNode.hasChildNodes()) {
                List<TreeNode> childNodes = rootNode.getChildNodes();
                foreach (TreeNode childNode in childNodes) {
                    List<AbstractPos> newPath = currPath.listShallowClone();
                    AbstractPos pos = (AbstractPos)childNode.getValue();
                    newPath.Add(pos);
                    boardPosPermTraverseTreePaths(childNode, newPath, candTileSeq, currScore + pos.getScore(), maxScorePosComboList);
                }
            }
            else { // [SC] reached the final leaf; no more moves in the combo
                if (currScore > 0) {
                    CandidateTilePos newCandTilePos = new CandidateTilePos(candTileSeq, currPath, currScore);

                    if (maxScorePosComboList.Count == 0) {
                        maxScorePosComboList.Add(newCandTilePos);
                    }
                    else if (maxScorePosComboList[0].getTotalScore() == currScore) {
                        maxScorePosComboList.Add(newCandTilePos);
                    }
                    else if (maxScorePosComboList[0].getTotalScore() < currScore) {
                        maxScorePosComboList.Clear();
                        maxScorePosComboList.Add(newCandTilePos);
                    }
                }
            }
        }

        ////// END: A code for creating all possible permutations of board positions 
        ////// of tiles in given list, starting from left-most tile in the array
        /////////////////////////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////
        ////// START: A code for creating all possible permutations of tiles in list

        private void tileListPermAddChildNodes(List<TileZeroTile> tileList, int childValIndex, TreeNode rootNode) {
            List<TileZeroTile> newList = tileList.listShallowClone();
            TileZeroTile childNodeValue = newList.Pop(childValIndex);
            TreeNode childNode = rootNode.addChildNodeValue(childNodeValue);

            for (int tileIndex = 0; tileIndex < newList.Count; tileIndex++) {
                tileListPermAddChildNodes(newList, tileIndex, childNode);
            }
        }

        private void tileListPermTraverseTreePaths(TreeNode rootNode, List<TileZeroTile> currPath, int attrValueIndex, int attrIndex, List<CandidateTileSeq> candTileSeqList) {
            if (rootNode.hasChildNodes()) {
                List<TreeNode> childNodes = rootNode.getChildNodes();
                foreach (TreeNode childNode in childNodes) {
                    List<TileZeroTile> newPath = currPath.listShallowClone();
                    newPath.Add((TileZeroTile)childNode.getValue());
                    tileListPermTraverseTreePaths(childNode, newPath, attrValueIndex, attrIndex, candTileSeqList);
                }
            }
            else {
                //////////////////////////////////////////////////////////////////////////////
                // [2016.12.08] new code
                CandidateTileSeq newCts = new CandidateTileSeq(attrValueIndex, attrIndex, currPath);
                List<CandidateTileSeq> cstToRemove = new List<CandidateTileSeq>();

                bool addFlag = true;
                foreach (CandidateTileSeq oldCts in candTileSeqList) {
                    // [SC] if true the new cts is a subset of existing cts
                    if (newCts.isOrderedSubsetOf(oldCts)) {
                        addFlag = false;
                        break;
                    }
                    // [SC] if true an existing cts is a subset of the new cts
                    else if (oldCts.isOrderedSubsetOf(newCts)) {
                        cstToRemove.Add(oldCts);
                    }
                }

                candTileSeqList.RemoveAll(p => cstToRemove.Contains(p));

                if (addFlag) {
                    candTileSeqList.Add(newCts);
                }
                //
                //////////////////////////////////////////////////////////////////////////////
            }
        }

        ////// END: A code for creating all possible permutations of tiles in list
        /////////////////////////////////////////////////////////////////
        #endregion very hard AI
        ////// END: very hard ai functionality
        /////////////////////////////////////////////////////////////////
    }
}
