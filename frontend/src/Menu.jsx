import React, { useEffect, useContext, useState } from "react";
import { useNavigate } from "react-router-dom";
import AuthContext from "./context/AuthProvider";
import axios from "./api/axios";

const Menu = () => {
  const navigate = useNavigate();
  const { auth, setAuth } = useContext(AuthContext);
  const [profile, setProfile] = useState(null);

  useEffect(() => {
    if (!auth?.token) {
      navigate("/login", { replace: true });
      return;
    }

    const load = async () => {
      try {
        const id = auth?.user?.userId;
        if (!id) return;

        const res = await axios.get(`/UserData/GetUser/${id}`, {
          headers: { Authorization: `Bearer ${auth.token}` },
        });

        setProfile(res.data);
      } catch (e) {
        console.warn("Failed to load user profile", e);
      }
    };

    load();
  }, [auth, navigate]);

  const userName = auth?.user?.userName ?? "";
  const gameNr = profile?.gameNr ?? profile?.GameNr ?? 0;
  const maxTurns = profile?.maxTurns ?? profile?.MaxTurns ?? 0;

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
        <div><strong>Games played:</strong> {gameNr}</div>
        <div><strong>Max turns reached:</strong> {maxTurns}</div>
      </div>

      <div style={{ marginTop: 24, display: "flex", gap: 12 }}>
        <button onClick={() => navigate("/game")} className="signup">
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
