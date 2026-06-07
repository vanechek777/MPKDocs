"""Локальные настройки админки (подключение 1С), JSON на диске."""

from __future__ import annotations

import json
from pathlib import Path
from threading import Lock

_CONFIG_PATH = Path(__file__).resolve().parents[2] / "data" / "runtime_config.json"
_lock = Lock()


def _ensure_parent() -> None:
    _CONFIG_PATH.parent.mkdir(parents=True, exist_ok=True)


def load_runtime_config() -> dict:
    with _lock:
        if not _CONFIG_PATH.is_file():
            return {}
        try:
            return json.loads(_CONFIG_PATH.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return {}


def save_runtime_config(data: dict) -> None:
    with _lock:
        _ensure_parent()
        _CONFIG_PATH.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")


def get_onec_config() -> dict:
    cfg = load_runtime_config().get("onec", {})
    if not isinstance(cfg, dict):
        return {}
    return cfg


def _normalize_api_url(url: str | None) -> str | None:
    if not url or not str(url).strip():
        return None
    trimmed = str(url).strip()
    if not trimmed.startswith(("http://", "https://")):
        return None
    return trimmed.rstrip("/")


def get_api_endpoints() -> list[dict]:
    raw = load_runtime_config().get("api_endpoints", [])
    if not isinstance(raw, list):
        return []
    out: list[dict] = []
    seen: set[str] = set()
    for item in raw:
        if not isinstance(item, dict):
            continue
        u = _normalize_api_url(item.get("url"))
        if not u:
            continue
        key = u.lower()
        if key in seen:
            continue
        seen.add(key)
        label = item.get("label")
        lbl = str(label).strip() if isinstance(label, str) and label.strip() else None
        out.append({"url": u, "label": lbl})
    return out


def set_api_endpoints(endpoints: list[dict]) -> list[dict]:
    normalized: list[dict] = []
    seen: set[str] = set()
    for item in endpoints:
        if not isinstance(item, dict):
            continue
        u = _normalize_api_url(item.get("url"))
        if not u:
            continue
        key = u.lower()
        if key in seen:
            continue
        seen.add(key)
        label = item.get("label")
        lbl = str(label).strip() if isinstance(label, str) and label.strip() else None
        normalized.append({"url": u, "label": lbl})
    root = load_runtime_config()
    root["api_endpoints"] = normalized
    save_runtime_config(root)
    return normalized


def patch_onec_config(*, base_url: str | None, username: str | None, password: str | None) -> dict:
    root = load_runtime_config()
    onec = dict(root.get("onec") or {})
    if base_url is not None:
        onec["base_url"] = base_url.strip() or None
    if username is not None:
        onec["username"] = username.strip() or None
    if password is not None and password.strip():
        onec["password"] = password.strip()
    root["onec"] = onec
    save_runtime_config(root)
    return onec
