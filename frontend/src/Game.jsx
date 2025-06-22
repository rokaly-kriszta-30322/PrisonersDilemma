import { useRef, useState, useEffect, useContext } from "react";
import axios from "./api/axios";
import AuthContext from "./context/AuthProvider";
import { useNavigate } from "react-router-dom";
import Nav from './Nav';
import PlayerInfo from './PlayerInfo';
import PlayerList from './PlayerList';
import History from './History';

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
  const [history, setHistory] = useState([]);

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

  useEffect(() => {
    if (!auth?.token) return;
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
    if (!auth?.token) return;
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

    const fetchHistory = async () => {
      try {
        const response = await axios.get('/GameSession/history', {
          headers: {
            Authorization: `Bearer ${auth.token}`
          }
        });
        setHistory(response.data);
      } catch (error) {
        console.error('Error fetching interaction history:', error);
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
      fetchHistory();
      pollPending();
      pollGameData();
    }, 2000);

    fetchHistory();
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
    setSelectedPlayer('');
    setChoice('');
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
          setIsActive={setIsActive}
          pendingInteraction={pendingInteraction}
          setPendingInteraction={setPendingInteraction}
          selectedBotId={selectedBotId}
          setSelectedBotId={setSelectedBotId}
          onBuy={onBuy}
          handleActivate={handleActivate}
          handleDeactivate={handleDeactivate}
          handleToggleActive={handleToggleActive}
          respondToInteraction={respondToInteraction}
        />
        <PlayerList
          filteredPlayers={filteredPlayers}
          auth={auth}
          pendingInteraction={pendingInteraction}
          setPendingInteraction={setPendingInteraction}
          onSendChoice={onSendChoice}
          respondToInteraction={respondToInteraction}
        />
        
      </div>
      <History history={history} />
    </>
  );
};

export default GamePage;
