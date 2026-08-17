from pydantic import BaseModel, Field
from typing import List

# --- LO QUE RECIBE LA API DESDE UNITY ---
class Posicion(BaseModel):
    x: float
    y: float
    z: float

class EventoToqueObjeto(BaseModel):
    player_id: str = Field(..., description="ID o nombre del jugador")
    object_id: str = Field(..., description="ID del objeto interactuado")
    prefab_name: str = Field(..., description="Nombre del prefab a duplicar en Unity")
    touch_count: int = Field(..., description="Cantidad de toques registrados")
    player_position: Posicion

# --- LO QUE LA API LE RESPONDE A UNITY ---
class ComandoDuplicado(BaseModel):
    action: str = "SPAWN_TEMPORARY_OBJECTS"
    prefab_name: str
    positions: List[Posicion]
    duration_seconds: float = 5.0

