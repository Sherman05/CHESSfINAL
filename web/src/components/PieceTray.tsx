import { PieceType, PieceColor } from '../types';
import { PIECE_SHORT_NAMES } from '../gameLogic';

interface PieceTrayProps {
  color: 'white' | 'black';
  onDragStart: (pieceType: PieceType, pieceColor: PieceColor) => void;
}

const TRAY_PIECES: PieceType[] = [
  PieceType.King,
  PieceType.Konnet,
  PieceType.Prince,
  PieceType.Ritter,
  PieceType.Knekht,
  PieceType.VerKnekht,
  PieceType.Razvedchik,
];

export default function PieceTray({ color, onDragStart }: PieceTrayProps) {
  const pieceColor = color === 'white' ? PieceColor.White : PieceColor.Black;

  const handleDragStart = (
    e: React.DragEvent<HTMLDivElement>,
    pieceType: PieceType,
  ) => {
    e.dataTransfer.setData(
      'application/chess-piece',
      JSON.stringify({ type: pieceType, color: pieceColor }),
    );
    e.dataTransfer.effectAllowed = 'copy';
    onDragStart(pieceType, pieceColor);
  };

  return (
    <div className={`piece-tray ${color}`}>
      {TRAY_PIECES.map((pt) => (
        <div
          key={pt}
          className="piece-tray-item"
          draggable
          onDragStart={(e) => handleDragStart(e, pt)}
          title={PIECE_SHORT_NAMES[pt]}
        >
          {PIECE_SHORT_NAMES[pt]}
        </div>
      ))}
    </div>
  );
}
