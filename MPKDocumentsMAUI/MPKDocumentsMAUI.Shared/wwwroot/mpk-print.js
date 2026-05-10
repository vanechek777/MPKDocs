/**
 * Печать готового HTML внутри текущего WebView2 (MAUI Blazor).
 * window.open(''|blob:|about:) на WinUI часто уходит в системный обработчик ссылок — не используем.
 * @param {string} html
 */
export function openPrintWindow(html) {
    const iframe = document.createElement("iframe");
    iframe.setAttribute(
        "style",
        "position:fixed;inset:0;width:100%;height:100%;border:0;opacity:0;pointer-events:none;z-index:-1"
    );
    document.body.appendChild(iframe);

    const win = iframe.contentWindow;
    const doc = iframe.contentDocument;
    if (!win || !doc) {
        iframe.remove();
        return;
    }

    doc.open();
    doc.write(html);
    doc.close();

    let printed = false;
    const cleanup = () => {
        try {
            iframe.remove();
        } catch {
            /* ignore */
        }
    };

    const runPrint = () => {
        if (printed) return;
        printed = true;
        try {
            win.focus();
            win.print();
        } catch {
            /* ignore */
        }
        setTimeout(cleanup, 750);
    };

    iframe.addEventListener("load", () => setTimeout(runPrint, 80), { once: true });
    setTimeout(runPrint, 400);
}

/**
 * Полноэкранный предпросмотр HTML в WebView (iOS/Android): без окна печати.
 * @param {string} html
 */
export function openHtmlPreview(html) {
    try {
        if (typeof document.body.style.overflow !== "undefined")
            document.body.dataset.mpkOverflow = document.body.style.overflow || "";
        document.body.style.overflow = "hidden";
    } catch {
        /* ignore */
    }

    const root = document.createElement("div");
    root.className = "mpk-html-preview-root";
    root.setAttribute(
        "style",
        "position:fixed;inset:0;z-index:99990;background:rgba(0,0,0,0.55);backdrop-filter:blur(8px);display:flex;align-items:stretch;justify-content:center;padding:max(12px,env(safe-area-inset-top)) 12px max(12px,env(safe-area-inset-bottom));box-sizing:border-box"
    );

    const panel = document.createElement("div");
    panel.setAttribute(
        "style",
        "flex:1;max-width:720px;display:flex;flex-direction:column;background:#121212;border:1px solid rgba(255,255,255,0.12);border-radius:16px;overflow:hidden;box-shadow:0 20px 60px rgba(0,0,0,0.55)"
    );

    const bar = document.createElement("div");
    bar.setAttribute(
        "style",
        "display:flex;align-items:center;justify-content:flex-end;gap:8px;padding:10px 12px;border-bottom:1px solid rgba(255,255,255,0.08);background:rgba(30,30,30,0.95)"
    );

    const closeBtn = document.createElement("button");
    closeBtn.type = "button";
    closeBtn.textContent = "Закрыть";
    closeBtn.setAttribute(
        "style",
        "appearance:none;border:1px solid rgba(255,255,255,0.2);background:rgba(255,255,255,0.06);color:#fff;border-radius:10px;padding:8px 14px;font-weight:600;cursor:pointer"
    );

    const printBtn = document.createElement("button");
    printBtn.type = "button";
    printBtn.textContent = "Печать";
    printBtn.setAttribute(
        "style",
        "appearance:none;border:1px solid rgba(255,80,80,0.35);background:rgba(255,0,0,0.18);color:#fff;border-radius:10px;padding:8px 14px;font-weight:600;cursor:pointer"
    );

    const frameHolder = document.createElement("div");
    frameHolder.setAttribute("style", "flex:1;min-height:0;position:relative;background:#0a0a0a");

    const iframe = document.createElement("iframe");
    iframe.setAttribute("title", "НЭП");
    iframe.setAttribute(
        "style",
        "position:absolute;inset:0;width:100%;height:100%;border:0;background:#fff"
    );

    const cleanup = () => {
        try {
            root.remove();
        } catch {
            /* ignore */
        }
        try {
            if (document.body.dataset.mpkOverflow !== undefined) {
                document.body.style.overflow = document.body.dataset.mpkOverflow || "";
                delete document.body.dataset.mpkOverflow;
            } else document.body.style.overflow = "";
        } catch {
            /* ignore */
        }
    };

    closeBtn.addEventListener("click", cleanup);
    root.addEventListener("click", (e) => {
        if (e.target === root) cleanup();
    });

    printBtn.addEventListener("click", () => {
        try {
            const w = iframe.contentWindow;
            if (!w) return;
            w.focus();
            w.print();
        } catch {
            /* ignore */
        }
    });

    const docWin = () => {
        try {
            return iframe.contentWindow?.document;
        } catch {
            return null;
        }
    };

    bar.appendChild(printBtn);
    bar.appendChild(closeBtn);
    panel.appendChild(bar);
    frameHolder.appendChild(iframe);
    panel.appendChild(frameHolder);
    root.appendChild(panel);
    document.body.appendChild(root);

    const w = iframe.contentWindow;
    const d = iframe.contentDocument;
    if (!w || !d) {
        cleanup();
        return;
    }
    d.open();
    d.write(html);
    d.close();
}
