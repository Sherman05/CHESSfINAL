import { AppStage } from '../types';

interface MenuPopupProps {
  stage: AppStage;
  sessionActive: boolean;
  onAbout: () => void;
  onSavePosition: () => void;
  onSaveAs: () => void;
  onEndGame: () => void;
  onCreateShortcut: () => void;
  onExit: () => void;
  onClose: () => void;
}

export default function MenuPopup({
  stage,
  sessionActive,
  onAbout,
  onSavePosition,
  onSaveAs,
  onEndGame,
  onCreateShortcut,
  onExit,
  onClose,
}: MenuPopupProps) {
  const isGame = stage === 'game';

  // Position the menu near the bottom-left (next to the menu button)
  const style: React.CSSProperties = {
    left: 8,
    bottom: 48,
  };

  return (
    <>
      <div className="menu-overlay" onClick={onClose} />
      <div className="menu-popup" style={style}>
        <button className="menu-item" onClick={onAbout}>
          О программе
        </button>
        <div className="menu-divider" />
        <button
          className="menu-item"
          onClick={onSavePosition}
          disabled={!sessionActive}
        >
          Сохранить позицию
        </button>
        <button
          className="menu-item"
          onClick={onSaveAs}
          disabled={!sessionActive}
        >
          Сохранить как...
        </button>
        <div className="menu-divider" />
        <button
          className="menu-item"
          onClick={onEndGame}
          disabled={!isGame}
        >
          Завершить партию
        </button>
        <div className="menu-divider" />
        <button className="menu-item" onClick={onCreateShortcut}>
          Создать ярлык
        </button>
        <div className="menu-divider" />
        <button className="menu-item" onClick={onExit}>
          Выход
        </button>
      </div>
    </>
  );
}
