import { PieceType, PieceColor } from './types';
import type { Piece, Position, AppMode } from './types';

// ---------------------------------------------------------------------------
// Short and full Russian names for each piece type
// ---------------------------------------------------------------------------

export const PIECE_SHORT_NAMES: Record<PieceType, string> = {
  [PieceType.King]: 'Кр',
  [PieceType.Konnet]: 'Кт',
  [PieceType.Prince]: 'Пр',
  [PieceType.Ritter]: 'Рт',
  [PieceType.Knekht]: 'Кн',
  [PieceType.VerKnekht]: 'ВК',
  [PieceType.Razvedchik]: 'Рк',
};

export const PIECE_FULL_NAMES: Record<PieceType, string> = {
  [PieceType.King]: 'Король',
  [PieceType.Konnet]: 'Коннет',
  [PieceType.Prince]: 'Принц',
  [PieceType.Ritter]: 'Риттер',
  [PieceType.Knekht]: 'Кнехт',
  [PieceType.VerKnekht]: 'Верховный Кнехт',
  [PieceType.Razvedchik]: 'Разведчик',
};

// ---------------------------------------------------------------------------
// Castle zones
// ---------------------------------------------------------------------------

export const WHITE_CASTLE = new Set(['c1', 'd1', 'e1', 'f1']);
export const BLACK_CASTLE = new Set(['c8', 'd8', 'e8', 'f8']);
export const ALL_CASTLE = new Set([...WHITE_CASTLE, ...BLACK_CASTLE]);

// ---------------------------------------------------------------------------
// Initial position
// ---------------------------------------------------------------------------

/**
 * Returns the starting position for a chess-T1 game.
 *
 * White (row 1): a1-Рт, b1-Рк, c1-Пр, d1-Кт, e1-Кр, f1-Пр, g1-Рк, h1-Рт
 * Black (row 8): mirrored.
 * White Knekht row 2 (a2..h2), Black Knekht row 7 (a7..h7).
 */
export function getInitialPosition(): Position {
  const pos: Position = {};

  // White back rank (row 1)
  const backRank: PieceType[] = [
    PieceType.Ritter,
    PieceType.Razvedchik,
    PieceType.Prince,
    PieceType.Konnet,
    PieceType.King,
    PieceType.Prince,
    PieceType.Razvedchik,
    PieceType.Ritter,
  ];

  const columns = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'];

  for (let i = 0; i < 8; i++) {
    // White back rank
    pos[`${columns[i]}1`] = { color: PieceColor.White, type: backRank[i] };
    // Black back rank
    pos[`${columns[i]}8`] = { color: PieceColor.Black, type: backRank[i] };
    // White Knekht row
    pos[`${columns[i]}2`] = { color: PieceColor.White, type: PieceType.Knekht };
    // Black Knekht row
    pos[`${columns[i]}7`] = { color: PieceColor.Black, type: PieceType.Knekht };
  }

  return pos;
}

// ---------------------------------------------------------------------------
// Coordinate helpers
// ---------------------------------------------------------------------------

/** Convert a cell name like "e4" to zero-based [col, row]. a1 = [0, 0]. */
export function cellToCoords(cell: string): [number, number] {
  const col = cell.charCodeAt(0) - 'a'.charCodeAt(0);
  const row = parseInt(cell[1], 10) - 1;
  return [col, row];
}

/** Convert zero-based [col, row] back to a cell name. [0, 0] = "a1". */
export function coordsToCell(col: number, row: number): string {
  return String.fromCharCode('a'.charCodeAt(0) + col) + (row + 1);
}

// ---------------------------------------------------------------------------
// Position cloning
// ---------------------------------------------------------------------------

export function copyPosition(pos: Position): Position {
  const copy: Position = {};
  for (const key in pos) {
    copy[key] = { ...pos[key] };
  }
  return copy;
}

// ---------------------------------------------------------------------------
// Promotion rules
// ---------------------------------------------------------------------------

/**
 * Knekht promotion: when a Knekht reaches the last rank it is automatically
 * promoted to VerKnekht. Returns the promoted piece or null if no promotion.
 */
export function checkKnechtPromotion(cell: string, piece: Piece): Piece | null {
  if (piece.type !== PieceType.Knekht) return null;

  const row = parseInt(cell[1], 10);
  if (piece.color === PieceColor.White && row === 8) {
    return { color: piece.color, type: PieceType.VerKnekht };
  }
  if (piece.color === PieceColor.Black && row === 1) {
    return { color: piece.color, type: PieceType.VerKnekht };
  }
  return null;
}

/**
 * VerKnekht promotion: when a VerKnekht reaches the last rank the player
 * chooses which piece type to promote to. Returns the list of allowed types,
 * or null if no promotion applies.
 */
export function checkVerKnechtPromotion(
  cell: string,
  piece: Piece,
): PieceType[] | null {
  if (piece.type !== PieceType.VerKnekht) return null;

  const row = parseInt(cell[1], 10);
  const lastRank = piece.color === PieceColor.White ? 8 : 1;
  if (row !== lastRank) return null;

  return [
    PieceType.Ritter,
    PieceType.Konnet,
    PieceType.Prince,
    PieceType.Razvedchik,
  ];
}

/**
 * Prince promotion: when a Prince reaches the last rank the player
 * chooses which piece type to promote to. Returns the list of allowed types,
 * or null if no promotion applies.
 */
export function checkPrincePromotion(
  cell: string,
  piece: Piece,
): PieceType[] | null {
  if (piece.type !== PieceType.Prince) return null;

  const row = parseInt(cell[1], 10);
  const lastRank = piece.color === PieceColor.White ? 8 : 1;
  if (row !== lastRank) return null;

  return [
    PieceType.Ritter,
    PieceType.Konnet,
    PieceType.Razvedchik,
  ];
}

// ---------------------------------------------------------------------------
// Razvedchik exchange
// ---------------------------------------------------------------------------

/**
 * A Razvedchik can swap places with an enemy piece that is inside the
 * opponent's castle zone (and that piece is not a King).
 */
export function isRazvedchikExchange(
  piece: Piece,
  targetCell: string,
  targetPiece: Piece | undefined,
): boolean {
  if (piece.type !== PieceType.Razvedchik) return false;
  if (!targetPiece) return false;
  if (targetPiece.color === piece.color) return false;
  if (targetPiece.type === PieceType.King) return false;

  // The target cell must be in the opponent's castle zone
  if (piece.color === PieceColor.White) {
    return BLACK_CASTLE.has(targetCell);
  }
  return WHITE_CASTLE.has(targetCell);
}

// ---------------------------------------------------------------------------
// UI helpers
// ---------------------------------------------------------------------------

/**
 * Returns the move indicator text shown on the board.
 *   White move:  "1. __ хб"
 *   Black move:  "1 … __ хч"
 */
export function getIndicatorText(
  moveNumber: number,
  whiteTurn: boolean,
): string {
  if (whiteTurn) {
    return `${moveNumber}. __ хб`;
  }
  return `${moveNumber} \u2026 __ хч`;
}

/**
 * Returns the screenshot filename for the current half-move.
 *   White move:  "001w"
 *   Black move:  "001b"
 */
export function getScreenshotName(
  moveNumber: number,
  whiteTurn: boolean,
): string {
  const num = String(moveNumber).padStart(3, '0');
  return `${num}${whiteTurn ? 'w' : 'b'}`;
}

// ---------------------------------------------------------------------------
// Turn recalculation from history index
// ---------------------------------------------------------------------------

/**
 * Given the current history index, whether the first move was White, and the
 * app mode, derive whose turn it is and the full-move number.
 *
 * White-first (analysisFirstWhite === true or mode === 'party'):
 *   whiteTurn  = idx % 2 === 0
 *   moveNumber = Math.floor(idx / 2) + 1
 *
 * Black-first (analysisFirstWhite === false, mode === 'analysis'):
 *   whiteTurn  = idx % 2 === 1
 *   moveNumber = Math.floor((idx + 1) / 2) + 1
 */
export function recalculateTurn(
  historyIndex: number,
  analysisFirstWhite: boolean,
  mode: AppMode,
): { whiteTurn: boolean; moveNumber: number } {
  const whiteFirst = mode === 'party' || analysisFirstWhite;

  if (whiteFirst) {
    return {
      whiteTurn: historyIndex % 2 === 0,
      moveNumber: Math.floor(historyIndex / 2) + 1,
    };
  }

  // Black moves first
  return {
    whiteTurn: historyIndex % 2 === 1,
    moveNumber: Math.floor((historyIndex + 1) / 2) + 1,
  };
}
