(function () {
    "use strict";

    const root = document.documentElement;
    const mobileQuery = window.matchMedia("(max-width: 900px)");
    let liveRegion;

    function updateThemeButton(theme) {
        const button = document.getElementById("theme-toggle");
        if (!button) {
            return;
        }

        button.setAttribute(
            "aria-label",
            theme === "dark" ? "Cambiar a tema claro" : "Cambiar a tema oscuro");
    }

    function applyTheme(theme) {
        root.dataset.theme = theme;

        try {
            localStorage.setItem("theme", theme);
        } catch (error) {
            // El tema sigue funcionando aunque el navegador bloquee el almacenamiento.
        }

        updateThemeButton(theme);
    }

    function syncSidebarState() {
        const sidebar = document.getElementById("sidebar");
        if (!sidebar) {
            return;
        }

        if (mobileQuery.matches && !sidebar.classList.contains("open")) {
            sidebar.setAttribute("inert", "");
        } else {
            sidebar.removeAttribute("inert");
        }
    }

    function closeMenu(returnFocus) {
        const sidebar = document.getElementById("sidebar");
        const menuButton = document.getElementById("menu-toggle");

        sidebar?.classList.remove("open");
        menuButton?.setAttribute("aria-expanded", "false");
        menuButton?.setAttribute("aria-label", "Abrir menú");
        syncSidebarState();

        if (returnFocus) {
            menuButton?.focus();
        }
    }

    function openMenu() {
        const sidebar = document.getElementById("sidebar");
        const menuButton = document.getElementById("menu-toggle");

        if (!sidebar || !menuButton) {
            return;
        }

        sidebar.classList.add("open");
        sidebar.removeAttribute("inert");
        menuButton.setAttribute("aria-expanded", "true");
        menuButton.setAttribute("aria-label", "Cerrar menú");

        sidebar.querySelector("a")?.focus();
    }

    function getLiveRegion() {
        if (liveRegion) {
            return liveRegion;
        }

        liveRegion = document.createElement("span");
        liveRegion.className = "visually-hidden";
        liveRegion.setAttribute("role", "status");
        liveRegion.setAttribute("aria-live", "polite");
        document.body.appendChild(liveRegion);
        return liveRegion;
    }

    function announce(message) {
        const region = getLiveRegion();
        region.textContent = "";
        window.setTimeout(function () {
            region.textContent = message;
        }, 50);
    }

    async function copyText(text) {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
            return;
        }

        const area = document.createElement("textarea");
        area.value = text;
        area.setAttribute("readonly", "");
        area.style.position = "fixed";
        area.style.opacity = "0";
        document.body.appendChild(area);
        area.select();
        document.execCommand("copy");
        area.remove();
    }

    function enhanceCodeBlocks() {
        document.querySelectorAll(".lesson-body pre").forEach(function (pre) {
            if (pre.querySelector(".code-copy")) {
                return;
            }

            const button = document.createElement("button");
            button.type = "button";
            button.className = "code-copy";
            button.textContent = "Copiar";
            button.title = "Copiar bloque de código";
            button.setAttribute("aria-label", "Copiar bloque de código");

            button.addEventListener("click", async function () {
                const code = pre.querySelector("code");
                const text = code ? code.innerText : pre.innerText;

                try {
                    await copyText(text);
                    button.textContent = "Copiado";
                    button.classList.add("is-copied");
                    announce("Código copiado al portapapeles.");
                } catch (error) {
                    button.textContent = "No se pudo";
                    announce("No se pudo copiar el código.");
                }

                window.setTimeout(function () {
                    button.textContent = "Copiar";
                    button.classList.remove("is-copied");
                }, 2000);
            });

            pre.appendChild(button);
        });
    }

    function onEnhancedLoad() {
        updateThemeButton(root.dataset.theme || "light");
        closeMenu(false);
        enhanceCodeBlocks();
    }

    document.addEventListener("click", function (event) {
        if (!(event.target instanceof Element)) {
            return;
        }

        if (event.target.closest("#theme-toggle")) {
            applyTheme(root.dataset.theme === "dark" ? "light" : "dark");
            return;
        }

        if (event.target.closest("#menu-toggle")) {
            const sidebar = document.getElementById("sidebar");

            if (!sidebar) {
                return;
            }

            if (sidebar.classList.contains("open")) {
                closeMenu(true);
            } else {
                openMenu();
            }

            return;
        }

        if (event.target.closest("#sidebar a")) {
            closeMenu(false);
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            closeMenu(true);
        }
    });

    mobileQuery.addEventListener?.("change", function () {
        closeMenu(false);
    });

    updateThemeButton(root.dataset.theme || "light");
    syncSidebarState();
    enhanceCodeBlocks();

    if (window.Blazor) {
        Blazor.addEventListener("enhancedload", onEnhancedLoad);
    }
})();
