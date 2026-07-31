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

    // 2. Universal Theme Toggle Handling (Cyber Void vs Cyber Holographic)
    const themeToggleBtn = document.getElementById("themeToggleBtn");
    const savedTheme = localStorage.getItem("digipose_global_theme") || localStorage.getItem("digipose_theme") || localStorage.getItem("digipose_store_theme") || "dark";

    function applyUniversalTheme(theme) {
        const isLight = theme === "light";
        if (isLight) {
            document.body.classList.add("light-theme");
            document.documentElement.setAttribute("data-theme", "light");
        } else {
            document.body.classList.remove("light-theme");
            document.documentElement.setAttribute("data-theme", "dark");
        }
        localStorage.setItem("digipose_global_theme", theme);
        localStorage.setItem("digipose_theme", theme);
        localStorage.setItem("digipose_store_theme", theme);
        if (themeToggleBtn) {
            themeToggleBtn.innerHTML = isLight ? '<i class="fa-solid fa-moon"></i>' : '<i class="fa-solid fa-sun"></i>';
        }
        const storeThemeIcon = document.getElementById("themeIcon");
        if (storeThemeIcon && !themeToggleBtn?.contains(storeThemeIcon)) {
            storeThemeIcon.className = isLight ? "fa-solid fa-moon" : "fa-solid fa-sun";
        }
    }
    applyUniversalTheme(savedTheme);

    if (themeToggleBtn) {
        themeToggleBtn.addEventListener("click", function () {
            const current = document.body.classList.contains("light-theme") ? "light" : "dark";
            const nextTheme = current === "light" ? "dark" : "light";
            applyUniversalTheme(nextTheme);
        });
    }

    // 3. Universal Language Selector Toggle
    const langToggleBtn = document.getElementById("langToggleBtn");
    const langText = document.getElementById("langText");
    let currentLang = localStorage.getItem("digipose_global_lang") || localStorage.getItem("digipose_lang") || localStorage.getItem("digipose_store_lang") || "EN";

    function applyLangState(lang) {
        if (langText) {
            langText.textContent = lang === "VI" ? "Tiếng Việt" : "English";
        }
        if (langToggleBtn) {
            langToggleBtn.title = `Switch Language (Current: ${lang === "VI" ? "Tiếng Việt" : "English"})`;
        }
        localStorage.setItem("digipose_global_lang", lang);
        localStorage.setItem("digipose_lang", lang);
        localStorage.setItem("digipose_store_lang", lang);
        const storeLangSpan = document.getElementById("langShortCode");
        if (storeLangSpan) storeLangSpan.textContent = lang;
    }
    applyLangState(currentLang);

    if (langToggleBtn) {
        langToggleBtn.addEventListener("click", function () {
            currentLang = currentLang === "EN" ? "VI" : "EN";
            applyLangState(currentLang);
        });
    }

    // 3b. Profile Dropdown Caret Single Icon Toggle (Up when open, Down when closed via 180deg smooth rotation)
    const profileDropdownEl = document.getElementById("profileDropdown");
    const profileCaret = document.getElementById("hudProfileCaret");
    if (profileDropdownEl && profileCaret) {
        const parentDropdown = profileDropdownEl.closest('.dropdown');
        if (parentDropdown) {
            parentDropdown.addEventListener('show.bs.dropdown', function () {
                profileCaret.style.transform = "rotate(180deg)";
            });
            parentDropdown.addEventListener('hide.bs.dropdown', function () {
                profileCaret.style.transform = "rotate(0deg)";
            });
        }
    }

    // 4. Live Telemetry Clock & Real Network Latency Monitor in Footer
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
        const ms = String(now.getMilliseconds()).padStart(3, "0");
        footerClock.textContent = `${year}-${month}-${day} ${hours}:${mins}:${secs}.${ms} UTC+7`;
    }
    updateClock();
    setInterval(updateClock, 50);

    const globalPingVal = document.getElementById("globalPingVal");
    const globalStatusLabel = document.getElementById("globalStatusLabel");
    const globalPingDot = document.getElementById("globalPingDot");
    async function executeGlobalTelemetryPing() {
        if (!globalPingVal || !globalStatusLabel) return;
        const t0 = performance.now();
        try {
            const res = await fetch('/api/v1/POS/health/ping', { cache: 'no-store' });
            const rtt = Math.round(performance.now() - t0);
            globalPingVal.textContent = `${rtt}ms`;
            globalPingVal.style.color = rtt < 15 ? '#00FF66' : rtt < 80 ? '#FFB000' : '#FF3333';
            if (res.ok) {
                globalStatusLabel.textContent = "ONLINE";
                globalStatusLabel.style.color = "#00FF66";
                if (globalPingDot) globalPingDot.style.background = "#00FF66";
            }
        } catch (e) {
            globalStatusLabel.textContent = "OFFLINE [WIRE_ERR]";
            globalStatusLabel.style.color = "#FF3333";
            globalPingVal.textContent = "ERR";
            if (globalPingDot) globalPingDot.style.background = "#FF3333";
        }
    }
    executeGlobalTelemetryPing();
    setInterval(executeGlobalTelemetryPing, 5000);

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

    // 7. Phase 6.1 Realtime SignalR Cyber-HUD Telemetry Bridge (Admin Monitor)
    if (typeof signalR !== "undefined") {
        const hudAlertsList = [...initialAlerts];
        const telemetryConnection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/pos")
            .withAutomaticReconnect([0, 2000, 5000, 10000])
            .build();

        telemetryConnection.on("OnTelemetryAlert", function (payload) {
            console.log(">>> [CYBER_TELEMETRY_RECEIVE]: Real-time transaction alert received from POS Terminal:", payload);
            
            // Add transaction revenue notification
            hudAlertsList.unshift({
                text: `Order #${payload.invoiceNumber || payload.orderId} Completed (+${(payload.revenueDelta || 0).toLocaleString()} VND)`,
                icon: "fa-solid fa-bolt",
                colorClass: "text-success"
            });

            // Process critical low stock notifications
            if (payload.lowStockAlerts && Array.isArray(payload.lowStockAlerts)) {
                payload.lowStockAlerts.forEach(alertText => {
                    hudAlertsList.unshift({
                        text: alertText,
                        icon: "fa-solid fa-triangle-exclamation",
                        colorClass: "text-danger"
                    });
                });
            }

            // Cap memory list at 15 items to prevent DOM bloat
            if (hudAlertsList.length > 15) hudAlertsList.length = 15;
            
            window.setNotificationCount(hudAlertsList.length, hudAlertsList);

            // Play Cyber micro-beep audio feedback / highlight neon badge
            const badgeEl = document.getElementById("hudNotifBadge");
            if (badgeEl) {
                badgeEl.style.boxShadow = "0 0 12px #FF3333, 0 0 24px #FF3333";
                setTimeout(() => { badgeEl.style.boxShadow = "0 0 8px #00FF66"; }, 1500);
            }
        });

        telemetryConnection.start().then(() => {
            console.log(">>> [RADAR_LINK_OK]: SignalR connected to /hubs/pos");
            return telemetryConnection.invoke("JoinAdminTelemetryGroup");
        }).catch(err => {
            console.warn(">>> [RADAR_LINK_WARN]: SignalR offline in developer sandbox or disconnected:", err);
        });
    }
});
