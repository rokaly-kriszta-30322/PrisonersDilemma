import axios from 'axios';

const API_BASE = import.meta.env.VITE_API_BASE || "http://localhost:5078";

export default axios.create({
  baseURL: API_BASE,
});