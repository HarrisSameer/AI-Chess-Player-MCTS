namespace Chess
{
	public class PieceList
	{

		/// <summary>
		/// Indices of squares occupied by given piece type (only elements up to Count are valid, the rest are unused/garbage)
		/// </summary>
		public int[] occupiedSquares;
		/// <summary>
		/// Map to go from index of a square, to the index in the occupiedSquares array where that square is stored
		/// </summary>
		int[] map;
		int numPieces;

		public PieceList(int maxPieceCount = 16)
		{
			occupiedSquares = new int[maxPieceCount];
			map = new int[64];
			numPieces = 0;
		}

		public int Count
		{
			get
			{
				return numPieces;
			}
		}

        public void AddPieceAtSquare(int square)
        {
            occupiedSquares[numPieces] = square;
            map[square] = numPieces;
            numPieces++;
        }

        public void RemovePieceAtSquare(int square)
        {
            int pieceIndex = map[square]; // get the index of this element in the occupiedSquares array
            occupiedSquares[pieceIndex] = occupiedSquares[numPieces - 1]; // move last element in array to the place of the removed element
            map[occupiedSquares[pieceIndex]] = pieceIndex; // update map to point to the moved element's new location in the array
            numPieces--;
        }

        public void MovePiece(int startSquare, int targetSquare)
        {
            int pieceIndex = map[startSquare]; // get the index of this element in the occupiedSquares array

            if (pieceIndex >= occupiedSquares.Length)
            {
                int x = 5;
                x -= 2;
            }

            occupiedSquares[pieceIndex] = targetSquare;
            map[targetSquare] = pieceIndex;
        }

        //public void AddPieceAtSquare(int square)
        //{
        //    if (numPieces >= occupiedSquares.Length)
        //    {
        //        return;
        //    }
        //    occupiedSquares[numPieces] = square;
        //    map[square] = numPieces;
        //    numPieces++;
        //}

        //public void RemovePieceAtSquare(int square)
        //{
        //    int pieceIndex = map[square]; // get the index of this element in the occupiedSquares array
        //    if (pieceIndex >= numPieces)
        //    {
        //        return;
        //    }
        //    // Check if the piece to be removed is not the last element
        //    if (pieceIndex != numPieces - 1)
        //    {
        //        // Move the last element in the array to the place of the removed element
        //        occupiedSquares[pieceIndex] = occupiedSquares[numPieces - 1];
        //        // Update the map to point to the moved element's new location in the array
        //        map[occupiedSquares[pieceIndex]] = pieceIndex;
        //    }
        //    numPieces--;
        //}

        //public void MovePiece(int startSquare, int targetSquare)
        //{
        //    RemovePieceAtSquare(startSquare);
        //    AddPieceAtSquare(targetSquare);
        //}

        public int this[int index] => occupiedSquares[index];

	}
}