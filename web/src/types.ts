export const PieceType = {
  King: 0,
  Konnet: 1,
  Prince: 2,
  Ritter: 3,
  Knekht: 4,
  VerKnekht: 5,
  Razvedchik: 6,
} as const;

export type PieceType = (typeof PieceType)[keyof typeof PieceType];

export const PieceColor = {
  White: 0,
  Black: 1,
} as const;

export type PieceColor = (typeof PieceColor)[keyof typeof PieceColor];

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
