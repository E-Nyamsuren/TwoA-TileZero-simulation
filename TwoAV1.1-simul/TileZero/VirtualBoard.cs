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
Filename: VirtualBoard.cs
Description:
    It is a logical implementation of a TileZero board. Belongs to the Model component.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileZero 
{
    public class VirtualBoard 
    {
        private const int rowCount = Cfg.BOARD_ROW_COUNT;
        private const int colCount = Cfg.BOARD_COL_COUNT;

        private TileZeroTile[,] tileArray;

        private Game game;

        public VirtualBoard(Game game) {
            this.game = game;
            resetBoard();
        }

        public void resetBoard() {
            tileArray = new TileZeroTile[rowCount, colCount];
        }

        public int addTile(int rowIndex, int colIndex, TileZeroTile tile, bool validCheck, TileZeroTile[,] tileArrayP) {
            if (tileArrayP == null) tileArrayP = tileArray;

            int result = isValidMove(rowIndex, colIndex, tile, validCheck, tileArrayP, true);

            if (result != Cfg.NONE) tileArrayP[rowIndex, colIndex] = tile;

            return result;
        }

        public int getColCount() {
            return colCount;
        }

        public int getRowCount() {
            return rowCount;
        }

        // [SC] returns true if the indicated cell has a tile
        private bool hasTile(int rowIndex, int colIndex, TileZeroTile[,] tileArrayP) {
            if (tileArrayP[rowIndex, colIndex] != null) return true;
            else return false;
        }

        // [SC] returns true if the left cell adjacent to the indicated cell has a tile
        private bool hasLeftTile(int rowIndex, int colIndex, TileZeroTile[,] tileArrayP) {
            if (colIndex > 0 && tileArrayP[rowIndex, colIndex - 1] != null) return true;
            else return false;
        }

        // [SC] returns true if the right cell adjacent to the indicated cell has a tile
        private bool hasRightTile(int rowIndex, int colIndex, TileZeroTile[,] tileArrayP) {
            if (colIndex < (colCount - 1) && tileArrayP[rowIndex, colIndex + 1] != null) return true;
            else return false;
        }

        // [SC] returns true if the top cell adjacent to the indicated cell has a tile
        private bool hasTopTile(int rowIndex, int colIndex, TileZeroTile[,] tileArrayP) {
            if (rowIndex > 0 && tileArrayP[rowIndex - 1, colIndex] != null) return true;
            else return false;
        }

        // [SC] returns true if the bottom cell adjacent to the indicated cell has a tile
        private bool hasBottomTile(int rowIndex, int colIndex, TileZeroTile[,] tileArrayP) {
            if (rowIndex < (rowCount - 1) && tileArrayP[rowIndex + 1, colIndex] != null) return true;
            else return false;
        }

        public int isValidMove(int rowIndex, int colIndex, TileZeroTile tile, bool validCheck, TileZeroTile[,] tileArrayP, bool showMsg) {
            if (tileArrayP == null) tileArrayP = tileArray;

            int horizScore = 0;
            int vertScore = 0;

            if (rowIndex < 0 || rowIndex >= rowCount) {
                if (showMsg) Cfg.log("Invalid row index: " + rowIndex + ".");
                return Cfg.NONE;
            }

            if (colIndex < 0 || colIndex >= colCount) {
                if (showMsg) Cfg.log("Invalid column index: " + colIndex + ".");
                return Cfg.NONE;
            }

            if (hasTile(rowIndex, colIndex, tileArrayP)) {
                if (showMsg) Cfg.log("The cell already has a tile.");
                return Cfg.NONE;
            }

            if (validCheck) {

                // [SC] check if there is any tile adjacent to the destinatio position
                if (!hasLeftTile(rowIndex, colIndex, tileArrayP) && !hasRightTile(rowIndex, colIndex, tileArrayP)
                    && !hasBottomTile(rowIndex, colIndex, tileArrayP) && !hasTopTile(rowIndex, colIndex, tileArrayP)
                    ) {
                    if (showMsg) Cfg.log("A new tile should be placed next to the existing one.");
                    return Cfg.NONE;
                }

                // [SC] temporarily put the tile
                tileArrayP[rowIndex, colIndex] = tile;

                // [SC] check validity of the horizontal sequence of tiles
                if (hasLeftTile(rowIndex, colIndex, tileArrayP) || hasRightTile(rowIndex, colIndex, tileArrayP)) {
                    horizScore = isValidSequence(rowIndex, colIndex, Cfg.HORIZONTAL, tileArrayP, showMsg);
                    if (horizScore == Cfg.NONE) {
                        tileArrayP[rowIndex, colIndex] = null;
                        return Cfg.NONE;
                    }
                    else if (horizScore == Cfg.MAX_SEQ_SCORE) {
                        // [SC] reward for completing a TileZero
                        horizScore = Cfg.TILEZERO_REWARD;
                    }
                }

                // [SC] check validity of the vertical sequence of tiles
                if (hasTopTile(rowIndex, colIndex, tileArrayP) || hasBottomTile(rowIndex, colIndex, tileArrayP)) {
                    vertScore = isValidSequence(rowIndex, colIndex, Cfg.VERTICAL, tileArrayP, showMsg);
                    if (vertScore == Cfg.NONE) {
                        tileArrayP[rowIndex, colIndex] = null;
                        return Cfg.NONE;
                    }
                    else if (vertScore == Cfg.MAX_SEQ_SCORE) {
                        // [SC] reward for completing a TileZero
                        vertScore = Cfg.TILEZERO_REWARD;
                    }
                }

                // [SC] remove the temporary tile
                tileArrayP[rowIndex, colIndex] = null;
            }

            return horizScore + vertScore;
        }

        private int isValidSequence(int rowIndex, int colIndex, int orientation, TileZeroTile[,] tileArrayP, bool showMsg) {
            int[] uniqueColors = new int[Cfg.MAX_VAL_INDEX];
            int uniqueColorCount = 0;

            int[] uniqueShapes = new int[Cfg.MAX_VAL_INDEX];
            int uniqueShapeCount = 0;

            int sequenceLength = 0;

            int currRow = rowIndex;
            int currCol = colIndex;

            for (int currIndex = 0; currIndex < Cfg.MAX_VAL_INDEX; currIndex++) {
                uniqueColors[currIndex] = Cfg.NONE;
                uniqueShapes[currIndex] = Cfg.NONE;
            }

            // [SC] start with the left-most or top-most tile in the sequence
            if (orientation == Cfg.HORIZONTAL) {
                while (currCol > 0 && tileArrayP[currRow, currCol - 1] != null)
                    currCol--;
            }
            else {
                while (currRow > 0 && tileArrayP[currRow - 1, currCol] != null)
                    currRow--;
            }

            // [SC] checking the validity of colors and shapes, and color-shape combination of the sequence
            while (currRow < rowCount && currCol < colCount) {
                TileZeroTile currTile = tileArrayP[currRow, currCol];

                if (currTile == null)
                    break;

                // [SC] checking the validity of colors
                int currColorIndex = currTile.getColorIndex();
                if (uniqueColors[currColorIndex] == Cfg.NONE) {
                    uniqueColors[currColorIndex] = currColorIndex;
                    uniqueColorCount++;
                }
                else if (uniqueColorCount == 1) {
                }
                else {
                    if (showMsg) Cfg.log("Invalid color sequence.");
                    return Cfg.NONE;
                }

                // [SC] checking the validity of shapes
                int currShapeIndex = currTile.getShapeIndex();
                if (uniqueShapes[currShapeIndex] == Cfg.NONE) {
                    uniqueShapes[currShapeIndex] = currShapeIndex;
                    uniqueShapeCount++;
                }
                else if (uniqueShapeCount == 1) {
                }
                else {
                    if (showMsg) Cfg.log("Invalid shape sequence.");
                    return Cfg.NONE;
                }

                sequenceLength++;

                if (sequenceLength > 1) {
                    if ((uniqueColorCount == 1 && uniqueShapeCount == 1) || // [SC] both shape and color are same
                        (uniqueColorCount > 1 && uniqueShapeCount > 1) // both shape and color are different
                        ) {
                        if (showMsg) Cfg.log("Invalid combination of color and shape.");
                        return Cfg.NONE;
                    }
                }

                // [TODO] update row
                if (orientation == Cfg.HORIZONTAL) currCol++;
                else currRow++;
            }

            return sequenceLength;
        }

        public TileZeroTile[,] getBoardCopy() {
            return Cfg.createBoardCopy(tileArray);
        }

        public override string ToString() {
            string boardStr = "";

            string indexRow = "   ";
            string brRow = "   ";
            for (int colIndex = 0; colIndex < colCount; colIndex++) {
                if (colIndex < 10) indexRow += "0" + colIndex + " ";
                else indexRow += colIndex + " ";

                brRow += "---";
            }
            boardStr += indexRow + "\n";
            boardStr += brRow + "\n";

            for (int rowIndex=0; rowIndex<rowCount; rowIndex++) {
                string rowStr = "";

                if (rowIndex < 10) rowStr += "0" + rowIndex + "|";
                else rowStr += rowIndex + "|";

                for (int colIndex=0; colIndex<colCount; colIndex++){
                    TileZeroTile tile = tileArray[rowIndex, colIndex];
                    if (tile == null) {
                        rowStr += "-- ";
                    }
                    else {
                        rowStr += tile.ToString() + " ";
                    }
                }
                boardStr += rowStr + "\n";
            }

            return boardStr;
        }
    }
}
