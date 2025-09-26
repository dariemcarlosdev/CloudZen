// anim.js - reveal on scroll for [data-reveal] elements
// Last significant change: Added scrollToElementById for Blazor interop to scroll to Highlighted Projects section in WhoIAm.razor when navigated from CaseStudies.razor.

/**
 * Initializes reveal animations for elements with the [data-reveal] attribute.
 * Uses IntersectionObserver to add the 'animate-in' class when elements enter the viewport.
 * Staggers the animation for each element.
 */
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

/**
 * Triggers a download of a file with the given filename and base64-encoded content.
 * Used by Blazor WebAssembly to save files (e.g., PDF resume) to the user's device.
 * @param {string} filename - The name for the downloaded file.
 * @param {string} bytesBase64 - The base64-encoded file content.
 */
window.saveAsFile = function (filename, bytes) {
    const blob = new Blob([bytes], { type: "application/pdf" });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(link.href);
}

//window.scrollToHero = function () {
//    var hero = document.getElementById('hero');
//    if (hero) {
//        hero.scrollIntoView({ behavior: 'smooth', block: 'start' });
//    }
//};


window.scrollToHero = function () {
    var hero = document.getElementById('hero');
    var header = document.querySelector('header');
    if (hero) {
        var headerHeight = header ? header.offsetHeight : 0;
        var heroTop = hero.getBoundingClientRect().top + window.pageYOffset - headerHeight;
        window.scrollTo({ top: heroTop, behavior: 'smooth' });
    }
};
window.scrollToHero = function () {
    var hero = document.getElementById('hero');
    var header = document.querySelector('header');
    if (hero) {
        var headerHeight = header ? header.offsetHeight : 0;
        var heroTop = hero.getBoundingClientRect().top + window.pageYOffset - headerHeight;
        window.scrollTo({ top: heroTop, behavior: 'smooth' });
    }
};

// Last significant change: Added for Blazor navigation from CaseStudies.razor to WhoIAm.razor Highlighted Projects section.
window.scrollToElementById = function (id) {
    var el = document.getElementById(id);
    if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
};
