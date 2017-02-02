using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace TileZero 
{
    public static class Test 
    {
        public static void testCaseOne () {
            /*
            	-0 -1 -2 -3 -4 -5 -6 -7 -8 -9 10 11 12 13 14
            -0	-- -- -- -- -- -- -- -- -- 10 -- -- -- -- --
            -1	-- -- -- -- -- -- -- -- -- 15 -- -- -- -- --
            -2	-- -- -- -- -- -- -- -- -- 12 -- -- -- -- --
            -3	-- -- -- -- -- -- -- -- -- 14 44 24 04 -- --
            -4	-- -- -- -- -- -- -- -- 21 11 41 -- -- -- --
            -5	-- -- -- -- -- -- -- 15 11 13 -- -- -- -- --
            -6	-- -- -- -- 20 -- 12 -- 41 -- -- -- -- -- --
            -7	-- -- -- -- 22 -- 42 05 31 -- -- -- -- -- --
            -8	-- -- -- -- 23 20 22 -- 01 05 03 04 02 -- --
            -9	-- -- -- -- -- 50 52 53 51 -- -- -- -- -- --
            10	-- 41 42 45 44 40 -- -- -- -- -- -- -- -- --
            11	-- -- 52 -- -- 30 -- -- -- -- -- -- -- -- --
            12	-- -- -- -- -- -- -- -- -- -- -- -- -- -- --
            13	-- -- -- -- -- -- -- -- -- -- -- -- -- -- --
            14	-- -- -- -- -- -- -- -- -- -- -- -- -- -- --

            Player 0: Very Hard AI - active player
            03
            03
            33
            33
            43
            53

            Player 1: Medium Shape AI
            05
            31
            34
            51
            31
            04
             * */

            /*Game game = new Game();
            
            // [SC] creating board
            VirtualBoard board = new VirtualBoard(game);
            game.virtualBoard = board;

            int tileID = 0;
            board.addTile(0, 9, new TileZeroTile(1, 0, tileID++), false, null);

            board.addTile(1, 9, new TileZeroTile(1, 5, tileID++), false, null);
            
            board.addTile(2, 9, new TileZeroTile(1, 2, tileID++), false, null);

            board.addTile(3, 9, new TileZeroTile(1, 4, tileID++), false, null);
            board.addTile(3, 10, new TileZeroTile(4, 4, tileID++), false, null);
            board.addTile(3, 11, new TileZeroTile(2, 4, tileID++), false, null);
            board.addTile(3, 12, new TileZeroTile(0, 4, tileID++), false, null);

            board.addTile(4, 8, new TileZeroTile(2, 1, tileID++), false, null);
            board.addTile(4, 9, new TileZeroTile(1, 1, tileID++), false, null);
            board.addTile(4, 10, new TileZeroTile(4, 1, tileID++), false, null);

            board.addTile(5, 7, new TileZeroTile(1, 5, tileID++), false, null);
            board.addTile(5, 8, new TileZeroTile(1, 1, tileID++), false, null);
            board.addTile(5, 9, new TileZeroTile(1, 3, tileID++), false, null);
            
            board.addTile(6, 4, new TileZeroTile(2, 0, tileID++), false, null);
            board.addTile(6, 6, new TileZeroTile(1, 2, tileID++), false, null);
            board.addTile(6, 8, new TileZeroTile(4, 1, tileID++), false, null);

            board.addTile(7, 4, new TileZeroTile(2, 2, tileID++), false, null);
            board.addTile(7, 6, new TileZeroTile(4, 2, tileID++), false, null);
            board.addTile(7, 7, new TileZeroTile(0, 5, tileID++), false, null);
            board.addTile(7, 8, new TileZeroTile(3, 1, tileID++), false, null);

            board.addTile(8, 4, new TileZeroTile(2, 3, tileID++), false, null);
            board.addTile(8, 5, new TileZeroTile(2, 0, tileID++), false, null);
            board.addTile(8, 6, new TileZeroTile(2, 2, tileID++), false, null);
            board.addTile(8, 8, new TileZeroTile(0, 1, tileID++), false, null);
            board.addTile(8, 9, new TileZeroTile(0, 5, tileID++), false, null);
            board.addTile(8, 10, new TileZeroTile(0, 3, tileID++), false, null);
            board.addTile(8, 11, new TileZeroTile(0, 4, tileID++), false, null);
            board.addTile(8, 12, new TileZeroTile(0, 2, tileID++), false, null);

            board.addTile(9, 5, new TileZeroTile(5, 0, tileID++), false, null);
            board.addTile(9, 6, new TileZeroTile(5, 2, tileID++), false, null);
            board.addTile(9, 7, new TileZeroTile(5, 3, tileID++), false, null);
            board.addTile(9, 8, new TileZeroTile(5, 1, tileID++), false, null);

            board.addTile(10, 1, new TileZeroTile(4, 1, tileID++), false, null);
            board.addTile(10, 2, new TileZeroTile(4, 2, tileID++), false, null);
            board.addTile(10, 3, new TileZeroTile(4, 5, tileID++), false, null);
            board.addTile(10, 4, new TileZeroTile(4, 4, tileID++), false, null);
            board.addTile(10, 5, new TileZeroTile(4, 0, tileID++), false, null);

            board.addTile(11, 2, new TileZeroTile(5, 2, tileID++), false, null);
            board.addTile(11, 5, new TileZeroTile(3, 0, tileID++), false, null);

            // [SC] creating players
            List<Player> players = new List<Player>();
            game.players = players;

            // [SC] creating player one
            Player playerOne = new Player(0, Cfg.VERY_HARD_AI, game);
            players.Add(playerOne);
            playerOne.addTile(new TileZeroTile(0, 3, tileID++));
            playerOne.addTile(new TileZeroTile(0, 3, tileID++));
            playerOne.addTile(new TileZeroTile(3, 3, tileID++));
            playerOne.addTile(new TileZeroTile(3, 3, tileID++));
            playerOne.addTile(new TileZeroTile(4, 3, tileID++));
            playerOne.addTile(new TileZeroTile(5, 3, tileID++));

            // creating player two
            Player playerTwo = new Player(1, Cfg.MEDIUM_SHAPE_AI, game);
            players.Add(playerTwo);
            playerTwo.addTile(new TileZeroTile(0, 5, tileID++));
            playerTwo.addTile(new TileZeroTile(3, 1, tileID++));
            playerTwo.addTile(new TileZeroTile(3, 4, tileID++));
            playerTwo.addTile(new TileZeroTile(5, 1, tileID++));
            playerTwo.addTile(new TileZeroTile(3, 1, tileID++));
            playerTwo.addTile(new TileZeroTile(0, 4, tileID++));

            // [SC]
            printMsg(game.getVirtualBoard().ToString());
            printMsg("#################################################################");
            printMsg(game.getPlayerByIndex(0).PlayerTilesToString());
            printMsg("#################################################################");
            printMsg(game.getPlayerByIndex(1).PlayerTilesToString());

            game.activePlayerIndex = 0;
            game.activeGameFlag = true;
            playerOne.invokeAI();

            printMsg(Cfg.getLog());

            printMsg(game.getVirtualBoard().ToString());
            printMsg("#################################################################");
            printMsg(game.getPlayerByIndex(0).PlayerTilesToString());
            printMsg("#################################################################");
            printMsg(game.getPlayerByIndex(1).PlayerTilesToString());*/
        }

        public static void printMsg(string msg) {
            Console.WriteLine(msg);
            Debug.WriteLine(msg);
        }
    }
}
