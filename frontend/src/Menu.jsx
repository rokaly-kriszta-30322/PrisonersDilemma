import React, { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import AuthContext from "./context/AuthProvider";

export default function Menu() {
  const navigate = useNavigate();
  const { auth, setAuth } = useContext(AuthContext);

  useEffect(() => {
    if (!auth?.token) navigate("/login", { replace: true });
  }, [auth, navigate]);

  const user = auth?.user;

  const userName = user?.userName ?? user?.username ?? "";
  const gameNr = user?.gameNr ?? user?.game_nr ?? user?.gameNumber ?? "";
  const maxTurns = user?.maxTurns ?? user?.max_turns ?? "";

  return (
    <div className="menu-page" style={{ padding: 24 }}>
      <h2>Menu</h2>

      <div style={{ marginTop: 16 }}>
        <div><strong>Name:</strong> {userName}</div>
        <div><strong>Games played:</strong> {gameNr}</div>
        <div><strong>Max turns reached:</strong> {maxTurns}</div>
      </div>

      <div style={{ marginTop: 24 }}>
        <button onClick={() => navigate("/game")} className="coop">
          Join game
        </button>
      </div>
    </div>
  );
}
