import React from 'react';
import { Link } from 'react-router-dom';

const GameOverPage = () => (
  <div className="signup">
    <h1>Game Over</h1>
    <p>You have run out of money ¯\_(ツ)_/¯.</p>
    <Link to="/login">Back to login</Link>
  </div>
);

export default GameOverPage;