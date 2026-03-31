import { AppMode, AppStage } from '../types';
import { getIndicatorText } from '../gameLogic';

interface BottomToolbarProps {
  mode: AppMode;
  stage: AppStage;
  moveNumber: number;
  whiteTurn: boolean;
  analysisFirstWhite: boolean;
  deleteMode: boolean;
  onMenu: () => void;
  onPrevMove: () => void;
  onNextMove: () => void;
  onDeleteMode: () => void;
  onReverse: () => void;
  /* Analysis setup_position specific */
  onAnalysisReset: () => void;
  onToggleFirstMove: () => void;
  onAnalysisOk: () => void;
  historyIndex: number;
  historyLength: number;
}

export default function BottomToolbar({
  mode,
  stage,
  moveNumber,
  whiteTurn,
  analysisFirstWhite,
  deleteMode,
  onMenu,
  onPrevMove,
  onNextMove,
  onDeleteMode,
  onReverse,
  onAnalysisReset,
  onToggleFirstMove,
  onAnalysisOk,
  historyIndex,
  historyLength,
}: BottomToolbarProps) {
  const isGame = stage === 'game';
  const isSetup = stage === 'setup_position';
  const isAnalysisSetup = mode === 'analysis' && isSetup;

  const indicatorText = isGame
    ? getIndicatorText(moveNumber, whiteTurn)
    : '';

  const canPrev = isGame && historyIndex > 0;
  const canNext = isGame && historyIndex < historyLength - 1;

  return (
    <div className="bottom-toolbar">
      <button onClick={onMenu} title="Меню">
        &#x2630; Меню
      </button>

      <div className="separator" />

      {isGame && (
        <>
          <span className="indicator">{indicatorText}</span>

          <button
            onClick={onPrevMove}
            disabled={!canPrev}
            title="Предыдущий ход"
          >
            &#x25C0;
          </button>

          <button
            onClick={onNextMove}
            disabled={!canNext}
            title="Следующий ход"
          >
            &#x25B6;
          </button>

          <div className="separator" />

          <button
            className={deleteMode ? 'active' : ''}
            onClick={onDeleteMode}
            title="Удалить фигуру"
          >
            &#x2716; Удалить
          </button>
        </>
      )}

      {isSetup && !isAnalysisSetup && (
        <button
          className={deleteMode ? 'active' : ''}
          onClick={onDeleteMode}
          title="Удалить фигуру"
        >
          &#x2716; Удалить
        </button>
      )}

      {isAnalysisSetup && (
        <div className="analysis-setup">
          <button onClick={onAnalysisReset}>Сброс</button>

          <button
            className="toggle-btn"
            onClick={onToggleFirstMove}
          >
            1-й ход: {analysisFirstWhite ? 'белые' : 'чёрные'}
          </button>

          <button
            className={deleteMode ? 'active' : ''}
            onClick={onDeleteMode}
            title="Удалить фигуру"
          >
            &#x2716; Удалить
          </button>

          <button className="ok-btn" onClick={onAnalysisOk}>
            Ок
          </button>
        </div>
      )}

      <div className="spacer" />

      <button onClick={onReverse} title="Перевернуть доску">
        &#x21C5; Перевернуть
      </button>
    </div>
  );
}
