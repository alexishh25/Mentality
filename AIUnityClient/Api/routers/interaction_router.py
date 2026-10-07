from __future__ import annotations
from fastapi import APIRouter, HTTPException
import random
from services import ai_service
from models.Agent_dto import EventoToqueObjeto, ComandoDuplicado, Posicion
from services.ai_service import AIService

router = APIRouter(prefix="/gameplay", tags=["Gameplay Mechanics"])

ai_service = AIService()

@router.post("/object-touch", response_model=ComandoDuplicado)
async def procesar_toque_objeto(evento: EventoToqueObjeto):
    # Validación: Solo actuar si superó los 3 toques
    if evento.touch_count <= 3:
        raise HTTPException(
            status_code=400, 
            detail="El objeto no ha sido tocado más de 3 veces."
        )

    # Lógica de duplicación: Generar 4 posiciones aleatorias cerca del jugador
    posiciones_spawn = []
    base_pos = evento.player_position
    
    for _ in range(4):
        nueva_pos = Posicion(
            x=round(base_pos.x + random.uniform(-5.0, 5.0), 2),
            y=base_pos.y,
            z=round(base_pos.z + random.uniform(-5.0, 5.0), 2)
        )
        posiciones_spawn.append(nueva_pos)

    # Respuesta estructurada para Unity
    return ComandoDuplicado(
        action="SPAWN_TEMPORARY_OBJECTS",
        prefab_name=evento.prefab_name,
        positions=posiciones_spawn,
        duration_seconds=5.0
    )

@router.post("/touch-event", response_model=ComandoDuplicado)
async def handle_touch_event(event: EventoToqueObjeto):
    try:
        # Llama 3 calcula las posiciones y la cantidad
        command = ai_service.process_touch_event(event)
        return command
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error procesando la IA: {str(e)}")