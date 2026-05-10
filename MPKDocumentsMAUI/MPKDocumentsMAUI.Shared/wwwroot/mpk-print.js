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
