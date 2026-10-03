export type SoundName = string;

/** Lightweight sound facade used by UI components. Audio can be wired in later without changing callers. */
export const sound = {
  isEnabled: true,
  play(_name: SoundName): void {
    // Intentionally silent until packaged audio assets are available.
  },
  setEnabled(_enabled: boolean): void {
    // Reserved for user audio preferences.
  },
  toggle(): void {
    this.isEnabled = !this.isEnabled;
  },
};
