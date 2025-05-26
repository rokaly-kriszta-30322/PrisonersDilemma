import { useRef, useState, useEffect, useContext } from "react";
import axios from "./api/axios";
import React from 'react';
import { Link } from 'react-router-dom';
import AuthContext from "./context/AuthProvider";
import { useNavigate } from "react-router-dom";

const GamePage = () => {

  const { auth, setAuth } = useContext(AuthContext);

  const [activePlayers, setActivePlayers] = useState([]);
  const [selectedPlayer, setSelectedPlayer] = useState('');
  const [choice, setChoice] = useState('');
  const [pendingInteraction, setPendingInteraction] = useState(null);
  const [bots, setBots] = useState([]);
  const [selectedBotId, setSelectedBotId] = useState(null);
  const [isActive, setIsActive] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    if (auth.user.role !== 'admin') return;
    const fetchBots = async () => {
      try {
        const response = await axios.get('/UserData/bots', {
          headers: {
            Authorization: `Bearer ${auth.token}`
          }
        });
        setBots(response.data);
      } catch (error) {
        console.error("Failed to fetch bots:", error);
      }
    };

    fetchBots();
  }, [auth.token]);

  useEffect(() => {
    const fetchActivePlayers = async () => {
      try {
        const response = await axios.get('/UserData/users/active', {
          headers: {
            Authorization: `Bearer ${auth.token}`
          }
        });
        setActivePlayers(response.data);
      } catch (err) {
        console.error("Failed to fetch players:", err);
      }
    };

    fetchActivePlayers();

    const interval = setInterval(fetchActivePlayers, 5000);
    return () => clearInterval(interval);

  }, [auth.token]);

  useEffect(() => {
    const botInitiationInterval = setInterval(async () => {
      const bots = activePlayers.filter(p => p.role === 'bot');

      for (const bot of bots) {
        try {
          const pendingRes = await axios.get(`/BotStrategy/pending/bot/${bot.userId}`, {
            headers: { Authorization: `Bearer ${auth.token}` },
            validateStatus: () => true
          });
          console.log(`status ${pendingRes.status}`);
          if (pendingRes.status === 204) {
            const candidates = activePlayers.filter(p => p.userId !== bot.userId);
            console.log(`candidates ${candidates.length}`);
            activePlayers.forEach(p => console.log(`User: ${p.userName}, Id: ${p.userId} (${typeof p.userId})`));
            console.log(`bot userid ${bot.userId}`);
            if (candidates.length === 0) continue;
            
            const randomTarget = candidates[Math.floor(Math.random() * candidates.length)];

            console.log(`Bots`, candidates);

            await axios.post(`/BotStrategy/interaction/initiate`, {
              botId: bot.userId,
              targetName: randomTarget.userName
            }, {
              headers: { Authorization: `Bearer ${auth.token}` }
            });

            console.log(`Bot ${bot.userName} initiated with ${randomTarget.userName}`);
          }

        } catch (err) {
          console.error(`Bot ${bot.userName} initiation failed:`, err);

          if (err.response && err.response.data) {
            console.error(`Error message from backend: ${err.response.data}`);
          }
        }
      }

    }, 10000);

    return () => clearInterval(botInitiationInterval);
  }, [activePlayers, auth.token]);

  useEffect(() => {
    const pollPending = async () => {
      try {
        const response = await axios.get('/GameSession/game/pending', {
          headers: { Authorization: `Bearer ${auth.token}` }
        });
        if (response.status === 200) {
          setPendingInteraction(response.data);
        } else {
          setPendingInteraction(null);
        }
      } catch (err) {
        setPendingInteraction(null);
      }
    };

    const pollGameData = async () => {
      try {
        const response = await axios.get('/GameData/data', {
          headers: { Authorization: `Bearer ${auth.token}` }
        });

        const money = response.data.moneyPoints;

        if (money <= 0) {
          try {
            await axios.post('/GameData/reset', {}, {
              headers: {
                Authorization: `Bearer ${auth.token}`
              }
            });
            navigate('/gameover');
          } catch (err) {
            console.warn("Reset backend call failed.");
          }
          return;
        }

        setAuth(prev => ({
          ...prev,
          user: {
            ...prev.user,
            gameData: response.data
          }
        }));
      } catch (err) {
        console.error("Failed to fetch game data:", err);
      }
    };

    const interval = setInterval(() => {
      pollPending();
      pollGameData();
    }, 2000);

    pollPending();
    pollGameData();

    return () => clearInterval(interval);
  }, [auth.token]);

  const handleActivate = async () => {
    if (!selectedBotId) return;
    try {
      await axios.post(`/UserData/bot/activate/${selectedBotId}`, {}, {
        headers: {
          Authorization: `Bearer ${auth.token}`
        }
      });
      alert("Bot activated");
    } catch (error) {
      console.error("Activation failed:", error);
    }
  };

  const handleDeactivate = async () => {
    if (!selectedBotId) return;
    try {
      await axios.post(`/UserData/bot/deactivate/${selectedBotId}`, {}, {
        headers: {
          Authorization: `Bearer ${auth.token}`,
          'Content-Type': 'application/json'
        }
      });
      alert("Bot deactivated");
    } catch (error) {
      console.error("Deactivation failed:", error);
    }
  };

  const respondToInteraction = async (choice) => {
    try {
      await axios.post('/GameSession/game/respond', {
        pendingId: pendingInteraction.pendingId,
        targetChoice: choice
      }, {
        headers: { Authorization: `Bearer ${auth.token}` }
      });
  
      alert(`You chose to ${choice}`);
      setPendingInteraction(null);

    } catch (err) {
      console.error("Failed to respond to interaction:", err);
    }
  };

  const onSendChoice = async ({ initiatorUsername, targetUsername, choice }) => {
    try {
      await axios.post('/GameSession/game/interaction', {
        userName1: initiatorUsername,
        userName2: targetUsername,
        choice1: choice
      }, {
        headers: {
          Authorization: `Bearer ${auth.token}`
        }
      });
      alert("Choice sent!");
    } catch (err) {
      if (err.response && err.response.data) {
        alert(err.response.data);
      } else {
        alert("Failed to send choice due to an unknown error.");
      }
      console.error("Failed to send choice:", err);
    }
  };

  const handleToggleActive = async () => {
  try {
    const newStatus = !isActive;
    await axios.post('/UserData/users/set-active', newStatus, {
      headers: {
        Authorization: `Bearer ${auth.token}`,
        'Content-Type': 'application/json'
      }
    });
    setIsActive(newStatus);
  } catch (err) {
    console.error("Failed to toggle active status:", err);
  }
};

  const handleLogout = async () => {
    try {

      await axios.post('/UserData/logout', {}, {
        headers: {
          Authorization: `Bearer ${auth.token}`
        }
      });
    } catch (err) {
      console.warn("Logout backend call failed.");
    }

    navigate('/login');
    
    setAuth({});
    
    localStorage.removeItem("auth");
  };

  const onBuy = async () => {
    if (auth.user.gameData.moneyPoints <= 500) {
      alert("Not enough money!");
      return;
    }
    try {
      await axios.post('/GameSession/game/buy', {}, {
        headers: {
          Authorization: `Bearer ${auth.token}`
        }
      });
      alert("Buy successful!");
    } catch (err) {
      console.error("Buy failed:", err);
    }
  };

  const handleSend = () => {
  if (selectedPlayer && choice) {
    onSendChoice({
      initiatorUsername: auth.user.userName,
      targetUsername: selectedPlayer,
      choice
    });

    setChoice('');
  }
};

const filteredPlayers = activePlayers.filter(p => p.userId !== auth.user.userId);

  return (
    <div className="game-container">

      {pendingInteraction && (
        <div className="popup-overlay">
          <div className="popup">
            <h3>New Deal from {pendingInteraction.fromUser}</h3>
            <button onClick={() => respondToInteraction("Coop")}>Coop</button>
            <button onClick={() => respondToInteraction("Deflect")}>Deflect</button>
          </div>
        </div>
      )}

      <div className="player-info">
        <h2>{auth.user.userName}</h2>
        <p>Money: ${auth.user.gameData.moneyPoints}</p>
        <div className="matrix-box">
          <strong>Matrix:</strong>
          <pre>{JSON.stringify({
            CoopCoop: auth.user.gameData.coopCoop,
            CoopDeflect: auth.user.gameData.coopDeflect,
            DeflectCoop: auth.user.gameData.deflectCoop,
            DeflectDeflect: auth.user.gameData.deflectDeflect
          }, null, 2)}</pre>
        </div>
      </div>
      <button onClick={handleLogout} className="logout-button">Logout</button>
      <button onClick={onBuy} className="buy-button">
        Buy
      </button>

      <div className="interaction-box">
        <table>
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
                  <button
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
                  <button
                    onClick={() =>
                      onSendChoice({
                        initiatorUsername: auth.user.userName,
                        targetUsername: player.userName,
                        choice: 'Deflect'
                      })
                    }
                  >
                    Deflect
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <>
          {auth.user?.role === 'admin' && (
            <div>
              <h3>Bot Management</h3>
              <select
                value={selectedBotId || ""}
                onChange={(e) => setSelectedBotId(parseInt(e.target.value))}
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
              <button onClick={handleToggleActive}>
                {isActive ? "Spectate" : "Join"}
              </button>
            </div>
          )}
        </>
      </div>
    </div>
    
  );
};

export default GamePage;
