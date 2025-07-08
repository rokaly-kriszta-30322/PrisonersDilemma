import React from 'react';
import axios from './api/axios';

const PlayerInfo = ({
  auth,
  bots,
  isActive,
  setIsActive,
  pendingInteraction,
  setPendingInteraction,
  selectedBotId,
  setSelectedBotId,
  onBuy,
  handleActivate,
  handleDeactivate,
  handleToggleActive,
  respondToInteraction,
  botBehavior,
  setBotBehavior,
  handleToggleMode
}) => {

  return (
    <div className="player-info">
        {auth.user?.gameData ? (
        <div className="player">
          <h2>Player Info</h2>
          <h3>Money: ${auth.user?.gameData?.moneyPoints ?? "Loading..."}</h3>
          <div className="matrix-box">
            <table className="matrix-table">
              <thead>
                <tr>
                  <th>Other \ You</th>
                  <th>Coop</th>
                  <th>Defect</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>Coop</td>
                  <td>{auth.user.gameData.coopCoop}</td>
                  <td>{auth.user.gameData.coopDeflect}</td>
                </tr>
                <tr>
                  <td>Defect</td>
                  <td>{auth.user.gameData.deflectCoop}</td>
                  <td>{auth.user.gameData.deflectDeflect}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
        ) : (
          <div className="player">
            <h2>Player Info</h2>
            <h3>Loading...</h3>
          </div>
        )}
        <button onClick={onBuy} className="buy-button">
          Buy
        </button>
        <>
          {auth.user?.role === 'admin' && (
            <>
              <div>
                <h2>Bot Management</h2>
                <select
                  value={selectedBotId || ""}
                  onChange={(e) => {
                    const value = e.target.value;
                    setSelectedBotId(value ? parseInt(value) : null);
                  } }
                >
                  <option value="" disabled>Select a bot</option>
                  {bots.map((bot) => (
                    <option key={bot.userId} value={bot.userId}>
                      {bot.userName}
                    </option>
                  ))}
                </select>
                <div style={{ marginTop: "10px" }}>
                  <button onClick={handleActivate} disabled={!selectedBotId}>
                    Activate
                  </button>
                  <button onClick={handleDeactivate} disabled={!selectedBotId} style={{ marginLeft: "10px" }}>
                    Deactivate
                  </button>
                </div>
              </div>
              
              {selectedBotId && (
                <div style={{ marginTop: "10px" }}>
                  <label>
                    <input
                      type="checkbox"
                      checked={botBehavior.chaosMode}
                      onChange={() => handleToggleMode("chaosMode")}
                    />
                    Chaos Mode (unchecked = Order)
                  </label>
                  <br />
                  <label>
                    <input
                      type="checkbox"
                      checked={botBehavior.activeMode}
                      onChange={() => handleToggleMode("activeMode")}
                    />
                    Active Mode (unchecked = Passive)
                  </label>
                </div>
              )}

              <button onClick={handleToggleActive}>
                {isActive ? "Spectate" : "Join"}
              </button>
            </>
          )}
        </>
    </div>
  );
};

export default PlayerInfo;
