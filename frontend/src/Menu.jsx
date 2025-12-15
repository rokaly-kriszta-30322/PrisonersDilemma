import React, { useEffect, useContext } from "react";
import { useNavigate } from "react-router-dom";
import AuthContext from "./context/AuthProvider";
import axios from "./api/axios";

const Menu = () => {
  const navigate = useNavigate();
  const { auth, setAuth } = useContext(AuthContext);

  useEffect(() => {
    if (!auth?.token) navigate("/login", { replace: true });
  }, [auth, navigate]);

  const user = auth?.user;

  const userName = user?.userName ?? user?.username ?? "";
  const gamesPlayed = user?.gameNr ?? user?.game_nr ?? user?.gameNumber ?? "";
  const maxTurnsReached = user?.maxTurns ?? user?.max_turns ?? "";

  const handleLogout = async () => {
    try {
      await axios.post(
        "/UserData/logout",
        {},
        {
          headers: {
            Authorization: `Bearer ${auth.token}`,
          },
        }
      );
    } catch (err) {
      console.warn("Logout backend call failed.", err);
    } finally {
      setAuth({});
      localStorage.removeItem("auth");
      navigate("/login", { replace: true });
    }
  };

  return (
    <div className="menu-page" style={{ padding: 24 }}>
      <h2>Menu</h2>

      <div style={{ marginTop: 16 }}>
        <div><strong>Name:</strong> {userName}</div>
        <div><strong>Games played:</strong> {gamesPlayed}</div>
        <div><strong>Max turns reached:</strong> {maxTurnsReached}</div>
      </div>

      <div style={{ marginTop: 24, display: "flex", gap: 12 }}>
        <button onClick={() => navigate("/game")} className="coop">
          Join game
        </button>

        <button onClick={handleLogout} className="deflect">
          Logout
        </button>
      </div>
    </div>
  );
};

export default Menu;
