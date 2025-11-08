import * as signalR from '@microsoft/signalr';

// Read API base URL from Vite env var VITE_API_URL (set by the AppHost) and append the hub path.
const apiBase = import.meta.env.VITE_API_URL ?? 'https://localhost:7134';
const hubUrl = `${apiBase.replace(/\/$/, '')}/game-hub`;

const connection = new signalR.HubConnectionBuilder()
  .withUrl(hubUrl)
  .withAutomaticReconnect()
  .configureLogging(signalR.LogLevel.Information)
  .build();

export default connection;