import { AppMode, AppStage } from '../types';

interface TopToolbarProps {
  mode: AppMode;
  stage: AppStage;
  onResetToInitial: () => void;
  onStartParty: () => void;
  onStartAnalysis: () => void;
  onMinimize: () => void;
  onToggleAlwaysOnTop: () => void;
  onClose: () => void;
  alwaysOnTop: boolean;
}

export default function TopToolbar({
  mode,
  stage,
  onResetToInitial,
  onStartParty,
  onStartAnalysis,
  onMinimize,
  onToggleAlwaysOnTop,
  onClose,
  alwaysOnTop,
}: TopToolbarProps) {
  const isGame = stage === 'game';

  return (
    <div className="top-toolbar">
      <button
        onClick={onResetToInitial}
        disabled={isGame}
        title="Начальная расстановка"
      >
        Начальная расстановка
      </button>

      <button
        className={`btn-party${mode === 'party' ? ' active' : ''}`}
        onClick={onStartParty}
        disabled={isGame && mode !== 'party'}
        title="Партия"
      >
        Партия
      </button>

      <button
        className={`btn-analysis${mode === 'analysis' ? ' active' : ''}`}
        onClick={onStartAnalysis}
        disabled={isGame && mode !== 'analysis'}
        title="Анализ"
      >
        Анализ
      </button>

      <div className="spacer" />

      <button
        className="btn-window"
        onClick={onMinimize}
        title="Свернуть"
      >
        &#x2014;
      </button>

      <button
        className={`btn-window${alwaysOnTop ? ' active' : ''}`}
        onClick={onToggleAlwaysOnTop}
        title="Поверх всех окон"
      >
        &#x25A0;
      </button>

      <button
        className="btn-window"
        onClick={onClose}
        title="Закрыть"
      >
        &#x2715;
      </button>
    </div>
  );
}
