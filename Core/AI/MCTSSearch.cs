namespace Chess
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
    using UnityEngine;
    using static System.Math;
	class MCTSSearch : ISearch
	{
		public event System.Action<Move> onSearchComplete;

		MoveGenerator moveGenerator;

		Move bestMove;
		int bestEval;
		bool abortSearch;

		MCTSSettings settings;
		Board board;
		Evaluation evaluation;

		System.Random rand;

		// Diagnostics
		public SearchDiagnostics Diagnostics { get; set; }
		System.Diagnostics.Stopwatch searchStopwatch;

		public MCTSSearch(Board board, MCTSSettings settings)
		{
			this.board = board;
			this.settings = settings;
			evaluation = new Evaluation();
			moveGenerator = new MoveGenerator();
			rand = new System.Random();
		}

		public void StartSearch()
		{
			InitDebugInfo();

			// Initialize search settings
			bestEval = 0;
			bestMove = Move.InvalidMove;

			moveGenerator.promotionsToGenerate = settings.promotionsToSearch;
			abortSearch = false;
			Diagnostics = new SearchDiagnostics();

			SearchMoves();

			onSearchComplete?.Invoke(bestMove);

			if (!settings.useThreading)
			{
				LogDebugInfo();
			}
		}

		public void EndSearch()
		{
			if (settings.useTimeLimit)
			{
				abortSearch = true;
			}
		}

        void SearchMoves()
        {
            // Initialize the root node of the tree
            Node root = new Node(board, null);

            // Start the search timer
            var searchStopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Keep searching until the search playout limit is reached
            int playoutCount = 0;
            while (!abortSearch && (!settings.limitNumOfPlayouts || playoutCount < settings.maxNumOfPlayouts))
            {
                // Select a leaf node to expand
                Node leaf = TreePolicy(root);

                // Simulate a random game from the leaf node
                int[] score = DefaultPolicy(leaf.board);
                // Update the statistics of the node and its ancestors
                Backup(leaf, score);

                playoutCount++;
            }

            // Find the best move among the children of the root node

            //Node bestChild = BestChild(root, 0, board.WhiteToMove);
            Node bestChild = BestChild(root, 0);

            if (bestChild != null)
            {
                bestMove = bestChild.move;
                bestEval = (int)(bestChild.value / (double)bestChild.visits);
            }

            // Stop the search timer
            searchStopwatch.Stop();

            // Update the search diagnostics
            Diagnostics.searchTime = searchStopwatch.ElapsedMilliseconds;
            //Diagnostics.nodesExpanded = root.visits;
        }

        void Backup(Node leaf, int[] score)
        {
            Node currentNode = leaf;
            while (currentNode != null)
            {
                currentNode.visits++;
                currentNode.value += score[currentNode.board.WhiteToMove ? 0 : 1];
                currentNode = currentNode.parent;
            }
        }

        //Node TreePolicy(Node node)
        //{
        //    while (!IsLeaf(node))
        //    {
        //        // Select the child node with the highest UCB value
        //        node = BestChild(node, settings.explorationConstant);
        //    }
        //    return node;
        //}

        bool IsLeaf(Node node)
        {
            return node.children.Count == 0;
        }

        Node TreePolicy(Node node)
        {
            while (!IsLeaf(node))
            {
                if (node.children.Count == 0)
                {
                    ExpandNode(node);
                    return node;
                }
                // Select the child node with the highest UCB value
                node = BestChild(node, settings.explorationConstant);
            }
            ExpandNode(node);
            return node;
        }

        void ExpandNode(Node leaf)
        {
            var newLegalMoves = moveGenerator.GenerateMoves(leaf.board, true, true);
            foreach (var newMove in newLegalMoves)
            {
                var newBoard = leaf.board.Clone();
                newBoard.MakeMove(newMove);
                var child = new Node(newBoard, leaf);
                child.move = newMove;
                leaf.children.Add(child);
            }
        }

        //void ExpandNode(Node leaf)
        //{
        //    var newLegalMoves = moveGenerator.GenerateMoves(leaf.board, false, true);
        //    foreach (var newMove in newLegalMoves)
        //    {
        //        var newBoard = leaf.board.GetLightweightClone();
        //        SimMove simMove = new SimMove(newMove.StartSquare / 8, newMove.StartSquare % 8, newMove.TargetSquare / 8, newMove.TargetSquare % 8);
        //        SimPiece piece = newBoard[simMove.startCoord1, simMove.startCoord2];
        //        newBoard[simMove.endCoord1, simMove.endCoord2] = newBoard[simMove.startCoord1, simMove.startCoord2];
        //        newBoard[simMove.startCoord1, simMove.startCoord2] = null;
        //        var childBoard = leaf.board.Clone();
        //        var child = new Node(childBoard, leaf);
        //        child.move = newMove;
        //        leaf.children.Add(child);
        //    }
        //}

        //Node BestChild(Node node, double explorationConstant, bool whiteToMove)   2
        //{
        //    Node bestChild = null;
        //    double bestValue = double.MinValue;
        //    for (int i = 0; i < node.children.Count; i++)
        //    {
        //        Node child = node.children[i];
        //        if (!whiteToMove)
        //        {
        //            child.value *= -1;
        //        }
        //        double ucb = child.value + explorationConstant * System.Math.Sqrt(System.Math.Log(node.visits) / child.visits);
        //        if (ucb > bestValue)
        //        {
        //            bestValue = ucb;
        //            bestChild = child;
        //        }
        //    }
        //    return bestChild;
        //}

        Node BestChild(Node node, double explorationConstant)    
        {
            Node bestChild = null;
            double bestValue = double.MinValue;
            for (int i = 0; i<node.children.Count; i++)
            {
                Node child = node.children[i];
                if (child.visits == 0)
                {
                    return child;
                }

                double ucbValue = child.value / (double)child.visits + explorationConstant * Math.Sqrt(Math.Log(node.visits) / (double)child.visits);

                if (ucbValue > bestValue)
                {
                    bestChild = child;
                    bestValue = ucbValue;
                }
            }
            return bestChild;
        }

        //int[] DefaultPolicy(Board board)                            1
        //{
        //    int[] score = new int[2];
        //    // Create a copy of the current board state
        //    SimPiece[,] simBoard = board.GetLightweightClone();
        //    MoveGenerator moveGenerator = new MoveGenerator();
        //    Evaluation evaluation = new Evaluation();
        //    // While the game is not over
        //    while (!IsGameOver(simBoard))
        //    {
        //        // Generate a list of legal moves for the current player
        //        List<Move> legalMoves = moveGenerator.GenerateMoves(board, true, true);
        //        // Select a random move from the list
        //        Move move = legalMoves[rand.Next(legalMoves.Count)];
        //        // Apply the move to the board
        //        board.MakeMove(move);
        //        // Generate a list of possible moves for the opponent
        //        List<SimMove> simMoves = moveGenerator.GetPossibleSimMoves(simBoard, board.WhiteToMove);
        //        // Select a random move from the list
        //        SimMove simMove = simMoves[rand.Next(simMoves.Count)];
        //        // Apply the move to the board
        //        simBoard[simMove.endCoord1, simMove.endCoord2] = simBoard[simMove.startCoord1, simMove.startCoord2];
        //        simBoard[simMove.startCoord1, simMove.startCoord2] = new SimPiece(!board.WhiteToMove, SimPieceType.None);
        //    }
        //    // Evaluate the final state of the board
        //    float tempScore = evaluation.EvaluateSimBoard(simBoard, board.WhiteToMove);
        //    score[0] = (int)tempScore;
        //    return score;
        //}

        //    Node BestChild(Node node, double explorationConstant, bool whiteToMove)    2
        //    {
        //        Node bestChild = null;
        //    double bestValue = double.MinValue;
        //        for (int i = 0; i<node.children.Count; i++)
        //        {
        //            Node child = node.children[i];
        //    double value = child.value;
        //            if (!whiteToMove)
        //            {
        //                // Change the value to the opposite for black team
        //                value *= -1;
        //            }
        //double ucb = value + explorationConstant * System.Math.Sqrt(System.Math.Log(node.visits) / child.visits);
        //            if (ucb > bestValue)
        //            {
        //                bestValue = ucb;
        //                bestChild = child;
        //            }
        //        }
        //        return bestChild;
        //    }


        //int[] DefaultPolicy(Board board)           2
        //{
        //    int[] score = new int[2];
        //    // Create a copy of the current board state
        //    SimPiece[,] simBoard = board.GetLightweightClone();
        //    MoveGenerator moveGenerator = new MoveGenerator();
        //    Evaluation evaluation = new Evaluation();
        //    // While the game is not over
        //    while (!IsGameOver(simBoard))
        //    {
        //        // Generate a list of legal moves for the current player
        //        List<Move> legalMoves = moveGenerator.GenerateMoves(board, true, true);
        //        // If there are no legal moves, the game is over
        //        if (legalMoves.Count == 0) break;
        //        // Select a random move from the list
        //        Move move = legalMoves[rand.Next(legalMoves.Count)];
        //        // Apply the move to the board
        //        board.MakeMove(move);
        //        // Generate a list of possible moves for the opponent
        //        List<SimMove> simMoves = moveGenerator.GetPossibleSimMoves(simBoard, board.WhiteToMove);
        //        // If there are no legal moves, the game is over
        //        if (simMoves.Count == 0) break;
        //        // Select a random move from the list
        //        SimMove simMove = simMoves[rand.Next(simMoves.Count)];
        //        // Apply the move to the board
        //        simBoard[simMove.endCoord1, simMove.endCoord2] = simBoard[simMove.startCoord1, simMove.startCoord2];
        //        simBoard[simMove.startCoord1, simMove.startCoord2] = new SimPiece(!board.WhiteToMove, SimPieceType.None);
        //    }
        //    // Evaluate the final state of the board
        //    float tempScore = evaluation.EvaluateSimBoard(simBoard, board.WhiteToMove);
        //    score[0] = (int)tempScore;
        //    return score;
        //}

        //Node BestChild(Node node, double explorationConstant, bool whiteToMove)   3
        //{
        //    Node bestChild = null;
        //    double bestValue = double.NegativeInfinity;
        //    for (int i = 0; i < node.children.Count; i++)
        //    {
        //        Node child = node.children[i];
        //        double value = child.value / child.visits + explorationConstant * Math.Sqrt(Math.Log(node.visits) / child.visits);
        //        if (whiteToMove)
        //        {
        //            if (value > bestValue)
        //            {
        //                bestChild = child;
        //                bestValue = value;
        //            }
        //        }
        //        else
        //        {
        //            if (-value > bestValue)
        //            {
        //                bestChild = child;
        //                bestValue = -value;
        //            }
        //        }
        //    }
        //    return bestChild;
        //}

        int[] DefaultPolicy(Board board)
        {
            int[] score = new int[2];
            // Create a copy of the current board state
            SimPiece[,] simBoard = board.GetLightweightClone();
            MoveGenerator moveGenerator = new MoveGenerator();
            int playoutDepth = 0;
            // While the game is not over
            while (!IsGameOver(simBoard) && playoutDepth<settings.playoutDepthLimit)
            {
                // Generate a list of legal moves for the current player
                List<SimMove> legalMoves = moveGenerator.GetPossibleSimMoves(simBoard, board.WhiteToMove);
                // If there are no legal moves, the game is over
                if (legalMoves.Count == 0) break;
                // Select a random move from the list
                SimMove move = legalMoves[rand.Next(legalMoves.Count)];
                // Apply the move to the simulated board
                SimPiece piece = simBoard[move.startCoord1, move.startCoord2];
                simBoard[move.endCoord1, move.endCoord2] = simBoard[move.startCoord1, move.startCoord2];
                simBoard[move.startCoord1, move.startCoord2] = null;
                playoutDepth++;
            }
            // Evaluate the final state of the board for both players' perspectives
            score[0] = (int)evaluation.EvaluateSimBoard(simBoard, true);
            score[1] = (int)evaluation.EvaluateSimBoard(simBoard, false);
            return score;
        }

        bool IsGameOver(SimPiece[,] simBoard)
        {
            bool whiteKingExist = false;
            bool blackKingExist = false;
            // check if both kings are still present on the board
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (simBoard[i, j] != null)
                    {
                        if (simBoard[i, j].type == SimPieceType.King && simBoard[i, j].team == true)
                            whiteKingExist = true;
                        if (simBoard[i, j].type == SimPieceType.King && simBoard[i, j].team == false)
                            blackKingExist = true;
                    }
                }
            }
            if (!whiteKingExist || !blackKingExist)
                return true;
            return false;
        }

        void LogDebugInfo()
		{
			// Optional
		}

		void InitDebugInfo()
		{
			searchStopwatch = System.Diagnostics.Stopwatch.StartNew();
			// Optional
		}
	}

    class Node
    {
        public Node parent;
        public List<Node> children;
        public Move move;
        public int visits;
        public int value;
        public Board board;

        public Node(Board board, Node parent)
        {
            this.board = board;
            this.parent = parent;
            this.children = new List<Node>();
            this.move = Move.InvalidMove;
            this.visits = 0;
            this.value = 0;
        }
    }
}
