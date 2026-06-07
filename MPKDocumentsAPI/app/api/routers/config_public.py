from fastapi import APIRouter

from app.core.runtime_config import get_api_endpoints

router = APIRouter(prefix="/config", tags=["config"])


@router.get("/api-endpoints")
async def public_api_endpoints():
    """Список базовых URL API для клиентов (без авторизации, для экрана входа)."""
    items = get_api_endpoints()
    return {
        "endpoints": [
            {"url": e["url"], "label": e.get("label")}
            for e in items
        ]
    }
