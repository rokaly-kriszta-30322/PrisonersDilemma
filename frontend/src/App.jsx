import './App.css';
import SignUp from './SignUp';
import LogIn from './LogIn';
import { Route, Routes } from 'react-router-dom';
import React, { createContext, useState, useEffect } from 'react';
import GamePage from './Game';
import GameOverPage from './GameOver';

function App() {
  return (
    <main className="App">
      <Routes>
        <Route path="/signup" element={<SignUp />} />
        <Route path="/login" element={<LogIn />} />
        <Route path="/game" element={<GamePage />} />
        <Route path="/gameover" element={<GameOverPage />} />
      </Routes>
    </main>
  );
}

export default App;