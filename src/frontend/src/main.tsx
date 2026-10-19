import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import App from './App.js';
import{Anmeldung}from'./Anmeldung.js';
import './style.css';

const element = document.getElementById('root');
if (!element) throw new Error('Der Anwendungsbereich fehlt.');
createRoot(element).render(<StrictMode><Anmeldung><App /></Anmeldung></StrictMode>);
