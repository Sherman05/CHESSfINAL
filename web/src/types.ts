export enum PieceType {
  King,
  Konnet,
  Prince,
  Ritter,
  Knekht,
  VerKnekht,
  Razvedchik,
}

export enum PieceColor {
  White,
  Black,
}

export interface Piece {
  color: PieceColor;
  type: PieceType;
}

/** Maps cell names like "a1" to the piece occupying that cell. */
export type Position = Record<string, Piece>;

export type AppMode = null | 'party' | 'analysis';
export type AppStage = 'startup' | 'game' | 'setup_position';

export interface GameState {
  mode: AppMode;
  stage: AppStage;
  sessionActive: boolean;
  whiteTurn: boolean;
  moveNumber: number;
  moveHistory: Position[];
  historyIndex: number;
  partyFolder: string | null;
  boardReversed: boolean;
  analysisFirstWhite: boolean;
  promotionPending: boolean;
  lastMoveCell: string | null;
  alwaysOnTop: boolean;
}
