/* ====================================================================
   CYBER-CINEMATIC MILITARY HUD INTERACTIVE CONTROLLER
   DigiPOSE ERP Client-Side Telemetry & UI Handler
   ==================================================================== */

document.addEventListener("DOMContentLoaded", function () {
    // 1. Sidebar Toggle Handling (Default is SHOWN; toggle hides completely)
    const sidebarToggleBtn = document.getElementById("sidebarToggle");
    const savedSidebarState = localStorage.getItem("digipose_sidebar_collapsed");

    // Default is shown unless explicitly saved as collapsed
    if (savedSidebarState === "true") {
        document.body.classList.add("sidebar-collapsed");
    } else {
        document.body.classList.remove("sidebar-collapsed");
    }

    if (sidebarToggleBtn) {
        sidebarToggleBtn.addEventListener("click", function () {
            document.body.classList.toggle("sidebar-collapsed");
            const isCollapsed = document.body.classList.contains("sidebar-collapsed");
            localStorage.setItem("digipose_sidebar_collapsed", isCollapsed ? "true" : "false");
        });
    }

    // 2. Theme Toggle Handling (Cyber Void vs Cyber Holographic - Icon Only)
    const themeToggleBtn = document.getElementById("themeToggleBtn");
    const savedTheme = localStorage.getItem("digipose_theme");

    if (savedTheme === "light") {
        document.body.classList.add("light-theme");
        if (themeToggleBtn) {
            themeToggleBtn.innerHTML = '<i class="fa-solid fa-moon"></i>';
        }
    }

    if (themeToggleBtn) {
        themeToggleBtn.addEventListener("click", function () {
            document.body.classList.toggle("light-theme");
            const isLight = document.body.classList.contains("light-theme");
            localStorage.setItem("digipose_theme", isLight ? "light" : "dark");
            themeToggleBtn.innerHTML = isLight ? '<i class="fa-solid fa-moon"></i>' : '<i class="fa-solid fa-sun"></i>';
        });
    }

    // 3. Language Selector Toggle (Icon + Text Label)
    const langToggleBtn = document.getElementById("langToggleBtn");
    const langText = document.getElementById("langText");
    let currentLang = localStorage.getItem("digipose_lang") || "EN";

    function applyLangState(lang) {
        if (langText) {
            langText.textContent = lang === "VI" ? "Tiếng Việt" : "English";
        }
        if (langToggleBtn) {
            langToggleBtn.title = `Switch Language (Current: ${lang === "VI" ? "Tiếng Việt" : "English"})`;
        }
    }
    applyLangState(currentLang);

    if (langToggleBtn) {
        langToggleBtn.addEventListener("click", function () {
            currentLang = currentLang === "EN" ? "VI" : "EN";
            localStorage.setItem("digipose_lang", currentLang);
            applyLangState(currentLang);
        });
    }

    // 3b. Profile Dropdown Caret Single Icon Toggle (Up when open, Down when closed)
    const profileDropdownEl = document.getElementById("profileDropdown");
    const profileCaret = document.getElementById("hudProfileCaret");
    if (profileDropdownEl && profileCaret) {
        const parentDropdown = profileDropdownEl.closest('.dropdown');
        if (parentDropdown) {
            parentDropdown.addEventListener('show.bs.dropdown', function () {
                profileCaret.className = "fa-solid fa-caret-up text-cyan ms-2";
            });
            parentDropdown.addEventListener('hide.bs.dropdown', function () {
                profileCaret.className = "fa-solid fa-caret-down text-cyan ms-2";
            });
        }
    }

    // 4. Live Telemetry Clock in Footer
    const footerClock = document.getElementById("hudFooterClock");
    function updateClock() {
        if (!footerClock) return;
        const now = new Date();
        const year = now.getFullYear();
        const month = String(now.getMonth() + 1).padStart(2, "0");
        const day = String(now.getDate()).padStart(2, "0");
        const hours = String(now.getHours()).padStart(2, "0");
        const mins = String(now.getMinutes()).padStart(2, "0");
        const secs = String(now.getSeconds()).padStart(2, "0");
        const ms = String(Math.floor(now.getMilliseconds() / 10)).padStart(2, "0");
        footerClock.textContent = `${year}-${month}-${day} ${hours}:${mins}:${secs}.${ms} UTC+7`;
    }
    updateClock();
    setInterval(updateClock, 50);

    // 5. Active Menu Highlight based on current URL path
    const currentPath = window.location.pathname.toLowerCase();
    const menuLinks = document.querySelectorAll(".hud-menu-link");

    menuLinks.forEach(link => {
        const href = link.getAttribute("href");
        if (href && href !== "#") {
            const cleanHref = href.toLowerCase();
            if (currentPath === cleanHref || (cleanHref !== "/" && currentPath.startsWith(cleanHref))) {
                link.classList.add("active");
            } else {
                link.classList.remove("active");
            }
        }
    });

    // 5b. Sidebar Menu Accordion Dropdown Toggles
    const menuGroups = document.querySelectorAll(".hud-menu-group");
    menuGroups.forEach((group, index) => {
        const titleEl = group.querySelector(".hud-menu-category-title");
        if (!titleEl) return;

        const groupTitleText = titleEl.textContent.trim().toLowerCase().replace(/[^a-z0-9]/g, "_");
        const storageKey = `digipose_group_collapse_${groupTitleText}_${index}`;
        const hasActiveChild = group.querySelector(".hud-menu-link.active") !== null;

        // Auto-expand if group contains the active navigation item
        if (hasActiveChild) {
            group.classList.remove("is-collapsed");
            localStorage.setItem(storageKey, "open");
        } else {
            const savedState = localStorage.getItem(storageKey);
            if (savedState === "collapsed") {
                group.classList.add("is-collapsed");
            }
        }

        titleEl.addEventListener("click", function () {
            group.classList.toggle("is-collapsed");
            const isCollapsed = group.classList.contains("is-collapsed");
            localStorage.setItem(storageKey, isCollapsed ? "collapsed" : "open");
        });
    });

    // 6. Dynamic Notification Manager (Hides badge if count is 0, renders items dynamically)
    window.setNotificationCount = function (count, items) {
        const badge = document.getElementById("hudNotifBadge");
        const menu = document.getElementById("hudNotifMenu");
        if (!badge) return;

        const numCount = parseInt(count, 10) || 0;
        if (numCount > 0) {
            badge.textContent = numCount;
            badge.style.display = "inline-block";
        } else {
            badge.style.display = "none";
        }

        if (items && Array.isArray(items) && menu) {
            let html = '<li><h6 class="dropdown-header text-cyan" style="color:#00E5FF; font-family:\'Orbitron\';">TELEMETRY ALERTS</h6></li>';
            if (items.length === 0) {
                html += '<li><span class="dropdown-item text-muted" style="font-family:\'Roboto Mono\'; font-size:0.85rem;">No active notifications.</span></li>';
            } else {
                items.forEach(item => {
                    html += `<li><a class="dropdown-item" href="#"><i class="${item.icon || 'fa-solid fa-bell'} ${item.colorClass || 'text-cyan'} me-2"></i> ${item.text}</a></li>`;
                });
            }
            menu.innerHTML = html;
        }
    };

    // Initialize default active telemetry alerts
    const initialAlerts = [
        { text: "SKU-109 inventory below minimum threshold", icon: "fa-solid fa-triangle-exclamation", colorClass: "text-warning" },
        { text: "Work shift #8812 closed successfully", icon: "fa-solid fa-circle-check", colorClass: "text-success" },
        { text: "5 new tax invoices generated in session", icon: "fa-solid fa-file-invoice", colorClass: "text-info" }
    ];
    window.setNotificationCount(initialAlerts.length, initialAlerts);
});
