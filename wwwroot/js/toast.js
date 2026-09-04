/*
 * THHToast — hệ thông báo dạng popup (toast) dùng chung cho toàn WebApp.
 * Thuần vanilla, không phụ thuộc jQuery. Nội dung luôn tiếng Việt.
 *
 *   THHToast.show(message, type, opts)   type: 'success' | 'error' | 'warning' | 'info'
 *   THHToast.success(msg) / .error(msg) / .warning(msg) / .info(msg)
 *   opts = { title?, duration?, action?: { label, href } }   // action => toast không tự đóng
 *
 * Server render sẵn hàng đợi vào  window.__THH_TOASTS__ = [{ message, type, title?, action? }]
 */
(function () {
    "use strict";
    if (window.THHToast) return;

    var ROOT_ID = "thh-toast-root";
    var STYLE_ID = "thh-toast-style";
    var MAX = 4;

    var CONF = {
        success: { icon: "fa-circle-check", ring: "border-emerald-200", bar: "bg-emerald-500", iconColor: "text-emerald-500", duration: 3500 },
        error: { icon: "fa-circle-xmark", ring: "border-red-200", bar: "bg-red-500", iconColor: "text-red-500", duration: 6000 },
        warning: { icon: "fa-triangle-exclamation", ring: "border-amber-200", bar: "bg-amber-500", iconColor: "text-amber-500", duration: 6000 },
        info: { icon: "fa-circle-info", ring: "border-blue-200", bar: "bg-blue-500", iconColor: "text-blue-500", duration: 4500 }
    };

    function injectStyle() {
        if (document.getElementById(STYLE_ID)) return;
        var s = document.createElement("style");
        s.id = STYLE_ID;
        s.textContent =
            "#" + ROOT_ID + "{position:fixed;top:1rem;right:1rem;z-index:10000;display:flex;flex-direction:column;gap:.6rem;max-width:calc(100vw - 2rem);width:24rem;pointer-events:none}" +
            "@media (max-width:480px){#" + ROOT_ID + "{left:1rem;width:auto}}" +
            ".thh-toast{pointer-events:auto;position:relative;overflow:hidden;display:flex;gap:.75rem;align-items:flex-start;" +
            "background:#fff;border:1px solid #e5e7eb;border-radius:1rem;box-shadow:0 12px 32px rgba(0,0,0,.12);padding:.9rem 1rem;" +
            "font-family:'Nunito',sans-serif;transform:translateX(120%);opacity:0;transition:transform .35s cubic-bezier(.34,1.56,.64,1),opacity .35s}" +
            ".thh-toast.thh-in{transform:translateX(0);opacity:1}" +
            ".thh-toast.thh-out{transform:translateX(120%);opacity:0}" +
            ".thh-toast__bar{position:absolute;left:0;top:0;bottom:0;width:4px}" +
            ".thh-toast__icon{font-size:1.15rem;line-height:1.4;flex-shrink:0}" +
            ".thh-toast__body{flex:1;min-width:0}" +
            ".thh-toast__title{font-weight:800;font-size:.9rem;color:#1f2937;margin-bottom:.1rem}" +
            ".thh-toast__msg{font-size:.85rem;color:#4b5563;line-height:1.4;word-wrap:break-word}" +
            ".thh-toast__action{margin-top:.5rem;display:inline-flex;align-items:center;gap:.35rem;font-size:.8rem;font-weight:800;" +
            "color:#2563eb;text-decoration:none}" +
            ".thh-toast__action:hover{text-decoration:underline}" +
            ".thh-toast__close{flex-shrink:0;width:1.5rem;height:1.5rem;border:0;background:transparent;color:#9ca3af;cursor:pointer;" +
            "border-radius:.5rem;font-size:.8rem;line-height:1}" +
            ".thh-toast__close:hover{color:#ef4444;background:#fef2f2}";
        document.head.appendChild(s);
    }

    function root() {
        var el = document.getElementById(ROOT_ID);
        if (!el) {
            el = document.createElement("div");
            el.id = ROOT_ID;
            el.setAttribute("aria-live", "polite");
            (document.body || document.documentElement).appendChild(el);
        }
        return el;
    }

    function esc(str) {
        return String(str == null ? "" : str)
            .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;").replace(/'/g, "&#39;");
    }

    function dismiss(node) {
        if (!node || node.__dismissed) return;
        node.__dismissed = true;
        if (node.__timer) clearTimeout(node.__timer);
        node.classList.remove("thh-in");
        node.classList.add("thh-out");
        setTimeout(function () { if (node.parentNode) node.parentNode.removeChild(node); }, 400);
    }

    function show(message, type, opts) {
        injectStyle();
        opts = opts || {};
        type = CONF[type] ? type : "info";
        var c = CONF[type];
        var host = root();

        // giới hạn số toast hiển thị cùng lúc
        while (host.children.length >= MAX) dismiss(host.firstChild);

        var node = document.createElement("div");
        node.className = "thh-toast";
        node.setAttribute("role", type === "error" ? "alert" : "status");

        var actionHtml = "";
        if (opts.action && opts.action.href) {
            actionHtml = '<a class="thh-toast__action" href="' + esc(opts.action.href) + '">' +
                '<i class="fa-solid fa-arrow-right"></i>' + esc(opts.action.label || "Xem") + "</a>";
        }
        var titleHtml = opts.title ? '<div class="thh-toast__title">' + esc(opts.title) + "</div>" : "";

        node.innerHTML =
            '<span class="thh-toast__bar ' + c.bar + '"></span>' +
            '<i class="thh-toast__icon fa-solid ' + c.icon + " " + c.iconColor + '"></i>' +
            '<div class="thh-toast__body">' + titleHtml +
            '<div class="thh-toast__msg">' + esc(message) + "</div>" + actionHtml + "</div>" +
            '<button type="button" class="thh-toast__close" aria-label="Đóng thông báo">' +
            '<i class="fa-solid fa-xmark"></i></button>';

        node.querySelector(".thh-toast__close").addEventListener("click", function () { dismiss(node); });

        host.appendChild(node);
        requestAnimationFrame(function () { node.classList.add("thh-in"); });

        var sticky = !!(opts.action && opts.action.href);
        var duration = opts.duration != null ? opts.duration : c.duration;
        if (!sticky && duration > 0) {
            var start = function () { node.__timer = setTimeout(function () { dismiss(node); }, duration); };
            start();
            node.addEventListener("mouseenter", function () { if (node.__timer) clearTimeout(node.__timer); });
            node.addEventListener("mouseleave", function () { if (!node.__dismissed) start(); });
        }
        return node;
    }

    // hiển thị hàng đợi từ server (_ToastHost đặt vào window.__THH_TOASTS__)
    function flushQueue() {
        var q = window.__THH_TOASTS__;
        if (!Array.isArray(q) || !q.length) return;
        window.__THH_TOASTS__ = [];
        q.forEach(function (t) {
            if (!t || !t.message) return;
            show(t.message, t.type || "info", {
                title: t.title || undefined,
                action: (t.actionLabel && t.actionHref) ? { label: t.actionLabel, href: t.actionHref } : undefined
            });
        });
    }

    var api = {
        show: show,
        success: function (m, o) { return show(m, "success", o); },
        error: function (m, o) { return show(m, "error", o); },
        warning: function (m, o) { return show(m, "warning", o); },
        info: function (m, o) { return show(m, "info", o); },
        flushQueue: flushQueue,
        clear: function () { var h = document.getElementById(ROOT_ID); if (h) while (h.firstChild) dismiss(h.firstChild); }
    };
    window.THHToast = api;

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", flushQueue);
    } else {
        flushQueue();
    }
})();
