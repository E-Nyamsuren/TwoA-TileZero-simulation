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
Filename: TileZeroTile.cs
Description:
    Defines a class that represents a TileZero tile.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileZero 
{
    public class TileZeroTile 
    {
        private int colorIndex;
        private int shapeIndex;
        private int tileID;
        private bool playableFlag;
        private bool canDrop;

        public TileZeroTile(int colorIndex, int shapeIndex, int tileID) {
            this.colorIndex = colorIndex;
            this.shapeIndex = shapeIndex;
            this.tileID = tileID;
            resetTile();
        }

        public int getColorIndex() {
            return colorIndex;
        }

        public int getShapeIndex() {
            return shapeIndex;
        }

        public int getTileID() {
            return tileID;
        }

        // [SC] returns true if this tile has the same color and shape as another tile 
        public bool sameVisTile(TileZeroTile tile) {
            if (this.colorIndex == tile.getColorIndex() && this.shapeIndex == tile.getShapeIndex()) return true;
            else return false;
        }

        public bool sameTile(int colorIndex, int shapeIndex, int tileID) {
            if (this.colorIndex == colorIndex && this.shapeIndex == shapeIndex && this.tileID == tileID) return true;
            else return false;
        }

        public void setPlayable(bool playableFlag) {
            this.playableFlag = playableFlag;
        }

        public bool getPlayable() {
            return playableFlag;
        }

        public void setCanDrop(bool canDrop) {
            this.canDrop = canDrop;
        }

        public bool getCanDrop() {
            return canDrop;
        }

        public void resetTile() {
            setPlayable(true);
            setCanDrop(true);
        }

        public override string ToString() {
            return getColorIndex() + "" + getShapeIndex();
        }
    }
}
