// PharmaCore UI interactions
(function () {
    "use strict";

    function ready(fn) {
        if (document.readyState !== "loading") { fn(); }
        else { document.addEventListener("DOMContentLoaded", fn); }
    }

    /* ---------- Sticky header shadow ---------- */
    function initStickyHeader() {
        var header = document.querySelector(".ph-header");
        if (!header) { return; }
        var onScroll = function () {
            header.classList.toggle("is-stuck", window.scrollY > 8);
        };
        window.addEventListener("scroll", onScroll, { passive: true });
        onScroll();
    }

    /* ---------- Mobile navigation ---------- */
    function initNav() {
        var toggle = document.querySelector(".ph-nav-toggle");
        var links = document.querySelector(".ph-nav-links");
        if (!toggle || !links) { return; }

        toggle.addEventListener("click", function () {
            var open = links.classList.toggle("is-open");
            toggle.classList.toggle("is-open", open);
            toggle.setAttribute("aria-expanded", open ? "true" : "false");
        });

        links.addEventListener("click", function (e) {
            if (e.target.closest("a")) {
                links.classList.remove("is-open");
                toggle.classList.remove("is-open");
                toggle.setAttribute("aria-expanded", "false");
            }
        });
    }

    /* ---------- Admin sidebar (mobile) ---------- */
    function initAdminSidebar() {
        var toggle = document.querySelector(".ph-sidebar-toggle");
        var sidebar = document.querySelector(".ph-sidebar");
        var backdrop = document.querySelector(".ph-sidebar-backdrop");
        if (!toggle || !sidebar) { return; }

        var close = function () {
            sidebar.classList.remove("is-open");
            if (backdrop) { backdrop.classList.remove("is-open"); }
        };

        toggle.addEventListener("click", function () {
            sidebar.classList.add("is-open");
            if (backdrop) { backdrop.classList.add("is-open"); }
        });

        if (backdrop) { backdrop.addEventListener("click", close); }

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") { close(); }
        });
    }

    /* ---------- Scroll reveal ---------- */
    function initReveal() {
        var items = document.querySelectorAll(".ph-reveal");
        if (!items.length) { return; }

        var revealAll = function () {
            items.forEach(function (el) { el.classList.add("is-visible"); });
        };

        if (!("IntersectionObserver" in window)) {
            revealAll();
            return;
        }

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add("is-visible");
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.08, rootMargin: "0px 0px -30px 0px" });

        items.forEach(function (el, i) {
            el.style.transitionDelay = (Math.min(i % 4, 3) * 70) + "ms";
            observer.observe(el);
        });

        window.setTimeout(revealAll, 2000);
    }

    /* ---------- Animated counters ---------- */
    function initCounters() {
        var counters = document.querySelectorAll("[data-count]");
        if (!counters.length) { return; }

        var run = function (el) {
            var target = parseFloat(el.getAttribute("data-count")) || 0;
            var suffix = el.getAttribute("data-suffix") || "";
            var duration = 1400;
            var start = null;

            var step = function (ts) {
                if (!start) { start = ts; }
                var progress = Math.min((ts - start) / duration, 1);
                var eased = 1 - Math.pow(1 - progress, 3);
                var value = target * eased;
                el.textContent = (Number.isInteger(target)
                    ? Math.round(value).toLocaleString()
                    : value.toFixed(1)) + suffix;
                if (progress < 1) { requestAnimationFrame(step); }
            };

            requestAnimationFrame(step);
        };

        if (!("IntersectionObserver" in window)) {
            counters.forEach(run);
            return;
        }

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    run(entry.target);
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.4 });

        counters.forEach(function (el) { observer.observe(el); });
    }

    /* ---------- Animated bar charts ---------- */
    function initBars() {
        var bars = document.querySelectorAll(".ph-bar");
        if (!bars.length) { return; }

        var paint = function (bar) {
            var value = (bar.getAttribute("data-height") || "30%").trim();
            if (!/^\d+(\.\d+)?%$/.test(value)) {
                value = "30%";
            }
            bar.style.setProperty("--h", value);
        };

        if (!("IntersectionObserver" in window)) {
            bars.forEach(paint);
            return;
        }

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    paint(entry.target);
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.3 });

        bars.forEach(function (bar) { observer.observe(bar); });
    }

    /* ---------- Modals ---------- */
    function initModals() {
        var openers = document.querySelectorAll("[data-modal-open]");
        var closers = document.querySelectorAll("[data-modal-close]");

        var close = function (modal) {
            if (!modal) { return; }
            modal.classList.remove("is-open");
            document.body.style.overflow = "";
        };

        openers.forEach(function (btn) {
            btn.addEventListener("click", function () {
                var id = btn.getAttribute("data-modal-open");
                var modal = document.getElementById(id);
                if (!modal) { return; }
                modal.classList.add("is-open");
                document.body.style.overflow = "hidden";
                var first = modal.querySelector("input, select, textarea");
                if (first) { setTimeout(function () { first.focus(); }, 120); }
            });
        });

        closers.forEach(function (btn) {
            btn.addEventListener("click", function () {
                close(btn.closest(".ph-modal"));
            });
        });

        document.querySelectorAll(".ph-modal").forEach(function (modal) {
            modal.addEventListener("click", function (e) {
                if (e.target === modal) { close(modal); }
            });
        });

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") {
                document.querySelectorAll(".ph-modal.is-open").forEach(close);
            }
        });
    }

    /* ---------- Active nav highlighting ---------- */
    function initActiveNav() {
        var path = window.location.pathname.toLowerCase();
        var links = document.querySelectorAll("[data-nav-match] a, .ph-sidebar-nav a");
        links.forEach(function (link) {
            var href = (link.getAttribute("href") || "").toLowerCase();
            if (!href || href === "/" || href === "#") { return; }
            if (path === href || path.startsWith(href + "/")) {
                link.classList.add("is-active");
            }
        });
    }

    /* ---------- Password visibility toggle ---------- */
    function initPasswordToggle() {
        document.querySelectorAll("[data-toggle-password]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var input = document.getElementById(btn.getAttribute("data-toggle-password"));
                if (!input) { return; }
                var isText = input.type === "text";
                input.type = isText ? "password" : "text";
                btn.querySelector("i").className = isText ? "bi bi-eye" : "bi bi-eye-slash";
            });
        });
    }

    /* ---------- Table search filter ---------- */
    function initTableFilter() {
        var input = document.querySelector("[data-table-search]");
        if (!input) { return; }
        var table = document.getElementById(input.getAttribute("data-table-search"));
        if (!table) { return; }

        input.addEventListener("input", function () {
            var term = input.value.toLowerCase().trim();
            table.querySelectorAll("tbody tr").forEach(function (row) {
                row.style.display = row.textContent.toLowerCase().indexOf(term) > -1 ? "" : "none";
            });
        });
    }

    ready(function () {
        initStickyHeader();
        initNav();
        initAdminSidebar();
        initReveal();
        initCounters();
        initBars();
        initModals();
        initActiveNav();
        initPasswordToggle();
        initTableFilter();
    });
})();
