// anim.js - reveal on scroll for [data-reveal] elements
window.initReveal = function() {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
    const els = document.querySelectorAll('[data-reveal]');
    const io = new window.IntersectionObserver((entries, obs) => {
        entries.forEach((entry, i) => {
            if (entry.isIntersecting) {
                setTimeout(() => {
                    entry.target.classList.add('animate-in');
                }, i * 120); // stagger
                obs.unobserve(entry.target);
            }
        });
    }, { threshold: 0.15 });
    els.forEach(el => io.observe(el));
}
