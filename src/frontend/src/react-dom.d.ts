// Lokale Typen ausschließlich für die zwei verwendeten React-DOM-Client-APIs.
// Die tatsächliche Implementierung stammt aus dem festgelegten react-dom-Paket.
declare module 'react-dom/client' {
  import type { ReactNode } from 'react';
  export interface Root { render(children: ReactNode): void; unmount(): void }
  export function createRoot(container: Element | DocumentFragment): Root;
}
declare module '*.css';
