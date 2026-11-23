"use client";

import { useCallback, useEffect, useSyncExternalStore } from "react";

const SFX_STORAGE_KEY = "lingual_sfx_enabled";
const SFX_CHANGE_EVENT = "lingual:sfx-changed";

let sharedAudioCtx: AudioContext | null = null;
let userHasInteracted = false;
let listenersBound = false;

function getAudioContextConstructor(): typeof AudioContext | null {
  if (typeof window === "undefined") return null;
  const win = window as Window & { webkitAudioContext?: typeof AudioContext };
  return window.AudioContext ?? win.webkitAudioContext ?? null;
}

function bindUnlockListeners() {
  if (typeof window === "undefined" || listenersBound) return;
  listenersBound = true;

  const markInteracted = () => {
    userHasInteracted = true;
    if (sharedAudioCtx && sharedAudioCtx.state === "suspended") {
      void sharedAudioCtx.resume().catch(() => {});
    }
  };

  window.addEventListener("pointerdown", markInteracted, { passive: true });
  window.addEventListener("keydown", markInteracted, { passive: true });
}

function ensureAudioContext(): AudioContext | null {
  if (typeof window === "undefined") return null;
  bindUnlockListeners();
  if (!userHasInteracted) return null;

  const Ctor = getAudioContextConstructor();
  if (!Ctor) return null;

  if (!sharedAudioCtx) {
    try {
      sharedAudioCtx = new Ctor();
    } catch {
      return null;
    }
  }

  if (sharedAudioCtx.state === "suspended") {
    void sharedAudioCtx.resume().catch(() => {});
  }
  return sharedAudioCtx;
}

export function isSfxEnabled(): boolean {
  if (typeof window === "undefined") return true;
  const raw = window.localStorage.getItem(SFX_STORAGE_KEY);
  if (raw === "false" || raw === "0") return false;
  return true;
}

export function setSfxEnabled(enabled: boolean): void {
  if (typeof window === "undefined") return;
  window.localStorage.setItem(SFX_STORAGE_KEY, enabled ? "true" : "false");
  window.dispatchEvent(new CustomEvent(SFX_CHANGE_EVENT, { detail: enabled }));
}

function playTone(
  ctx: AudioContext,
  frequency: number,
  startOffset: number,
  duration: number,
  type: OscillatorType = "sine",
  peakGain = 0.14,
  endFrequency?: number
) {
  const now = ctx.currentTime + startOffset;
  const osc = ctx.createOscillator();
  const gain = ctx.createGain();

  osc.type = type;
  osc.frequency.setValueAtTime(frequency, now);
  if (endFrequency !== undefined) {
    osc.frequency.exponentialRampToValueAtTime(Math.max(40, endFrequency), now + duration);
  }

  gain.gain.setValueAtTime(0.0001, now);
  gain.gain.linearRampToValueAtTime(peakGain, now + 0.012);
  gain.gain.exponentialRampToValueAtTime(0.0001, now + duration);

  osc.connect(gain);
  gain.connect(ctx.destination);

  osc.start(now);
  osc.stop(now + duration + 0.02);
}

/**
 * Pleasant ascending triad C5 -> E5 -> G5; supports optional combo pitch boost.
 */
export function playCorrect(combo = 1): void {
  if (!isSfxEnabled()) return;
  const ctx = ensureAudioContext();
  if (!ctx) return;
  const semitoneBoost = Math.min(Math.max(combo - 1, 0), 6);
  const ratio = Math.pow(2, semitoneBoost / 12);
  playTone(ctx, 523.25 * ratio, 0, 0.1, "triangle", 0.13);
  playTone(ctx, 659.25 * ratio, 0.065, 0.11, "triangle", 0.14);
  playTone(ctx, 783.99 * ratio, 0.13, 0.16, "sine", 0.15);
}

/**
 * Gentle low descending tone 220Hz -> 165Hz.
 */
export function playWrong(): void {
  if (!isSfxEnabled()) return;
  const ctx = ensureAudioContext();
  if (!ctx) return;
  playTone(ctx, 220, 0, 0.14, "sawtooth", 0.07, 185);
  playTone(ctx, 185, 0.11, 0.18, "triangle", 0.1, 165);
}

/**
 * Soft paper swoosh/flip sound for 3D flashcards.
 */
export function playFlip(): void {
  if (!isSfxEnabled()) return;
  const ctx = ensureAudioContext();
  if (!ctx) return;
  playTone(ctx, 320, 0, 0.075, "sine", 0.06, 520);
}

/**
 * Light tactile UI click for word tiles / chips / keys.
 */
export function playClick(): void {
  if (!isSfxEnabled()) return;
  const ctx = ensureAudioContext();
  if (!ctx) return;
  playTone(ctx, 620, 0, 0.04, "sine", 0.055, 480);
}

/**
 * Crisp countdown tick for the final 5 seconds.
 */
export function playTick(urgent = false): void {
  if (!isSfxEnabled()) return;
  const ctx = ensureAudioContext();
  if (!ctx) return;
  playTone(ctx, urgent ? 980 : 820, 0, 0.055, "square", 0.045);
}

/**
 * Short joyful fanfare when completing a game, quiz, or study session.
 */
export function playVictory(): void {
  if (!isSfxEnabled()) return;
  const ctx = ensureAudioContext();
  if (!ctx) return;
  playTone(ctx, 523.25, 0, 0.11, "triangle", 0.13);
  playTone(ctx, 659.25, 0.09, 0.11, "triangle", 0.13);
  playTone(ctx, 783.99, 0.18, 0.12, "triangle", 0.14);
  playTone(ctx, 1046.5, 0.28, 0.28, "sine", 0.16);
}

function subscribeSfx(callback: () => void): () => void {
  if (typeof window === "undefined") return () => {};
  const handleStorage = (event: StorageEvent) => {
    if (event.key === SFX_STORAGE_KEY) callback();
  };
  window.addEventListener(SFX_CHANGE_EVENT, callback);
  window.addEventListener("storage", handleStorage);
  return () => {
    window.removeEventListener(SFX_CHANGE_EVENT, callback);
    window.removeEventListener("storage", handleStorage);
  };
}

export function useSoundEffects() {
  const enabled = useSyncExternalStore(subscribeSfx, isSfxEnabled, () => true);

  useEffect(() => {
    bindUnlockListeners();
  }, []);

  const toggleSound = useCallback(() => {
    userHasInteracted = true;
    const next = !isSfxEnabled();
    setSfxEnabled(next);
    if (next) {
      playClick();
    }
  }, []);

  return {
    sfxEnabled: enabled,
    toggleSound,
    playCorrect,
    playWrong,
    playFlip,
    playClick,
    playTick,
    playVictory,
  };
}
