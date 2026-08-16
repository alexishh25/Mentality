from pydantic import BaseModel

class LogEvent(BaseModel):

    timestamp: str

    source: str

    category: str

    message: str