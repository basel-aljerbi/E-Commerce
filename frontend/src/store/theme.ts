import { create } from 'zustand';
import { persist } from 'zustand/middleware';

type Theme = 'light' | 'dark';

interface ThemeState {
  theme: Theme;
  toggleTheme: () => void;
  setTheme: (theme: Theme) => void;
}

export const useThemeStore = create<ThemeState>()(
  persist(
    (set, get) => ({
      theme: 'light',

      toggleTheme: () => {
        const next = get().theme === 'light' ? 'dark' : 'light';
        set({ theme: next });
        applyTheme(next);
      },

      setTheme: (theme: Theme) => {
        set({ theme });
        applyTheme(theme);
      },
    }),
    { name: 'theme-storage' }
  )
);

function applyTheme(theme: Theme) {
  const root = document.documentElement;
  if (theme === 'dark') {
    root.classList.add('dark');
  } else {
    root.classList.remove('dark');
  }
}

// Initialize theme on load
const saved = localStorage.getItem('theme-storage');
if (saved) {
  try {
    const parsed = JSON.parse(saved);
    if (parsed.state?.theme) applyTheme(parsed.state.theme as Theme);
  } catch { /* ignore */ }
}
