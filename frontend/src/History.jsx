const History = ({ history }) => {
  const reversed = [...history].reverse().slice(0, 10);

  const renderChoiceTile = (choice) => {
    if (choice === "Coop") {
      return <div className="tile tile_green" title="Coop">Coop</div>;
    } else if (choice === "Deflect") {
      return <div className="tile tile_red" title="Deflect">Defect</div>;
    } else {
      return <div>{choice}</div>;
    }
  };

  return (
    <div className="history-bar">
      <table className="history-table">
        <thead>
          <tr>
            <td><strong>Nr</strong></td>
            {reversed.map((_, index) => (
              <th key={index}>{reversed.length - index}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          <tr>
            <td><strong>Your Choice</strong></td>
            {reversed.map((h, index) => (
              <td key={index}>{renderChoiceTile(h.yourChoice)}</td>
            ))}
          </tr>
          <tr>
            <td><strong>Opponent</strong></td>
            {reversed.map((h, index) => (
              <td key={index}>{h.opponentName}</td>
            ))}
          </tr>
          <tr>
            <td><strong>Opponent's Choice</strong></td>
            {reversed.map((h, index) => (
              <td key={index}>{renderChoiceTile(h.opponentChoice)}</td>
            ))}
          </tr>
        </tbody>
      </table>
    </div>
  );
};

export default History;