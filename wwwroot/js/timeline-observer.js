/**
 * Intersection Observer for the "How I Work" timeline.
 * Adds .animate-in to elements as they scroll into the viewport.
 */

let observer = null;

export function initTimelineObserver() {
    const targets = document.querySelectorAll(
        '.timeline-heading, .timeline-stage, .timeline-badge'
    );

    if (!targets.length) return;

    observer = new IntersectionObserver(
        (entries) => {
            entries.forEach((entry) => {
                if (entry.isIntersecting) {
                    // Double-rAF ensures browser has painted the initial state
                    // before we trigger the transition to the final state
                    requestAnimationFrame(() => {
                        requestAnimationFrame(() => {
                            entry.target.classList.add('animate-in');
                        });
                    });
                    observer.unobserve(entry.target);
                }
            });
        },
        { threshold: 0.15, rootMargin: '0px 0px -60px 0px' }
    );

    targets.forEach((el) => observer.observe(el));
}

export function destroyTimelineObserver() {
    observer?.disconnect();
    observer = null;
}
