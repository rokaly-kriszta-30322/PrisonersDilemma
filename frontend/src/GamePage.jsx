import { useState, useEffect, useContext, useRef } from "react";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import axios from "./api/axios";
import AuthContext from "./context/AuthProvider";
import { useNavigate } from "react-router-dom";
import Nav from './Nav';
import PlayerInfo from './PlayerInfo';
import PlayerList from './PlayerList';
import History from './History';

const API_BASE = import.meta.env.VITE_API_BASE || "http://localhost:5078";

const GamePage = () => {

  const { auth, setAuth } = useContext(AuthContext);

  const [activePlayers, setActivePlayers] = useState([]);
  const [pendingInteraction, setPendingInteraction] = useState(null);
  const [bots, setBots] = useState([]);
  const [selectedBotId, setSelectedBotId] = useState(null);
  const [isActive, setIsActive] = useState(true);
  const navigate = useNavigate();
  const [history, setHistory] = useState([]);
  const [botBehavior, setBotBehavior] = useState({
    chaosMode: true,
    activeMode: true
  });
  const connectionRef = useRef(null);

  useEffect(() => {
    if (!selectedBotId) return;
    const fetchBehavior = async () => {
      try {
        const response = await axios.get(`/BotStrategy/GetBotBehavior/${selectedBotId}`, {
          headers: { Authorization: `Bearer ${auth.token}` }
        });
        setBotBehavior({
          chaosMode: response.data.chaosMode ?? response.data.ChaosMode,
          activeMode: response.data.activeMode ?? response.data.ActiveMode
        });
      } catch (err) {
        console.error("Failed to fetch bot behavior:", err);
      }
    };
    fetchBehavior();
  }, [selectedBotId]);

  useEffect(() => {
    if (!auth?.token) return;
    fetchActivePlayers();
    fetchHistory();
    fetchGameData();
  }, [auth?.token]);

  useEffect(() => {
    if (!auth?.token) return;

    const connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE}/gamehub`, { 
        accessTokenFactory: () => auth.token,
        withCredentials: false
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    const onPendingInteractionReceived = (data) => {
      setPendingInteraction({
        pendingId: data.PendingId ?? data.pendingId ?? data.pendingID,
        fromUser: data.FromUser ?? data.fromUser,
        userChoice: data.UserChoice ?? data.userChoice,
      });
    };

    const onGameStateUpdated = () => fetchGameData();
    const onHistoryUpdated = () => fetchHistory();
    const onActiveUsersChanged = () => fetchActivePlayers();
    const onGameOver = () => navigate("/gameover");

    connection.on("PendingInteractionReceived", onPendingInteractionReceived);
    connection.on("GameStateUpdated", onGameStateUpdated);
    connection.on("HistoryUpdated", onHistoryUpdated);
    connection.on("ActiveUsersChanged", onActiveUsersChanged);
    connection.on("GameOver", onGameOver);

    connection
      .start()
      .then(() => {
        fetchActivePlayers();
        fetchHistory();
        fetchGameData();
      })
      .catch(err => console.error("SignalR connection failed:", err));

    connectionRef.current = connection;

    return () => {
      try {
        connection.off("PendingInteractionReceived");
        connection.off("GameStateUpdated");
        connection.off("HistoryUpdated");
        connection.off("ActiveUsersChanged");
        connection.off("GameOver");
      } catch {}
      connection.stop().catch(() => {});
      connectionRef.current = null;
    };
  }, [auth?.token]);

  useEffect(() => {
    if (auth?.user?.role !== 'admin') return;
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

  const fetchActivePlayers = async () => {
    try {
      const response = await axios.get('/UserData/users/active', {
        headers: { Authorization: `Bearer ${auth.token}` }
      });
      setActivePlayers(response.data);
    } catch (err) {
      console.error("Failed to fetch players:", err);
    }
  };

  const fetchHistory = async () => {
    try {
      const response = await axios.get('/GameSession/history', {
        headers: { Authorization: `Bearer ${auth.token}` }
      });
      setHistory(response.data);
    } catch (error) {
      console.error('Error fetching interaction history:', error);
    }
  };

  const fetchGameData = async () => {
    try {
      const response = await axios.get('/GameData/data', {
        headers: { Authorization: `Bearer ${auth.token}` }
      });

      const money = response.data.moneyPoints;
      if (money <= 0) {
        navigate('/gameover');
        return;
      }

      setAuth(prev => ({
        ...(prev || {}),
        user: {
          ...(prev?.user || {}),
          gameData: response.data
        }
      }));
    } catch (err) {
      console.error("Failed to fetch game data:", err);
    }
  };

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

  const handleToggleMode = async (mode) => {
    if (!selectedBotId) return;

    const updatedBehavior = {
      userId: selectedBotId,
      chaosMode: mode === "chaosMode" ? !botBehavior.chaosMode : botBehavior.chaosMode,
      activeMode: mode === "activeMode" ? !botBehavior.activeMode : botBehavior.activeMode
    };

    try {
      await axios.post('/BotStrategy/SetBotBehavior', updatedBehavior, {
        headers: { Authorization: `Bearer ${auth.token}` }
      });
      setBotBehavior({
        chaosMode: updatedBehavior.chaosMode,
        activeMode: updatedBehavior.activeMode
      });
    } catch (err) {
      console.error("Failed to update bot behavior:", err);
    }
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

  const filteredPlayers = auth?.user ? activePlayers.filter(p => p.userId !== auth.user.userId) : [];

  return (
    <>
      <Nav />
      <div className="game-container">
        <PlayerInfo
          auth={auth}
          bots={bots}
          isActive={isActive}
          selectedBotId={selectedBotId}
          setSelectedBotId={setSelectedBotId}
          onBuy={onBuy}
          handleActivate={handleActivate}
          handleDeactivate={handleDeactivate}
          handleToggleActive={handleToggleActive}
          botBehavior={botBehavior}
          handleToggleMode={handleToggleMode}
        />
        <PlayerList
          filteredPlayers={filteredPlayers}
          auth={auth}
          pendingInteraction={pendingInteraction}
          onSendChoice={onSendChoice}
          respondToInteraction={respondToInteraction}
        />
        
      </div>
      <History history={history} />
    </>
  );
};

export default GamePage;
