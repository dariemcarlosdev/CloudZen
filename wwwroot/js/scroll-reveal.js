// ══════════════════════════════════════════════════════════════
// CloudZen scroll-reveal — adds .is-visible to .reveal* elements
// when they enter the viewport. Idempotent: safe to call per page.
// Exposed as window.initScrollReveal() for Blazor JS interop.
// ══════════════════════════════════════════════════════════════
(function () {
    let observer = null;

    function ensureObserver() {
        if (observer) return observer;

        // No IntersectionObserver (or reduced motion) → show everything immediately.
        const reduceMotion = window.matchMedia &&
            window.matchMedia('(prefers-reduced-motion: reduce)').matches;

        if (!('IntersectionObserver' in window) || reduceMotion) {
            observer = {
                observe: function (el) { el.classList.add('is-visible'); }
            };
            return observer;
        }

        observer = new IntersectionObserver(function (entries, obs) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('is-visible');
                    obs.unobserve(entry.target);
                }
            });
        }, { threshold: 0.12, rootMargin: '0px 0px -8% 0px' });

        return observer;
    }

    window.initScrollReveal = function () {
        const obs = ensureObserver();
        const targets = document.querySelectorAll(
            '.reveal:not(.is-visible), .reveal-left:not(.is-visible), ' +
            '.reveal-right:not(.is-visible), .reveal-scale:not(.is-visible)'
        );
        targets.forEach(function (el) { obs.observe(el); });
    };
})();
