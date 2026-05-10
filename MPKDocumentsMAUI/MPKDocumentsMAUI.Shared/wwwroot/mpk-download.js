/**
 * Сохранение файла из WebView: по возможности диалог «Сохранить как» (showSaveFilePicker),
 * иначе скачивание через временную ссылку с атрибутом download.
 * @param {string} fileName
 * @param {string} base64
 * @param {string} [mime]
 */
export async function downloadBase64(fileName, base64, mime) {
    const blob = base64ToBlob(base64, mime);
    const name = (fileName && String(fileName).trim()) || "download";

    if (typeof window.showSaveFilePicker === "function") {
        try {
            const handle = await window.showSaveFilePicker(buildSaveOptions(name, mime));
            const writable = await handle.createWritable();
            await writable.write(blob);
            await writable.close();
            return;
        } catch (e) {
            if (e && e.name === "AbortError") return;
        }
    }

    downloadWithAnchor(blob, name);
}

function base64ToBlob(base64, mime) {
    const bin = atob(base64);
    const len = bin.length;
    const arr = new Uint8Array(len);
    for (let i = 0; i < len; i++) {
        arr[i] = bin.charCodeAt(i);
    }
    return new Blob([arr], { type: mime || "application/octet-stream" });
}

/** @param {string} suggestedName @param {string} [mime] */
function buildSaveOptions(suggestedName, mime) {
    const opts = { suggestedName };
    const dot = suggestedName.lastIndexOf(".");
    const ext = dot > 0 && dot < suggestedName.length - 1 ? suggestedName.slice(dot) : "";
    if (ext && ext.length <= 16 && /^\.[a-zA-Z0-9._-]+$/.test(ext)) {
        const m = mime && String(mime).trim() ? String(mime).trim() : "application/octet-stream";
        opts.types = [{ description: "Файл", accept: { [m]: [ext.toLowerCase()] } }];
    }
    return opts;
}

function downloadWithAnchor(blob, fileName) {
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName || "download";
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
}
