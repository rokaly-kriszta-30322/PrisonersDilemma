import './App.css';
import SignUp from './SignUp';
import LogIn from './LogIn';
import { Route, Routes, Navigate } from 'react-router-dom';
import React, { createContext, useState, useEffect } from 'react';
import GamePage from './GamePage';
import GameOverPage from './GameOver';
import Menu from './Menu';

function App() {
  return (
    <main className="App">
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />

        <Route path="/signup" element={<SignUp />} />
        <Route path="/login" element={<LogIn />} />
        <Route path="/menu" element={<Menu />} />
        <Route path="/game" element={<GamePage />} />
        <Route path="/gameover" element={<GameOverPage />} />
      </Routes>
    </main>
  );
}

export default App;