using System;
using System.Collections.Generic;

namespace JC.GameLib;

// Game status enum
public enum GameStatus : byte
{
    InProgress = 0,
    WhiteCheckmate = 1,  // White wins (black is checkmated)
    BlackCheckmate = 2,  // Black wins (white is checkmated)
    Stalemate = 3,       // Draw - no legal moves but not in check
    Draw = 4             // Other draw conditions (future use)
}

public class Game
{
    // Pieces constants as bytes
    public const byte NONE = 0,
        WHITE_KING = 1,
        WHITE_PAWN = 2,
        WHITE_BISHOP = 3,
        WHITE_KNIGHT = 4,
        WHITE_ROOK = 5,
        WHITE_QUEEN = 6,
        BLACK_KING = 7,
        BLACK_PAWN = 8,
        BLACK_BISHOP = 9,
        BLACK_KNIGHT = 10,
        BLACK_ROOK = 11,
        BLACK_QUEEN = 12;

    // board object
    public byte[] board;
    public List<MoveDto> moves;
    
    // Player tracking - 0 means unassigned
    public byte whitePlayerID = 0;
    public byte blackPlayerID = 0;
    
    // Game settings
    public bool allowUndo = false;
    public byte whiteVetoesPerTurn = 0;  // 0 = no veto power
    public byte blackVetoesPerTurn = 0;  // 0 = no veto power
    
    // VETO tracking - count of vetoes used this turn
    private byte whiteVetoesUsedThisTurn = 0;
    private byte blackVetoesUsedThisTurn = 0;
    
    // Game state tracking
    public bool whiteToMove = true;
    public GameStatus gameStatus = GameStatus.InProgress;
    private bool whiteKingMoved = false;
    private bool blackKingMoved = false;
    private bool whiteKingsideRookMoved = false;
    private bool whiteQueensideRookMoved = false;
    private bool blackKingsideRookMoved = false;
    private bool blackQueensideRookMoved = false;
    
    // For en passant - store the position of pawn that just moved two squares (255 = none)
    private byte enPassantTargetSquare = 255;
    
    // Undo history - stack of move details needed to reverse
    private Stack<MoveSnapshot> undoHistory = new();

    // Helper methods for board position calculations
    private int GetRow(byte pos) => pos / 8;
    private int GetCol(byte pos) => pos % 8;
    
    // Helper method to check if a player is allowed to make a move
    public bool IsPlayersTurn(byte playerID)
    {
        if (whiteToMove && playerID == whitePlayerID) return true;
        if (!whiteToMove && playerID == blackPlayerID) return true;
        return false;
    }
    
    // Get remaining vetoes for a specific player (calculated dynamically)
    public byte GetVetoesRemaining(byte playerID)
    {
        if (playerID == whitePlayerID)
        {
            if (whiteVetoesPerTurn == 0) return 0;
            return (byte)(whiteVetoesPerTurn - whiteVetoesUsedThisTurn);
        }
        if (playerID == blackPlayerID)
        {
            if (blackVetoesPerTurn == 0) return 0;
            return (byte)(blackVetoesPerTurn - blackVetoesUsedThisTurn);
        }
        return 0;
    }
    
    // Get veto info for response
    public (byte whiteRemaining, byte blackRemaining) GetVetoInfo()
    {
        byte whiteRemaining = whiteVetoesPerTurn > 0 ? (byte)(whiteVetoesPerTurn - whiteVetoesUsedThisTurn) : (byte)0;
        byte blackRemaining = blackVetoesPerTurn > 0 ? (byte)(blackVetoesPerTurn - blackVetoesUsedThisTurn) : (byte)0;
        return (whiteRemaining, blackRemaining);
    }

    public bool IsValidMove(byte move_from, byte move_to, out bool isCastling, out bool isEnPassant, out byte capturedEnPassantSquare)
    {   
        // Track if this is a special move
        isCastling = false;
        isEnPassant = false;
        capturedEnPassantSquare = 0;

        // Check if game is already over
        if (IsGameOver())
            return false;

        // check if positions are valid
        // bytes are unsigned, so we don't have to worry about under zero.
        if (move_from > 63 || move_to > 63) return false;

        // check if the move is valid
        if (move_from == move_to) return false;

        if (TargetIsSelfKing(move_from, move_to)) return false;

        // Check if it's the correct player's turn
        byte movingPiece = board[move_from];
        if (whiteToMove && !IsWhitePiece(movingPiece)) return false;
        if (!whiteToMove && !IsBlackPiece(movingPiece)) return false;


        // what piece is moving?
        switch (movingPiece)
        {
            case NONE: return false; // no piece
            case WHITE_KING:
                if (!IsValidKingMove(move_from, move_to, out isCastling)) return false;
                break;
            case WHITE_PAWN:
                if (!IsValidPawnMove(move_from, move_to, out isEnPassant, out capturedEnPassantSquare)) return false;
                break;
            case WHITE_BISHOP:
                if (!IsValidBishopMove(move_from, move_to)) return false;
                break;
            case WHITE_KNIGHT:
                if (!IsValidKnightMove(move_from, move_to)) return false;
                break;
            case WHITE_ROOK:
                if (!IsValidRookMove(move_from, move_to)) return false;
                break;
            case WHITE_QUEEN:
                if (!IsValidQueenMove(move_from, move_to)) return false;
                break;
            case BLACK_KING:
                if (!IsValidKingMove(move_from, move_to, out isCastling)) return false;
                break;
            case BLACK_PAWN:
                if (!IsValidPawnMove(move_from, move_to, out isEnPassant, out capturedEnPassantSquare)) return false;
                break;
            case BLACK_BISHOP:
                if (!IsValidBishopMove(move_from, move_to)) return false;
                break;
            case BLACK_KNIGHT:
                if (!IsValidKnightMove(move_from, move_to)) return false;
                break;
            case BLACK_ROOK:
                if (!IsValidRookMove(move_from, move_to)) return false;
                break;
            case BLACK_QUEEN:
                if (!IsValidQueenMove(move_from, move_to)) return false;
                break;
            default: return false;
        }

        // Check if this move would leave/put own king in check
        if (WouldKingBeInCheck(move_from, move_to, isEnPassant, capturedEnPassantSquare)) return false;

        return true;
    }

    // game functions
    public bool Move(byte move_from, byte move_to, byte promotionPiece = NONE)
    {
        // Track if this is a special move
        bool isCastling = false;
        bool isEnPassant = false;
        byte capturedEnPassantSquare = 0;

        if (!IsValidMove(move_from, move_to, out isCastling, out isEnPassant, out capturedEnPassantSquare)) return false;

        byte movingPiece = board[move_from];

        // Save state before making the move (if undo or veto is enabled)
        if (allowUndo || whiteVetoesPerTurn > 0 || blackVetoesPerTurn > 0)
        {
            SaveMoveForUndo(move_from, move_to, isCastling, isEnPassant, capturedEnPassantSquare);
        }

        // Reset en passant target
        byte newEnPassantTarget = 255;

        // if the move is valid return true and make the move
        board[move_to] = board[move_from];
        board[move_from] = NONE;
        
        // Handle en passant capture
        if (isEnPassant)
        {
            board[capturedEnPassantSquare] = NONE;
        }
        
        // Handle castling - move the rook
        if (isCastling)
        {
            if (move_to == 6) // White kingside (e1->g1)
            {
                board[5] = board[7]; // Move rook from h1 to f1
                board[7] = NONE;
            }
            else if (move_to == 2) // White queenside (e1->c1)
            {
                board[3] = board[0]; // Move rook from a1 to d1
                board[0] = NONE;
            }
            else if (move_to == 62) // Black kingside (e8->g8)
            {
                board[61] = board[63]; // Move rook from h8 to f8
                board[63] = NONE;
            }
            else if (move_to == 58) // Black queenside (e8->c8)
            {
                board[59] = board[56]; // Move rook from a8 to d8
                board[56] = NONE;
            }
        }
        
        // Track if pawn moved two squares (for en passant next turn)
        if (movingPiece == WHITE_PAWN || movingPiece == BLACK_PAWN)
        {
            int fromRow = GetRow(move_from);
            int toRow = GetRow(move_to);
            if (Math.Abs(toRow - fromRow) == 2)
            {
                newEnPassantTarget = (byte)(move_from + (move_to - move_from) / 2);
            }
            
            // Handle pawn promotion
            if ((movingPiece == WHITE_PAWN && toRow == 7) || (movingPiece == BLACK_PAWN && toRow == 0))
            {
                // Default to queen if no promotion piece specified or invalid piece
                if (promotionPiece == NONE || 
                    (movingPiece == WHITE_PAWN && (promotionPiece < WHITE_BISHOP || promotionPiece > WHITE_QUEEN)) ||
                    (movingPiece == BLACK_PAWN && (promotionPiece < BLACK_BISHOP || promotionPiece > BLACK_QUEEN)))
                {
                    board[move_to] = movingPiece == WHITE_PAWN ? WHITE_QUEEN : BLACK_QUEEN;
                }
                else
                {
                    board[move_to] = promotionPiece;
                }
            }
        }
        
        // Update castling rights
        if (movingPiece == WHITE_KING) whiteKingMoved = true;
        if (movingPiece == BLACK_KING) blackKingMoved = true;
        if (move_from == 0) whiteQueensideRookMoved = true; // a1
        if (move_from == 7) whiteKingsideRookMoved = true; // h1
        if (move_from == 56) blackQueensideRookMoved = true; // a8
        if (move_from == 63) blackKingsideRookMoved = true; // h8
        
        // Also update if rook was captured
        if (move_to == 0) whiteQueensideRookMoved = true;
        if (move_to == 7) whiteKingsideRookMoved = true;
        if (move_to == 56) blackQueensideRookMoved = true;
        if (move_to == 63) blackKingsideRookMoved = true;
        
        enPassantTargetSquare = newEnPassantTarget;
        
        // Add move to history
        moves.Add(new MoveDto() { from = move_from, to = move_to });
        
        // Switch turns and reset veto used count for the new turn
        whiteToMove = !whiteToMove;
        
        // Reset veto used count for the player whose turn it now is
        if (whiteToMove)
            whiteVetoesUsedThisTurn = 0;
        else
            blackVetoesUsedThisTurn = 0;
        
        // Check for checkmate or stalemate after the move
        gameStatus = CheckGameStatus();
        
        return true;
    }

    public bool Undo(byte playerID)
    {
        // Check if undo is allowed
        if (!allowUndo)
            return false;
        
        // Check if there are any moves to undo
        if (undoHistory.Count == 0 || moves.Count == 0)
            return false;
        
        // Regular undo - doesn't consume VETO power
        return PerformUndo();
    }
    
    public bool Veto(byte playerID)
    {
        // VETO is independent of allowUndo - it has its own permission system
        // Check if there are any moves to undo
        if (undoHistory.Count == 0 || moves.Count == 0)
            return false;
        
        // Check if it's the player's turn (they can only veto on their turn)
        if (!IsPlayersTurn(playerID))
            return false;
        
        // Check VETO count
        bool isWhitePlayer = playerID == whitePlayerID;
        byte vetoesPerTurn = isWhitePlayer ? whiteVetoesPerTurn : blackVetoesPerTurn;
        byte vetoesUsed = isWhitePlayer ? whiteVetoesUsedThisTurn : blackVetoesUsedThisTurn;
        
        // If veto system is not enabled for this player (vetoesPerTurn == 0), reject
        if (vetoesPerTurn == 0)
            return false;
        
        // If player has used all their vetoes, reject
        if (vetoesUsed >= vetoesPerTurn)
            return false;
        
        // Perform the undo
        bool success = PerformUndo();
        
        // Increment veto used count if undo was successful
        if (success)
        {
            if (isWhitePlayer)
                whiteVetoesUsedThisTurn++;
            else
                blackVetoesUsedThisTurn++;
        }
        
        return success;
    }
    
    private bool PerformUndo()
    {
        // Get the move snapshot
        var snapshot = undoHistory.Pop();
        
        // Remove the last move from history
        moves.RemoveAt(moves.Count - 1);
        
        // Reverse the move
        board[snapshot.From] = snapshot.MovingPiece;
        board[snapshot.To] = snapshot.CapturedPiece;
        
        // Handle special move reversals
        if (snapshot.WasCastling)
        {
            // Reverse castling - move the rook back
            if (snapshot.To == 6) // White kingside
            {
                board[7] = board[5]; // Move rook back from f1 to h1
                board[5] = NONE;
            }
            else if (snapshot.To == 2) // White queenside
            {
                board[0] = board[3]; // Move rook back from d1 to a1
                board[3] = NONE;
            }
            else if (snapshot.To == 62) // Black kingside
            {
                board[63] = board[61]; // Move rook back from f8 to h8
                board[61] = NONE;
            }
            else if (snapshot.To == 58) // Black queenside
            {
                board[56] = board[59]; // Move rook back from d8 to a8
                board[59] = NONE;
            }
        }
        
        if (snapshot.WasEnPassant)
        {
            // Restore the captured pawn
            board[snapshot.EnPassantCaptureSquare] = snapshot.CapturedPiece;
            board[snapshot.To] = NONE;
        }
        
        // Restore game state flags
        whiteToMove = snapshot.WhiteToMove;
        whiteKingMoved = snapshot.WhiteKingMoved;
        blackKingMoved = snapshot.BlackKingMoved;
        whiteKingsideRookMoved = snapshot.WhiteKingsideRookMoved;
        whiteQueensideRookMoved = snapshot.WhiteQueensideRookMoved;
        blackKingsideRookMoved = snapshot.BlackKingsideRookMoved;
        blackQueensideRookMoved = snapshot.BlackQueensideRookMoved;
        enPassantTargetSquare = snapshot.EnPassantTargetSquare;
        gameStatus = snapshot.GameStatus;
        
        return true;
    }
    
    public int GetUndoCount()
    {
        return undoHistory.Count;
    }
    
    private void SaveMoveForUndo(byte from, byte to, bool isCastling, bool isEnPassant, byte enPassantCaptureSquare)
    {
        // Only save if either undo or veto is enabled
        if (!allowUndo && whiteVetoesPerTurn == 0 && blackVetoesPerTurn == 0)
            return;
            
        var snapshot = new MoveSnapshot
        {
            From = from,
            To = to,
            MovingPiece = board[from],
            CapturedPiece = board[to],
            WasCastling = isCastling,
            WasEnPassant = isEnPassant,
            EnPassantCaptureSquare = isEnPassant ? enPassantCaptureSquare : (byte)0,
            WhiteToMove = whiteToMove,
            WhiteKingMoved = whiteKingMoved,
            BlackKingMoved = blackKingMoved,
            WhiteKingsideRookMoved = whiteKingsideRookMoved,
            WhiteQueensideRookMoved = whiteQueensideRookMoved,
            BlackKingsideRookMoved = blackKingsideRookMoved,
            BlackQueensideRookMoved = blackQueensideRookMoved,
            EnPassantTargetSquare = enPassantTargetSquare,
            GameStatus = gameStatus
        };
        
        undoHistory.Push(snapshot);
    }

    private bool IsWhitePiece(byte piece)
    {
        return piece != 0 && piece >= WHITE_KING && piece <= WHITE_QUEEN;
    }
    
    private bool IsBlackPiece(byte piece)
    {
        return piece >= BLACK_KING && piece <= BLACK_QUEEN;
    }

    private bool IsValidKnightMove(byte move_from, byte move_to)
    {

        int fromRow = GetRow(move_from);
        int fromCol = GetCol(move_from);
        int toRow = GetRow(move_to);
        int toCol = GetCol(move_to);

        int rowDiff = Math.Abs(toRow - fromRow);
        int colDiff = Math.Abs(toCol - fromCol);

        // Knight moves in L-shape: 2 squares in one direction, 1 in perpendicular, and a new move, 3 in any direction.
        return (rowDiff == 2 && colDiff == 1) || (rowDiff == 1 && colDiff == 2) || (rowDiff == 3 && colDiff == 0) || (colDiff == 3 && rowDiff == 0);
    }

    private bool IsValidKingMove(byte move_from, byte move_to, out bool isCastling)
    {
        isCastling = false;
        int fromRow = GetRow(move_from);
        int fromCol = GetCol(move_from);
        int toRow = GetRow(move_to);
        int toCol = GetCol(move_to);

        int rowDiff = Math.Abs(toRow - fromRow);
        int colDiff = Math.Abs(toCol - fromCol);

        // Normal king move - one square in any direction
        if (rowDiff <= 1 && colDiff <= 1)
            return true;
            
        // Check for castling
        if (rowDiff == 0 && colDiff == 2)
        {
            bool isWhite = IsWhitePiece(board[move_from]);
            
            // Can't castle if king has moved
            if (isWhite && whiteKingMoved) return false;
            if (!isWhite && blackKingMoved) return false;
            
            // Can't castle if in check
            if (IsSquareUnderAttack((byte)move_from, !isWhite)) return false;
            
            // Kingside castling (moving 2 squares right)
            if (toCol == 6)
            {
                if (isWhite)
                {
                    // White kingside: check rook hasn't moved and path is clear
                    if (whiteKingsideRookMoved) return false;
                    if (board[7] != WHITE_ROOK) return false;
                    if (board[5] != NONE || board[6] != NONE) return false;
                    if (IsSquareUnderAttack(5, true) || IsSquareUnderAttack(6, true)) return false;
                }
                else
                {
                    // Black kingside: check rook hasn't moved and path is clear
                    if (blackKingsideRookMoved) return false;
                    if (board[63] != BLACK_ROOK) return false;
                    if (board[61] != NONE || board[62] != NONE) return false;
                    if (IsSquareUnderAttack(61, false) || IsSquareUnderAttack(62, false)) return false;
                }
                isCastling = true;
                return true;
            }
            // Queenside castling (moving 2 squares left)
            else if (toCol == 2)
            {
                if (isWhite)
                {
                    // White queenside: check rook hasn't moved and path is clear
                    if (whiteQueensideRookMoved) return false;
                    if (board[0] != WHITE_ROOK) return false;
                    if (board[1] != NONE || board[2] != NONE || board[3] != NONE) return false;
                    if (IsSquareUnderAttack(2, true) || IsSquareUnderAttack(3, true)) return false;
                }
                else
                {
                    // Black queenside: check rook hasn't moved and path is clear
                    if (blackQueensideRookMoved) return false;
                    if (board[56] != BLACK_ROOK) return false;
                    if (board[57] != NONE || board[58] != NONE || board[59] != NONE) return false;
                    if (IsSquareUnderAttack(58, false) || IsSquareUnderAttack(59, false)) return false;
                }
                isCastling = true;
                return true;
            }
        }

        return false;
    }

    private bool IsValidPawnMove(byte move_from, byte move_to, out bool isEnPassant, out byte capturedSquare)
    {
        isEnPassant = false;
        capturedSquare = 0;

        int fromRow = GetRow(move_from);
        int fromCol = GetCol(move_from);
        int toRow = GetRow(move_to);
        int toCol = GetCol(move_to);

        int direction = IsWhitePiece(board[move_from]) ? 1 : -1; // White moves up (increasing row), black moves down
      
        int rowDiff = toRow - fromRow;
        int colDiff = Math.Abs(toCol - fromCol);

        // Moving forward one square
        if (rowDiff == direction && colDiff == 0)
            return board[move_to] == NONE;

        // Moving forward two squares - can jump over ONE friendly piece
        if (rowDiff == 2 * direction && colDiff == 0)
        {
            int middleSquare = move_from + direction * 8;
            byte middlePiece = board[middleSquare];
            
            // Destination must be empty
            if (board[move_to] != NONE) return false;
            
            // Middle square can be empty OR have a friendly piece (jump rule)
            if (middlePiece == NONE) return true;
            
            // Check if middle piece is friendly (can jump it)
            bool movingPieceIsWhite = IsWhitePiece(board[move_from]);
            bool middlePieceIsWhite = IsWhitePiece(middlePiece);
            return movingPieceIsWhite == middlePieceIsWhite;
        }

        // Diagonal capture (also handles pawn attack logic)
        if (rowDiff == direction && colDiff == 1)
        {
            // Regular capture (can capture enemy OR friendly pieces)
            if (board[move_to] != NONE)
                return true;
            
            // En passant capture
            if (move_to == enPassantTargetSquare)
            {
                capturedSquare = (byte)(move_to - direction * 8);
                isEnPassant = true;
                return true;
            }
        }

        return false;
    }

    private bool IsValidRookMove(byte move_from, byte move_to)
    {

        int fromRow = GetRow(move_from);
        int fromCol = GetCol(move_from);
        int toRow = GetRow(move_to);
        int toCol = GetCol(move_to);

        // Rook moves horizontally or vertically
        if (fromRow != toRow && fromCol != toCol)
            return false;

        // Check if path is clear
        return IsPathClear(move_from, move_to);
    }

    private bool IsValidBishopMove(byte move_from, byte move_to)
    {

        int fromRow = GetRow(move_from);
        int fromCol = GetCol(move_from);
        int toRow = GetRow(move_to);
        int toCol = GetCol(move_to);

        int rowDiff = Math.Abs(toRow - fromRow);
        int colDiff = Math.Abs(toCol - fromCol);

        // Bishop moves diagonally
        if (rowDiff != colDiff)
            return false;

        // Check if path is clear
        return IsPathClear(move_from, move_to);
    }

    private bool IsValidQueenMove(byte move_from, byte move_to)
    {
        int fromRow = GetRow(move_from);
        int fromCol = GetCol(move_from);
        int toRow = GetRow(move_to);
        int toCol = GetCol(move_to);

        int rowDiff = Math.Abs(toRow - fromRow);
        int colDiff = Math.Abs(toCol - fromCol);

        // Queen moves like a rook (horizontal/vertical) or bishop (diagonal)
        bool isRookMove = (fromRow == toRow || fromCol == toCol);
        bool isBishopMove = (rowDiff == colDiff);
        
        if (!isRookMove && !isBishopMove)
            return false;
            
        return IsPathClear(move_from, move_to);
    }

    private bool TargetIsSelfKing(byte move_from, byte move_to)
    {
        if (IsWhitePiece(board[move_from])) if (board[move_to] == WHITE_KING) return true;
        if (IsBlackPiece(board[move_from])) if (board[move_to] == BLACK_KING) return true;
        return false;
    }

    private bool IsPathClear(byte move_from, byte move_to)
    {
        int fromRow = GetRow(move_from);
        int fromCol = GetCol(move_from);
        int toRow = GetRow(move_to);
        int toCol = GetCol(move_to);

        int rowStep = toRow > fromRow ? 1 : (toRow < fromRow ? -1 : 0); 
        int colStep = toCol > fromCol ? 1 : (toCol < fromCol ? -1 : 0);

        // Calculate single step value: rowStep * 8 + colStep
        // Examples: up = 8, down = -8, right = 1, left = -1, 
        //           up-right = 9, down-left = -9, up-left = 7, down-right = -7
        int step = rowStep * 8 + colStep;

        int currentPos = move_from + step;
        int piecesInPath = 0;
        byte jumpedPiece = NONE;
        bool movingPieceIsWhite = IsWhitePiece(board[move_from]);

        while (currentPos != move_to)
        {
            if (board[currentPos] != NONE)
            {
                piecesInPath++;
                jumpedPiece = board[currentPos];
                
                // Can only jump over exactly one piece
                if (piecesInPath > 1)
                    return false;
                    
                // Can only jump over friendly pieces
                bool jumpedPieceIsWhite = IsWhitePiece(jumpedPiece);
                if (movingPieceIsWhite != jumpedPieceIsWhite)
                    return false;
            }

            currentPos += step;
        }

        return true;
    }
    
    // Check if making a move would put/leave own king in check
    private bool WouldKingBeInCheck(byte move_from, byte move_to, bool isEnPassant, byte enPassantCaptureSquare)
    {
        // Make the move temporarily
        byte originalPiece = board[move_from];
        byte capturedPiece = board[move_to];
        byte enPassantCaptured = isEnPassant ? board[enPassantCaptureSquare] : NONE;
        
        board[move_to] = board[move_from];
        board[move_from] = NONE;
        if (isEnPassant)
            board[enPassantCaptureSquare] = NONE;
        
        // Find king position
        byte kingPiece = IsWhitePiece(originalPiece) ? WHITE_KING : BLACK_KING;
        byte kingPos = 255;
        
        for (byte i = 0; i < 64; i++)
        {
            if (board[i] == kingPiece)
            {
                kingPos = i;
                break;
            }
        }
        
        // Check if king is under attack by OPPONENT
        // If originalPiece is white, we check if black is attacking (byWhite = false)
        // If originalPiece is black, we check if white is attacking (byWhite = true)
        bool inCheck = IsSquareUnderAttack(kingPos, !IsWhitePiece(originalPiece));
        
        // Undo the move
        board[move_from] = originalPiece;
        board[move_to] = capturedPiece;
        if (isEnPassant)
            board[enPassantCaptureSquare] = enPassantCaptured;
        
        return inCheck;
    }
    
    // Check if a square is under attack by opponent
    // byWhite = true means checking if white is attacking this square
    private bool IsSquareUnderAttack(byte square, bool byWhite)
    {
        // Check all opponent pieces to see if they can attack this square
        for (byte i = 0; i < 64; i++)
        {
            byte piece = board[i];
            if (piece == NONE) continue;
            
            // Check if this is an opponent piece
            if (byWhite && !IsWhitePiece(piece)) continue;
            if (!byWhite && !IsBlackPiece(piece)) continue;
            
            // Check if this piece can attack the square
            bool canAttack = piece switch
            {
                WHITE_PAWN or BLACK_PAWN => CanPawnAttackSquare(i, square, piece),
                WHITE_KNIGHT or BLACK_KNIGHT => IsValidKnightMove(i, square),
                WHITE_BISHOP or BLACK_BISHOP => IsValidBishopMove(i, square),
                WHITE_ROOK or BLACK_ROOK => IsValidRookMove(i, square),
                WHITE_QUEEN or BLACK_QUEEN => IsValidQueenMove(i, square),
                WHITE_KING or BLACK_KING => IsOneSquareAway(i, square),
                _ => false
            };
            
            if (canAttack) return true;
        }
        
        return false;
    }
    
    // Check if pawn at pawnPos can attack targetPos (used for check detection)
    private bool CanPawnAttackSquare(byte pawnPos, byte targetPos, byte pawnPiece)
    {
        int fromRow = GetRow(pawnPos);
        int fromCol = GetCol(pawnPos);
        int toRow = GetRow(targetPos);
        int toCol = GetCol(targetPos);
        
        int direction = IsWhitePiece(pawnPiece) ? 1 : -1;
        int rowDiff = toRow - fromRow;
        int colDiff = Math.Abs(toCol - fromCol);
        
        // Pawns attack diagonally one square
        return rowDiff == direction && colDiff == 1;
    }
    
    // Check if two squares are one square away (used for king attack detection)
    private bool IsOneSquareAway(byte pos1, byte pos2)
    {
        int fromRow = GetRow(pos1);
        int fromCol = GetCol(pos1);
        int toRow = GetRow(pos2);
        int toCol = GetCol(pos2);
        
        int rowDiff = Math.Abs(toRow - fromRow);
        int colDiff = Math.Abs(toCol - fromCol);
        
        return rowDiff <= 1 && colDiff <= 1;
    }
    
    // Check if the current player has any legal moves
    private bool HasLegalMoves(bool forWhite)
    {
        // Try every possible move for the current player
        for (byte from = 0; from < 64; from++)
        {
            byte piece = board[from];
            if (piece == NONE) continue;
            
            // Check if this is the correct color's piece
            if (forWhite && !IsWhitePiece(piece)) continue;
            if (!forWhite && !IsBlackPiece(piece)) continue;
            
            // Try all possible destination squares
            for (byte to = 0; to < 64; to++)
            {
                if (from == to) continue;
                
                // Check if this move would be valid
                if (IsLegalMove(from, to, piece))
                    return true;
            }
        }
        
        return false;
    }
    
    // Check if a move is legal (validates piece movement and check rules)
    private bool IsLegalMove(byte from, byte to, byte piece)
    {
        // Don't capture own king
        if (TargetIsSelfKing(from, to)) return false;
        
        bool isCastling = false;
        bool isEnPassant = false;
        byte capturedSquare = 0;
        
        // Validate piece-specific move
        bool validMove = piece switch
        {
            WHITE_KING or BLACK_KING => IsValidKingMove(from, to, out isCastling),
            WHITE_PAWN or BLACK_PAWN => IsValidPawnMove(from, to, out isEnPassant, out capturedSquare),
            WHITE_BISHOP or BLACK_BISHOP => IsValidBishopMove(from, to),
            WHITE_KNIGHT or BLACK_KNIGHT => IsValidKnightMove(from, to),
            WHITE_ROOK or BLACK_ROOK => IsValidRookMove(from, to),
            WHITE_QUEEN or BLACK_QUEEN => IsValidQueenMove(from, to),
            _ => false
        };
        
        if (!validMove) return false;
        
        // Check if this move would leave king in check
        return !WouldKingBeInCheck(from, to, isEnPassant, capturedSquare);
    }
    
    // Check if current position is checkmate or stalemate
    private GameStatus CheckGameStatus()
    {
        // Determine which player's turn it is
        bool currentPlayerIsWhite = whiteToMove;
        
        // Find the current player's king
        byte kingPiece = currentPlayerIsWhite ? WHITE_KING : BLACK_KING;
        byte kingPos = 255;
        
        for (byte i = 0; i < 64; i++)
        {
            if (board[i] == kingPiece)
            {
                kingPos = i;
                break;
            }
        }
        
        // Check if king is in check by OPPONENT
        // If current player is white, check if black is attacking (byWhite = false)
        // If current player is black, check if white is attacking (byWhite = true)
        bool inCheck = IsSquareUnderAttack(kingPos, !currentPlayerIsWhite);
        
        // Check if current player has any legal moves
        bool hasLegalMoves = HasLegalMoves(currentPlayerIsWhite);
        
        // Determine game status
        if (!hasLegalMoves)
        {
            if (inCheck)
            {
                // Checkmate - the OTHER player wins
                return currentPlayerIsWhite ? GameStatus.BlackCheckmate : GameStatus.WhiteCheckmate;
            }
            else
            {
                // Stalemate - draw
                return GameStatus.Stalemate;
            }
        }
        
        return GameStatus.InProgress;
    }
    
    // Public method to get current game status
    public GameStatus GetGameStatus()
    {
        return gameStatus;
    }
    
    // Public method to check if game is over
    public bool IsGameOver()
    {
        return gameStatus != GameStatus.InProgress;
    }

    // constructors
    // custom board
    public Game(byte[] setupboard, byte whitePlayer, byte blackPlayer, bool enableUndo = false, byte whiteVetoes = 0, byte blackVetoes = 0)
    {
        moves = new();
        board = new byte[64];
        for (int i = 0; i < 64; i++)
        {
            board[i] = setupboard[i];
        }
        whitePlayerID = whitePlayer;
        blackPlayerID = blackPlayer;
        allowUndo = enableUndo;
        whiteVetoesPerTurn = whiteVetoes;
        blackVetoesPerTurn = blackVetoes;
        whiteVetoesUsedThisTurn = 0;
        blackVetoesUsedThisTurn = 0;
    }

    // default board
    public Game(byte whitePlayer, byte blackPlayer, bool enableUndo = false, byte whiteVetoes = 0, byte blackVetoes = 0)
    {
        moves = new();
        whitePlayerID = whitePlayer;
        blackPlayerID = blackPlayer;
        allowUndo = enableUndo;
        whiteVetoesPerTurn = whiteVetoes;
        blackVetoesPerTurn = blackVetoes;
        whiteVetoesUsedThisTurn = 0;
        blackVetoesUsedThisTurn = 0;
        board = new byte[64]
        {
            WHITE_ROOK, WHITE_KNIGHT, WHITE_BISHOP, WHITE_QUEEN, WHITE_KING, WHITE_BISHOP, WHITE_KNIGHT, WHITE_ROOK,
            WHITE_PAWN, WHITE_PAWN, WHITE_PAWN, WHITE_PAWN, WHITE_PAWN, WHITE_PAWN, WHITE_PAWN, WHITE_PAWN,
            NONE, NONE, NONE, NONE, NONE, NONE, NONE, NONE,
            NONE, NONE, NONE, NONE, NONE, NONE, NONE, NONE,
            NONE, NONE, NONE, NONE, NONE, NONE, NONE, NONE,
            NONE, NONE, NONE, NONE, NONE, NONE, NONE, NONE, 
            BLACK_PAWN, BLACK_PAWN, BLACK_PAWN, BLACK_PAWN, BLACK_PAWN, BLACK_PAWN, BLACK_PAWN, BLACK_PAWN,
            BLACK_ROOK, BLACK_KNIGHT, BLACK_BISHOP, BLACK_QUEEN,BLACK_KING, BLACK_BISHOP, BLACK_KNIGHT, BLACK_ROOK
        };
    }
}

// Minimal snapshot of move data for undo functionality
public class MoveSnapshot
{
    public byte From { get; set; }
    public byte To { get; set; }
    public byte MovingPiece { get; set; }
    public byte CapturedPiece { get; set; }
    public bool WasCastling { get; set; }
    public bool WasEnPassant { get; set; }
    public byte EnPassantCaptureSquare { get; set; }
    public bool WhiteToMove { get; set; }
    public bool WhiteKingMoved { get; set; }
    public bool BlackKingMoved { get; set; }
    public bool WhiteKingsideRookMoved { get; set; }
    public bool WhiteQueensideRookMoved { get; set; }
    public bool BlackKingsideRookMoved { get; set; }
    public bool BlackQueensideRookMoved { get; set; }
    public byte EnPassantTargetSquare { get; set; }
    public GameStatus GameStatus { get; set; }
}
