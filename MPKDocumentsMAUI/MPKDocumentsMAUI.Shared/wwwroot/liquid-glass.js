/**
 * Liquid Glass Navbar
 * kube.io/blog/liquid-glass-css-svg/
 *
 * FIX: ResizeObserver вместо setTimeout → правильные размеры с первого раза
 * FIX: filterUnits="userSpaceOnUse" → displacement map в реальных пикселях
 */
(function () {

    function smootherstep(t) {
        const c = Math.max(0, Math.min(1, t));
        return c * c * c * (c * (c * 6 - 15) + 10);
    }

    function snellDisplacement(t, n2) {
        const n1 = 1.0;
        const eps = 1e-4;
        const dh = (smootherstep(Math.min(1, t + eps)) - smootherstep(Math.max(0, t - eps))) / (2 * eps);

        const nLen = Math.sqrt(dh * dh + 1);
        const sinI = Math.abs(dh) / nLen;
        const sinT = (n1 / n2) * sinI;
        if (sinT >= 1) return 0;

        const cosI = Math.sqrt(1 - sinI * sinI);
        const cosT = Math.sqrt(1 - sinT * sinT);
        return (sinT / cosT) - (sinI / cosI);
    }

    function pillDistAndNormal(px, py, W, H, R) {
        const hw = W / 2, hh = H / 2;
        const x = px - hw, y = py - hh;
        const r = Math.min(R, hw, hh);
        const cx = Math.max(-(hw - r), Math.min(hw - r, x));
        const cy = Math.max(-(hh - r), Math.min(hh - r, y));
        const dx = x - cx, dy = y - cy;
        const len = Math.sqrt(dx * dx + dy * dy);
        const distFromEdge = r - len;
        const nx = len > 1e-4 ? dx / len : 0;
        const ny = len > 1e-4 ? dy / len : 0;
        return { distFromEdge, nx, ny };
    }

    function generateDisplacementMap(W, H, padding, borderRadius, bezelWidth, n2) {
        const N = 127;
        const mags = new Float32Array(N);
        for (let i = 0; i < N; i++) {
            mags[i] = snellDisplacement(i / (N - 1), n2);
        }
        const maxMag = mags.reduce((m, v) => Math.max(m, Math.abs(v)), 1e-6);

        const fullW = W + padding * 2;
        const fullH = H + padding * 2;

        const canvas = document.createElement('canvas');
        canvas.width = fullW;
        canvas.height = fullH;
        const ctx = canvas.getContext('2d');
        const img = ctx.createImageData(fullW, fullH);

        for (let py = 0; py < fullH; py++) {
            for (let px = 0; px < fullW; px++) {
                const pillX = px - padding;
                const pillY = py - padding;

                const i = (py * fullW + px) * 4;

                // За пределами реального элемента искажения нет (нейтральный серый)
                if (pillX < 0 || pillX >= W || pillY < 0 || pillY >= H) {
                    img.data[i] = 128; img.data[i + 1] = 128;
                    img.data[i + 2] = 128; img.data[i + 3] = 255;
                    continue;
                }

                const { distFromEdge, nx, ny } = pillDistAndNormal(pillX, pillY, W, H, borderRadius);

                if (distFromEdge <= 0 || distFromEdge > bezelWidth) {
                    img.data[i] = 128; img.data[i + 1] = 128;
                    img.data[i + 2] = 128; img.data[i + 3] = 255;
                } else {
                    const t = distFromEdge / bezelWidth;
                    const si = Math.min(N - 1, Math.floor(t * N));
                    const normMag = mags[si] / maxMag;
                    const dx = -nx * normMag;
                    const dy = -ny * normMag;
                    img.data[i]     = Math.max(0, Math.min(255, Math.round(128 + dx * 127)));
                    img.data[i + 1] = Math.max(0, Math.min(255, Math.round(128 + dy * 127)));
                    img.data[i + 2] = 128;
                    img.data[i + 3] = 255;
                }
            }
        }
        ctx.putImageData(img, 0, 0);
        return {
            dataUrl: canvas.toDataURL('image/png'),
            scale: maxMag * bezelWidth
        };
    }

    function applyToFilter(idPrefix, W, H, dataUrl, scale) {
        const filter  = document.getElementById(`${idPrefix}-filter`);
        const feImg   = document.getElementById(`${idPrefix}-feimage`);
        const feDisp  = document.getElementById(`${idPrefix}-fedisplace`);

        if (filter) {
            // Для слоя .lg-pill-bg (который уже увеличен на 48px) координаты 0,0 идеально совпадают
            filter.setAttribute('x', '0');
            filter.setAttribute('y', '0');
            filter.setAttribute('width', String(W + 48));
            filter.setAttribute('height', String(H + 48));
        }

        if (feImg) {
            feImg.setAttributeNS('http://www.w3.org/1999/xlink', 'href', dataUrl);
            feImg.setAttribute('href', dataUrl);
            feImg.setAttribute('x', '0');
            feImg.setAttribute('y', '0');
            feImg.setAttribute('width', String(W + 48));
            feImg.setAttribute('height', String(H + 48));
        }

        if (feDisp) {
            feDisp.setAttribute('scale', String(Math.round(scale)));
        }
    }

    function rebuildPill(W, H) {
        if (W < 10 || H < 10) return;
        const padding = 24;
        const borderRadius = H / 2;
        const bezelWidth   = H * 0.45;
        const { dataUrl, scale } = generateDisplacementMap(W, H, padding, borderRadius, bezelWidth, 1.5);
        // WebView2 (Windows) часто даёт “кольца” при большом displacement scale.
        // Жёстко ограничиваем масштаб: эффект остаётся, но без паразитных контуров.
        const cappedScale = Math.min(scale, 18);
        applyToFilter('lg-nav', W, H, dataUrl, cappedScale);
    }

    function rebuildFab(W, H) {
        if (W < 10 || H < 10) return;
        const padding = 24;
        const borderRadius = H / 2; // FAB is a circle, so H/2 is perfect
        const bezelWidth   = H * 0.45;
        const { dataUrl, scale } = generateDisplacementMap(W, H, padding, borderRadius, bezelWidth, 1.5);
        const cappedScale = Math.min(scale, 14);
        applyToFilter('lg-fab', W, H, dataUrl, cappedScale);
    }

    let roPill = null;
    let roFab = null;
    let pillEl = null;
    let fabEl = null;

    function attachPill(el) {
        if (!el || el === pillEl) return;
        pillEl = el;
        if (roPill) roPill.disconnect();
        roPill = new ResizeObserver(entries => {
            for (const e of entries) {
                rebuildPill(Math.ceil(e.contentRect.width), Math.ceil(e.contentRect.height));
            }
        });
        roPill.observe(pillEl);

        // Первый прогон сразу после привязки
        const r = pillEl.getBoundingClientRect();
        rebuildPill(Math.ceil(r.width), Math.ceil(r.height));
    }

    function attachFab(el) {
        if (!el || el === fabEl) return;
        fabEl = el;
        if (roFab) roFab.disconnect();
        roFab = new ResizeObserver(entries => {
            for (const e of entries) {
                rebuildFab(Math.ceil(e.contentRect.width), Math.ceil(e.contentRect.height));
            }
        });
        roFab.observe(fabEl);

        // Первый прогон сразу после привязки
        const r = fabEl.getBoundingClientRect();
        rebuildFab(Math.ceil(r.width), Math.ceil(r.height));
    }

    function scanAndAttach() {
        attachPill(document.querySelector('.lg-navbar-pill'));
        attachFab(document.querySelector('.fab-btn'));
    }

    function init() {
        scanAndAttach();

        // В Blazor элементы могут появляться/исчезать после DOMContentLoaded.
        // MutationObserver ловит дорендер и переподключает ResizeObserver.
        const mo = new MutationObserver(() => scanAndAttach());
        mo.observe(document.documentElement, { childList: true, subtree: true });

        window.addEventListener('resize', () => {
            if (pillEl) {
                const r = pillEl.getBoundingClientRect();
                rebuildPill(Math.ceil(r.width), Math.ceil(r.height));
            }
            if (fabEl) {
                const r = fabEl.getBoundingClientRect();
                rebuildFab(Math.ceil(r.width), Math.ceil(r.height));
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    function rebuildAll() {
        scanAndAttach();
        if (pillEl) {
            const r = pillEl.getBoundingClientRect();
            rebuildPill(Math.ceil(r.width), Math.ceil(r.height));
        }
        if (fabEl) {
            const r = fabEl.getBoundingClientRect();
            rebuildFab(Math.ceil(r.width), Math.ceil(r.height));
        }
    }

    // Публичный API на случай ручного вызова из Blazor/DevTools
    window.LiquidGlass = { rebuildAll };

    // -------------------------------------------------------------
    // Popover positioning (keeps popovers inside viewport)
    // -------------------------------------------------------------
    function clamp(v, min, max) {
        return Math.max(min, Math.min(max, v));
    }

    /**
     * Positions popover under anchor and clamps to viewport.
     * Works around WebView2/Blazor stacking/layout quirks.
     */
    function positionPopover(anchorEl, popoverEl, opts) {
        try {
            if (!anchorEl || !popoverEl) return;
            const gap = (opts && opts.gap) ?? 10;
            const pad = (opts && opts.pad) ?? 12;

            // Reset first so measurements are correct
            popoverEl.style.position = 'fixed';
            popoverEl.style.left = '0px';
            popoverEl.style.top = '0px';
            popoverEl.style.right = 'auto';
            popoverEl.style.bottom = 'auto';
            popoverEl.style.transform = 'none';
            popoverEl.style.margin = '0';

            const a = anchorEl.getBoundingClientRect();
            const p = popoverEl.getBoundingClientRect();

            const vw = document.documentElement.clientWidth;
            const vh = document.documentElement.clientHeight;

            // Prefer below; if not enough space, place above.
            const belowTop = a.bottom + gap;
            const aboveTop = a.top - gap - p.height;
            const top = (belowTop + p.height <= vh - pad) ? belowTop : clamp(aboveTop, pad, Math.max(pad, vh - pad - p.height));

            // Align left edge to anchor; clamp inside viewport.
            const left = clamp(a.left, pad, Math.max(pad, vw - pad - p.width));

            popoverEl.style.left = `${left}px`;
            popoverEl.style.top = `${top}px`;
            popoverEl.style.zIndex = '10002';
        } catch (_) {
            // ignore
        }
    }

    function storageSet(key, value) {
        try {
            localStorage.setItem(key, value);
        } catch (_) {}
    }
    function storageGet(key) {
        try {
            return localStorage.getItem(key);
        } catch (_) {
            return null;
        }
    }
    function storageRemove(key) {
        try {
            localStorage.removeItem(key);
        } catch (_) {}
    }

    /** Список черновиков: слушатель на document + capture — WebView2 надёжнее, чем Blazor @oncontextmenu. */
    function wireDraftsContextMenu(listId, dotNetRef) {
        unwireDraftsContextMenu();
        if (!listId || !dotNetRef) return;
        var ac = new AbortController();
        var listIdStr = String(listId);
        document._mpkDraftsCtxPack = { ac: ac, listId: listIdStr, ref: dotNetRef };
        document.addEventListener(
            'contextmenu',
            function (ev) {
                var pack = document._mpkDraftsCtxPack;
                if (!pack || !pack.listId) return;
                var list = document.getElementById(pack.listId);
                if (!list) return;
                var t = ev.target;
                var card = t && t.closest ? t.closest('[data-draft-id]') : null;
                if (!card || !list.contains(card)) return;
                ev.preventDefault();
                ev.stopPropagation();
                var raw = card.getAttribute('data-draft-id');
                var id = raw ? parseInt(raw, 10) : NaN;
                if (Number.isNaN(id)) return;
                var title = card.getAttribute('data-draft-title') || '';
                pack.ref.invokeMethodAsync('OpenDraftContextMenuFromJs', ev.clientX, ev.clientY, id, title).catch(function () {});
            },
            { capture: true, signal: ac.signal }
        );
    }

    function unwireDraftsContextMenu() {
        var pack = document._mpkDraftsCtxPack;
        if (pack && pack.ac) {
            pack.ac.abort();
        }
        document._mpkDraftsCtxPack = null;
    }

    var MPK_SETTINGS_KEYS = {
        DARK_THEME: 'mpk_dark_theme',
        NOTIFICATIONS: 'mpk_notifications',
        REDUCED_MOTION: 'mpk_reduced_motion',
        HAPTICS: 'mpk_haptics',
        COMPACT_LISTS: 'mpk_compact_lists',
    };

    function settingsGet(key) {
        return storageGet(key);
    }
    function settingsSet(key, value) {
        storageSet(key, value);
    }
    function applyDarkTheme(isDark) {
        var root = document.documentElement;
        if (isDark) root.classList.remove('mpk-theme-light');
        else root.classList.add('mpk-theme-light');
        try {
            root.style.colorScheme = isDark ? 'dark' : 'light';
        } catch (_) {}
    }
    /** v: null/'' → тёмная тема по умолчанию; '1'/'true' → тёмная; '0'/'false' → светлая */
    function applyDarkThemeFromStored(v) {
        if (v === null || v === undefined || v === '') {
            applyDarkTheme(true);
            return;
        }
        var isDark = v === '1' || v === 'true';
        applyDarkTheme(isDark);
    }
    function applyReducedMotionFromStored(v) {
        var root = document.documentElement;
        var on = v === '1' || v === 'true';
        if (on) root.classList.add('mpk-reduced-motion');
        else root.classList.remove('mpk-reduced-motion');
    }
    function applyCompactListsFromStored() {
        var v = settingsGet(MPK_SETTINGS_KEYS.COMPACT_LISTS);
        var on = v === '1' || v === 'true';
        if (on) document.documentElement.classList.add('mpk-compact-lists');
        else document.documentElement.classList.remove('mpk-compact-lists');
    }
    function bootstrapUxFromStorage() {
        applyDarkThemeFromStored(settingsGet(MPK_SETTINGS_KEYS.DARK_THEME));
        applyReducedMotionFromStored(settingsGet(MPK_SETTINGS_KEYS.REDUCED_MOTION) || '0');
        applyCompactListsFromStored();
    }
    function tryHapticPulse() {
        try {
            if (navigator.vibrate) navigator.vibrate(18);
        } catch (_) {}
    }

    window.MPKDocuments = window.MPKDocuments || {};
    window.MPKDocuments.positionPopover = positionPopover;
    window.MPKDocuments.storageSet = storageSet;
    window.MPKDocuments.storageGet = storageGet;
    window.MPKDocuments.storageRemove = storageRemove;
    window.MPKDocuments.wireDraftsContextMenu = wireDraftsContextMenu;
    window.MPKDocuments.unwireDraftsContextMenu = unwireDraftsContextMenu;
    window.MPKDocuments.settingsGet = settingsGet;
    window.MPKDocuments.settingsSet = settingsSet;
    window.MPKDocuments.applyDarkTheme = applyDarkTheme;
    window.MPKDocuments.applyDarkThemeFromStored = applyDarkThemeFromStored;
    window.MPKDocuments.applyReducedMotionFromStored = applyReducedMotionFromStored;
    window.MPKDocuments.bootstrapUxFromStorage = bootstrapUxFromStorage;
    window.MPKDocuments.tryHapticPulse = tryHapticPulse;
    window.MPKDocuments.applyCompactListsFromStored = applyCompactListsFromStored;
    window.MPKDocuments.MPK_SETTINGS_KEYS = MPK_SETTINGS_KEYS;
})();
