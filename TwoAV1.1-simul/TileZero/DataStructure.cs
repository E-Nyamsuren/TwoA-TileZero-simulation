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
Filename: DataStructure.cs
Description:
    Defines a set of abstract datastructures used by the TileZero game.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileZero
{
    public class AbstractTile 
    {
        public int colorIndex;
        public int shapeIndex;
        public int tileID;
        public bool playableFlag;

        public AbstractTile(int colorIndex, int shapeIndex, int tileID, bool playableFlag) {
            this.colorIndex = colorIndex;
            this.shapeIndex = shapeIndex;
            this.tileID = tileID;
            this.playableFlag = playableFlag;
        }
    }

    // [SC] the same list of tiles can be placed on a board in different possible orders (tile sequences)
    // [SC] object of this class represents one possible sequence of tiles
    public class CandidateTileSeq 
    {
        private int attrValueIndex;                         // [SC] indicates which attribute value is same among tiles of this sequence
        private int attrIndex;                              // [SC] indicates which attribute is same among tiles of this sequence (color or shape)
        private List<TileZeroTile> tileSequence;            // [SC] ordered sequence of tile objects
        private List<CandidateTilePos> candTilePosList;     // [SC] a list of CandidateTilePos objects

        public CandidateTileSeq(int attrValueIndex, int attrIndex, List<TileZeroTile> tileSequence) {
            this.attrValueIndex = attrValueIndex;
            this.attrIndex = attrIndex;
            this.tileSequence = tileSequence;
            candTilePosList = new List<CandidateTilePos>();
        }

        public int getTileCount() {
            return tileSequence.Count; // [SC][TODO] for now let it crash if tileSequence == null; but it should not happen
        }

        public TileZeroTile getTileAt(int tileIndex) {
            //[TODO] if (tileSequence != null && tileSequence.Count > tileIndex)
            return tileSequence[tileIndex];
        }

        public void addCandTilePos(CandidateTilePos candTilePos) {
            candTilePosList.Add(candTilePos);
        }

        public List<CandidateTilePos> getPosComboList() {
            return candTilePosList;
        }

        public bool isOrderedSubsetOf(CandidateTileSeq cts) {
            if (getTileCount() > cts.getTileCount()) {
                return false;
            }
            else {
                for (int tileIndex = 0; tileIndex < getTileCount(); tileIndex++) {
                    if (!getTileAt(tileIndex).sameVisTile(cts.getTileAt(tileIndex))) {
                        return false;
                    }
                }
                return true;
            }
        }

        public string TileSeqToString(){
            string str = "(";
            foreach (TileZeroTile tile in tileSequence) {
                str += tile.ToString() + ",";
            }
            return str + ")";
        }
    }

    // [SC] the same sequence of tiles can be placed on board with different possible combinations of tile positions
    // [SC] object of this class represents one possible combination of tile positions
    public class CandidateTilePos 
    {
        private CandidateTileSeq candTileSequence;          // [SC] a reference to parent CandidateTileSeq object that contains this objects
        private List<AbstractPos> posList;                  // [SC] a list of lists where child list contains: (arrayIndex of the tile, row position on board, col position on board, score if tile is placed)  
        private int totalScore;                             // [SC] a total score if all tiles are placed on board 

        public CandidateTilePos(CandidateTileSeq candTileSequence, List<AbstractPos> posList, int totalScore) {
            this.candTileSequence = candTileSequence;
            this.posList = posList;
            this.totalScore = totalScore;
            candTileSequence.addCandTilePos(this);
        }

        public int getTotalScore() {
            return totalScore;
        }

        public void setTotalScore(int totalScore) {
            this.totalScore = totalScore;
        }

        public int getComboLength() {
            return posList.Count;
        }

        public AbstractPos getAbstrPosAt(int index) {
            //if (posList != null && posList.Count > index)
            return posList[index];
        }

        public CandidateTileSeq getCandidateTileSeq() {
            return candTileSequence;
        }
    }

    public class AbstractPos 
    {
        private int tileIndex;
        private int rowIndex;
        private int colIndex;
        private int score;

        public AbstractPos(int tileIndex, int rowIndex, int colIndex, int score) {
            this.tileIndex = tileIndex;
            this.rowIndex = rowIndex;
            this.colIndex = colIndex;
            this.score = score;
        }

        public int getScore() {
            return score;
        }


        public int getTileIndex() {
            return tileIndex;
        }

        public int getRowIndex() {
            return rowIndex;
        }

        public int getColIndex() {
            return colIndex;
        }
    }

    public class TreeNode 
    {
        private TreeNode parentNode;
        private List<TreeNode> childNodes;
        private Object value;

        public TreeNode(Object value) {
            parentNode = null;
            childNodes = new List<TreeNode>();
            this.value = value;
        }

        public void addChildeNode(TreeNode childNode) {
            childNodes.Add(childNode);
            childNode.setParetNode(this);
        }

        public void setParetNode(TreeNode parentNode) {
            this.parentNode = parentNode;
        }

        public TreeNode addChildNodeValue(Object value) {
            TreeNode childNode = new TreeNode(value);
            addChildeNode(childNode);
            return childNode;
        }

        public bool hasChildNodes() {
            if (childNodes != null && childNodes.Count > 0) return true;
            else return false;
        }

        public List<TreeNode> getChildNodes() {
            return childNodes;
        }

        public Object getValue() {
            return value;
        }
    }
}
