"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { usePathname } from "next/navigation";
import { ArrowLeft, ArrowRight, MousePointerClick, Sparkles, X } from "lucide-react";
import { useLanguage } from "@/lib/language-context";
import {
  getTourStepsForPage,
  getTourStorageKey,
  isAutoTourPage,
  resolveTourPageKey,
  type SpotlightPlacement,
  type SpotlightStep,
  type TourPageKey,
} from "@/lib/tour-steps";

export const ONBOARDING_STORAGE_KEY = "lingual_spotlight_v1_home";
export const OPEN_ONBOARDING_EVENT = "lingual:open-onboarding";

export function triggerOnboardingTour() {
  if (typeof window !== "undefined") {
    window.dispatchEvent(new CustomEvent(OPEN_ONBOARDING_EVENT));
  }
}

interface CutoutRect {
  left: number;
  top: number;
  width: number;
  height: number;
  radius: number;
}

interface PopoverCoords {
  left: number;
  top: number;
  placement: "top" | "bottom" | "left" | "right" | "center";
  arrowOffset: number;
  isMobileSheet: boolean;
  mobileDockTop?: boolean;
}

const POPOVER_GAP = 16;
const VIEWPORT_MARGIN = 14;
const MOBILE_BREAKPOINT = 640;

function isElementVisible(el: HTMLElement): boolean {
  const rect = el.getBoundingClientRect();
  if (rect.width <= 0 || rect.height <= 0) return false;
  const style = window.getComputedStyle(el);
  return style.display !== "none" && style.visibility !== "hidden" && style.opacity !== "0";
}

function collectAvailableSteps(pageKey: TourPageKey): SpotlightStep[] {
  const candidates = getTourStepsForPage(pageKey);
  const available = candidates.filter((step) => {
    const el = document.querySelector<HTMLElement>(step.target);
    return el !== null && isElementVisible(el);
  });
  if (available.length > 0) return available;
  // Graceful fallback to nav spotlight if page sections haven't mounted
  return getTourStepsForPage("fallback").filter((step) => {
    const el = document.querySelector<HTMLElement>(step.target);
    return el !== null && isElementVisible(el);
  });
}

function computeCutoutRect(el: HTMLElement, step: SpotlightStep): CutoutRect {
  const rect = el.getBoundingClientRect();
  const pad = step.padding ?? 8;
  const radius = step.radius ?? 16;
  const vw = window.innerWidth;
  const vh = window.innerHeight;

  const left = Math.max(4, rect.left - pad);
  const top = Math.max(4, rect.top - pad);
  const right = Math.min(vw - 4, rect.right + pad);
  const bottom = Math.min(vh - 4, rect.bottom + pad);

  return {
    left,
    top,
    width: Math.max(32, right - left),
    height: Math.max(32, bottom - top),
    radius,
  };
}

function computePopoverCoords(
  cutout: CutoutRect,
  popW: number,
  popH: number,
  preferred: SpotlightPlacement = "auto"
): PopoverCoords {
  const vw = window.innerWidth;
  const vh = window.innerHeight;

  if (vw <= MOBILE_BREAKPOINT) {
    const targetCenterY = cutout.top + cutout.height / 2;
    const dockTop = targetCenterY > vh * 0.55;
    return {
      left: 12,
      top: dockTop ? VIEWPORT_MARGIN : Math.max(VIEWPORT_MARGIN, vh - popH - 14),
      placement: "center",
      arrowOffset: 0,
      isMobileSheet: true,
      mobileDockTop: dockTop,
    };
  }

  const spaceBottom = vh - (cutout.top + cutout.height) - VIEWPORT_MARGIN;
  const spaceTop = cutout.top - VIEWPORT_MARGIN;
  const spaceRight = vw - (cutout.left + cutout.width) - VIEWPORT_MARGIN;
  const spaceLeft = cutout.left - VIEWPORT_MARGIN;

  const fits: Record<"top" | "bottom" | "left" | "right", boolean> = {
    bottom: spaceBottom >= popH + POPOVER_GAP,
    top: spaceTop >= popH + POPOVER_GAP,
    right: spaceRight >= popW + POPOVER_GAP,
    left: spaceLeft >= popW + POPOVER_GAP,
  };

  let chosen: "top" | "bottom" | "left" | "right" | "center" = "bottom";

  if (preferred !== "auto" && fits[preferred]) {
    chosen = preferred;
  } else {
    const order: ("bottom" | "top" | "right" | "left")[] = ["bottom", "top", "right", "left"];
    const firstFit = order.find((side) => fits[side]);
    if (firstFit) {
      chosen = firstFit;
    } else {
      // Element fills most of the viewport: dock smoothly at bottom-right inside viewport
      return {
        left: Math.max(VIEWPORT_MARGIN, vw - popW - 22),
        top: Math.max(VIEWPORT_MARGIN, vh - popH - 22),
        placement: "center",
        arrowOffset: 0,
        isMobileSheet: false,
      };
    }
  }

  const targetCenterX = cutout.left + cutout.width / 2;
  const targetCenterY = cutout.top + cutout.height / 2;

  let left = VIEWPORT_MARGIN;
  let top = VIEWPORT_MARGIN;
  let arrowOffset = 24;

  if (chosen === "bottom") {
    top = cutout.top + cutout.height + POPOVER_GAP;
    left = Math.min(Math.max(VIEWPORT_MARGIN, targetCenterX - popW / 2), vw - popW - VIEWPORT_MARGIN);
    arrowOffset = Math.min(Math.max(24, targetCenterX - left), popW - 24);
  } else if (chosen === "top") {
    top = cutout.top - popH - POPOVER_GAP;
    left = Math.min(Math.max(VIEWPORT_MARGIN, targetCenterX - popW / 2), vw - popW - VIEWPORT_MARGIN);
    arrowOffset = Math.min(Math.max(24, targetCenterX - left), popW - 24);
  } else if (chosen === "right") {
    left = cutout.left + cutout.width + POPOVER_GAP;
    top = Math.min(Math.max(VIEWPORT_MARGIN, targetCenterY - popH / 2), vh - popH - VIEWPORT_MARGIN);
    arrowOffset = Math.min(Math.max(24, targetCenterY - top), popH - 24);
  } else if (chosen === "left") {
    left = cutout.left - popW - POPOVER_GAP;
    top = Math.min(Math.max(VIEWPORT_MARGIN, targetCenterY - popH / 2), vh - popH - VIEWPORT_MARGIN);
    arrowOffset = Math.min(Math.max(24, targetCenterY - top), popH - 24);
  }

  return {
    left: Math.min(Math.max(VIEWPORT_MARGIN, left), vw - popW - VIEWPORT_MARGIN),
    top: Math.min(Math.max(VIEWPORT_MARGIN, top), vh - popH - VIEWPORT_MARGIN),
    placement: chosen,
    arrowOffset,
    isMobileSheet: false,
  };
}

export default function OnboardingTour() {
  const pathname = usePathname();
  const { translate: t, language } = useLanguage();

  const [isOpen, setIsOpen] = useState(false);
  const [activePageKey, setActivePageKey] = useState<TourPageKey>("home");
  const [steps, setSteps] = useState<SpotlightStep[]>([]);
  const [stepIndex, setStepIndex] = useState(0);
  const [cutout, setCutout] = useState<CutoutRect | null>(null);
  const [popoverPos, setPopoverPos] = useState<PopoverCoords | null>(null);

  const popoverRef = useRef<HTMLDivElement | null>(null);
  const activeStep = steps[stepIndex] ?? null;

  const closeTour = useCallback(
    (persist = true) => {
      if (persist && typeof window !== "undefined") {
        window.localStorage.setItem(getTourStorageKey(activePageKey), "true");
      }
      setIsOpen(false);
      setCutout(null);
      setPopoverPos(null);
    },
    [activePageKey]
  );

  const startTourForPath = useCallback((path: string) => {
    const pageKey = resolveTourPageKey(path);
    const available = collectAvailableSteps(pageKey);
    if (available.length === 0) return;
    setActivePageKey(pageKey);
    setSteps(available);
    setStepIndex(0);
    setIsOpen(true);
  }, []);

  const nextStep = useCallback(() => {
    if (stepIndex >= steps.length - 1) {
      closeTour(true);
      return;
    }
    setStepIndex((idx) => Math.min(steps.length - 1, idx + 1));
  }, [closeTour, stepIndex, steps.length]);

  const prevStep = useCallback(() => {
    setStepIndex((idx) => Math.max(0, idx - 1));
  }, []);

  // Auto-open per-page spotlight tour on first visit (and close tour when route changes)
  useEffect(() => {
    const closeTimer = window.setTimeout(() => {
      setIsOpen(false);
    }, 0);

    const pageKey = resolveTourPageKey(pathname);
    if (!isAutoTourPage(pageKey)) {
      return () => window.clearTimeout(closeTimer);
    }

    const done = window.localStorage.getItem(getTourStorageKey(pageKey));
    if (done === "true") {
      return () => window.clearTimeout(closeTimer);
    }

    const openTimer = window.setTimeout(() => {
      startTourForPath(pathname);
    }, 620);

    return () => {
      window.clearTimeout(closeTimer);
      window.clearTimeout(openTimer);
    };
  }, [pathname, startTourForPath]);

  // Manual trigger via the (?) button in AppNavigation
  useEffect(() => {
    const onOpen = () => {
      startTourForPath(pathname);
    };
    window.addEventListener(OPEN_ONBOARDING_EVENT, onOpen);
    return () => window.removeEventListener(OPEN_ONBOARDING_EVENT, onOpen);
  }, [pathname, startTourForPath]);

  // Recalculate cutout & popover position whenever step changes, scrolls, or resizes
  const measureCurrentTarget = useCallback(() => {
    if (!isOpen || !activeStep) return;
    const el = document.querySelector<HTMLElement>(activeStep.target);
    if (!el || !isElementVisible(el)) {
      // Skip missing element gracefully
      if (stepIndex < steps.length - 1) {
        setStepIndex((idx) => idx + 1);
      } else {
        closeTour(true);
      }
      return;
    }

    const nextCutout = computeCutoutRect(el, activeStep);
    setCutout(nextCutout);

    const popW = popoverRef.current?.offsetWidth || Math.min(368, window.innerWidth - 28);
    const popH = popoverRef.current?.offsetHeight || 230;
    setPopoverPos(computePopoverCoords(nextCutout, popW, popH, activeStep.placement ?? "auto"));
  }, [activeStep, closeTour, isOpen, stepIndex, steps.length]);

  // Scroll target smoothly into viewport on step change & animate cutout smoothly during scroll
  useEffect(() => {
    if (!isOpen || !activeStep) return;
    const el = document.querySelector<HTMLElement>(activeStep.target);
    if (!el || !isElementVisible(el)) {
      const skipTimer = window.setTimeout(() => {
        if (stepIndex < steps.length - 1) {
          setStepIndex((idx) => idx + 1);
        } else {
          closeTour(true);
        }
      }, 0);
      return () => window.clearTimeout(skipTimer);
    }

    const rect = el.getBoundingClientRect();
    const isNearViewport =
      rect.top >= 78 &&
      rect.bottom <= window.innerHeight - 28 &&
      rect.height < window.innerHeight * 0.78;

    if (!isNearViewport) {
      if (rect.height > window.innerHeight * 0.65) {
        // Large section: align its top comfortably below the sticky navbar
        const targetTop = Math.max(0, window.scrollY + rect.top - 92);
        window.scrollTo({ top: targetTop, behavior: "smooth" });
      } else {
        el.scrollIntoView({ behavior: "smooth", block: "center", inline: "nearest" });
      }
    }

    let rafId = 0;
    const startTime = performance.now();
    const trackDuringScroll = (now: number) => {
      measureCurrentTarget();
      if (now - startTime < 520) {
        rafId = window.requestAnimationFrame(trackDuringScroll);
      }
    };
    rafId = window.requestAnimationFrame(trackDuringScroll);

    const resizeObserver = new ResizeObserver(() => measureCurrentTarget());
    resizeObserver.observe(el);

    window.addEventListener("scroll", measureCurrentTarget, true);
    window.addEventListener("resize", measureCurrentTarget);

    return () => {
      window.cancelAnimationFrame(rafId);
      resizeObserver.disconnect();
      window.removeEventListener("scroll", measureCurrentTarget, true);
      window.removeEventListener("resize", measureCurrentTarget);
    };
  }, [activeStep, closeTour, isOpen, measureCurrentTarget, stepIndex, steps.length]);

  // Re-measure popover height after text content updates (including language switch)
  useEffect(() => {
    if (!isOpen || !cutout || !activeStep) return;
    const id = window.requestAnimationFrame(() => {
      if (!popoverRef.current) return;
      const popW = popoverRef.current.offsetWidth || 368;
      const popH = popoverRef.current.offsetHeight || 230;
      const nextPos = computePopoverCoords(cutout, popW, popH, activeStep.placement ?? "auto");
      setPopoverPos(nextPos);
    });
    return () => window.cancelAnimationFrame(id);
  }, [isOpen, cutout, activeStep, language]);

  // Keyboard navigation (Right/Enter/Space = next, Left = prev, Esc = close)
  useEffect(() => {
    if (!isOpen) return;
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        e.preventDefault();
        closeTour(true);
      } else if (e.key === "ArrowRight" || e.key === "Enter" || e.key === " ") {
        e.preventDefault();
        nextStep();
      } else if (e.key === "ArrowLeft") {
        e.preventDefault();
        prevStep();
      }
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [closeTour, isOpen, nextStep, prevStep]);

  if (!isOpen || !activeStep || !cutout || typeof document === "undefined") {
    return null;
  }

  const isFirst = stepIndex === 0;
  const isLast = stepIndex === steps.length - 1;
  const progressPct = Math.round(((stepIndex + 1) / steps.length) * 100);
  const title = language === "vi" ? activeStep.titleVi : activeStep.titleEn;
  const desc = language === "vi" ? activeStep.descVi : activeStep.descEn;
  const badge = language === "vi" ? activeStep.badgeVi : activeStep.badgeEn;
  const tip = language === "vi" ? activeStep.tipVi : activeStep.tipEn;

  const arrowStyle: React.CSSProperties | undefined =
    popoverPos && !popoverPos.isMobileSheet && popoverPos.placement !== "center"
      ? popoverPos.placement === "top" || popoverPos.placement === "bottom"
        ? { left: `${popoverPos.arrowOffset}px` }
        : { top: `${popoverPos.arrowOffset}px` }
      : undefined;

  return createPortal(
    <div className="spotlight-root" role="presentation">
      {/* Click-anywhere backdrop catcher: clicking outside the popover smoothly advances */}
      <div
        className="spotlight-click-layer"
        onClick={nextStep}
        aria-label={t("Nhấn chuột để chuyển sang hướng dẫn tiếp theo", "Click anywhere to continue walkthrough")}
      />

      {/* Morphing spotlight hole + illuminated highlight ring */}
      <div
        className="spotlight-cutout"
        aria-hidden="true"
        style={{
          left: `${cutout.left}px`,
          top: `${cutout.top}px`,
          width: `${cutout.width}px`,
          height: `${cutout.height}px`,
          borderRadius: `${cutout.radius}px`,
        }}
      >
        <span className="spotlight-pulse-ring" style={{ borderRadius: `${cutout.radius}px` }} />
      </div>

      {/* Contextual Popover Tooltip */}
      <section
        ref={popoverRef}
        className={`spotlight-popover ${popoverPos?.isMobileSheet ? (popoverPos.mobileDockTop ? "is-mobile-sheet is-mobile-top" : "is-mobile-sheet") : ""} placement-${popoverPos?.placement ?? "bottom"}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="spotlight-tour-title"
        style={
          popoverPos
            ? popoverPos.isMobileSheet
              ? undefined
              : {
                  left: `${popoverPos.left}px`,
                  top: `${popoverPos.top}px`,
                }
            : { opacity: 0, pointerEvents: "none" }
        }
        onClick={(e) => e.stopPropagation()}
      >
        {popoverPos && !popoverPos.isMobileSheet && popoverPos.placement !== "center" && (
          <span
            className={`spotlight-arrow arrow-${popoverPos.placement}`}
            style={arrowStyle}
            aria-hidden="true"
          />
        )}

        <div className="spotlight-popover-top">
          <div className="spotlight-badge-group">
            <span className="tutorial-step-pill">
              <Sparkles size={12} aria-hidden="true" />
              {t(`Bước ${stepIndex + 1} / ${steps.length}`, `Step ${stepIndex + 1} / ${steps.length}`)}
            </span>
            <span className="spotlight-eyebrow">{badge}</span>
          </div>
          <button
            type="button"
            className="tutorial-close-btn"
            onClick={() => closeTour(true)}
            aria-label={t("Đóng hướng dẫn", "Close walkthrough")}
            title={t("Đóng (Esc)", "Close (Esc)")}
          >
            <X size={15} />
          </button>
        </div>

        <div className="tutorial-progress-bar" aria-hidden="true">
          <span style={{ width: `${progressPct}%` }} />
        </div>

        <div className="spotlight-body">
          <h2 id="spotlight-tour-title">{title}</h2>
          <p className="spotlight-desc">{desc}</p>
          {tip && <div className="spotlight-tip">{tip}</div>}
        </div>

        <div className="spotlight-click-hint">
          <MousePointerClick size={13} aria-hidden="true" />
          <span>
            {isLast
              ? t("Nhấn chuột bất kỳ đâu để hoàn tất hướng dẫn", "Click anywhere to finish the guide")
              : t("Nhấn chuột bất kỳ đâu trên nền tối để tiếp tục", "Click anywhere on the screen to continue")}
          </span>
        </div>

        <div className="spotlight-footer">
          <button
            type="button"
            className="tutorial-skip-btn"
            onClick={() => closeTour(true)}
          >
            {t("Bỏ qua", "Skip")}
          </button>

          <div className="spotlight-dots" aria-hidden="true">
            {steps.map((s, idx) => (
              <button
                key={s.id}
                type="button"
                className={`tutorial-dot ${idx === stepIndex ? "is-active" : ""}`}
                onClick={() => setStepIndex(idx)}
                tabIndex={-1}
              />
            ))}
          </div>

          <div className="tutorial-nav-btns">
            {!isFirst && (
              <button
                type="button"
                className="tutorial-secondary-btn spotlight-btn-compact"
                onClick={prevStep}
                aria-label={t("Bước trước", "Previous step")}
              >
                <ArrowLeft size={14} />
              </button>
            )}
            <button
              type="button"
              className="tutorial-primary-btn spotlight-btn-compact"
              onClick={nextStep}
            >
              <span>{isLast ? t("Hoàn tất", "Got it") : t("Tiếp", "Next")}</span>
              {!isLast && <ArrowRight size={14} />}
            </button>
          </div>
        </div>
      </section>
    </div>,
    document.body
  );
}
