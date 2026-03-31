import { useState, useEffect, useCallback, useRef } from 'react';
import './App.css';

import type {
  GameState,
  Position,
} from './types';

import {
  PieceType,
  PieceColor,
} from './types';

import {
  getInitialPosition,
  copyPosition,
  recalculateTurn,
  checkKnechtPromotion,
  checkVerKnechtPromotion,
  checkPrincePromotion,
  getScreenshotName,
  PIECE_SHORT_NAMES,
} from './gameLogic';

import {
  saveSession,
  loadSession,
  loadConfig,
  saveConfig,
} from './sessionStorage';

import TopToolbar from './components/TopToolbar';
import BottomToolbar from './components/BottomToolbar';
import PieceTray from './components/PieceTray';
import IntroPage from './components/IntroPage';
import PromotionDialog from './components/PromotionDialog';
import type { PromotionInfo } from './components/PromotionDialog';
import MenuPopup from './components/MenuPopup';

// ---------------------------------------------------------------------------
// Default game state
// ---------------------------------------------------------------------------

function defaultGameState(): GameState {
  return {
    mode: null,
    stage: 'startup',
    sessionActive: false,
    whiteTurn: true,
    moveNumber: 1,
    moveHistory: [],
    historyIndex: 0,
    partyFolder: null,
    boardReversed: false,
    analysisFirstWhite: true,
    promotionPending: false,
    lastMoveCell: null,
    alwaysOnTop: false,
  };
}

// ---------------------------------------------------------------------------
// App
// ---------------------------------------------------------------------------

export default function App() {
  // ---- Core state ----
  const [gameState, setGameState] = useState<GameState>(defaultGameState);
  const [position, setPosition] = useState<Position>(() => getInitialPosition());

  // ---- UI overlays ----
  const [showIntro, setShowIntro] = useState(false);
  const [showMenu, setShowMenu] = useState(false);
  const [showAbout, setShowAbout] = useState(false);
  const [promotionInfo, setPromotionInfo] = useState<PromotionInfo | null>(null);
  const [deleteMode, setDeleteMode] = useState(false);

  // ---- Drag state ----
  const [selectedCell, setSelectedCell] = useState<string | null>(null);
  const [dropTarget, setDropTarget] = useState<string | null>(null);

  // Ref for board element (used for screenshot)
  const boardRef = useRef<HTMLDivElement>(null);

  // ---- Mount: load session / show intro ----
  useEffect(() => {
    const config = loadConfig();
    if (!config.skipIntro) {
      setShowIntro(true);
    }

    const saved = loadSession();
    if (saved) {
      setPosition(saved.position);
      setGameState((prev) => ({ ...prev, ...saved, sessionActive: true }));
    }
  }, []);

  // ---- Persist session on meaningful state changes ----
  useEffect(() => {
    if (gameState.sessionActive) {
      saveSession({ ...gameState, position });
    }
  }, [gameState, position]);

  // ---- Helpers ----
  const { mode, stage, whiteTurn, moveNumber, boardReversed, analysisFirstWhite } =
    gameState;

  const columns = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'];

  // ---- Handlers: Toolbar actions ----

  const resetToInitial = useCallback(() => {
    const pos = getInitialPosition();
    setPosition(pos);
    setGameState((prev) => ({
      ...prev,
      moveHistory: [pos],
      historyIndex: 0,
    }));
  }, []);

  const startPartyMode = useCallback(() => {
    const pos = getInitialPosition();
    setPosition(pos);
    setGameState({
      ...defaultGameState(),
      mode: 'party',
      stage: 'game',
      sessionActive: true,
      whiteTurn: true,
      moveNumber: 1,
      moveHistory: [pos],
      historyIndex: 0,
    });
    setDeleteMode(false);
    setSelectedCell(null);
  }, []);

  const startAnalysisMode = useCallback(() => {
    setGameState((prev) => ({
      ...prev,
      mode: 'analysis',
      stage: 'setup_position',
      sessionActive: true,
      analysisFirstWhite: true,
    }));
    setDeleteMode(false);
    setSelectedCell(null);
  }, []);

  const analysisReset = useCallback(() => {
    setPosition({});
    setGameState((prev) => ({
      ...prev,
      moveHistory: [],
      historyIndex: 0,
    }));
  }, []);

  const toggleAnalysisFirstMove = useCallback(() => {
    setGameState((prev) => ({
      ...prev,
      analysisFirstWhite: !prev.analysisFirstWhite,
    }));
  }, []);

  const analysisOk = useCallback(() => {
    const pos = copyPosition(position);
    const firstWhite = gameState.analysisFirstWhite;
    setGameState((prev) => ({
      ...prev,
      stage: 'game',
      whiteTurn: firstWhite,
      moveNumber: 1,
      moveHistory: [pos],
      historyIndex: 0,
    }));
    setDeleteMode(false);
    setSelectedCell(null);
  }, [position, gameState.analysisFirstWhite]);

  const prevMove = useCallback(() => {
    setGameState((prev) => {
      if (prev.historyIndex <= 0) return prev;
      const newIdx = prev.historyIndex - 1;
      const { whiteTurn: wt, moveNumber: mn } = recalculateTurn(
        newIdx,
        prev.analysisFirstWhite,
        prev.mode,
      );
      return {
        ...prev,
        historyIndex: newIdx,
        whiteTurn: wt,
        moveNumber: mn,
      };
    });
    setGameState((prev) => {
      if (prev.moveHistory[prev.historyIndex]) {
        setPosition(copyPosition(prev.moveHistory[prev.historyIndex]));
      }
      return prev;
    });
  }, []);

  const nextMove = useCallback(() => {
    setGameState((prev) => {
      if (prev.historyIndex >= prev.moveHistory.length - 1) return prev;
      const newIdx = prev.historyIndex + 1;
      const { whiteTurn: wt, moveNumber: mn } = recalculateTurn(
        newIdx,
        prev.analysisFirstWhite,
        prev.mode,
      );
      return {
        ...prev,
        historyIndex: newIdx,
        whiteTurn: wt,
        moveNumber: mn,
      };
    });
    setGameState((prev) => {
      if (prev.moveHistory[prev.historyIndex]) {
        setPosition(copyPosition(prev.moveHistory[prev.historyIndex]));
      }
      return prev;
    });
  }, []);

  const reverseBoard = useCallback(() => {
    setGameState((prev) => ({ ...prev, boardReversed: !prev.boardReversed }));
  }, []);

  const toggleDeleteMode = useCallback(() => {
    setDeleteMode((prev) => !prev);
    setSelectedCell(null);
  }, []);

  const endGame = useCallback(() => {
    setGameState((prev) => ({
      ...prev,
      stage: 'startup',
      mode: null,
      sessionActive: false,
    }));
    setPosition(getInitialPosition());
    setDeleteMode(false);
    setSelectedCell(null);
  }, []);

  // ---- Handlers: Save position as image ----

  const savePositionAsImage = useCallback(() => {
    const boardEl = boardRef.current;
    if (!boardEl) return;

    // Use canvas-based approach: create a simple text representation
    // For a full implementation, use html2canvas or similar
    const canvas = document.createElement('canvas');
    canvas.width = 640;
    canvas.height = 640;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const cellSize = 80;
    const cols = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'];

    for (let row = 7; row >= 0; row--) {
      for (let col = 0; col < 8; col++) {
        const isLight = (row + col) % 2 === 0;
        ctx.fillStyle = isLight ? '#f0d9b5' : '#b58863';
        const x = col * cellSize;
        const y = (7 - row) * cellSize;
        ctx.fillRect(x, y, cellSize, cellSize);

        const cellName = `${cols[col]}${row + 1}`;
        const piece = position[cellName];
        if (piece) {
          const isWhitePiece = piece.color === PieceColor.White;
          // Draw piece circle
          const cx = x + cellSize / 2;
          const cy = y + cellSize / 2;
          const r = cellSize * 0.35;
          ctx.beginPath();
          ctx.arc(cx, cy, r, 0, Math.PI * 2);
          ctx.fillStyle = isWhitePiece ? '#fff' : '#333';
          ctx.fill();
          ctx.strokeStyle = isWhitePiece ? '#999' : '#111';
          ctx.lineWidth = 2;
          ctx.stroke();
          // Draw label
          ctx.fillStyle = isWhitePiece ? '#333' : '#fff';
          ctx.font = 'bold 16px sans-serif';
          ctx.textAlign = 'center';
          ctx.textBaseline = 'middle';
          ctx.fillText(PIECE_SHORT_NAMES[piece.type], cx, cy);
        }
      }
    }

    const link = document.createElement('a');
    link.download = `${getScreenshotName(moveNumber, whiteTurn)}.png`;
    link.href = canvas.toDataURL('image/png');
    link.click();
  }, [position, moveNumber, whiteTurn]);

  // ---- Handlers: Close / window ----

  const closeApp = useCallback(() => {
    try {
      window.close();
    } catch {
      alert('Закройте вкладку вручную.');
    }
  }, []);

  const handleMinimize = useCallback(() => {
    // Not fully possible in a browser tab, but attempt
    try {
      // @ts-expect-error electron-specific
      if (window.electronAPI?.minimize) {
        // @ts-expect-error electron-specific
        window.electronAPI.minimize();
      }
    } catch {
      // noop in browser
    }
  }, []);

  const handleToggleAlwaysOnTop = useCallback(() => {
    setGameState((prev) => {
      const next = !prev.alwaysOnTop;
      try {
        // @ts-expect-error electron-specific
        if (window.electronAPI?.setAlwaysOnTop) {
          // @ts-expect-error electron-specific
          window.electronAPI.setAlwaysOnTop(next);
        }
      } catch {
        // noop
      }
      return { ...prev, alwaysOnTop: next };
    });
  }, []);

  // ---- Handlers: Intro ----

  const closeIntro = useCallback(() => {
    setShowIntro(false);
  }, []);

  const skipIntroForever = useCallback(() => {
    setShowIntro(false);
    saveConfig({ skipIntro: true });
  }, []);

  // ---- Handlers: Board interaction ----

  const handleCellClick = useCallback(
    (cell: string) => {
      // Delete mode
      if (deleteMode) {
        if (position[cell]) {
          const newPos = copyPosition(position);
          delete newPos[cell];
          setPosition(newPos);
        }
        return;
      }

      // Setup position mode: just select/deselect
      if (stage === 'setup_position') {
        setSelectedCell((prev) => (prev === cell ? null : cell));
        return;
      }

      // Game mode: select piece or move
      if (stage === 'game') {
        const piece = position[cell];

        if (selectedCell) {
          // Attempt move
          if (selectedCell === cell) {
            setSelectedCell(null);
            return;
          }
          const movingPiece = position[selectedCell];
          if (!movingPiece) {
            setSelectedCell(null);
            return;
          }

          // Check turn
          const isWhitePiece = movingPiece.color === PieceColor.White;
          if (isWhitePiece !== whiteTurn) {
            setSelectedCell(cell);
            return;
          }

          // Execute the move
          executeMove(selectedCell, cell, movingPiece);
          setSelectedCell(null);
        } else {
          // Select a piece
          if (piece) {
            const isWhitePiece = piece.color === PieceColor.White;
            if (isWhitePiece === whiteTurn) {
              setSelectedCell(cell);
            }
          }
        }
      }
    },
    [deleteMode, position, stage, selectedCell, whiteTurn],
  );

  const executeMove = useCallback(
    (from: string, to: string, piece: { color: PieceColor; type: PieceType }) => {
      const newPos = copyPosition(position);
      delete newPos[from];
      newPos[to] = { color: piece.color, type: piece.type };

      // Check Knekht auto-promotion
      const knechtPromo = checkKnechtPromotion(to, newPos[to]);
      if (knechtPromo) {
        newPos[to] = knechtPromo;
      }

      // Check VerKnekht promotion (player chooses)
      const vkOptions = checkVerKnechtPromotion(to, newPos[to]);
      if (vkOptions) {
        setPosition(newPos);
        setPromotionInfo({
          cell: to,
          pieceColor: piece.color,
          options: vkOptions,
        });
        setGameState((prev) => ({ ...prev, promotionPending: true }));
        return;
      }

      // Check Prince promotion (player chooses)
      const princeOptions = checkPrincePromotion(to, newPos[to]);
      if (princeOptions) {
        setPosition(newPos);
        setPromotionInfo({
          cell: to,
          pieceColor: piece.color,
          options: princeOptions,
        });
        setGameState((prev) => ({ ...prev, promotionPending: true }));
        return;
      }

      // Commit the move
      commitMove(newPos, to);
    },
    [position],
  );

  const commitMove = useCallback(
    (newPos: Position, toCell: string) => {
      setPosition(newPos);
      setGameState((prev) => {
        // Truncate future history if we navigated back
        const history = prev.moveHistory.slice(0, prev.historyIndex + 1);
        history.push(copyPosition(newPos));
        const newIdx = history.length - 1;
        const { whiteTurn: wt, moveNumber: mn } = recalculateTurn(
          newIdx,
          prev.analysisFirstWhite,
          prev.mode,
        );
        return {
          ...prev,
          moveHistory: history,
          historyIndex: newIdx,
          whiteTurn: wt,
          moveNumber: mn,
          lastMoveCell: toCell,
          promotionPending: false,
        };
      });
    },
    [],
  );

  const onPromotionChosen = useCallback(
    (pieceType: PieceType) => {
      if (!promotionInfo) return;
      const newPos = copyPosition(position);
      newPos[promotionInfo.cell] = {
        color: promotionInfo.pieceColor,
        type: pieceType,
      };
      setPromotionInfo(null);
      commitMove(newPos, promotionInfo.cell);
    },
    [promotionInfo, position, commitMove],
  );

  // ---- Handlers: Drag & drop on board ----

  const handleDragOver = useCallback(
    (e: React.DragEvent<HTMLDivElement>, cell: string) => {
      e.preventDefault();
      e.dataTransfer.dropEffect = stage === 'setup_position' ? 'copy' : 'move';
      setDropTarget(cell);
    },
    [stage],
  );

  const handleDragLeave = useCallback(() => {
    setDropTarget(null);
  }, []);

  const handleDrop = useCallback(
    (e: React.DragEvent<HTMLDivElement>, cell: string) => {
      e.preventDefault();
      setDropTarget(null);

      const data = e.dataTransfer.getData('application/chess-piece');
      if (data) {
        // Dropping from tray (setup mode)
        try {
          const { type, color } = JSON.parse(data) as {
            type: PieceType;
            color: PieceColor;
          };
          if (stage === 'setup_position') {
            const newPos = copyPosition(position);
            newPos[cell] = { color, type };
            setPosition(newPos);
          }
        } catch {
          // ignore
        }
        return;
      }

      const fromCell = e.dataTransfer.getData('text/plain');
      if (fromCell && stage === 'game') {
        const piece = position[fromCell];
        if (piece) {
          const isWhitePiece = piece.color === PieceColor.White;
          if (isWhitePiece === whiteTurn) {
            executeMove(fromCell, cell, piece);
          }
        }
      }
    },
    [stage, position, whiteTurn, executeMove],
  );

  const handleCellDragStart = useCallback(
    (e: React.DragEvent<HTMLDivElement>, cell: string) => {
      const piece = position[cell];
      if (!piece) {
        e.preventDefault();
        return;
      }
      if (stage === 'game') {
        const isWhitePiece = piece.color === PieceColor.White;
        if (isWhitePiece !== whiteTurn) {
          e.preventDefault();
          return;
        }
      }
      e.dataTransfer.setData('text/plain', cell);
      e.dataTransfer.effectAllowed = 'move';
      setSelectedCell(cell);
    },
    [position, stage, whiteTurn],
  );

  // ---- Handlers: PieceTray drag start (no-op, data set in PieceTray) ----

  const handleTrayDragStart = useCallback(
    (_pieceType: PieceType, _pieceColor: PieceColor) => {
      // Data is already set in PieceTray's onDragStart handler.
    },
    [],
  );

  // ---- Menu handlers ----

  const handleMenuSavePosition = useCallback(() => {
    setShowMenu(false);
    savePositionAsImage();
  }, [savePositionAsImage]);

  const handleMenuSaveAs = useCallback(() => {
    setShowMenu(false);
    savePositionAsImage();
  }, [savePositionAsImage]);

  const handleMenuEndGame = useCallback(() => {
    setShowMenu(false);
    endGame();
  }, [endGame]);

  const handleMenuAbout = useCallback(() => {
    setShowMenu(false);
    setShowAbout(true);
  }, []);

  const handleMenuCreateShortcut = useCallback(() => {
    setShowMenu(false);
    // In a browser environment, suggest bookmarking
    alert('Добавьте страницу в закладки для быстрого доступа.');
  }, []);

  const handleMenuExit = useCallback(() => {
    setShowMenu(false);
    closeApp();
  }, [closeApp]);

  // ---- Render board ----

  const renderBoard = () => {
    const rows = boardReversed
      ? [1, 2, 3, 4, 5, 6, 7, 8]
      : [8, 7, 6, 5, 4, 3, 2, 1];
    const cols = boardReversed
      ? [...columns].reverse()
      : columns;

    return (
      <div className="board-container">
        <div className="board" ref={boardRef}>
          {rows.map((row) =>
            cols.map((col) => {
              const cell = `${col}${row}`;
              const colIdx = col.charCodeAt(0) - 'a'.charCodeAt(0);
              const isLight = (colIdx + (row - 1)) % 2 === 1;
              const piece = position[cell];
              const isSelected = selectedCell === cell;
              const isDropTarget = dropTarget === cell;
              const isLastMove = gameState.lastMoveCell === cell;

              let cellClass = `cell ${isLight ? 'light' : 'dark'}`;
              if (isSelected) cellClass += ' highlight';
              if (isDropTarget) cellClass += ' drop-target';
              if (isLastMove && !isSelected) cellClass += ' highlight';

              return (
                <div
                  key={cell}
                  className={cellClass}
                  onClick={() => handleCellClick(cell)}
                  onDragOver={(e) => handleDragOver(e, cell)}
                  onDragLeave={handleDragLeave}
                  onDrop={(e) => handleDrop(e, cell)}
                  draggable={!!piece}
                  onDragStart={(e) => handleCellDragStart(e, cell)}
                >
                  {piece && (
                    <div
                      className={`piece-label ${
                        piece.color === PieceColor.White
                          ? 'white-piece'
                          : 'black-piece'
                      }`}
                    >
                      {PIECE_SHORT_NAMES[piece.type]}
                    </div>
                  )}
                </div>
              );
            }),
          )}
        </div>
      </div>
    );
  };

  // ---- Render ----

  return (
    <div className="app">
      <TopToolbar
        mode={mode}
        stage={stage}
        onResetToInitial={resetToInitial}
        onStartParty={startPartyMode}
        onStartAnalysis={startAnalysisMode}
        onMinimize={handleMinimize}
        onToggleAlwaysOnTop={handleToggleAlwaysOnTop}
        onClose={closeApp}
        alwaysOnTop={gameState.alwaysOnTop}
      />

      <div className="center">
        {stage === 'setup_position' && (
          <PieceTray color="white" onDragStart={handleTrayDragStart} />
        )}

        {renderBoard()}

        {stage === 'setup_position' && (
          <PieceTray color="black" onDragStart={handleTrayDragStart} />
        )}
      </div>

      <BottomToolbar
        mode={mode}
        stage={stage}
        moveNumber={moveNumber}
        whiteTurn={whiteTurn}
        analysisFirstWhite={analysisFirstWhite}
        deleteMode={deleteMode}
        onMenu={() => setShowMenu(true)}
        onPrevMove={prevMove}
        onNextMove={nextMove}
        onDeleteMode={toggleDeleteMode}
        onReverse={reverseBoard}
        onAnalysisReset={analysisReset}
        onToggleFirstMove={toggleAnalysisFirstMove}
        onAnalysisOk={analysisOk}
        historyIndex={gameState.historyIndex}
        historyLength={gameState.moveHistory.length}
      />

      {showIntro && (
        <IntroPage onClose={closeIntro} onSkipForever={skipIntroForever} />
      )}

      {promotionInfo && (
        <PromotionDialog info={promotionInfo} onChosen={onPromotionChosen} />
      )}

      {showMenu && (
        <MenuPopup
          stage={stage}
          sessionActive={gameState.sessionActive}
          onAbout={handleMenuAbout}
          onSavePosition={handleMenuSavePosition}
          onSaveAs={handleMenuSaveAs}
          onEndGame={handleMenuEndGame}
          onCreateShortcut={handleMenuCreateShortcut}
          onExit={handleMenuExit}
          onClose={() => setShowMenu(false)}
        />
      )}

      {showAbout && (
        <div className="intro-overlay" onClick={() => setShowAbout(false)}>
          <div className="intro-modal" onClick={(e) => e.stopPropagation()}>
            <div className="intro-header">О программе</div>
            <div className="intro-body">
              <p>
                <strong>Шахматы T1</strong> — шахматный вариант с уникальными
                фигурами и правилами.
              </p>
              <p>Версия 1.0</p>
            </div>
            <div className="intro-footer">
              <button
                className="btn-primary"
                onClick={() => setShowAbout(false)}
              >
                Закрыть
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
