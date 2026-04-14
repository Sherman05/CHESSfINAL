import React, { useRef, useEffect, useState, useCallback } from 'react';
import type {
  Piece,
  Position,
  AppStage,
  AppMode,
} from '../types';
import {
  PieceColor,
  PieceType,
} from '../types';
import {
  PIECE_SHORT_NAMES,
  ALL_CASTLE,
  cellToCoords,
  coordsToCell,
  checkKnechtPromotion,
  checkVerKnechtPromotion,
  checkPrincePromotion,
  isRazvedchikExchange,
  copyPosition,
} from '../gameLogic';
import './Board.css';

/* ------------------------------------------------------------------ */
/*  Types                                                              */
/* ------------------------------------------------------------------ */

export interface BoardProps {
  position: Position;
  reversed: boolean;
  lastMoveCell: string | null;
  stage: AppStage;
  mode: AppMode;
  whiteTurn: boolean;
  deletionMode: boolean;
  selectedForDeletion: string | null;
  onMoveMade: (from: string, to: string) => void;
  onPieceExited: () => void;
  onPromotionNeeded: (cell: string, piece: Piece, options: PieceType[]) => void;
  onRazvedchikExchange: (from: string, to: string) => void;
  onCellClickForDeletion: (cell: string) => void;
  onPiecePlacedFromTray?: (cell: string, piece: Piece) => void;
}

/* ------------------------------------------------------------------ */
/*  Constants                                                          */
/* ------------------------------------------------------------------ */

const BORDER_COLOR = '#4A90C8';
const NOTATION_COLOR = '#1B3A5C';
const CELL_WHITE = '#FFFFFF';
const CELL_BLACK = '#808080';
const CELL_CASTLE = '#C8C8C8';
const HIGHLIGHT_START = 'rgba(255,255,0,0.4)';
const HIGHLIGHT_HOVER = 'rgba(0,255,0,0.3)';
const HIGHLIGHT_LAST_MOVE = 'rgba(206,210,107,0.5)';

const FILES = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'];
const RANKS = ['1', '2', '3', '4', '5', '6', '7', '8'];

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */

/** Visual col/row on screen (0-based), accounting for board reversal. */
function viewCol(col: number, reversed: boolean): number {
  return reversed ? 7 - col : col;
}
function viewRow(row: number, reversed: boolean): number {
  return reversed ? row : 7 - row;
}

/** Map a pixel position to the board cell, or null if outside. */
function pixelToCell(
  px: number,
  py: number,
  offsetX: number,
  offsetY: number,
  cellSize: number,
  reversed: boolean,
): string | null {
  const bx = px - offsetX;
  const by = py - offsetY;
  if (bx < 0 || by < 0 || bx >= cellSize * 8 || by >= cellSize * 8) {
    return null;
  }
  const vc = Math.floor(bx / cellSize);
  const vr = Math.floor(by / cellSize);
  const col = reversed ? 7 - vc : vc;
  const row = reversed ? vr : 7 - vr;
  return coordsToCell(col, row);
}

/* ------------------------------------------------------------------ */
/*  Component                                                          */
/* ------------------------------------------------------------------ */

const Board: React.FC<BoardProps> = (props) => {
  const {
    position,
    reversed,
    lastMoveCell,
    stage,
    mode,
    whiteTurn,
    deletionMode,
    selectedForDeletion,
    onMoveMade,
    onPieceExited,
    onPromotionNeeded,
    onRazvedchikExchange,
    onCellClickForDeletion,
    // onPiecePlacedFromTray -- reserved for tray integration
  } = props;

  const canvasRef = useRef<HTMLCanvasElement>(null);
  const wrapperRef = useRef<HTMLDivElement>(null);

  /* ---- Drag state (refs to avoid re-renders during drag) ---- */
  const dragPieceRef = useRef<Piece | null>(null);
  const dragFromCellRef = useRef<string | null>(null);
  const dragPosRef = useRef<{ x: number; y: number } | null>(null);
  const dragOffsetRef = useRef<{ dx: number; dy: number }>({ dx: 0, dy: 0 });
  const hoverCellRef = useRef<string | null>(null);
  const isDraggingRef = useRef(false);

  /* ---- Layout state ---- */
  const [canvasSize, setCanvasSize] = useState<{ w: number; h: number }>({
    w: 600,
    h: 600,
  });

  const cellSize = Math.floor((Math.min(canvasSize.w, canvasSize.h) - 100) / 8);
  const boardPx = cellSize * 8;
  const offsetX = Math.floor((canvasSize.w - boardPx) / 2);
  const offsetY = Math.floor((canvasSize.h - boardPx) / 2);

  /* ---- Cursor class ---- */
  const [cursorClass, setCursorClass] = useState('cursor-default');

  /* ================================================================ */
  /*  Resize observer                                                  */
  /* ================================================================ */
  useEffect(() => {
    const wrapper = wrapperRef.current;
    if (!wrapper) return;

    const measure = () => {
      const rect = wrapper.getBoundingClientRect();
      setCanvasSize({ w: Math.floor(rect.width), h: Math.floor(rect.height) });
    };
    measure();

    const ro = new ResizeObserver(measure);
    ro.observe(wrapper);
    return () => ro.disconnect();
  }, []);

  /* ================================================================ */
  /*  Draw                                                             */
  /* ================================================================ */
  const draw = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const dpr = window.devicePixelRatio || 1;
    canvas.width = canvasSize.w * dpr;
    canvas.height = canvasSize.h * dpr;
    canvas.style.width = `${canvasSize.w}px`;
    canvas.style.height = `${canvasSize.h}px`;
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

    /* clear */
    ctx.clearRect(0, 0, canvasSize.w, canvasSize.h);

    /* ---- Board border ---- */
    ctx.strokeStyle = BORDER_COLOR;
    ctx.lineWidth = 3;
    ctx.strokeRect(offsetX - 2, offsetY - 2, boardPx + 4, boardPx + 4);

    /* ---- Cells ---- */
    for (let vr = 0; vr < 8; vr++) {
      for (let vc = 0; vc < 8; vc++) {
        const col = reversed ? 7 - vc : vc;
        const row = reversed ? vr : 7 - vr;
        const cell = coordsToCell(col, row);

        const x = offsetX + vc * cellSize;
        const y = offsetY + vr * cellSize;

        /* base color */
        let bgColor: string;
        if (ALL_CASTLE.has(cell)) {
          bgColor = CELL_CASTLE;
        } else {
          bgColor = (col + row) % 2 === 0 ? CELL_BLACK : CELL_WHITE;
        }
        ctx.fillStyle = bgColor;
        ctx.fillRect(x, y, cellSize, cellSize);

        /* castle hatching */
        if (ALL_CASTLE.has(cell)) {
          ctx.save();
          ctx.beginPath();
          ctx.rect(x, y, cellSize, cellSize);
          ctx.clip();
          ctx.strokeStyle = '#999999';
          ctx.lineWidth = 1;
          const step = 8;
          for (let d = -cellSize; d < cellSize * 2; d += step) {
            ctx.beginPath();
            ctx.moveTo(x + d, y);
            ctx.lineTo(x + d + cellSize, y + cellSize);
            ctx.stroke();
          }
          ctx.restore();
        }

        /* last-move highlight */
        if (lastMoveCell === cell) {
          ctx.fillStyle = HIGHLIGHT_LAST_MOVE;
          ctx.fillRect(x, y, cellSize, cellSize);
        }

        /* start-cell highlight (drag origin) */
        if (isDraggingRef.current && dragFromCellRef.current === cell) {
          ctx.fillStyle = HIGHLIGHT_START;
          ctx.fillRect(x, y, cellSize, cellSize);
        }

        /* hover highlight */
        if (isDraggingRef.current && hoverCellRef.current === cell && cell !== dragFromCellRef.current) {
          ctx.fillStyle = HIGHLIGHT_HOVER;
          ctx.fillRect(x, y, cellSize, cellSize);
        }

        /* deletion selection highlight */
        if (deletionMode && selectedForDeletion === cell) {
          ctx.fillStyle = 'rgba(255,0,0,0.35)';
          ctx.fillRect(x, y, cellSize, cellSize);
        }
      }
    }

    /* ---- Pieces ---- */
    const fontSize = Math.max(10, Math.floor(cellSize * 0.32));
    ctx.font = `bold ${fontSize}px sans-serif`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';

    for (const cell of Object.keys(position)) {
      /* Skip the piece being dragged (it's drawn under the cursor) */
      if (isDraggingRef.current && dragFromCellRef.current === cell) continue;

      const piece = position[cell];
      const [col, row] = cellToCoords(cell);
      const vc = viewCol(col, reversed);
      const vr = viewRow(row, reversed);
      const cx = offsetX + vc * cellSize + cellSize / 2;
      const cy = offsetY + vr * cellSize + cellSize / 2;
      drawPiece(ctx, piece, cx, cy, cellSize);
    }

    /* ---- Dragged piece ---- */
    if (isDraggingRef.current && dragPieceRef.current && dragPosRef.current) {
      const dp = dragPosRef.current;
      const doff = dragOffsetRef.current;
      drawPiece(
        ctx,
        dragPieceRef.current,
        dp.x + doff.dx,
        dp.y + doff.dy,
        cellSize,
      );
    }

    /* ---- Notation ---- */
    const noteFont = Math.max(9, Math.floor(cellSize * 0.22));
    ctx.font = `bold ${noteFont}px sans-serif`;
    ctx.fillStyle = NOTATION_COLOR;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';

    for (let i = 0; i < 8; i++) {
      const fileLabel = reversed ? FILES[7 - i] : FILES[i];
      const rankLabel = reversed ? RANKS[i] : RANKS[7 - i];

      /* files (bottom) */
      ctx.fillText(fileLabel, offsetX + i * cellSize + cellSize / 2, offsetY + boardPx + 18);
      /* files (top) */
      ctx.fillText(fileLabel, offsetX + i * cellSize + cellSize / 2, offsetY - 18);
      /* ranks (left) */
      ctx.fillText(rankLabel, offsetX - 18, offsetY + i * cellSize + cellSize / 2);
      /* ranks (right) */
      ctx.fillText(rankLabel, offsetX + boardPx + 18, offsetY + i * cellSize + cellSize / 2);
    }
  }, [
    canvasSize,
    cellSize,
    boardPx,
    offsetX,
    offsetY,
    position,
    reversed,
    lastMoveCell,
    deletionMode,
    selectedForDeletion,
  ]);

  /* Redraw on every relevant change and on animation frames during drag */
  useEffect(() => {
    draw();
  }, [draw]);

  /* ================================================================ */
  /*  Draw a single piece (styled circle with abbreviation)            */
  /* ================================================================ */
  function drawPiece(
    ctx: CanvasRenderingContext2D,
    piece: Piece,
    cx: number,
    cy: number,
    cs: number,
  ) {
    const radius = cs * 0.38;
    const isWhite = piece.color === PieceColor.White;

    ctx.beginPath();
    ctx.arc(cx, cy, radius, 0, Math.PI * 2);
    ctx.fillStyle = isWhite ? '#F0F0F0' : '#333333';
    ctx.fill();
    ctx.strokeStyle = isWhite ? '#333333' : '#CCCCCC';
    ctx.lineWidth = 2;
    ctx.stroke();

    const fontSize = Math.max(10, Math.floor(cs * 0.30));
    ctx.font = `bold ${fontSize}px sans-serif`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillStyle = isWhite ? '#333333' : '#F0F0F0';
    ctx.fillText(PIECE_SHORT_NAMES[piece.type], cx, cy);
  }

  /* ================================================================ */
  /*  Coordinate helpers for events                                    */
  /* ================================================================ */
  function canvasCoords(
    e: React.MouseEvent | React.TouchEvent | MouseEvent | TouchEvent,
  ): { x: number; y: number } {
    const canvas = canvasRef.current!;
    const rect = canvas.getBoundingClientRect();
    let clientX: number, clientY: number;
    if ('touches' in e) {
      const touch = (e as TouchEvent).touches[0] ?? (e as TouchEvent).changedTouches[0];
      clientX = touch.clientX;
      clientY = touch.clientY;
    } else {
      clientX = (e as MouseEvent).clientX;
      clientY = (e as MouseEvent).clientY;
    }
    return { x: clientX - rect.left, y: clientY - rect.top };
  }

  /* ================================================================ */
  /*  After-drop logic                                                 */
  /* ================================================================ */
  const handleDropResult = useCallback(
    (fromCell: string, toCell: string) => {
      const piece = position[fromCell];
      if (!piece) return;

      const targetPiece = position[toCell];

      /* Same cell -> cancel */
      if (fromCell === toCell) return;

      /* Landing on own piece (not razvedchik exchange) -> cancel */
      if (targetPiece && targetPiece.color === piece.color) {
        /* Razvedchik exchange check */
        if (isRazvedchikExchange(piece, toCell, targetPiece)) {
          onRazvedchikExchange(fromCell, toCell);
          return;
        }
        return;
      }

      /* Execute the move */
      onMoveMade(fromCell, toCell);

      /* Now check promotions on the destination cell. We build a
         hypothetical position after the move to run checks. */
      const afterPos = copyPosition(position);
      delete afterPos[fromCell];
      afterPos[toCell] = { ...piece };

      /* Knekht auto-promotion */
      const knechtResult = checkKnechtPromotion(toCell, afterPos[toCell]);
      if (knechtResult) {
        /* auto-promote: parent handles via onMoveMade already, but signal: */
        return;
      }

      /* VerKnekht promotion choice */
      const vkOptions = checkVerKnechtPromotion(toCell, afterPos[toCell]);
      if (vkOptions) {
        onPromotionNeeded(toCell, afterPos[toCell], vkOptions);
        return;
      }

      /* Prince promotion choice */
      const prOptions = checkPrincePromotion(toCell, afterPos[toCell]);
      if (prOptions) {
        onPromotionNeeded(toCell, afterPos[toCell], prOptions);
        return;
      }
    },
    [position, onMoveMade, onPromotionNeeded, onRazvedchikExchange],
  );

  /* ================================================================ */
  /*  Mouse / Touch handlers                                           */
  /* ================================================================ */
  const handlePointerDown = useCallback(
    (e: React.MouseEvent | React.TouchEvent) => {
      e.preventDefault();
      if (stage === 'startup') return;

      const { x, y } = canvasCoords(e);
      const cell = pixelToCell(x, y, offsetX, offsetY, cellSize, reversed);
      if (!cell) return;

      /* Deletion mode */
      if (deletionMode) {
        if (position[cell]) {
          onCellClickForDeletion(cell);
        }
        return;
      }

      const piece = position[cell];
      if (!piece) return;

      /* In game mode, enforce turn */
      if (stage === 'game' && mode !== null) {
        const isWhitePiece = piece.color === PieceColor.White;
        if (isWhitePiece !== whiteTurn) return;
      }

      /* Start drag */
      const [col, row] = cellToCoords(cell);
      const vc = viewCol(col, reversed);
      const vr = viewRow(row, reversed);
      const pieceCx = offsetX + vc * cellSize + cellSize / 2;
      const pieceCy = offsetY + vr * cellSize + cellSize / 2;

      dragPieceRef.current = piece;
      dragFromCellRef.current = cell;
      dragPosRef.current = { x, y };
      dragOffsetRef.current = { dx: pieceCx - x, dy: pieceCy - y };
      hoverCellRef.current = cell;
      isDraggingRef.current = true;

      setCursorClass('cursor-grabbing');
      draw();
    },
    [
      stage,
      mode,
      whiteTurn,
      deletionMode,
      position,
      cellSize,
      offsetX,
      offsetY,
      reversed,
      onCellClickForDeletion,
      draw,
    ],
  );

  const handlePointerMove = useCallback(
    (e: React.MouseEvent | React.TouchEvent) => {
      e.preventDefault();
      if (!isDraggingRef.current) {
        /* Update cursor based on hoverable piece */
        const { x, y } = canvasCoords(e);
        const cell = pixelToCell(x, y, offsetX, offsetY, cellSize, reversed);
        if (cell && position[cell]) {
          setCursorClass(deletionMode ? 'cursor-pointer' : 'cursor-grab');
        } else {
          setCursorClass('cursor-default');
        }
        return;
      }

      const { x, y } = canvasCoords(e);
      dragPosRef.current = { x, y };
      hoverCellRef.current = pixelToCell(x, y, offsetX, offsetY, cellSize, reversed);
      draw();
    },
    [cellSize, offsetX, offsetY, reversed, position, deletionMode, draw],
  );

  const handlePointerUp = useCallback(
    (e: React.MouseEvent | React.TouchEvent) => {
      e.preventDefault();
      if (!isDraggingRef.current) return;

      const { x, y } = canvasCoords(e);
      const fromCell = dragFromCellRef.current!;
      let toCell = pixelToCell(x, y, offsetX, offsetY, cellSize, reversed);

      /* If between cells / ambiguous, snap to hoverCell */
      if (!toCell && hoverCellRef.current) {
        toCell = hoverCellRef.current;
      }

      /* Reset drag state */
      isDraggingRef.current = false;
      dragPieceRef.current = null;
      dragFromCellRef.current = null;
      dragPosRef.current = null;
      hoverCellRef.current = null;
      setCursorClass('cursor-default');

      /* Outside board -> piece exits */
      if (!toCell) {
        onPieceExited();
        draw();
        return;
      }

      /* Same cell -> cancel */
      if (toCell === fromCell) {
        draw();
        return;
      }

      handleDropResult(fromCell, toCell);
      draw();
    },
    [
      cellSize,
      offsetX,
      offsetY,
      reversed,
      onPieceExited,
      handleDropResult,
      draw,
    ],
  );

  /* Cancel drag if mouse leaves canvas entirely */
  const handlePointerLeave = useCallback(
    (_e: React.MouseEvent) => {
      if (!isDraggingRef.current) return;
      /* Treat as drop outside */
      isDraggingRef.current = false;
      dragPieceRef.current = null;
      dragFromCellRef.current = null;
      dragPosRef.current = null;
      hoverCellRef.current = null;
      setCursorClass('cursor-default');
      onPieceExited();
      draw();
    },
    [onPieceExited, draw],
  );

  /* ================================================================ */
  /*  Render                                                           */
  /* ================================================================ */
  return (
    <div ref={wrapperRef} className="board-wrapper">
      <canvas
        ref={canvasRef}
        className={`board-canvas ${cursorClass}`}
        width={canvasSize.w}
        height={canvasSize.h}
        onMouseDown={handlePointerDown}
        onMouseMove={handlePointerMove}
        onMouseUp={handlePointerUp}
        onMouseLeave={handlePointerLeave}
        onTouchStart={handlePointerDown}
        onTouchMove={handlePointerMove}
        onTouchEnd={handlePointerUp}
      />
    </div>
  );
};

export default Board;
