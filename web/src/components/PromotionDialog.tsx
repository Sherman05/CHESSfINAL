import { PieceType, PieceColor } from '../types';
import { PIECE_SHORT_NAMES } from '../gameLogic';

export interface PromotionInfo {
  cell: string;
  pieceColor: PieceColor;
  options: PieceType[];
}

interface PromotionDialogProps {
  info: PromotionInfo;
  onChosen: (pieceType: PieceType) => void;
}

export default function PromotionDialog({
  info,
  onChosen,
}: PromotionDialogProps) {
  const { pieceColor, options } = info;
  const isWhite = pieceColor === PieceColor.White;

  // Position the dialog roughly in the center of the screen.
  // In a more advanced implementation this would be positioned near the cell.
  const style: React.CSSProperties = {
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
  };

  return (
    <>
      <div className="promotion-overlay" />
      <div className="promotion-dialog" style={style}>
        <div className="promo-title">
          Превращение — выберите фигуру
        </div>
        <div className="promo-options">
          {options.map((pt) => (
            <button
              key={pt}
              className={`promo-option ${isWhite ? 'white-promo' : 'black-promo'}`}
              onClick={() => onChosen(pt)}
              title={PIECE_SHORT_NAMES[pt]}
            >
              {PIECE_SHORT_NAMES[pt]}
            </button>
          ))}
        </div>
      </div>
    </>
  );
}
