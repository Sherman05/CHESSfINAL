interface IntroPageProps {
  onClose: () => void;
  onSkipForever: () => void;
}

export default function IntroPage({ onClose, onSkipForever }: IntroPageProps) {
  return (
    <div className="intro-overlay" onClick={onClose}>
      <div className="intro-modal" onClick={(e) => e.stopPropagation()}>
        <div className="intro-header">Шахматы T1 — Добро пожаловать</div>

        <div className="intro-body">
          <p>
            <strong>Шахматы T1</strong> — это вариант шахмат с уникальными
            фигурами и правилами. Игра ведётся на стандартной доске 8x8.
          </p>
          <p>
            <strong>Фигуры:</strong> Король (Кр), Коннет (Кт), Принц (Пр),
            Риттер (Рт), Кнехт (Кн), Верховный Кнехт (ВК), Разведчик (Рк).
          </p>
          <p>
            <strong>Режим «Партия»:</strong> Классическая игра двух игроков.
            Начальная расстановка устанавливается автоматически. Ходы
            записываются, и вы можете пролистывать историю ходов.
          </p>
          <p>
            <strong>Режим «Анализ»:</strong> Позволяет расставить фигуры
            произвольно. Выберите, кто ходит первым (белые или чёрные), и
            нажмите «Ок» для начала анализа.
          </p>
          <p>
            <strong>Превращение Кнехта:</strong> Кнехт, дойдя до последней
            горизонтали, автоматически превращается в Верховного Кнехта.
            Верховный Кнехт на последней горизонтали превращается в фигуру по
            выбору игрока.
          </p>
          <p>
            <strong>Превращение Принца:</strong> Принц, дойдя до последней
            горизонтали, превращается в фигуру по выбору игрока (Риттер,
            Коннет или Разведчик).
          </p>
          <p>
            <strong>Разведчик:</strong> Может обменяться местами с вражеской
            фигурой (кроме Короля) в зоне замка противника.
          </p>
          <p>
            Используйте кнопки на панели инструментов для управления игрой.
            Перетаскивайте фигуры мышью для совершения ходов.
          </p>
        </div>

        <div className="intro-footer">
          <button className="btn-danger" onClick={onSkipForever}>
            Не показывать больше
          </button>
          <button className="btn-secondary" onClick={onClose}>
            Пропустить
          </button>
          <button className="btn-primary" onClick={onClose}>
            Основной режим
          </button>
        </div>
      </div>
    </div>
  );
}
