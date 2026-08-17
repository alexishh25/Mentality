from fastapi import FastAPI

from routers.logs_router import router as logs_router
from routers.health_router import router as health_router
from routers.interaction_router import router as interaction_router

app = FastAPI(title="AI Backend")

app.include_router(logs_router)
app.include_router(health_router)
app.include_router(interaction_router)