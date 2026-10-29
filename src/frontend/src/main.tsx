import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import {Anwendung} from './App.js';
import './style.css';

const element = document.getElementById('root');
if (!element) throw new Error('Der Anwendungsbereich fehlt.');
createRoot(element).render(<StrictMode><Anwendung /></StrictMode>);
