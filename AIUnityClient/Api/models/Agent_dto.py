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


class AIResponseSchema(BaseModel):
    total_objects_calculated: int = Field(
        description="Cantidad total de objetos a generar según los toques y contexto."
    )
    reasoning: str = Field(
        description="Razón breve de la cantidad de objetos y distribución elegida."
    )
    duration_seconds: float = Field(
        default=5.0, description="Tiempo de vida en segundos antes de desaparecer."
    )
    calculated_positions: List[Posicion] = Field(
        description="Lista de coordenadas (x, y, z) alrededor del jugador o punto de toque."
    )

