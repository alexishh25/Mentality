from fastapi import APIRouter

from models.log_event import LogEvent
from services.log_service import LogService

router = APIRouter()

service = LogService()

@router.post("/log")

async def receive_log(event:LogEvent):

    service.print_event(event)

    return {
        "status":"ok"
    }