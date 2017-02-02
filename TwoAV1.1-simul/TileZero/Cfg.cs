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
Filename: Cfg.cs
Description:
    Contains configurational settings as constants, static variables and static utility methods for the TileZero game.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileZero 
{
    public static class Cfg 
    {
        private static Random rng = new Random();

        public const int BOARD_COL_COUNT = 15;
        public const int BOARD_ROW_COUNT = 15;

        public const int NONE = -1;

        public const int COLOR_ATTR = 1;
        public const int SHAPE_ATTR = 2;

        public const int MAX_VAL_INDEX = 6; // [SC] the the numeric id for color and shape features ranges from 0 to 5

        public const int MAX_TILE_ID = 3; // [SC] there can be three tiles of the same color:shape combination

        public const int MAX_BAG_SIZE = MAX_VAL_INDEX * MAX_VAL_INDEX * MAX_TILE_ID;

        public const int MAX_PLAYER_TILE_COUNT = 6;

        public const int START_TILE_COUNT = 3;

        public const int HORIZONTAL = 1;
        public const int VERTICAL = 2;

        public const long TURN_DURATION = 10000;

        public const int LAST_PLAYER_REWARD = 6;
        public const int MAX_SEQ_SCORE = 6;
        public const int TILEZERO_REWARD = MAX_SEQ_SCORE * 2;

        public const string VERY_EASY_AI = "Very Easy AI";
        public const string EASY_AI = "Easy AI";
        public const string MEDIUM_COLOR_AI = "Medium Color AI";
        public const string MEDIUM_SHAPE_AI = "Medium Shape AI";
        public const string HARD_AI = "Hard AI";
        public const string VERY_HARD_AI = "Very Hard AI";

        private static string logStr = "";
        private static bool logEnableFlag = true;

        public static string getTileFeatureID(int colorIndex, int shapeIndex) {
            return colorIndex + "" + shapeIndex;
        }

        public static void enableLog(bool flag) {
            logEnableFlag = flag;
        }

        public static void clearLog() {
            logStr = "";
        }

        public static void log(string str) {
            if (logEnableFlag) logStr += "\n" + str;
        }

        public static string getLog() {
            return logStr;
        }

        // Fisher-Yates shuffle
        public static void Shuffle<T>(this IList<T> list) {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        // [SC] Pop operation on a list
        public static T Pop<T>(this IList<T> list, int index) {
            if (list.Count > index) {
                T elem = list[index];
                list.RemoveAt(index);
                return elem;
            }
            return default(T);
        }

        // [SC] get a random element from a list
        public static T getRandomElement<T>(this IList<T> list) {
            int n = list.Count;
            if (n == 1) return list[0];
            else if (n > 1) return list[rng.Next(n)];
            else return default(T);
        }

        // [SC] do list shallow cloning
        public static List<T> listShallowClone<T>(this IList<T> list) {
            List<T> cloneList = new List<T>();
            foreach (T listItem in list) {
                cloneList.Add(listItem);
            }
            return cloneList;
        }

        // [SC] create a shallow clone of the 2D array
        public static TileZeroTile[,] createBoardCopy(TileZeroTile[,] tileArray) {
            if (tileArray == null) return null;

            int rowCount = tileArray.GetLength(0);
            int colCount = tileArray.GetLength(1);

            TileZeroTile[,] newTileArray = new TileZeroTile[rowCount, colCount];

            for (int currRowIndex = 0; currRowIndex < rowCount; currRowIndex++) {
                for (int currColIndex = 0; currColIndex < colCount; currColIndex++) {
                    newTileArray[currRowIndex, currColIndex] = tileArray[currRowIndex, currColIndex];
                }
            }

            return newTileArray;
        }
    }
}
