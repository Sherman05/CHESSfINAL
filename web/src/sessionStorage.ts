import type { GameState, Position } from './types';

const SESSION_KEY = 'chess-t1-session';
const CONFIG_KEY = 'chess-t1-config';

// ---------------------------------------------------------------------------
// Session persistence (game state + position)
// ---------------------------------------------------------------------------

export type SessionData = Partial<GameState> & { position: Position };

/** Save current session data to localStorage. */
export function saveSession(state: SessionData): void {
  try {
    localStorage.setItem(SESSION_KEY, JSON.stringify(state));
  } catch {
    // Storage full or unavailable — silently ignore.
  }
}

/** Load the previously saved session, or null if none exists. */
export function loadSession(): SessionData | null {
  try {
    const raw = localStorage.getItem(SESSION_KEY);
    if (!raw) return null;
    return JSON.parse(raw) as SessionData;
  } catch {
    return null;
  }
}

/** Remove the saved session from localStorage. */
export function clearSession(): void {
  try {
    localStorage.removeItem(SESSION_KEY);
  } catch {
    // Ignore.
  }
}

// ---------------------------------------------------------------------------
// App configuration persistence
// ---------------------------------------------------------------------------

export interface AppConfig {
  skipIntro: boolean;
}

const DEFAULT_CONFIG: AppConfig = { skipIntro: false };

/** Save application configuration to localStorage. */
export function saveConfig(config: AppConfig): void {
  try {
    localStorage.setItem(CONFIG_KEY, JSON.stringify(config));
  } catch {
    // Ignore.
  }
}

/** Load application configuration, falling back to defaults. */
export function loadConfig(): AppConfig {
  try {
    const raw = localStorage.getItem(CONFIG_KEY);
    if (!raw) return { ...DEFAULT_CONFIG };
    return { ...DEFAULT_CONFIG, ...JSON.parse(raw) };
  } catch {
    return { ...DEFAULT_CONFIG };
  }
}
