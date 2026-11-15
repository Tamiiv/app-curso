(function () {
    "use strict";

    const root = document.documentElement;

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

    function closeMenu() {
        const sidebar = document.getElementById("sidebar");
        const menuButton = document.getElementById("menu-toggle");

        sidebar?.classList.remove("open");
        menuButton?.setAttribute("aria-expanded", "false");
        menuButton?.setAttribute("aria-label", "Abrir menú");
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
            button.setAttribute("aria-label", "Copiar bloque de código");

            button.addEventListener("click", async function () {
                const code = pre.querySelector("code");
                const text = code ? code.innerText : pre.innerText;

                try {
                    await copyText(text);
                    button.textContent = "Copiado";
                    button.classList.add("is-copied");
                } catch (error) {
                    button.textContent = "No se pudo";
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
        closeMenu();
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
            const menuButton = document.getElementById("menu-toggle");

            if (!sidebar || !menuButton) {
                return;
            }

            const isOpen = sidebar.classList.toggle("open");
            menuButton.setAttribute("aria-expanded", isOpen ? "true" : "false");
            menuButton.setAttribute("aria-label", isOpen ? "Cerrar menú" : "Abrir menú");
            return;
        }

        if (event.target.closest("#sidebar a")) {
            closeMenu();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            closeMenu();
        }
    });

    updateThemeButton(root.dataset.theme || "light");
    enhanceCodeBlocks();

    if (window.Blazor) {
        Blazor.addEventListener("enhancedload", onEnhancedLoad);
    }
})();
