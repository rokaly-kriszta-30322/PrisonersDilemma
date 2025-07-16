const PlayerList = ({
    filteredPlayers,
    auth,
    pendingInteraction,
    onSendChoice,
    respondToInteraction
}) => {
  return (
    <div className="interaction-box">
        {pendingInteraction && (
          <div className="popup-overlay">
            <div className="popup">
              <h3>New Deal from {pendingInteraction.fromUser}</h3>
              <button onClick={() => respondToInteraction("Coop")}>Coop</button>
              <button onClick={() => respondToInteraction("Deflect")} style={{ marginLeft: "10px" }}>Defect</button>
            </div>
          </div>
        )}
        <table className="interaction-table">
        <thead>
            <tr>
            <th>Player</th>
            <th>Money</th>
            <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            {filteredPlayers.map((player) => (
            <tr key={player.userId}>
                <td>{player.userName}</td>
                <td>{player.moneyPoints}</td>
                <td>
                <button className="coop"
                    onClick={() =>
                    onSendChoice({
                        initiatorUsername: auth.user.userName,
                        targetUsername: player.userName,
                        choice: 'Coop'
                    })
                    }
                >
                    Coop
                </button>
                <button className="deflect"
                    onClick={() =>
                    onSendChoice({
                        initiatorUsername: auth.user.userName,
                        targetUsername: player.userName,
                        choice: 'Deflect'
                    })
                    }
                    style={{ marginLeft: "10px" }}
                >
                    Defect
                </button>
                </td>
            </tr>
            ))}
        </tbody>
        </table>
        
    </div>
  );
};

export default PlayerList;
