import { InjectionToken } from '@angular/core';

/**
 * Runtime configuration, loaded from /config.json before the app starts.
 * Edit config.json on the server (or generate it at container start) to point
 * the frontend at a different API without rebuilding.
 */
export interface AppConfig {
  /** Base URL of the RAG.API, including the /api prefix, without a trailing slash. */
  apiUrl: string;
}

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');

const DEFAULT_CONFIG: AppConfig = {
  apiUrl: 'https://localhost:7139/api'
};

export async function loadAppConfig(): Promise<AppConfig> {
  try {
    const response = await fetch('config.json', { cache: 'no-store' });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    const config = { ...DEFAULT_CONFIG, ...(await response.json()) } as AppConfig;
    config.apiUrl = config.apiUrl.replace(/\/+$/, '');
    return config;
  } catch (err) {
    console.warn('Could not load config.json, using defaults.', err);
    return DEFAULT_CONFIG;
  }
}
